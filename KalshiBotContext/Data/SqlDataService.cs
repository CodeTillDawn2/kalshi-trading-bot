using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BacklashBot.Services.Interfaces;
using BacklashDTOs.Exceptions;
using BacklashInterfaces.Constants;
using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;

using Polly;
using Polly.Retry;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
namespace KalshiBotData.Data
{
    /// <summary>
    /// Provides asynchronous data persistence services for real-time market data from Kalshi's trading platform.
    /// This service manages concurrent queues for different data types (order book, trades, fills, lifecycle events)
    /// and processes them using background worker tasks with retry logic for transient SQL errors.
    /// Implements the ISqlDataService interface for dependency injection and proper resource management.
    /// </summary>
    /// <remarks>
    /// The service uses Polly for retry policies on SQL operations and maintains separate queues to ensure
    /// data integrity and prevent blocking between different data types. All operations are asynchronous
    /// to support high-throughput real-time data processing without impacting the main application flow.
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
        /// Initializes a new instance of the SqlDataService with configuration and logging dependencies.
        /// Sets up concurrent queues for different data types and starts background worker tasks for processing.
        /// </summary>
        /// <param name="configuration">Application configuration containing database connection string.</param>
        /// <param name="logger">Logger for recording service operations and errors.</param>
        public SqlDataService(IConfiguration configuration, ILogger<ISqlDataService> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection connection string is not configured.");
            _orderBookQueue = new ConcurrentQueue<DatabaseOperation>();
            _tradeQueue = new ConcurrentQueue<DatabaseOperation>();
            _fillQueue = new ConcurrentQueue<DatabaseOperation>();
            _eventLifecycleQueue = new ConcurrentQueue<DatabaseOperation>();
            _marketLifecycleQueue = new ConcurrentQueue<DatabaseOperation>();
            _cts = new CancellationTokenSource();
            _workerTasks = new[]
            {
                Task.Run(() => ProcessQueueAsync(_orderBookQueue, "order_book", _cts.Token)),
                Task.Run(() => ProcessQueueAsync(_tradeQueue, "trade", _cts.Token)),
                Task.Run(() => ProcessQueueAsync(_fillQueue, "fill", _cts.Token)),
                Task.Run(() => ProcessQueueAsync(_eventLifecycleQueue, "event_lifecycle", _cts.Token)),
                Task.Run(() => ProcessQueueAsync(_marketLifecycleQueue, "market_lifecycle", _cts.Token))
            };
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

            var retryPolicy = Policy.Handle<SqlException>(ex => IsTransient(ex)).WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i));
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
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket message data.</param>
        /// <param name="offerType">The type of order book update: "SNP" for snapshots or "DEL" for deltas.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreOrderBookAsync(JsonElement data, string offerType)
        {
            var msg = data.GetProperty("msg");
            var marketTicker = msg.GetProperty("market_ticker").GetString() ?? string.Empty;
            var sid = data.GetProperty("sid").GetInt32();
            var kalshiSeq = data.TryGetProperty("seq", out var seq) ? seq.GetInt64() : (long?)null;

            if (offerType == "SNP")
            {
                foreach (var side in new[] { "yes", "no" })
                {
                    if (msg.TryGetProperty(side, out var orders) && orders.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var priceLevel in orders.EnumerateArray())
                        {
                            var price = side == "no" ? 100 - priceLevel[0].GetInt32() : priceLevel[0].GetInt32();
                            _orderBookQueue.Enqueue(new DatabaseOperation
                            {
                                StoredProcedure = "dbo.sp_InsertFeed_OrderBook",
                                Identifier = marketTicker,
                                SetParameters = cmd => SetOrderBookParameters(cmd, msg, sid, kalshiSeq, marketTicker, offerType, price, null, side, priceLevel[1].GetInt32())
                            });
                        }
                    }
                }
            }
            else // DEL
            {
                var price = msg.GetProperty("price").GetInt32();
                if (msg.GetProperty("side").GetString() == "no") price = 100 - price;
                _orderBookQueue.Enqueue(new DatabaseOperation
                {
                    StoredProcedure = "dbo.sp_InsertFeed_OrderBook",
                    Identifier = marketTicker,
                    SetParameters = cmd => SetOrderBookParameters(cmd, msg, sid, kalshiSeq, marketTicker, offerType, price,
                        msg.GetProperty("delta").GetInt32(), msg.GetProperty("side").GetString() ?? string.Empty, 0)
                });
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously stores ticker data from WebSocket messages into the database queue for processing.
        /// Captures current market prices, bid/ask spreads, volume, and open interest information.
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket ticker message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreTickerAsync(JsonElement data)
        {
            var msg = data.GetProperty("msg");
            var marketTicker = msg.GetProperty("market_ticker").GetString() ?? string.Empty;
            _tradeQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_Ticker",
                Identifier = marketTicker,
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
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket trade message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreTradeAsync(JsonElement data)
        {
            var msg = data.GetProperty("msg");
            var marketTicker = msg.GetProperty("market_ticker").GetString() ?? string.Empty;
            _tradeQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_Trade",
                Identifier = marketTicker,
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
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket fill message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreFillAsync(JsonElement data)
        {
            var msg = data.GetProperty("msg");
            var marketTicker = msg.GetProperty("market_ticker").GetString() ?? string.Empty;
            _fillQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_Fill",
                Identifier = marketTicker,
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
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket event lifecycle message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        public Task StoreEventLifecycleAsync(JsonElement data)
        {
            var msg = data.GetProperty("msg");
            var eventTicker = msg.GetProperty("event_ticker").GetString() ?? string.Empty;
            _eventLifecycleQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_Lifecycle_Event",
                Identifier = eventTicker,
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
        /// Validates required market ticker presence before queuing the operation.
        /// </summary>
        /// <param name="data">The JSON element containing the WebSocket market lifecycle message data.</param>
        /// <returns>A completed task since the operation is queued for background processing.</returns>
        /// <exception cref="ArgumentException">Thrown when required 'msg' or 'market_ticker' properties are missing.</exception>
        public Task StoreMarketLifecycleAsync(JsonElement data)
        {
            // Check if "msg" property exists
            if (!data.TryGetProperty("msg", out var msg))
            {
                return Task.FromException(new ArgumentException("The 'msg' property is missing in the provided JSON data."));
            }

            // Safely extract market_ticker with null as default
            string? marketTicker = msg.TryGetProperty("market_ticker", out var marketTickerProp)
                ? marketTickerProp.GetString()
                : null;

            // If market_ticker is null or empty, return an error as it appears critical
            if (string.IsNullOrEmpty(marketTicker))
            {
                return Task.FromException(new ArgumentException("The 'market_ticker' property is missing or empty."));
            }

            _marketLifecycleQueue.Enqueue(new DatabaseOperation
            {
                StoredProcedure = "dbo.sp_InsertFeed_LifeCycle_Market",
                Identifier = marketTicker,
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
        /// </summary>
        /// <param name="queue">The concurrent queue containing database operations to process.</param>
        /// <param name="operationName">Descriptive name of the operation type for logging and error handling.</param>
        /// <param name="cancellationToken">Token to signal when processing should stop.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessQueueAsync(ConcurrentQueue<DatabaseOperation> queue, string operationName, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var operation))
                {
                    var retryPolicy = Policy.Handle<SqlException>(ex => IsTransient(ex)).WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i));
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        try
                        {
                            await using var conn = new SqlConnection(_connectionString);
                            await conn.OpenAsync(cancellationToken);
                            await using var cmd = new SqlCommand(operation.StoredProcedure, conn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 60 };
                            operation.SetParameters(cmd);
                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                        }
                        catch (SqlException ex) when (ex.Message.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning(new KnownDuplicateInsertException(operationName, operation.Identifier,
                                $"Duplicate insert attempted for {operationName}: {operation.Identifier}", ex),
                                "Duplicate insert attempted for {OperationName}: {Identifier}", operationName, operation.Identifier);
                        }
                        catch (SqlException ex) when (ex.Message.Contains("deadlocked", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning(new MarketInterestScoreDeadlockException(
                                $"Deadlock occurred for {operationName}: {operation.Identifier}. SQL error: {ex.Message}", ex),
                                "Deadlock occurred for {OperationName}: {Identifier}. SQL error: {ex.Message}", operationName, operation.Identifier);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to execute {OperationName} for {Identifier}", operationName, operation.Identifier);
                        }
                    });
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
            cmd.Parameters.AddWithValue("@kalshi_seq", (object)kalshiSeq ?? DBNull.Value);
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
        /// Encapsulates the stored procedure name, parameter configuration, and an identifier for logging.
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
        }
    }
    
}
