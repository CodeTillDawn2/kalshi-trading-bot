using BacklashBot.Configuration;
using BacklashBot.Hubs;
using BacklashBot.Management;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OverseerBotShared;
using System.Diagnostics;
using System.Text.Json;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for broadcasting system status and health information to connected SignalR clients.
    /// Manages periodic check-in broadcasts at configurable intervals containing brain instance status, market data, performance metrics,
    /// and system health indicators to keep monitoring systems and dashboards updated.
    /// Includes retry logic for failed SignalR sends and performance metrics collection.
    /// </summary>
    public class BroadcastService : IBroadcastService
    {
        private readonly IHubContext<BacklashBotHub> _hubContext;
        private readonly IServiceFactory _serviceFactory;
        private readonly ILogger<IBroadcastService> _logger;
        private Task? _statusBroadcastTask;
        private readonly IStatusTrackerService _statusTracker;
        private readonly IConfiguration _configuration;
        private readonly IPerformanceMonitor _centralPerformanceMonitor;
        private readonly ICentralPerformanceMonitor _centralPerformanceMonitorInterface;

        /// <summary>
        /// The interval in seconds between broadcast operations.
        /// </summary>
        private int _broadcastIntervalSeconds = 30;

        /// <summary>
        /// Maximum number of retry attempts for failed SignalR sends.
        /// </summary>
        private int _maxRetryAttempts = 3;

        /// <summary>
        /// Delay between retry attempts for failed broadcasts.
        /// </summary>
        private TimeSpan _retryDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Counter for successful broadcast operations.
        /// </summary>
        private long _successfulBroadcasts = 0;

        /// <summary>
        /// Counter for failed broadcast operations.
        /// </summary>
        private long _failedBroadcasts = 0;

        /// <summary>
        /// Cumulative total time spent on broadcast operations in milliseconds.
        /// </summary>
        private double _totalBroadcastTime = 0;

        /// <summary>
        /// Lock object for thread-safe updates to performance metrics.
        /// </summary>
        private object _metricsLock = new object();

        /// <summary>
        /// Total size of broadcast data payloads in bytes.
        /// </summary>
        private long _totalDataSize = 0;

        /// <summary>
        /// Average broadcasts per minute.
        /// </summary>
        private double _broadcastsPerMinute = 0;

        /// <summary>
        /// Total memory used during broadcasts in bytes.
        /// </summary>
        private long _totalMemoryUsed = 0;

        /// <summary>
        /// Average CPU usage during broadcasts as percentage.
        /// </summary>
        private double _averageCpuDuringBroadcast = 0;

        /// <summary>
        /// Last broadcast timestamp for interval tracking.
        /// </summary>
        private DateTime _lastBroadcastTime = DateTime.MinValue;

        /// <summary>
        /// Total deviation from expected broadcast intervals in seconds.
        /// </summary>
        private double _totalIntervalDeviation = 0;

        /// <summary>
        /// Number of intervals measured.
        /// </summary>
        private long _intervalCount = 0;

        /// <summary>
        /// Service start time for throughput calculation.
        /// </summary>
        private DateTime _serviceStartTime;

        /// <summary>
        /// Enable all performance metrics tracking for this service.
        /// </summary>
        private bool _enablePerformanceMetrics = false;

        /// <summary>
        /// Initializes a new instance of the BroadcastService with required dependencies.
        /// </summary>
        /// <param name="hubContext">SignalR hub context for broadcasting messages to connected clients</param>
        /// <param name="serviceFactory">Factory for accessing other system services</param>
        /// <param name="statusTracker">Service for tracking system status and cancellation tokens</param>
        /// <param name="logger">Logger for recording service operations and errors</param>
        /// <param name="configuration">Configuration instance for reading settings</param>
        /// <param name="centralPerformanceMonitor">The performance monitor to send metrics to</param>
        /// <param name="centralPerformanceMonitorInterface">The central performance monitor interface</param>
        /// <param name="broadcastServiceConfig">Configuration options for broadcast service settings</param>
        public BroadcastService(
            IHubContext<BacklashBotHub> hubContext,
            IServiceFactory serviceFactory,
            IStatusTrackerService statusTracker,
            ILogger<IBroadcastService> logger,
            IConfiguration configuration,
            IPerformanceMonitor centralPerformanceMonitor,
            ICentralPerformanceMonitor centralPerformanceMonitorInterface,
            IOptions<BroadcastServiceConfig> broadcastServiceConfig)
        {
            _hubContext = hubContext;
            _statusTracker = statusTracker;
            _serviceFactory = serviceFactory;
            _logger = logger;
            _configuration = configuration;
            _centralPerformanceMonitor = centralPerformanceMonitor;
            _centralPerformanceMonitorInterface = centralPerformanceMonitorInterface;
            _serviceStartTime = DateTime.Now;

            // Configure broadcast settings from BroadcastServiceConfig
            var config = broadcastServiceConfig.Value;
            _broadcastIntervalSeconds = config.BroadcastIntervalSeconds;
            _maxRetryAttempts = config.BroadcastMaxRetryAttempts;
            _retryDelay = TimeSpan.FromSeconds(config.BroadcastRetryDelaySeconds);
            _enablePerformanceMetrics = config.EnablePerformanceMetrics;
        }

        /// <summary>
        /// Starts the broadcast service, initiating the periodic status broadcast loop.
        /// Creates a background task that broadcasts system status at configurable intervals to connected clients.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StartServicesAsync()
        {
            try
            {
                _logger.LogInformation("BroadcastService starting...");

                var cancellationToken = _statusTracker.GetCancellationToken();

                _statusBroadcastTask = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            if (BacklashBotHub.HasConnectedClients())
                            {
                                await BroadcastCheckInAsync();
                            }
                            else
                            {
                                _logger.LogDebug("No clients connected, skipping status broadcast.");
                            }
                            await Task.Delay(_broadcastIntervalSeconds * 1000, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Status broadcast task canceled.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in status broadcast cycle.");
                        }
                    }
                }, cancellationToken);

                _logger.LogInformation("BroadcastService started successfully.");
                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BroadcastService startup canceled due to cancellation request.");
                return;
            }
        }

        /// <summary>
        /// Broadcasts comprehensive system status information to all connected SignalR clients.
        /// Gathers data from various system services including market information, performance metrics,
        /// error counts, and operational status, then sends it as a CheckIn message with retry logic for failed sends.
        /// Collects performance metrics for timing and success rates.
        /// </summary>
        /// <returns>A task representing the asynchronous broadcast operation</returns>
        private async Task BroadcastCheckInAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            bool broadcastSuccessful = false;

            // Track broadcast interval adherence
            if (_enablePerformanceMetrics)
            {
                DateTime now = DateTime.Now;
                if (_lastBroadcastTime != DateTime.MinValue)
                {
                    double actualInterval = (now - _lastBroadcastTime).TotalSeconds;
                    double deviation = Math.Abs(actualInterval - _broadcastIntervalSeconds);
                    lock (_metricsLock)
                    {
                        _totalIntervalDeviation += deviation;
                        _intervalCount++;
                    }
                }
                _lastBroadcastTime = now;
            }

            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (!BacklashBotHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping status broadcast.");
                return;
            }

            _logger.LogInformation("Broadcasting system status check-in");
            try
            {
                var markets = await GetWatchedMarketsAsync();
                var errorHandler = _serviceFactory.GetBacklashErrorHandler();
                var performanceTracker = _centralPerformanceMonitorInterface;
                if (errorHandler == null || performanceTracker == null) return;

                var lastSnapshot = errorHandler.LastSuccessfulSnapshot;
                var lastErrorDate = errorHandler.LastErrorDate;

                // Get queue averages and CPU usage
                (double eventQueueAvg, double tickerQueueAvg, double notificationQueueAvg, double orderBookQueueAvg) = performanceTracker.GetQueueCountRollingAverages();
                var currentCpuUsage = performanceTracker.LastRefreshUsagePercentage;
                var webSocketService = _serviceFactory.GetWebSocketHostedService();
                var isWebSocketConnected = webSocketService?.IsConnected() ?? false;

                // Get brain instance name and status from Performance Monitor
                var isStartingUp = performanceTracker.IsStartingUp;
                var isShuttingDown = performanceTracker.IsShuttingDown;

                var brainInstanceName = performanceTracker.BrainInstance;
                _logger.LogInformation("Creating CheckInData with BrainInstanceName='{BrainInstanceName}'", brainInstanceName);

                var checkInData = new CheckInData
                {
                    BrainInstanceName = brainInstanceName,
                    Markets = markets,
                    ErrorCount = errorHandler.ErrorCount,
                    LastSnapshot = lastSnapshot == DateTime.MinValue ? (DateTime?)null : lastSnapshot
                };

                // Track data payload size
                if (_enablePerformanceMetrics)
                {
                    string json = JsonSerializer.Serialize(checkInData);
                    long dataSize = System.Text.Encoding.UTF8.GetByteCount(json);
                    lock (_metricsLock)
                    {
                        _totalDataSize += dataSize;
                    }
                }

                // Track memory usage before broadcast
                long memoryBefore = 0;
                if (_enablePerformanceMetrics)
                {
                    memoryBefore = GC.GetTotalMemory(false);
                }

                for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
                {
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("CheckIn", checkInData, cancellationToken);
                        broadcastSuccessful = true;
                        var perfMonitor = _centralPerformanceMonitorInterface;
                        _logger.LogInformation("Status broadcast completed from {BrainInstanceName} with {MarketCount} markets, ErrorCount: {ErrorCount}",
                            perfMonitor?.BrainInstance ?? "Unknown", markets.Count, errorHandler.ErrorCount);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Broadcast attempt {Attempt} failed. Exception: {Message}, Inner: {Inner}", attempt, ex.Message, ex.InnerException?.Message ?? "None");
                        if (attempt < _maxRetryAttempts)
                        {
                            await Task.Delay(_retryDelay, cancellationToken);
                        }
                        else
                        {
                            _logger.LogError(ex, "Broadcast failed after {MaxAttempts} attempts", _maxRetryAttempts);
                        }
                    }
                }

                // Track memory usage after broadcast
                if (_enablePerformanceMetrics)
                {
                    long memoryAfter = GC.GetTotalMemory(false);
                    long memoryUsed = memoryAfter - memoryBefore;
                    lock (_metricsLock)
                    {
                        _totalMemoryUsed += memoryUsed;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting system status");
            }
            finally
            {
                stopwatch.Stop();
                lock (_metricsLock)
                {
                    _totalBroadcastTime += stopwatch.Elapsed.TotalMilliseconds;
                    if (broadcastSuccessful)
                    {
                        _successfulBroadcasts++;
                    }
                    else
                    {
                        _failedBroadcasts++;
                    }
                    // Calculate throughput
                    if (_enablePerformanceMetrics)
                    {
                        _broadcastsPerMinute = (_successfulBroadcasts + _failedBroadcasts) / (DateTime.Now - _serviceStartTime).TotalMinutes;
                    }
                }

                // Post metrics to central performance monitor
                double avgTime = _successfulBroadcasts + _failedBroadcasts > 0 ? _totalBroadcastTime / (_successfulBroadcasts + _failedBroadcasts) : 0;
                double successRate = _successfulBroadcasts + _failedBroadcasts > 0 ? (_successfulBroadcasts * 100.0) / (_successfulBroadcasts + _failedBroadcasts) : 0;
                double avgDeviation = _intervalCount > 0 ? (_totalIntervalDeviation / _intervalCount) * 1000 : 0;

                RecordBroadcastMetricsPrivate(
                    _successfulBroadcasts,
                    _failedBroadcasts,
                    _totalBroadcastTime,
                    avgTime,
                    successRate,
                    _totalDataSize,
                    _broadcastsPerMinute,
                    _totalMemoryUsed,
                    avgDeviation,
                    _enablePerformanceMetrics
                );
            }
        }

        /// <summary>
        /// Retrieves the list of currently watched markets from the market data service.
        /// </summary>
        /// <returns>A list of market identifiers that are currently being watched</returns>
        private async Task<List<string>> GetWatchedMarketsAsync()
        {
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            var marketDataService = _serviceFactory.GetMarketDataService();
            if (marketDataService == null) return new List<string>();
            var markets = await marketDataService.FetchWatchedMarketsAsync();
            return markets;
        }

        /// <summary>
        /// Stops the broadcast service and cleans up resources.
        /// Waits for the background broadcast task to complete and clears connected clients.
        /// </summary>
        /// <returns>A task representing the asynchronous shutdown operation</returns>
        public async Task StopServicesAsync()
        {
            _logger.LogInformation("BroadcastService stopping...");
            try
            {
                var tasksToWait = new List<Task>();
                if (_statusBroadcastTask != null) tasksToWait.Add(_statusBroadcastTask);
                if (tasksToWait.Any())
                {
                    await Task.WhenAll(tasksToWait).ConfigureAwait(false);
                }
                BacklashBotHub.ClearConnectedClients();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BroadcastService tasks canceled as expected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping BroadcastService.");
            }
            _logger.LogInformation("BroadcastService stopped.");
        }

        /// <summary>
        /// Retrieves a configuration value from configuration, with a default fallback.
        /// </summary>
        /// <param name="key">The configuration key to retrieve</param>
        /// <param name="defaultValue">The default value if property is not found or invalid</param>
        /// <returns>The configuration value or default</returns>
        private int GetConfigValue(string key, int defaultValue)
        {
            var value = _configuration.GetValue<int>(key);
            return value > 0 ? value : defaultValue;
        }

        /// <summary>
        /// Retrieves a boolean configuration value from configuration, with a default fallback.
        /// </summary>
        /// <param name="key">The configuration key to retrieve</param>
        /// <param name="defaultValue">The default value if property is not found or invalid</param>
        /// <returns>The configuration value or default</returns>
        private bool GetConfigBoolValue(string key, bool defaultValue)
        {
            return _configuration.GetValue<bool>(key, defaultValue);
        }

        /// <summary>
        /// Gets the total number of successful broadcast operations.
        /// </summary>
        public long SuccessfulBroadcasts
        {
            get
            {
                lock (_metricsLock)
                {
                    return _successfulBroadcasts;
                }
            }
        }

        /// <summary>
        /// Gets the total number of failed broadcast operations.
        /// </summary>
        public long FailedBroadcasts
        {
            get
            {
                lock (_metricsLock)
                {
                    return _failedBroadcasts;
                }
            }
        }

        /// <summary>
        /// Gets the cumulative total time spent on broadcast operations in milliseconds.
        /// </summary>
        public double TotalBroadcastTimeMs
        {
            get
            {
                lock (_metricsLock)
                {
                    return _totalBroadcastTime;
                }
            }
        }

        /// <summary>
        /// Gets the average broadcast time per operation in milliseconds.
        /// </summary>
        public double AverageBroadcastTimeMs
        {
            get
            {
                lock (_metricsLock)
                {
                    return _successfulBroadcasts + _failedBroadcasts > 0
                        ? _totalBroadcastTime / (_successfulBroadcasts + _failedBroadcasts)
                        : 0;
                }
            }
        }

        /// <summary>
        /// Gets the broadcast success rate as a percentage (0-100).
        /// </summary>
        public double BroadcastSuccessRate
        {
            get
            {
                lock (_metricsLock)
                {
                    var total = _successfulBroadcasts + _failedBroadcasts;
                    return total > 0 ? (_successfulBroadcasts * 100.0) / total : 0;
                }
            }
        }

        /// <summary>
        /// Gets the total size of broadcast data payloads in bytes.
        /// </summary>
        public long TotalDataSize
        {
            get
            {
                lock (_metricsLock)
                {
                    return _totalDataSize;
                }
            }
        }

        /// <summary>
        /// Gets the average broadcasts per minute.
        /// </summary>
        public double BroadcastsPerMinute
        {
            get
            {
                lock (_metricsLock)
                {
                    return _broadcastsPerMinute;
                }
            }
        }

        /// <summary>
        /// Gets the total memory used during broadcasts in bytes.
        /// </summary>
        public long TotalMemoryUsed
        {
            get
            {
                lock (_metricsLock)
                {
                    return _totalMemoryUsed;
                }
            }
        }

        /// <summary>
        /// Gets the average interval deviation in milliseconds.
        /// </summary>
        public double AverageIntervalDeviationMs
        {
            get
            {
                lock (_metricsLock)
                {
                    return _intervalCount > 0 ? (_totalIntervalDeviation / _intervalCount) * 1000 : 0;
                }
            }
        }

        /// <summary>
        /// Sends comprehensive performance metrics from the CentralPerformanceMonitor to all connected clients.
        /// This method collects all available performance data and broadcasts it for monitoring and analysis.
        /// </summary>
        /// <param name="performanceMetrics">The comprehensive performance metrics data to broadcast.</param>
        /// <returns>A task representing the asynchronous broadcast operation.</returns>
        public async Task BroadcastPerformanceMetricsAsync(PerformanceMetricsData performanceMetrics)
        {
            if (!BacklashBotHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping performance metrics broadcast.");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            bool broadcastSuccessful = false;

            try
            {
                _logger.LogInformation("Broadcasting performance metrics");

                // Track data payload size
                if (_enablePerformanceMetrics)
                {
                    string json = JsonSerializer.Serialize(performanceMetrics);
                    long dataSize = System.Text.Encoding.UTF8.GetByteCount(json);
                    lock (_metricsLock)
                    {
                        _totalDataSize += dataSize;
                    }
                }

                // Track memory usage before broadcast
                long memoryBefore = 0;
                if (_enablePerformanceMetrics)
                {
                    memoryBefore = GC.GetTotalMemory(false);
                }

                var cancellationToken = _statusTracker.GetCancellationToken();

                for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
                {
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("PerformanceMetrics", performanceMetrics, cancellationToken);
                        broadcastSuccessful = true;
                        _logger.LogInformation("Performance metrics broadcast completed");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Performance metrics broadcast attempt {Attempt} failed. Exception: {Message}, Inner: {Inner}", attempt, ex.Message, ex.InnerException?.Message ?? "None");
                        if (attempt < _maxRetryAttempts)
                        {
                            await Task.Delay(_retryDelay, cancellationToken);
                        }
                        else
                        {
                            _logger.LogError(ex, "Performance metrics broadcast failed after {MaxAttempts} attempts", _maxRetryAttempts);
                        }
                    }
                }

                // Track memory usage after broadcast
                if (_enablePerformanceMetrics)
                {
                    long memoryAfter = GC.GetTotalMemory(false);
                    long memoryUsed = memoryAfter - memoryBefore;
                    lock (_metricsLock)
                    {
                        _totalMemoryUsed += memoryUsed;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting performance metrics");
            }
            finally
            {
                stopwatch.Stop();
                lock (_metricsLock)
                {
                    _totalBroadcastTime += stopwatch.Elapsed.TotalMilliseconds;
                    if (broadcastSuccessful)
                    {
                        _successfulBroadcasts++;
                    }
                    else
                    {
                        _failedBroadcasts++;
                    }
                }
            }
        }

        /// <summary>
        /// Records broadcast performance metrics using the IPerformanceMonitor interface.
        /// </summary>
        /// <param name="successfulBroadcasts">Number of successful broadcasts</param>
        /// <param name="failedBroadcasts">Number of failed broadcasts</param>
        /// <param name="totalBroadcastTime">Total time spent on broadcasts in milliseconds</param>
        /// <param name="avgTime">Average time per broadcast in milliseconds</param>
        /// <param name="successRate">Success rate as percentage</param>
        /// <param name="totalDataSize">Total data size in bytes</param>
        /// <param name="broadcastsPerMinute">Broadcasts per minute</param>
        /// <param name="totalMemoryUsed">Total memory used in bytes</param>
        /// <param name="avgDeviation">Average interval deviation in milliseconds</param>
        /// <param name="enablePerformanceMetrics">Whether performance metrics are enabled</param>
        private void RecordBroadcastMetricsPrivate(
            long successfulBroadcasts,
            long failedBroadcasts,
            double totalBroadcastTime,
            double avgTime,
            double successRate,
            long totalDataSize,
            double broadcastsPerMinute,
            long totalMemoryUsed,
            double avgDeviation,
            bool enablePerformanceMetrics)
        {
            string className = nameof(BroadcastService);
            string category = "Broadcast";

            if (!enablePerformanceMetrics)
            {
                // Send disabled metrics
                _centralPerformanceMonitor.RecordDisabledMetric(className, "SuccessfulBroadcasts", "Successful Broadcasts", "Number of successful broadcasts", successfulBroadcasts, "count", category, false);
                _centralPerformanceMonitor.RecordDisabledMetric(className, "FailedBroadcasts", "Failed Broadcasts", "Number of failed broadcasts", failedBroadcasts, "count", category, false);
                _centralPerformanceMonitor.RecordDisabledMetric(className, "TotalBroadcastTime", "Total Broadcast Time", "Cumulative time spent on broadcasts", totalBroadcastTime, "ms", category, false);
                _centralPerformanceMonitor.RecordDisabledMetric(className, "AverageBroadcastTime", "Average Broadcast Time", "Average time per broadcast", avgTime, "ms", category, false);
                _centralPerformanceMonitor.RecordDisabledMetric(className, "SuccessRate", "Broadcast Success Rate", "Percentage of successful broadcasts", successRate, "%", category, false);
                _centralPerformanceMonitor.RecordDisabledMetric(className, "TotalDataSize", "Total Data Size", "Total size of broadcast payloads", totalDataSize, "bytes", category, false);
                _centralPerformanceMonitor.RecordDisabledMetric(className, "BroadcastsPerMinute", "Broadcasts Per Minute", "Average broadcasts per minute", broadcastsPerMinute, "bpm", category, false);
                _centralPerformanceMonitor.RecordDisabledMetric(className, "TotalMemoryUsed", "Total Memory Used", "Total memory used during broadcasts", totalMemoryUsed, "bytes", category, false);
                _centralPerformanceMonitor.RecordDisabledMetric(className, "AverageIntervalDeviation", "Average Interval Deviation", "Average deviation from broadcast intervals", avgDeviation, "ms", category, false);
            }
            else
            {
                // Record actual metrics
                _centralPerformanceMonitor.RecordCounterMetric(className, "SuccessfulBroadcasts", "Successful Broadcasts", "Number of successful broadcasts", successfulBroadcasts, "count", category, true);
                _centralPerformanceMonitor.RecordCounterMetric(className, "FailedBroadcasts", "Failed Broadcasts", "Number of failed broadcasts", failedBroadcasts, "count", category, true);
                _centralPerformanceMonitor.RecordSpeedDialMetric(className, "TotalBroadcastTime", "Total Broadcast Time", "Cumulative time spent on broadcasts", totalBroadcastTime, "ms", category, null, null, null, true);
                _centralPerformanceMonitor.RecordSpeedDialMetric(className, "AverageBroadcastTime", "Average Broadcast Time", "Average time per broadcast", avgTime, "ms", category, null, null, null, true);
                _centralPerformanceMonitor.RecordProgressBarMetric(className, "SuccessRate", "Broadcast Success Rate", "Percentage of successful broadcasts", successRate, "%", category, null, null, null, true);
                _centralPerformanceMonitor.RecordCounterMetric(className, "TotalDataSize", "Total Data Size", "Total size of broadcast payloads", totalDataSize, "bytes", category, true);
                _centralPerformanceMonitor.RecordSpeedDialMetric(className, "BroadcastsPerMinute", "Broadcasts Per Minute", "Average broadcasts per minute", broadcastsPerMinute, "bpm", category, null, null, null, true);
                _centralPerformanceMonitor.RecordCounterMetric(className, "TotalMemoryUsed", "Total Memory Used", "Total memory used during broadcasts", totalMemoryUsed, "bytes", category, true);
                _centralPerformanceMonitor.RecordSpeedDialMetric(className, "AverageIntervalDeviation", "Average Interval Deviation", "Average deviation from broadcast intervals", avgDeviation, "ms", category, null, null, null, true);
            }
        }

        /// <summary>
        /// Disposes of the broadcast service resources.
        /// </summary>
        public void Dispose()
        {
            _statusBroadcastTask?.Dispose();
        }
    }
}
