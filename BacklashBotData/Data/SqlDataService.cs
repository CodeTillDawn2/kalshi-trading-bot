using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BacklashBot.Services.Interfaces;
using BacklashDTOs.Exceptions;
using BacklashInterfaces.Constants;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Runtime.InteropServices;

using Polly;
using Polly.Retry;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using BacklashInterfaces.PerformanceMetrics;
using BacklashBotData.Configuration;
namespace KalshiBotData.Data
{
    /// <summary>
    /// Provides asynchronous data persistence services for real-time market data from Kalshi's trading platform.
    /// This service manages concurrent queues for different data types (order book, trades, fills, lifecycle events)
    /// and processes them using configurable background worker tasks with retry logic for transient SQL errors.
    /// Supports batch processing, performance metrics collection, input validation, and configurable queue sizes
    /// to prevent resource exhaustion. Implements the ISqlDataService interface for dependency injection and proper resource management.
    /// </summary>
    /// <remarks>
    /// The service uses Polly for retry policies on SQL operations and maintains separate queues to ensure
    /// data integrity and prevent blocking between different data types. All operations are asynchronous
    /// to support high-throughput real-time data processing without impacting the main application flow.
    /// Configuration options include retry counts, delays, queue sizes, worker counts, and batch sizes.
    /// Performance metrics are collected for processing rates, queue depths, and success rates.
    /// Input validation ensures JSON data integrity before queuing operations.
    /// </remarks>
    public class SqlDataService : ISqlDataService
    {
        /// <summary>
        /// The database connection string retrieved from application configuration.
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Logger instance for recording operational events, errors, and performance metrics.
        /// </summary>
        private readonly ILogger<ISqlDataService> _logger;

        /// <summary>
        /// Collection of performance metrics receivers that will receive metrics automatically.
        /// </summary>
        private readonly IEnumerable<ISqlDataServicePerformanceMetrics> _performanceMetricsReceivers;

        /// <summary>
        /// Thread-safe queue for order book data operations awaiting database persistence.
        /// </summary>
        private readonly ConcurrentQueue<DatabaseOperation> _orderBookQueue;

        /// <summary>
        /// Thread-safe queue for trade data operations awaiting database persistence.
        /// </summary>
        private readonly ConcurrentQueue<DatabaseOperation> _tradeQueue;

        /// <summary>
        /// Thread-safe queue for fill data operations awaiting database persistence.
        /// </summary>
        private readonly ConcurrentQueue<DatabaseOperation> _fillQueue;

        /// <summary>
        /// Thread-safe queue for event lifecycle data operations awaiting database persistence.
        /// </summary>
        private readonly ConcurrentQueue<DatabaseOperation> _eventLifecycleQueue;

        /// <summary>
        /// Thread-safe queue for market lifecycle data operations awaiting database persistence.
        /// </summary>
        private readonly ConcurrentQueue<DatabaseOperation> _marketLifecycleQueue;

        /// <summary>
        /// Cancellation token source for coordinating graceful shutdown of background worker tasks.
        /// </summary>
        private readonly CancellationTokenSource _cts;

        /// <summary>
        /// Array of background tasks processing the respective data queues asynchronously.
        /// </summary>
        private readonly Task[] _workerTasks;

        /// <summary>
        /// Number of retry attempts for transient SQL errors.
        /// </summary>
        private readonly int _retryCount;

        /// <summary>
        /// Base delay between retry attempts.
        /// </summary>
        private readonly TimeSpan _retryDelay;

        /// <summary>
        /// Maximum size for each queue to prevent resource exhaustion.
        /// </summary>
        private readonly int _maxQueueSize;

        /// <summary>
        /// Number of worker tasks per queue type.
        /// </summary>
        private readonly int _workersPerQueue;

        /// <summary>
        /// Batch size for processing multiple operations together.
        /// </summary>
        private readonly int _batchSize;

        /// <summary>
        /// Total operations processed successfully.
        /// </summary>
        private long _totalProcessed;

        /// <summary>
        /// Total operations that failed.
        /// </summary>
        private long _totalFailed;

        /// <summary>
        /// Start time for throughput calculations.
        /// </summary>
        private readonly DateTime _startTime;

        /// <summary>
        /// Total latency accumulated for processed operations (in milliseconds).
        /// </summary>
        private long _totalLatencyMs;

        /// <summary>
        /// Number of operations measured for latency.
        /// </summary>
        private long _latencySampleCount;

        /// <summary>
        /// Flag to enable or disable performance metrics collection.
        /// </summary>
        private readonly bool _enablePerformanceMetrics;

        /// <summary>
        /// Initializes a new instance of the SqlDataService with configuration and logging dependencies.
        /// Sets up concurrent queues for different data types and starts background worker tasks for processing.
        /// </summary>
        /// <param name="configuration">Application configuration containing database connection string.</param>
        /// <param name="logger">Logger for recording service operations and errors.</param>
        /// <param name="performanceMetricsReceivers">Collection of services that will receive performance metrics automatically.</param>
        public SqlDataService(IConfiguration configuration, ILogger<ISqlDataService> logger,
                             IEnumerable<ISqlDataServicePerformanceMetrics>? performanceMetricsReceivers = null)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection connection string is not configured.");
            _startTime = DateTime.UtcNow;
            _performanceMetricsReceivers = performanceMetricsReceivers ?? Array.Empty<ISqlDataServicePerformanceMetrics>();

            // Load configuration options with defaults
            var dataConfig = configuration.GetSection("BacklashBotData").Get<BacklashBotDataConfig>();
            _retryCount = dataConfig.MaxRetryCount;
            _retryDelay = TimeSpan.FromSeconds(dataConfig.RetryDelaySeconds);
            _maxQueueSize = dataConfig.MaxQueueSize;
            _workersPerQueue = dataConfig.WorkersPerQueue;
            _batchSize = dataConfig.BatchSize;
            _enablePerformanceMetrics = dataConfig.EnablePerformanceMetrics;

            _orderBookQueue = new ConcurrentQueue<DatabaseOperation>();
            _tradeQueue = new ConcurrentQueue<DatabaseOperation>();
            _fillQueue = new ConcurrentQueue<DatabaseOperation>();
            _eventLifecycleQueue = new ConcurrentQueue<DatabaseOperation>();
            _marketLifecycleQueue = new ConcurrentQueue<DatabaseOperation>();
            _cts = new CancellationTokenSource();

            // Create worker tasks based on configuration
            var workerTasks = new List<Task>();
            for (int i = 0; i < _workersPerQueue; i++)
            {
                workerTasks.Add(Task.Run(() => ProcessQueueAsync(_orderBookQueue, "order_book", _cts.Token)));
                workerTasks.Add(Task.Run(() => ProcessQueueAsync(_tradeQueue, "trade", _cts.Token)));
                workerTasks.Add(Task.Run(() => ProcessQueueAsync(_fillQueue, "fill", _cts.Token)));
                workerTasks.Add(Task.Run(() => ProcessQueueAsync(_eventLifecycleQueue, "event_lifecycle", _cts.Token)));
                workerTasks.Add(Task.Run(() => ProcessQueueAsync(_marketLifecycleQueue, "market_lifecycle", _cts.Token)));
            }
            _workerTasks = workerTasks.ToArray();
        }

        /// <summary>
        /// Executes a SQL Server Agent job to import market snapshot data from external files into the database.
        /// Monitors the job execution status and waits for completion, with retry logic for transient failures.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="SqlException">Thrown when SQL operations fail after retries.</exception>
        /// <exception cref="Exception">Thrown when job execution fails or cannot be monitored.</exception>
        public async Task ExecuteSnapshotImportJobAsync(CancellationToken cancellationToken = default)
        {
            const string jobName = "ImportSnapshots";

            var retryPolicy = Policy.Handle<SqlException>(ex => IsTransient(ex)).WaitAndRetryAsync(_retryCount, i => _retryDelay * i);
            await retryPolicy.ExecuteAsync(async () =>
            {
                {
                    try
                    {
                        await using var conn = new SqlConnection(_connectionString);
                        await conn.OpenAsync(cancellationToken);

                        // Start the SQL Agent Job
                        await using (var startCmd = new SqlCommand("msdb.dbo.sp_start_job", conn)
                        {
                            CommandType = CommandType.StoredProcedure,
                            CommandTimeout = 2400
                        })
                        {
                            startCmd.Parameters.AddWithValue("@job_name", jobName);
                            await startCmd.ExecuteNonQueryAsync(cancellationToken);
                            _logger.LogInformation("Started SQL Agent job: {JobName}", jobName);
                        }

                        // Get job_id
                        Guid jobId;
                        await using (var getJobIdCmd = new SqlCommand("SELECT job_id FROM msdb.dbo.sysjobs WHERE name = @jobName", conn))
                        {
                            getJobIdCmd.Parameters.AddWithValue("@jobName", jobName);
                            var result = await getJobIdCmd.ExecuteScalarAsync(cancellationToken);
                            jobId = (Guid)result;
                        }

                        // Poll for job activity completion
                        bool isRunning = true;
                        while (isRunning)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                            await using var activityCmd = new SqlCommand(@"
                SELECT stop_execution_date 
                FROM msdb.dbo.sysjobactivity 
                WHERE job_id = @jobId 
                  AND stop_execution_date IS NULL", conn);
                            activityCmd.Parameters.AddWithValue("@jobId", jobId);

                            var result = await activityCmd.ExecuteScalarAsync(cancellationToken);
                            isRunning = result != null;
                        }

                        // Get last job run status
                        await using var statusCmd = new SqlCommand(@"
            SELECT TOP 1 run_status, message 
            FROM msdb.dbo.sysjobhistory 
            WHERE job_id = @jobId 
              AND step_id = 0 
            ORDER BY run_date DESC, run_time DESC", conn);
                        statusCmd.Parameters.AddWithValue("@jobId", jobId);

                        await using var reader = await statusCmd.ExecuteReaderAsync(cancellationToken);
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            int runStatus = Convert.ToInt32(reader["run_status"]); // 1 = success, 0 = failed, etc.
                            string message = reader["message"]?.ToString() ?? "";

                            if (runStatus == 1)
                            {
                                _logger.LogInformation("SQL Agent job '{JobName}' completed successfully.", jobName);
                            }
                            else
                            {
                                _logger.LogError("SQL Agent job '{JobName}' failed. Status: {Status}, Message: {Message}", jobName, runStatus, message);
                                throw new Exception($"Job failed: {message}");
                            }
                        }
                        else
                        {
                            throw new Exception("Could not retrieve job history.");
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError(ex, "SQL error occurred while executing job {JobName}", jobName);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error while executing job {JobName}", jobName);
                        throw;
                    }
                }
            });
        
    }

        /// <summary>
        /// Asynchronously stores order book data from WebSocket messages into the database queue for processing.
        /// Handles both snapshot (SNP) and delta (DEL) order book updates, converting prices for "no" side orders.
        /// Includes input validation and queue size checks.
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket message data.</param>
        /// <param name="offerType">The type of order book update: "SNP" for snapshots or "DEL" for deltas.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreOrderBookAsync(JsonElement data, string offerType)
        {
            // Input validation
            if (!data.TryGetProperty("msg", out var msg))
            {
                _logger.LogWarning("Invalid order book data: missing 'msg' property");
                return Task.CompletedTask;
            }
            if (!msg.TryGetProperty("market_ticker", out var marketTickerProp) || string.IsNullOrEmpty(marketTickerProp.GetString()))
            {
                _logger.LogWarning("Invalid order book data: missing or empty 'market_ticker'");
                return Task.CompletedTask;
            }
            if (!data.TryGetProperty("sid", out var sidProp) || !sidProp.TryGetInt32(out var sid))
            {
                _logger.LogWarning("Invalid order book data: missing or invalid 'sid'");
                return Task.CompletedTask;
            }

            var marketTicker = marketTickerProp.GetString()!;
            var kalshiSeq = data.TryGetProperty("seq", out var seq) ? seq.GetInt64() : (long?)null;

            // Queue size check
            if (_orderBookQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning("Order book queue at max capacity ({MaxSize}), dropping operation for {MarketTicker}", _maxQueueSize, marketTicker);
                return Task.CompletedTask;
            }

            if (offerType == "SNP")
            {
                foreach (var side in new[] { "yes", "no" })
                {
                    if (msg.TryGetProperty(side, out var orders) && orders.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var priceLevel in orders.EnumerateArray())
                        {
                            if (priceLevel.ValueKind != JsonValueKind.Array || priceLevel.GetArrayLength() < 2)
                            {
                                _logger.LogWarning("Invalid price level in order book data for {MarketTicker}", marketTicker);
                                continue;
                            }
                            var price = side == "no" ? 100 - priceLevel[0].GetInt32() : priceLevel[0].GetInt32();
                            _orderBookQueue.Enqueue(new DatabaseOperation
                            {
                                StoredProcedure = "dbo.sp_InsertFeed_OrderBook",
                                Identifier = marketTicker,
                                EnqueueTime = DateTime.UtcNow,
                                SetParameters = cmd => SetOrderBookParameters(cmd, msg, sid, kalshiSeq, marketTicker, offerType, price, null, side, priceLevel[1].GetInt32())
                            });
                        }
                    }
                }
            }
            else if (offerType == "DEL")
            {
                if (!msg.TryGetProperty("price", out var priceProp) || !priceProp.TryGetInt32(out var price))
                {
                    _logger.LogWarning("Invalid delta order book data: missing or invalid 'price' for {MarketTicker}", marketTicker);
                    return Task.CompletedTask;
                }
                if (!msg.TryGetProperty("side", out var sideProp) || sideProp.GetString() is not string side)
                {
                    _logger.LogWarning("Invalid delta order book data: missing or invalid 'side' for {MarketTicker}", marketTicker);
                    return Task.CompletedTask;
                }
                if (!msg.TryGetProperty("delta", out var deltaProp) || !deltaProp.TryGetInt32(out var delta))
                {
                    _logger.LogWarning("Invalid delta order book data: missing or invalid 'delta' for {MarketTicker}", marketTicker);
                    return Task.CompletedTask;
                }
                if (side == "no") price = 100 - price;
                _orderBookQueue.Enqueue(new DatabaseOperation
                {
                    StoredProcedure = "dbo.sp_InsertFeed_OrderBook",
                    Identifier = marketTicker,
                    EnqueueTime = DateTime.UtcNow,
                    SetParameters = cmd => SetOrderBookParameters(cmd, msg, sid, kalshiSeq, marketTicker, offerType, price, delta, side, 0)
                });
            }
            else
            {
                _logger.LogWarning("Unknown offer type '{OfferType}' for order book data", offerType);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously stores ticker data from WebSocket messages into the database queue for processing.
        /// Captures current market prices, bid/ask spreads, volume, and open interest information.
        /// Includes input validation and queue size checks.
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket ticker message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreTickerAsync(JsonElement data)
        {
            // Input validation
            if (!data.TryGetProperty("msg", out var msg))
            {
                _logger.LogWarning("Invalid ticker data: missing 'msg' property");
                return Task.CompletedTask;
            }
            if (!msg.TryGetProperty("market_ticker", out var marketTickerProp) || string.IsNullOrEmpty(marketTickerProp.GetString()))
            {
                _logger.LogWarning("Invalid ticker data: missing or empty 'market_ticker'");
                return Task.CompletedTask;
            }
            // Check required fields
            if (!msg.TryGetProperty("price", out _) || !msg.TryGetProperty("yes_bid", out _) || !msg.TryGetProperty("yes_ask", out _) ||
                !msg.TryGetProperty("volume", out _) || !msg.TryGetProperty("open_interest", out _) ||
                !msg.TryGetProperty("dollar_volume", out _) || !msg.TryGetProperty("dollar_open_interest", out _) ||
                !msg.TryGetProperty("ts", out _))
            {
                _logger.LogWarning("Invalid ticker data: missing required fields for {MarketTicker}", marketTickerProp.GetString());
                return Task.CompletedTask;
            }

            var marketTicker = marketTickerProp.GetString()!;

            // Queue size check
            if (_tradeQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning("Trade queue at max capacity ({MaxSize}), dropping ticker operation for {MarketTicker}", _maxQueueSize, marketTicker);
                return Task.CompletedTask;
            }

            _tradeQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_Ticker",
                Identifier = marketTicker,
                EnqueueTime = DateTime.UtcNow,
                SetParameters = cmd =>
                {
                    cmd.Parameters.AddWithValue("@market_id", msg.TryGetProperty("market_id", out var mid)
                        ? mid.GetString() : "00000000-0000-0000-0000-000000000000");
                    cmd.Parameters.AddWithValue("@market_ticker", marketTicker);
                    cmd.Parameters.AddWithValue("@price", msg.GetProperty("price").GetInt32());
                    cmd.Parameters.AddWithValue("@yes_bid", msg.GetProperty("yes_bid").GetInt32());
                    cmd.Parameters.AddWithValue("@yes_ask", msg.GetProperty("yes_ask").GetInt32());
                    cmd.Parameters.AddWithValue("@volume", msg.GetProperty("volume").GetInt32());
                    cmd.Parameters.AddWithValue("@open_interest", msg.GetProperty("open_interest").GetInt32());
                    cmd.Parameters.AddWithValue("@dollar_volume", msg.GetProperty("dollar_volume").GetInt32());
                    cmd.Parameters.AddWithValue("@dollar_open_interest", msg.GetProperty("dollar_open_interest").GetInt32());
                    cmd.Parameters.AddWithValue("@ts", msg.GetProperty("ts").GetInt64());
                    cmd.Parameters.AddWithValue("@LoggedDate", DateTime.Now);
                }
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously stores trade execution data from WebSocket messages into the database queue for processing.
        /// Records trade details including prices, volumes, and taker side information.
        /// Includes input validation and queue size checks.
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket trade message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreTradeAsync(JsonElement data)
        {
            // Input validation
            if (!data.TryGetProperty("msg", out var msg))
            {
                _logger.LogWarning("Invalid trade data: missing 'msg' property");
                return Task.CompletedTask;
            }
            if (!msg.TryGetProperty("market_ticker", out var marketTickerProp) || string.IsNullOrEmpty(marketTickerProp.GetString()))
            {
                _logger.LogWarning("Invalid trade data: missing or empty 'market_ticker'");
                return Task.CompletedTask;
            }
            // Check required fields
            if (!msg.TryGetProperty("yes_price", out _) || !msg.TryGetProperty("no_price", out _) ||
                !msg.TryGetProperty("count", out _) || !msg.TryGetProperty("taker_side", out _) ||
                !msg.TryGetProperty("ts", out _))
            {
                _logger.LogWarning("Invalid trade data: missing required fields for {MarketTicker}", marketTickerProp.GetString());
                return Task.CompletedTask;
            }

            var marketTicker = marketTickerProp.GetString()!;

            // Queue size check
            if (_tradeQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning("Trade queue at max capacity ({MaxSize}), dropping trade operation for {MarketTicker}", _maxQueueSize, marketTicker);
                return Task.CompletedTask;
            }

            _tradeQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_Trade",
                Identifier = marketTicker,
                EnqueueTime = DateTime.UtcNow,
                SetParameters = cmd =>
                {
                    cmd.Parameters.AddWithValue("@market_ticker", marketTicker);
                    cmd.Parameters.AddWithValue("@yes_price", msg.GetProperty("yes_price").GetInt32());
                    cmd.Parameters.AddWithValue("@no_price", msg.GetProperty("no_price").GetInt32());
                    cmd.Parameters.AddWithValue("@count", msg.GetProperty("count").GetInt32());
                    cmd.Parameters.AddWithValue("@taker_side", msg.GetProperty("taker_side").GetString());
                    cmd.Parameters.AddWithValue("@ts", msg.GetProperty("ts").GetInt64());
                    cmd.Parameters.AddWithValue("@LoggedDate", DateTime.Now);
                }
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously stores order fill data from WebSocket messages into the database queue for processing.
        /// Records fill details including trade IDs, order IDs, prices, and execution information.
        /// Includes input validation and queue size checks.
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket fill message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreFillAsync(JsonElement data)
        {
            // Input validation
            if (!data.TryGetProperty("msg", out var msg))
            {
                _logger.LogWarning("Invalid fill data: missing 'msg' property");
                return Task.CompletedTask;
            }
            if (!msg.TryGetProperty("market_ticker", out var marketTickerProp) || string.IsNullOrEmpty(marketTickerProp.GetString()))
            {
                _logger.LogWarning("Invalid fill data: missing or empty 'market_ticker'");
                return Task.CompletedTask;
            }
            // Check required fields
            if (!msg.TryGetProperty("is_taker", out _) || !msg.TryGetProperty("side", out _) ||
                !msg.TryGetProperty("count", out _) || !msg.TryGetProperty("action", out _) ||
                !msg.TryGetProperty("ts", out _))
            {
                _logger.LogWarning("Invalid fill data: missing required fields for {MarketTicker}", marketTickerProp.GetString());
                return Task.CompletedTask;
            }

            var marketTicker = marketTickerProp.GetString()!;

            // Queue size check
            if (_fillQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning("Fill queue at max capacity ({MaxSize}), dropping fill operation for {MarketTicker}", _maxQueueSize, marketTicker);
                return Task.CompletedTask;
            }

            _fillQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_Fill",
                Identifier = marketTicker,
                EnqueueTime = DateTime.UtcNow,
                SetParameters = cmd =>
                {
                    cmd.Parameters.AddWithValue("@trade_id", msg.TryGetProperty("trade_id", out var tradeId)
                        ? tradeId.GetString() : "00000000-0000-0000-0000-000000000000");
                    cmd.Parameters.AddWithValue("@order_id", msg.TryGetProperty("order_id", out var orderId)
                        ? orderId.GetString() : "00000000-0000-0000-0000-000000000000");
                    cmd.Parameters.AddWithValue("@market_ticker", marketTicker);
                    cmd.Parameters.AddWithValue("@is_taker", msg.GetProperty("is_taker").GetBoolean());
                    cmd.Parameters.AddWithValue("@side", msg.GetProperty("side").GetString());
                    cmd.Parameters.AddWithValue("@yes_price", msg.TryGetProperty("yes_price", out var yesPrice)
                        ? yesPrice.GetInt32() : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@no_price", msg.TryGetProperty("no_price", out var noPrice)
                        ? noPrice.GetInt32() : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@count", msg.GetProperty("count").GetInt32());
                    cmd.Parameters.AddWithValue(KalshiConstants.Parameter_Action, msg.GetProperty("action").GetString());
                    cmd.Parameters.AddWithValue("@ts", msg.GetProperty("ts").GetInt64());
                    cmd.Parameters.AddWithValue("@LoggedDate", DateTime.Now);
                }
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously stores event lifecycle data from WebSocket messages into the database queue for processing.
        /// Records event metadata including titles, subtitles, collateral types, and timing information.
        /// Includes input validation and queue size checks.
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket event lifecycle message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreEventLifecycleAsync(JsonElement data)
        {
            // Input validation
            if (!data.TryGetProperty("msg", out var msg))
            {
                _logger.LogWarning("Invalid event lifecycle data: missing 'msg' property");
                return Task.CompletedTask;
            }
            if (!msg.TryGetProperty("event_ticker", out var eventTickerProp) || string.IsNullOrEmpty(eventTickerProp.GetString()))
            {
                _logger.LogWarning("Invalid event lifecycle data: missing or empty 'event_ticker'");
                return Task.CompletedTask;
            }
            // Check required fields
            if (!msg.TryGetProperty("title", out _) || !msg.TryGetProperty("series_ticker", out _))
            {
                _logger.LogWarning("Invalid event lifecycle data: missing required fields for {EventTicker}", eventTickerProp.GetString());
                return Task.CompletedTask;
            }

            var eventTicker = eventTickerProp.GetString()!;

            // Queue size check
            if (_eventLifecycleQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning("Event lifecycle queue at max capacity ({MaxSize}), dropping operation for {EventTicker}", _maxQueueSize, eventTicker);
                return Task.CompletedTask;
            }

            _eventLifecycleQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_Lifecycle_Event",
                Identifier = eventTicker,
                EnqueueTime = DateTime.UtcNow,
                SetParameters = cmd =>
                {
                    cmd.Parameters.AddWithValue("@event_ticker", eventTicker);
                    cmd.Parameters.AddWithValue("@title", msg.GetProperty("title").GetString());
                    cmd.Parameters.AddWithValue("@sub_title", msg.TryGetProperty("sub_title", out var subTitle)
                        ? subTitle.GetString() : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@collateral_return_type", msg.TryGetProperty("collateral_return_type", out var crt)
                        ? crt.GetString() : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@series_ticker", msg.GetProperty("series_ticker").GetString());
                    cmd.Parameters.AddWithValue("@strike_date", msg.TryGetProperty("strike_date", out var sd)
                        ? sd.GetInt64() : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@strike_period", msg.TryGetProperty("strike_period", out var sp)
                        ? sp.GetString() : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@LoggedDate", DateTime.Now);
                }
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously stores market lifecycle data from WebSocket messages into the database queue for processing.
        /// Records market state changes including open/close times, determination times, and settlement results.
        /// Includes input validation and queue size checks.
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket market lifecycle message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreMarketLifecycleAsync(JsonElement data)
        {
            // Input validation
            if (!data.TryGetProperty("msg", out var msg))
            {
                _logger.LogWarning("Invalid market lifecycle data: missing 'msg' property");
                return Task.CompletedTask;
            }
            if (!msg.TryGetProperty("market_ticker", out var marketTickerProp) || string.IsNullOrEmpty(marketTickerProp.GetString()))
            {
                _logger.LogWarning("Invalid market lifecycle data: missing or empty 'market_ticker'");
                return Task.CompletedTask;
            }

            var marketTicker = marketTickerProp.GetString()!;

            // Queue size check
            if (_marketLifecycleQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning("Market lifecycle queue at max capacity ({MaxSize}), dropping operation for {MarketTicker}", _maxQueueSize, marketTicker);
                return Task.CompletedTask;
            }

            _marketLifecycleQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_LifeCycle_Market",
                Identifier = marketTicker,
                EnqueueTime = DateTime.UtcNow,
                SetParameters = cmd =>
                {
                    cmd.Parameters.AddWithValue("@market_ticker", marketTicker);

                    // Handle open_ts
                    cmd.Parameters.AddWithValue("@open_ts",
                        msg.TryGetProperty("open_ts", out var openTs)
                        ? openTs.GetInt64()
                        : (object?)null);

                    // Handle close_ts
                    cmd.Parameters.AddWithValue("@close_ts",
                        msg.TryGetProperty("close_ts", out var closeTs)
                        ? closeTs.GetInt64()
                        : (object?)null);

                    // Handle determination_ts
                    cmd.Parameters.AddWithValue("@determination_ts",
                        msg.TryGetProperty("determination_ts", out var detTs)
                        ? detTs.GetInt64()
                        : (object?)null);

                    // Handle settled_ts
                    cmd.Parameters.AddWithValue("@settled_ts",
                        msg.TryGetProperty("settled_ts", out var setTs)
                        ? setTs.GetInt64()
                        : (object?)null);

                    // Handle result
                    cmd.Parameters.AddWithValue("@result",
                        msg.TryGetProperty("result", out var result)
                        ? result.GetString()
                        : (object?)null);

                    // Handle is_deactivated
                    cmd.Parameters.AddWithValue("@is_deactivated",
                        msg.TryGetProperty("is_deactivated", out var isDeactivated)
                        ? isDeactivated.GetBoolean()
                        : false);

                    cmd.Parameters.AddWithValue("@LoggedDate", DateTime.Now);
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the total number of operations processed successfully.
        /// </summary>
        public long TotalProcessed => _totalProcessed;

        /// <summary>
        /// Gets the total number of operations that failed.
        /// </summary>
        public long TotalFailed => _totalFailed;

        /// <summary>
        /// Gets the current depth of the order book queue.
        /// </summary>
        public int OrderBookQueueDepth => _orderBookQueue.Count;

        /// <summary>
        /// Gets the current depth of the trade queue.
        /// </summary>
        public int TradeQueueDepth => _tradeQueue.Count;

        /// <summary>
        /// Gets the current depth of the fill queue.
        /// </summary>
        public int FillQueueDepth => _fillQueue.Count;

        /// <summary>
        /// Gets the current depth of the event lifecycle queue.
        /// </summary>
        public int EventLifecycleQueueDepth => _eventLifecycleQueue.Count;

        /// <summary>
        /// Gets the current depth of the market lifecycle queue.
        /// </summary>
        public int MarketLifecycleQueueDepth => _marketLifecycleQueue.Count;

        /// <summary>
        /// Gets the success rate as a percentage (0-100).
        /// </summary>
        public double SuccessRate
        {
            get
            {
                var total = _totalProcessed + _totalFailed;
                return total > 0 ? (_totalProcessed * 100.0) / total : 0;
            }
        }

        /// <summary>
        /// Gets the total number of queued operations across all queues.
        /// </summary>
        public int TotalQueuedOperations =>
            _orderBookQueue.Count + _tradeQueue.Count + _fillQueue.Count +
            _eventLifecycleQueue.Count + _marketLifecycleQueue.Count;

        /// <summary>
        /// Gets the operations per second (throughput) since service start.
        /// Returns 0 if performance metrics are disabled.
        /// </summary>
        public double OperationsPerSecond
        {
            get
            {
                if (!_enablePerformanceMetrics) return 0;
                var elapsed = DateTime.UtcNow - _startTime;
                return elapsed.TotalSeconds > 0 ? _totalProcessed / elapsed.TotalSeconds : 0;
            }
        }

        /// <summary>
        /// Gets the average latency in milliseconds for processed operations.
        /// Returns 0 if performance metrics are disabled.
        /// </summary>
        public double AverageLatencyMs
        {
            get
            {
                if (!_enablePerformanceMetrics) return 0;
                return _latencySampleCount > 0 ? (double)_totalLatencyMs / _latencySampleCount : 0;
            }
        }

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// Returns 0 if performance metrics are disabled.
        /// </summary>
        public double CpuUsage
        {
            get
            {
                if (!_enablePerformanceMetrics) return 0;
                try
                {
                    var process = Process.GetCurrentProcess();
                    return (process.TotalProcessorTime.TotalMilliseconds / (DateTime.UtcNow - _startTime).TotalMilliseconds) * 100;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the current memory usage in MB.
        /// Returns 0 if performance metrics are disabled.
        /// </summary>
        public double MemoryUsageMB
        {
            get
            {
                if (!_enablePerformanceMetrics) return 0;
                try
                {
                    var process = Process.GetCurrentProcess();
                    return process.WorkingSet64 / (1024.0 * 1024.0);
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Broadcasts current performance metrics to all registered receivers.
        /// Only sends metrics if performance monitoring is enabled.
        /// </summary>
        private void BroadcastPerformanceMetrics()
        {
            if (!_enablePerformanceMetrics || !_performanceMetricsReceivers.Any())
                return;

            try
            {
                foreach (var receiver in _performanceMetricsReceivers)
                {
                    try
                    {
                        receiver.ReceiveThroughputMetrics(OperationsPerSecond, _totalProcessed, _totalFailed);
                        receiver.ReceiveLatencyMetrics(AverageLatencyMs, _latencySampleCount);
                        receiver.ReceiveResourceMetrics(CpuUsage, MemoryUsageMB);
                        receiver.ReceiveQueueMetrics(OrderBookQueueDepth, TradeQueueDepth, FillQueueDepth,
                                                   EventLifecycleQueueDepth, MarketLifecycleQueueDepth, TotalQueuedOperations);
                        receiver.ReceiveSuccessRateMetrics(SuccessRate);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send performance metrics to receiver {ReceiverType}",
                            receiver.GetType().Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting performance metrics");
            }
        }

        /// <summary>
        /// Disposes of the service resources, canceling background worker tasks and cleaning up cancellation tokens.
        /// Waits for worker tasks to complete gracefully within a timeout period.
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                Task.WaitAll(_workerTasks, TimeSpan.FromSeconds(5)); // Timeout to avoid hanging
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                _logger.LogInformation("SqlDataService worker tasks were canceled as expected during disposal.");
            }
            _cts.Dispose();
        }

        /// <summary>
        /// Determines whether a SQL exception represents a transient error that should be retried.
        /// Checks against a predefined list of SQL error codes known to be transient in nature.
        /// </summary>
        /// <param name="ex">The SQL exception to evaluate.</param>
        /// <returns>True if the exception is transient and should be retried; otherwise, false.</returns>
        private static bool IsTransient(SqlException ex)
        {
            var transientErrors = new HashSet<int> { 1205, 1222, 49918, 49919, 49920, 4060, 40197, 40501, 40613, 40143, 233, 64 };
            return transientErrors.Contains(ex.Number);
        }


   
        /// <summary>
        /// Background worker method that continuously processes database operations from a specific queue.
        /// Executes stored procedures with retry logic for transient SQL errors and handles specific error conditions.
        /// Supports batch processing and collects performance metrics.
        /// </summary>
        /// <param name="queue">The concurrent queue containing database operations to process.</param>
        /// <param name="operationName">Descriptive name of the operation type for logging and error handling.</param>
        /// <param name="cancellationToken">Token to signal when processing should stop.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessQueueAsync(ConcurrentQueue<DatabaseOperation> queue, string operationName, CancellationToken cancellationToken)
        {
            var operations = new List<DatabaseOperation>();
            var stopwatch = new Stopwatch();
            DateTime lastMetricsLog = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                operations.Clear();
                // Collect batch
                for (int i = 0; i < _batchSize; i++)
                {
                    if (queue.TryDequeue(out var operation))
                    {
                        operations.Add(operation);
                    }
                    else
                    {
                        break;
                    }
                }

                if (operations.Count > 0)
                {
                    stopwatch.Restart();
                    var retryPolicy = Policy.Handle<SqlException>(ex => IsTransient(ex)).WaitAndRetryAsync(_retryCount, i => _retryDelay * i);
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        await using var conn = new SqlConnection(_connectionString);
                        await conn.OpenAsync(cancellationToken);
                        foreach (var operation in operations)
                        {
                            try
                            {
                                await using var cmd = new SqlCommand(operation.StoredProcedure, conn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 60 };
                                operation.SetParameters(cmd);
                                await cmd.ExecuteNonQueryAsync(cancellationToken);
                                Interlocked.Increment(ref _totalProcessed);

                                // Measure latency if enabled
                                if (_enablePerformanceMetrics)
                                {
                                    var latency = (DateTime.UtcNow - operation.EnqueueTime).TotalMilliseconds;
                                    Interlocked.Add(ref _totalLatencyMs, (long)latency);
                                    Interlocked.Increment(ref _latencySampleCount);
                                }
                            }
                            catch (SqlException ex) when (ex.Message.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogWarning(ex, "Duplicate insert attempted for {OperationName}: {Identifier}", operationName, operation.Identifier);
                                Interlocked.Increment(ref _totalFailed);
                            }
                            catch (SqlException ex) when (ex.Message.Contains("deadlocked", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogWarning(ex, "Deadlock occurred for {OperationName}: {Identifier}. SQL error: {SqlError}", operationName, operation.Identifier, ex.Message);
                                Interlocked.Increment(ref _totalFailed);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to execute {OperationName} for {Identifier}", operationName, operation.Identifier);
                                Interlocked.Increment(ref _totalFailed);
                            }
                        }
                    });
                    stopwatch.Stop();
                    _logger.LogInformation("Processed batch of {Count} {OperationName} operations in {ElapsedMs}ms", operations.Count, operationName, stopwatch.ElapsedMilliseconds);

                    // Log metrics periodically
                    if ((DateTime.UtcNow - lastMetricsLog).TotalMinutes >= 1)
                    {
                        _logger.LogInformation("Metrics - {OperationName}: Processed: {Processed}, Failed: {Failed}, Queue Depth: {Depth}",
                            operationName, Interlocked.Read(ref _totalProcessed), Interlocked.Read(ref _totalFailed), queue.Count);
                        lastMetricsLog = DateTime.UtcNow;

                        // Broadcast performance metrics to registered receivers
                        BroadcastPerformanceMetrics();
                    }
                }
                else
                {
                    await Task.Delay(10, cancellationToken); // Prevent tight loop
                }
            }
        }
        /// <summary>
        /// Configures the SQL command parameters for order book stored procedure execution.
        /// Maps WebSocket message data to the required stored procedure parameters.
        /// </summary>
        /// <param name="cmd">The SQL command to configure with parameters.</param>
        /// <param name="msg">The JSON message element containing order book data.</param>
        /// <param name="sid">The subscription ID for the order book data.</param>
        /// <param name="kalshiSeq">The Kalshi sequence number for ordering.</param>
        /// <param name="marketTicker">The market ticker identifier.</param>
        /// <param name="offerType">The type of order book offer (SNP or DEL).</param>
        /// <param name="price">The price level for the order book entry.</param>
        /// <param name="delta">The change in quantity for delta updates (null for snapshots).</param>
        /// <param name="side">The side of the order book (yes or no).</param>
        /// <param name="restingContracts">The number of resting contracts at this price level.</param>
        private void SetOrderBookParameters(SqlCommand cmd, JsonElement msg, int sid, long? kalshiSeq,
            string marketTicker, string offerType, int price, int? delta, string side, int restingContracts)
        {
            cmd.Parameters.AddWithValue("@market_id", msg.TryGetProperty("market_id", out var mid)
                ? mid.GetString() : "00000000-0000-0000-0000-000000000000");
            cmd.Parameters.AddWithValue("@sid", sid);
            cmd.Parameters.AddWithValue("@kalshi_seq", kalshiSeq.HasValue ? kalshiSeq.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@market_ticker", marketTicker);
            cmd.Parameters.AddWithValue("@offer_type", offerType);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@delta", delta ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@side", side);
            cmd.Parameters.AddWithValue("@resting_contracts", restingContracts);
            cmd.Parameters.AddWithValue("@LoggedDate", DateTime.Now);
        }
        /// <summary>
        /// Represents a database operation to be executed asynchronously by the background worker tasks.
        /// Encapsulates the stored procedure name, parameter configuration, identifier, and enqueue time for latency tracking.
        /// </summary>
        private struct DatabaseOperation
        {
            /// <summary>
            /// The name of the stored procedure to execute.
            /// </summary>
            public string StoredProcedure { get; init; }

            /// <summary>
            /// Action delegate that configures the SQL command parameters for the stored procedure.
            /// </summary>
            public Action<SqlCommand> SetParameters { get; init; }

            /// <summary>
            /// Identifier for the operation (typically market ticker) used for logging and error tracking.
            /// </summary>
            public string Identifier { get; init; }

            /// <summary>
            /// The time when the operation was enqueued, used for latency calculations.
            /// </summary>
            public DateTime EnqueueTime { get; init; }
        }
    }
    
}
