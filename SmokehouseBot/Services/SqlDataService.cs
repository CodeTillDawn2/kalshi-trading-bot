using Microsoft.Data.SqlClient;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs.Exceptions;
using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;

namespace SmokehouseBot.Services
{
    public class SqlDataService : ISqlDataService
    {
        private readonly string _connectionString;
        private readonly ILogger<ISqlDataService> _logger;
        private readonly ConcurrentQueue<DatabaseOperation> _orderBookQueue;
        private readonly ConcurrentQueue<DatabaseOperation> _tradeQueue;
        private readonly ConcurrentQueue<DatabaseOperation> _fillQueue;
        private readonly ConcurrentQueue<DatabaseOperation> _eventLifecycleQueue;
        private readonly ConcurrentQueue<DatabaseOperation> _marketLifecycleQueue;
        private readonly CancellationTokenSource _cts;
        private readonly Task[] _workerTasks;

        public SqlDataService(IConfiguration configuration, ILogger<ISqlDataService> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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

        public async Task ImportSnapshotsFromFilesAsync(CancellationToken cancellationToken = default)
        {
            const string jobName = "ImportSnapshots";

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
                                SetParameters = cmd => SetOrderBookParameters(cmd, msg, sid, kalshiSeq.Value, marketTicker, offerType, price, null, side, priceLevel[1].GetInt32())
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
                    SetParameters = cmd => SetOrderBookParameters(cmd, msg, sid, kalshiSeq.Value, marketTicker, offerType, price,
                        msg.GetProperty("delta").GetInt32(), msg.GetProperty("side").GetString() ?? string.Empty, 0)
                });
            }
            return Task.CompletedTask;
        }

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
                    cmd.Parameters.AddWithValue("@trade_id", "00000000-0000-0000-0000-000000000000");
                    cmd.Parameters.AddWithValue("@order_id", "00000000-0000-0000-0000-000000000000");
                    cmd.Parameters.AddWithValue("@market_ticker", marketTicker);
                    cmd.Parameters.AddWithValue("@is_taker", msg.GetProperty("is_taker").GetBoolean());
                    cmd.Parameters.AddWithValue("@side", msg.GetProperty("side").GetString());
                    cmd.Parameters.AddWithValue("@yes_price", msg.TryGetProperty("yes_price", out var yesPrice)
                        ? yesPrice.GetInt32() : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@no_price", msg.TryGetProperty("no_price", out var noPrice)
                        ? noPrice.GetInt32() : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@count", msg.GetProperty("count").GetInt32());
                    cmd.Parameters.AddWithValue("@action", msg.GetProperty("action").GetString());
                    cmd.Parameters.AddWithValue("@ts", msg.GetProperty("ts").GetInt64());
                    cmd.Parameters.AddWithValue("@LoggedDate", DateTime.Now);
                }
            });
            return Task.CompletedTask;
        }

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

        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                Task.WaitAll(_workerTasks, TimeSpan.FromSeconds(5)); // Timeout to avoid hanging
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                _logger.LogDebug("SqlDataService worker tasks were canceled as expected during disposal.");
            }
            _cts.Dispose();
        }


        private struct DatabaseOperation
        {
            public string StoredProcedure { get; init; }
            public Action<SqlCommand> SetParameters { get; init; }
            public string Identifier { get; init; }
        }

        private async Task ProcessQueueAsync(ConcurrentQueue<DatabaseOperation> queue, string operationName, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var operation))
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
                        _logger.LogError(ex, "Failed to execute {OperationName} for {Identifier}.", operationName, operation.Identifier);
                    }
                }
                else
                {
                    await Task.Delay(10, cancellationToken); // Prevent tight loop
                }
            }
        }
        private void SetOrderBookParameters(SqlCommand cmd, JsonElement msg, int sid, long kalshiSeq,
            string marketTicker, string offerType, int price, int? delta, string side, int restingContracts)
        {
            cmd.Parameters.AddWithValue("@market_id", msg.TryGetProperty("market_id", out var mid)
                ? mid.GetString() : "00000000-0000-0000-0000-000000000000");
            cmd.Parameters.AddWithValue("@sid", sid);
            cmd.Parameters.AddWithValue("@kalshi_seq", kalshiSeq);
            cmd.Parameters.AddWithValue("@market_ticker", marketTicker);
            cmd.Parameters.AddWithValue("@offer_type", offerType);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@delta", delta ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@side", side);
            cmd.Parameters.AddWithValue("@resting_contracts", restingContracts);
            cmd.Parameters.AddWithValue("@LoggedDate", DateTime.Now);
        }

    }
}