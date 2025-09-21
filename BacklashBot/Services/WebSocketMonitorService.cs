using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs.KalshiAPI;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for monitoring the Kalshi exchange status and managing WebSocket connection lifecycle.
    /// This service periodically checks the exchange's operational status and automatically connects/disconnects
    /// the WebSocket client based on exchange availability and bot initialization state. It ensures reliable
    /// real-time data streaming while handling connection failures and exchange downtime gracefully.
    /// </summary>
    /// <remarks>
    /// The service implements a background monitoring loop that:
    /// - Checks exchange status every minute during normal operation
    /// - Connects WebSocket only after market data initialization is complete
    /// - Automatically disconnects when exchange becomes inactive
    /// - Handles connection errors with appropriate retry logic and backoff
    /// - Provides manual trigger capability for immediate connection checks
    /// </remarks>
    public class WebSocketMonitorService : IWebSocketMonitorService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceFactory _serviceFactory;
        private readonly ILogger<IWebSocketMonitorService> _logger;
        private readonly IConfiguration _configuration;
        private bool _isWebSocketConnected = false;
        private Task _exchangeStatusMonitorTask;
        private readonly IScopeManagerService _scopeManagerService;
        private IStatusTrackerService _statusTrackerService;
        private IBotReadyStatus _readyStatus;

        // Configurable intervals
        private readonly int _monitoringIntervalMinutes;
        private readonly int _retryDelayMinutes;

        // Configuration and dependencies
        private readonly bool _enableMetrics;
        private readonly ICentralPerformanceMonitor _centralPerformanceMonitor;

        // Performance metrics
        private int _exchangeStatusCheckCount = 0;
        private int _exchangeStatusSuccessCount = 0;
        private int _connectionAttemptCount = 0;
        private int _connectionSuccessCount = 0;
        private readonly System.Diagnostics.Stopwatch _monitoringStopwatch = new System.Diagnostics.Stopwatch();

        // Enhanced metrics for granularity
        private readonly List<long> _responseTimesMs = new List<long>();
        private long _minResponseTimeMs = long.MaxValue;
        private long _maxResponseTimeMs = 0;
        private double _averageResponseTimeMs = 0;
        private double _responseTimeStdDev = 0;

        // Additional timing metrics
        private readonly System.Diagnostics.Stopwatch _connectionStopwatch = new System.Diagnostics.Stopwatch();
        private readonly System.Diagnostics.Stopwatch _cycleStopwatch = new System.Diagnostics.Stopwatch();
        private long _totalRetryDelayMs = 0;

        // WebSocket latency and throughput
        private readonly List<long> _websocketLatenciesMs = new List<long>();
        private int _messagesProcessedCount = 0;
        private DateTime _lastThroughputReset = DateTime.UtcNow;
        private int _queueDepth = 0;

        // Reliability metrics
        private DateTime _serviceStartTime = DateTime.UtcNow;
        private TimeSpan _totalUptime = TimeSpan.Zero;
        private DateTime _lastConnectionFailure = DateTime.MinValue;
        private DateTime _lastConnectionRecovery = DateTime.MinValue;
        private readonly List<TimeSpan> _timeBetweenFailures = new List<TimeSpan>();
        private readonly List<TimeSpan> _timeToRecovery = new List<TimeSpan>();


        /// <summary>
        /// Initializes a new instance of the WebSocketMonitorService with required dependencies.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency resolution.</param>
        /// <param name="serviceFactory">Factory providing access to various bot services including WebSocket client.</param>
        /// <param name="logger">Logger for recording service operations and errors.</param>
        /// <param name="configuration">Configuration provider for customizable settings.</param>
        /// <param name="scopeManagerService">Service for managing dependency injection scopes.</param>
        /// <param name="readyStatus">Status tracker for bot initialization completion.</param>
        /// <param name="statusTrackerService">Service for managing cancellation tokens and operation status.</param>
        /// <param name="centralPerformanceMonitor">Central performance monitoring service for posting metrics.</param>
        public WebSocketMonitorService(
            IServiceScopeFactory scopeFactory,
            IServiceFactory serviceFactory,
            ILogger<IWebSocketMonitorService> logger,
            IConfiguration configuration,
            IScopeManagerService scopeManagerService,
            IBotReadyStatus readyStatus,
            IStatusTrackerService statusTrackerService,
            ICentralPerformanceMonitor centralPerformanceMonitor)
        {
            _scopeManagerService = scopeManagerService;
            _serviceFactory = serviceFactory;
            _statusTrackerService = statusTrackerService;
            _scopeFactory = scopeFactory;
            _readyStatus = readyStatus;
            _logger = logger;
            _configuration = configuration;
            _centralPerformanceMonitor = centralPerformanceMonitor;

            // Load configuration values from injected options
            _monitoringIntervalMinutes = _configuration.GetValue<int>("Websocket:WebSocketMonitor.MonitoringIntervalMinutes", 1);
            _retryDelayMinutes = _configuration.GetValue<int>("Websocket:WebSocketMonitor.RetryDelayMinutes", 5);
            _enableMetrics = _configuration.GetValue<bool>("Websocket:WebSocketMonitor.EnableWebSocketMonitorMetrics", true);

            _logger.LogDebug("WebSocketMonitorService instance created with MonitoringInterval={MonitoringInterval}min, RetryDelay={RetryDelay}min, EnableWebSocketMonitorMetrics={EnableWebSocketMonitorMetrics}",
                _monitoringIntervalMinutes, _retryDelayMinutes, _enableMetrics);
        }

        /// <summary>
        /// Starts the WebSocket monitoring services by initiating the background exchange status monitoring task.
        /// This method begins the continuous monitoring loop that checks exchange status and manages WebSocket connections.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the monitoring operation if needed.</param>
        /// <remarks>
        /// The monitoring task runs in the background and will continue until cancelled or an error occurs.
        /// The task checks exchange status every minute and manages WebSocket connection state accordingly.
        /// </remarks>
        public void StartServices(CancellationToken cancellationToken)
        {
            _logger.LogDebug("WebSocketMonitorService starting...");

            try
            {
                _logger.LogDebug("Initial cache state: InitializationCompleted.IsCompleted={IsCompleted}", _readyStatus.InitializationCompleted.Task.IsCompleted);

                _logger.LogDebug("Starting exchange status monitoring...");
                _exchangeStatusMonitorTask = Task.Run(async () =>
                {
                    await MonitorAndManageWebSocketConnectionAsync();
                }, _statusTrackerService.GetCancellationToken());
                _logger.LogDebug("Started exchange status monitoring");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting WebSocketMonitorService");
                throw;
            }
            _logger.LogDebug("WebSocketMonitorService started.");
        }
        /// <summary>
        /// Stops the WebSocket monitoring services gracefully by closing any active WebSocket connection
        /// and waiting for the monitoring task to complete.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the shutdown operation if needed.</param>
        /// <returns>A task representing the asynchronous shutdown operation.</returns>
        /// <remarks>
        /// This method ensures clean shutdown by:
        /// - Closing the WebSocket connection if currently connected
        /// - Waiting for the monitoring task to complete or be cancelled
        /// - Handling any errors during shutdown gracefully
        /// </remarks>
        public async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WebSocketMonitorService.StopAsync called at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            try
            {
                if (_isWebSocketConnected)
                {
                    try
                    {
                        await _serviceFactory.GetKalshiWebSocketClient().ShutdownAsync();
                        _isWebSocketConnected = false;
                        _logger.LogInformation("WebSocket connection closed.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error closing WebSocket connection during shutdown");
                    }
                }
                if (_exchangeStatusMonitorTask != null && !_exchangeStatusMonitorTask.IsCompleted)
                {
                    _logger.LogDebug("Waiting for exchange status monitoring task to complete...");
                    try
                    {
                        await _exchangeStatusMonitorTask.ConfigureAwait(false);
                        _logger.LogInformation("Exchange status monitoring task completed.");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Exchange status monitoring task canceled as expected.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error waiting for exchange status monitoring task to complete");
                    }
                }

                // Log final metrics
                var (checks, successes, attempts, connSuccesses) = GetMetrics();
                _logger.LogInformation("Final monitoring metrics: ExchangeStatusChecks={Checks}, SuccessRate={Rate:P2}, ConnectionAttempts={Attempts}, SuccessRate={ConnRate:P2}",
                    checks,
                    checks > 0 ? (double)successes / checks : 0,
                    attempts,
                    attempts > 0 ? (double)connSuccesses / attempts : 0);

                var extended = GetExtendedMetrics();
                _logger.LogInformation("Extended final metrics: AvgResponseTime={Avg:F2}ms, StdDev={StdDev:F2}ms, Min={Min}ms, Max={Max}ms, TotalRetryDelay={Delay}ms, Uptime={Uptime:F2}%, MTBF={MTBF:F2}min, MTTR={MTTR:F2}min, MessagesProcessed={Msgs}, QueueDepth={Queue}",
                    extended.AverageResponseTimeMs,
                    extended.ResponseTimeStdDev,
                    extended.MinResponseTimeMs,
                    extended.MaxResponseTimeMs,
                    extended.TotalRetryDelayMs,
                    extended.UptimePercentage,
                    extended.MTBFMinutes,
                    extended.MTTRMinutes,
                    extended.MessagesProcessedCount,
                    extended.QueueDepth);
            }
            finally
            {
                _logger.LogDebug("WebSocketMonitorService.StopAsync completed at {0}", DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Triggers an immediate WebSocket connection check outside the normal monitoring cycle.
        /// This method performs a one-time check of exchange status and updates connection state accordingly.
        /// </summary>
        /// <returns>A task representing the asynchronous connection check operation.</returns>
        /// <remarks>
        /// This is useful for manual intervention or when an immediate status update is required.
        /// The check is performed synchronously without waiting for the next scheduled monitoring interval.
        /// </remarks>
        public async Task TriggerConnectionCheckAsync()
        {
            _logger.LogDebug("Triggering immediate WebSocket connection check");
            await MonitorAndManageWebSocketConnectionAsync(immediate: true);
        }

        /// <summary>
        /// Gets the current performance metrics for monitoring operations.
        /// </summary>
        /// <returns>A tuple containing exchange status check count, success count, connection attempt count, and success count.</returns>
        public (int ExchangeStatusChecks, int ExchangeStatusSuccesses, int ConnectionAttempts, int ConnectionSuccesses) GetMetrics()
        {
            return (_exchangeStatusCheckCount, _exchangeStatusSuccessCount, _connectionAttemptCount, _connectionSuccessCount);
        }

        /// <summary>
        /// Gets extended performance metrics including timing, reliability, and throughput data.
        /// Only returns meaningful data when metrics collection is enabled.
        /// </summary>
        /// <returns>A tuple containing extended metrics: average response time, standard deviation, min/max times, total retry delay, uptime percentage, MTBF, MTTR, messages processed, and queue depth.</returns>
        public (double AverageResponseTimeMs, double ResponseTimeStdDev, long MinResponseTimeMs, long MaxResponseTimeMs, long TotalRetryDelayMs, double UptimePercentage, double MTBFMinutes, double MTTRMinutes, int MessagesProcessedCount, int QueueDepth) GetExtendedMetrics()
        {
            return (
                _averageResponseTimeMs,
                _responseTimeStdDev,
                _minResponseTimeMs,
                _maxResponseTimeMs,
                _totalRetryDelayMs,
                GetUptimePercentage(),
                GetMTBF(),
                GetMTTR(),
                _messagesProcessedCount,
                _queueDepth
            );
        }

        /// <summary>
        /// Updates response time statistics after adding a new measurement.
        /// </summary>
        /// <param name="responseTimeMs">The new response time in milliseconds.</param>
        private void UpdateResponseTimeStats(long responseTimeMs)
        {
            if (_enableMetrics)
            {
                _responseTimesMs.Add(responseTimeMs);
                _minResponseTimeMs = Math.Min(_minResponseTimeMs, responseTimeMs);
                _maxResponseTimeMs = Math.Max(_maxResponseTimeMs, responseTimeMs);

                // Calculate average
                _averageResponseTimeMs = _responseTimesMs.Average();

                // Calculate standard deviation
                if (_responseTimesMs.Count > 1)
                {
                    double sumOfSquares = _responseTimesMs.Sum(rt => Math.Pow(rt - _averageResponseTimeMs, 2));
                    _responseTimeStdDev = Math.Sqrt(sumOfSquares / (_responseTimesMs.Count - 1));
                }

                // Post to central performance monitor
                _centralPerformanceMonitor.RecordExecutionTime("WebSocketMonitor.ExchangeStatusCheck", responseTimeMs, _enableMetrics);
            }
        }

        /// <summary>
        /// Records a connection failure for MTBF/MTTR calculations.
        /// </summary>
        private void RecordConnectionFailure()
        {
            if (_enableMetrics)
            {
                var now = DateTime.UtcNow;
                if (_lastConnectionFailure != DateTime.MinValue && _lastConnectionRecovery != DateTime.MinValue)
                {
                    _timeBetweenFailures.Add(now - _lastConnectionRecovery);
                }
                _lastConnectionFailure = now;
            }
        }

        /// <summary>
        /// Records a connection recovery for MTBF/MTTR calculations.
        /// </summary>
        private void RecordConnectionRecovery()
        {
            if (_enableMetrics)
            {
                var now = DateTime.UtcNow;
                if (_lastConnectionFailure != DateTime.MinValue)
                {
                    _timeToRecovery.Add(now - _lastConnectionFailure);
                }
                _lastConnectionRecovery = now;
            }
        }

        /// <summary>
        /// Updates uptime tracking.
        /// </summary>
        /// <param name="isConnected">Whether the WebSocket is currently connected.</param>
        private void UpdateUptime(bool isConnected)
        {
            if (_enableMetrics)
            {
                var now = DateTime.UtcNow;
                if (isConnected)
                {
                    _totalUptime += (now - _serviceStartTime);
                }
                _serviceStartTime = now;
            }
        }

        /// <summary>
        /// Records WebSocket message latency for performance tracking.
        /// Only records when metrics collection is enabled.
        /// </summary>
        /// <param name="latencyMs">The message latency in milliseconds.</param>
        public void RecordWebSocketLatency(long latencyMs)
        {
            if (_enableMetrics)
            {
                _websocketLatenciesMs.Add(latencyMs);
                _centralPerformanceMonitor.RecordExecutionTime("WebSocketMonitor.WebSocketLatency", latencyMs, _enableMetrics);
            }
        }

        /// <summary>
        /// Records a processed WebSocket message for throughput tracking.
        /// Only records when metrics collection is enabled.
        /// </summary>
        public void RecordMessageProcessed()
        {
            if (_enableMetrics)
            {
                _messagesProcessedCount++;
                // Reset throughput counter every minute
                if ((DateTime.UtcNow - _lastThroughputReset).TotalMinutes >= 1)
                {
                    _messagesProcessedCount = 1;
                    _lastThroughputReset = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Sets the current WebSocket message queue depth for monitoring.
        /// Only records when metrics collection is enabled.
        /// </summary>
        /// <param name="depth">The current queue depth.</param>
        public void SetQueueDepth(int depth)
        {
            if (_enableMetrics)
            {
                _queueDepth = depth;
            }
        }

        /// <summary>
        /// Calculates Mean Time Between Failures (MTBF) in minutes.
        /// </summary>
        /// <returns>MTBF in minutes.</returns>
        private double GetMTBF()
        {
            if (_timeBetweenFailures.Count == 0) return 0;
            return _timeBetweenFailures.Average(ts => ts.TotalMinutes);
        }

        /// <summary>
        /// Calculates Mean Time To Recovery (MTTR) in minutes.
        /// </summary>
        /// <returns>MTTR in minutes.</returns>
        private double GetMTTR()
        {
            if (_timeToRecovery.Count == 0) return 0;
            return _timeToRecovery.Average(ts => ts.TotalMinutes);
        }

        /// <summary>
        /// Calculates the WebSocket uptime percentage.
        /// </summary>
        /// <returns>Uptime percentage.</returns>
        private double GetUptimePercentage()
        {
            var totalTime = DateTime.UtcNow - _serviceStartTime;
            if (totalTime.TotalMilliseconds == 0) return 0;
            return (_totalUptime.TotalMilliseconds / totalTime.TotalMilliseconds) * 100;
        }


        /// <summary>
        /// Gets the current WebSocket connection status.
        /// </summary>
        /// <returns>True if the WebSocket is currently connected, false otherwise.</returns>
        /// <remarks>
        /// This method checks the actual WebSocket connection state rather than relying on internal flags.
        /// If there's a discrepancy between the monitor's flag and actual connection, it logs a warning.
        /// </remarks>
        public bool IsConnected()
        {
            try
            {
                // Check actual WebSocket connection state
                var kalshiWebSocketClient = _serviceFactory.GetKalshiWebSocketClient();
                if (kalshiWebSocketClient != null)
                {
                    bool actualConnectionState = kalshiWebSocketClient.IsConnected();

                    // Log discrepancy if monitor flag doesn't match actual state
                    if (_isWebSocketConnected != actualConnectionState)
                    {
                        _logger.LogWarning("WebSocket connection state discrepancy detected: MonitorFlag={MonitorFlag}, ActualState={ActualState}",
                            _isWebSocketConnected, actualConnectionState);

                        // Update monitor flag to match actual state
                        _isWebSocketConnected = actualConnectionState;
                    }

                    return actualConnectionState;
                }
                else
                {
                    _logger.LogWarning("KalshiWebSocketClient is null, falling back to monitor flag");
                    return _isWebSocketConnected;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking WebSocket connection state, falling back to monitor flag");
                return _isWebSocketConnected;
            }
        }

        /// <summary>
        /// Core monitoring method that checks exchange status and manages WebSocket connection state.
        /// This method runs continuously (or once if immediate) to monitor exchange availability and
        /// connect/disconnect the WebSocket client as appropriate.
        /// </summary>
        /// <param name="immediate">If true, performs a single check and returns. If false, runs continuous monitoring loop.</param>
        /// <returns>A task representing the asynchronous monitoring operation.</returns>
        /// <remarks>
        /// The monitoring logic:
        /// - Checks exchange status via API call
        /// - Updates data cache with current exchange and trading status
        /// - Connects WebSocket when exchange is active and initialization is complete
        /// - Disconnects WebSocket when exchange becomes inactive
        /// - Handles errors with appropriate retry delays
        /// - Respects cancellation tokens for graceful shutdown
        /// </remarks>
        private async Task MonitorAndManageWebSocketConnectionAsync(bool immediate = false)
        {
            _logger.LogDebug("MonitorAndManageWebSocketConnectionAsync started at {0}, Immediate={Immediate}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, immediate, _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            try
            {
                if (!immediate)
                {
                    while (!_statusTrackerService.GetCancellationToken().IsCancellationRequested)
                    {
                        try
                        {
                            _logger.LogDebug("Checking exchange status, Current _isWebSocketConnected: {IsConnected}", _isWebSocketConnected);

                            // Start timing for exchange status check and cycle
                            _monitoringStopwatch.Restart();
                            _cycleStopwatch.Restart();
                            if (_enableMetrics) _exchangeStatusCheckCount++;

                            using var scope = _scopeFactory.CreateScope();
                            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                            ExchangeStatus status = await apiService.GetExchangeStatusAsync();

                            // Record successful check
                            if (_enableMetrics) _exchangeStatusSuccessCount++;
                            var checkDuration = _monitoringStopwatch.ElapsedMilliseconds;
                            _monitoringStopwatch.Stop();
                            if (_enableMetrics) UpdateResponseTimeStats(checkDuration);

                            _serviceFactory.GetDataCache().ExchangeStatus = status.exchange_active;
                            _serviceFactory.GetDataCache().TradingStatus = status.trading_active;
                            _logger.LogDebug("Updated DataCache.ExchangeStatus to {Status} and TradingStatus to {tradingStatus}", _serviceFactory.GetDataCache().ExchangeStatus, _serviceFactory.GetDataCache().TradingStatus);
                            _logger.LogDebug("Exchange status check completed in {Duration}ms, Cycle time: {CycleTime}ms", checkDuration, _cycleStopwatch.ElapsedMilliseconds);

                            if (status.exchange_active && !_isWebSocketConnected)
                            {
                                // Only connect if market data initialization is complete
                                if (_readyStatus.InitializationCompleted.Task.IsCompleted && _readyStatus.InitializationCompleted.Task.Result)
                                {
                                    _logger.LogInformation("Exchange is active and initialization complete, connecting WebSocket");
                                    if (_enableMetrics) _connectionAttemptCount++;
                                    _connectionStopwatch.Restart();
                                    await _serviceFactory.GetKalshiWebSocketClient().ConnectAsync();
                                    long connectionDuration = _connectionStopwatch.ElapsedMilliseconds;
                                    _isWebSocketConnected = true;
                                    if (_enableMetrics)
                                    {
                                        _connectionSuccessCount++;
                                        RecordConnectionRecovery();
                                        UpdateUptime(true);
                                        _centralPerformanceMonitor.RecordExecutionTime("WebSocketMonitor.WebSocketConnection", connectionDuration, _enableMetrics);
                                    }
                                    _logger.LogDebug("WebSocket connected successfully in {Duration}ms", connectionDuration);
                                }
                                else
                                {
                                    _logger.LogDebug("Exchange is active but initialization not complete yet, waiting for MarketDataInitializer to finish");
                                }
                            }
                            else if (!status.exchange_active && _isWebSocketConnected)
                            {
                                _logger.LogWarning("Exchange is inactive, resetting WebSocket connection");
                                _serviceFactory.GetDataCache().LastWebSocketTimestamp = DateTime.UtcNow;
                                await _serviceFactory.GetKalshiWebSocketClient().ResetConnectionAsync();
                                _isWebSocketConnected = false;
                                if (_enableMetrics)
                                {
                                    RecordConnectionFailure();
                                    UpdateUptime(false);
                                }
                                _logger.LogWarning("WebSocket connection reset due to inactive exchange, will try again in {Interval} minutes", _monitoringIntervalMinutes);
                            }

                            await Task.Delay(TimeSpan.FromMinutes(_monitoringIntervalMinutes), _statusTrackerService.GetCancellationToken()).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogDebug("Exchange status monitoring cancelled at {0}", DateTime.UtcNow);
                            return;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in exchange status monitoring");
                            _isWebSocketConnected = false;
                            _serviceFactory.GetDataCache().ExchangeStatus = false;
                            _serviceFactory.GetDataCache().TradingStatus = false;
                            if (_enableMetrics)
                            {
                                RecordConnectionFailure();
                                UpdateUptime(false);
                            }

                            // Log current metrics
                            _logger.LogInformation("Monitoring metrics: ExchangeStatusChecks={Checks}, SuccessRate={Rate:P2}, ConnectionAttempts={Attempts}, SuccessRate={ConnRate:P2}",
                                _exchangeStatusCheckCount,
                                _exchangeStatusCheckCount > 0 ? (double)_exchangeStatusSuccessCount / _exchangeStatusCheckCount : 0,
                                _connectionAttemptCount,
                                _connectionAttemptCount > 0 ? (double)_connectionSuccessCount / _connectionAttemptCount : 0);

                            var extended = GetExtendedMetrics();
                            _logger.LogInformation("Extended metrics: AvgResponseTime={Avg:F2}ms, StdDev={StdDev:F2}ms, Min={Min}ms, Max={Max}ms, TotalRetryDelay={Delay}ms, Uptime={Uptime:F2}%, MTBF={MTBF:F2}min, MTTR={MTTR:F2}min, MessagesProcessed={Msgs}, QueueDepth={Queue}",
                                extended.AverageResponseTimeMs,
                                extended.ResponseTimeStdDev,
                                extended.MinResponseTimeMs,
                                extended.MaxResponseTimeMs,
                                extended.TotalRetryDelayMs,
                                extended.UptimePercentage,
                                extended.MTBFMinutes,
                                extended.MTTRMinutes,
                                extended.MessagesProcessedCount,
                                extended.QueueDepth);

                            if (_enableMetrics) _totalRetryDelayMs += (long)TimeSpan.FromMinutes(_retryDelayMinutes).TotalMilliseconds;
                            await Task.Delay(TimeSpan.FromMinutes(_retryDelayMinutes), _statusTrackerService.GetCancellationToken()).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    try
                    {
                        _logger.LogDebug("Performing immediate exchange status check");

                        // Start timing for exchange status check and cycle
                        _monitoringStopwatch.Restart();
                        _cycleStopwatch.Restart();
                        if (_enableMetrics) _exchangeStatusCheckCount++;

                        using var scope = _scopeFactory.CreateScope();
                        var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                        ExchangeStatus status = await apiService.GetExchangeStatusAsync();

                        // Record successful check
                        if (_enableMetrics) _exchangeStatusSuccessCount++;
                        var checkDuration = _monitoringStopwatch.ElapsedMilliseconds;
                        _monitoringStopwatch.Stop();
                        if (_enableMetrics) UpdateResponseTimeStats(checkDuration);

                        _serviceFactory.GetDataCache().ExchangeStatus = status.exchange_active;
                        _serviceFactory.GetDataCache().TradingStatus = status.trading_active;
                        _logger.LogDebug("Updated DataCache.ExchangeStatus to {Status} and TradingStatus to {tradingStatus}", _serviceFactory.GetDataCache().ExchangeStatus, _serviceFactory.GetDataCache().TradingStatus);
                        _logger.LogDebug("Exchange status check completed in {Duration}ms, Cycle time: {CycleTime}ms", checkDuration, _cycleStopwatch.ElapsedMilliseconds);

                        if (status.exchange_active && !_isWebSocketConnected)
                        {
                            // Only connect if market data initialization is complete
                            if (_readyStatus.InitializationCompleted.Task.IsCompleted && _readyStatus.InitializationCompleted.Task.Result)
                            {
                                _logger.LogInformation("Exchange is active and initialization complete, connecting WebSocket");
                                if (_enableMetrics) _connectionAttemptCount++;
                                _connectionStopwatch.Restart();
                                await _serviceFactory.GetKalshiWebSocketClient().ConnectAsync(0);
                                long connectionDuration = _connectionStopwatch.ElapsedMilliseconds;
                                _isWebSocketConnected = true;
                                if (_enableMetrics)
                                {
                                    _connectionSuccessCount++;
                                    RecordConnectionRecovery();
                                    UpdateUptime(true);
                                    _centralPerformanceMonitor.RecordExecutionTime("WebSocketMonitor.WebSocketConnection", connectionDuration, _enableMetrics);
                                }
                                _logger.LogInformation("WebSocket connected successfully in {Duration}ms", connectionDuration);
                            }
                            else
                            {
                                _logger.LogDebug("Exchange is active but initialization not complete yet, waiting for MarketDataInitializer to finish");
                            }
                        }
                        else if (!status.exchange_active && _isWebSocketConnected)
                        {
                            _logger.LogWarning("Exchange is inactive, resetting WebSocket connection");
                            await _serviceFactory.GetKalshiWebSocketClient().ResetConnectionAsync();
                            _isWebSocketConnected = false;
                            if (_enableMetrics)
                            {
                                RecordConnectionFailure();
                                UpdateUptime(false);
                            }
                            _logger.LogInformation("WebSocket connection reset due to inactive exchange");
                        }
                        else
                        {
                            _logger.LogDebug("Exchange status unchanged: Active={ExchangeActive}, Connected={IsConnected}", status.exchange_active, _isWebSocketConnected);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Immediate exchange status check cancelled at {0}", DateTime.UtcNow);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in immediate exchange status check");
                        _isWebSocketConnected = false;
                        _serviceFactory.GetDataCache().ExchangeStatus = false;
                        _serviceFactory.GetDataCache().TradingStatus = false;
                        if (_enableMetrics)
                        {
                            RecordConnectionFailure();
                            UpdateUptime(false);
                        }

                        // Log current metrics
                        _logger.LogInformation("Immediate check metrics: ExchangeStatusChecks={Checks}, SuccessRate={Rate:P2}, ConnectionAttempts={Attempts}, SuccessRate={ConnRate:P2}",
                            _exchangeStatusCheckCount,
                            _exchangeStatusCheckCount > 0 ? (double)_exchangeStatusSuccessCount / _exchangeStatusCheckCount : 0,
                            _connectionAttemptCount,
                            _connectionAttemptCount > 0 ? (double)_connectionSuccessCount / _connectionAttemptCount : 0);

                        var extended = GetExtendedMetrics();
                        _logger.LogInformation("Extended immediate metrics: AvgResponseTime={Avg:F2}ms, StdDev={StdDev:F2}ms, Min={Min}ms, Max={Max}ms, TotalRetryDelay={Delay}ms, Uptime={Uptime:F2}%, MTBF={MTBF:F2}min, MTTR={MTTR:F2}min, MessagesProcessed={Msgs}, QueueDepth={Queue}",
                            extended.AverageResponseTimeMs,
                            extended.ResponseTimeStdDev,
                            extended.MinResponseTimeMs,
                            extended.MaxResponseTimeMs,
                            extended.TotalRetryDelayMs,
                            extended.UptimePercentage,
                            extended.MTBFMinutes,
                            extended.MTTRMinutes,
                            extended.MessagesProcessedCount,
                            extended.QueueDepth);
                    }
                }
            }
            finally
            {
                _logger.LogDebug("MonitorAndManageWebSocketConnectionAsync completed at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            }
        }
    }
}
