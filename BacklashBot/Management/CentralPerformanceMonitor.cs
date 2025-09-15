// CentralPerformanceMonitor.cs
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using BacklashDTOs.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashInterfaces.PerformanceMetrics;
using BacklashDTOs.Data;
using BacklashBot.State.Interfaces;
using System.Collections.Concurrent;
using TradingStrategies.Configuration;

namespace BacklashBot.Management
{
    /// <summary>
    /// Central performance monitoring service that tracks system performance metrics,
    /// records execution times, and monitors queue depths for the Kalshi trading bot.
    /// This class provides comprehensive performance analytics including WebSocket event
    /// rates, API execution times, and queue utilization metrics with configurable alerting.
    /// </summary>
    /// <remarks>
    /// The performance monitor serves multiple purposes:
    /// - Tracks API method execution times for performance analysis
    /// - Monitors queue depths for event, ticker, and notification queues
    /// - Calculates WebSocket event averages per market
    /// - Provides rolling averages for queue utilization over 5-minute windows
    /// - Integrates with market refresh service metrics
    /// - Supports performance-based decision making for market management
    /// - Monitors performance thresholds and generates alerts when exceeded
    ///
    /// Configurable alerting thresholds include:
    /// - Queue high count percentage alerts
    /// - Refresh usage percentage alerts
    /// - Absolute queue count threshold alerts
    ///
    /// Key metrics tracked:
    /// - Method execution times with timestamps
    /// - Queue counts for various system queues
    /// - WebSocket event counts per market
    /// - Market refresh cycle performance
    /// - System startup and shutdown states
    /// </remarks>
    public class CentralPerformanceMonitor : ICentralPerformanceMonitor, IKalshiBotContextPerformanceMetrics
    {
        private readonly ILogger<ICentralPerformanceMonitor> _logger;
        private readonly IServiceFactory _serviceFactory;
        public readonly ConcurrentDictionary<string, List<(DateTime Timestamp, long Milliseconds)>> ApiExecutionTimes;
        private readonly TradingConfig _tradingConfig;
        private readonly ExecutionConfig _executionConfig;
        private readonly IServiceScopeFactory _scopeFactory;
        private IOrderBookService _orderbookService => _serviceFactory.GetOrderBookService();
        public string? BrainInstance { get; private set; }
        public TimeSpan RefreshInterval { get; private set; }
        public IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)>? DatabaseMetrics => _databaseMetrics;
        public IReadOnlyDictionary<string, object>? OverseerClientServiceMetrics => _overseerClientServiceMetrics;

        private bool _timerRunning = false;
        private readonly ConcurrentDictionary<string, List<(DateTime Timestamp, int Count)>> _queueCountSamples;
        private readonly ConcurrentDictionary<string, List<(DateTime Timestamp, double AvgTime, int TotalOps)>> _orderBookServiceMetrics;

        public double LastRefreshCycleSeconds { get; set; }
        public double LastRefreshCycleInterval { get; set; }
        public int LastRefreshMarketCount { get; set; }
        public double LastRefreshUsagePercentage { get; set; }
        public bool LastRefreshTimeAcceptable { get; set; }
        public DateTime? LastPerformanceSampleDate { get; set; }
        private readonly IScopeManagerService _scopeManagerService;
        private IStatusTrackerService _statusTrackerService;
        private IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)>? _databaseMetrics;
        private IReadOnlyDictionary<string, object>? _overseerClientServiceMetrics;

        public bool IsStartingUp { get; set; } = false;
        public bool IsShuttingDown { get; set; } = false;

        /// <summary>
        /// Gets or sets whether WebSocketConnectionManager performance metrics are being recorded.
        /// This flag indicates if the WebSocketConnectionManager is actively collecting and posting metrics.
        /// </summary>
        public bool WebSocketConnectionManagerMetricsRecording { get; private set; } = false;

        /// <summary>
        /// Updates the WebSocket metrics recording status and logs the change.
        /// </summary>
        /// <param name="isRecording">True if WebSocket metrics are being recorded, false otherwise.</param>
        /// <remarks>
        /// This method should be called by WebSocketConnectionManager when metrics collection starts or stops.
        /// It provides visibility into the WebSocket performance monitoring state.
        /// </remarks>
        public void UpdateWebSocketMetricsRecordingStatus(bool isRecording)
        {
            if (WebSocketConnectionManagerMetricsRecording != isRecording)
            {
                WebSocketConnectionManagerMetricsRecording = isRecording;
                _logger.LogInformation("WebSocket metrics recording status changed: {Status}",
                    isRecording ? "ENABLED" : "DISABLED");
            }
        }

        /// <summary>
        /// Initializes a new instance of the CentralPerformanceMonitor class.
        /// </summary>
        /// <param name="logger">Logger instance for recording performance monitoring operations.</param>
        /// <param name="serviceFactory">Factory for accessing system services and data caches.</param>
        /// <param name="executionConfig">Configuration settings for execution parameters.</param>
        /// <param name="tradingConfig">Configuration settings for trading parameters.</param>
        /// <param name="scopeFactory">Factory for creating service scopes.</param>
        /// <param name="scopeManagerService">Service for managing database operation scopes.</param>
        /// <param name="statusTrackerService">Service for tracking system status and cancellation tokens.</param>
        public CentralPerformanceMonitor(
            ILogger<ICentralPerformanceMonitor> logger,
            IServiceFactory serviceFactory,
            IOptions<ExecutionConfig> executionConfig,
            IOptions<TradingConfig> tradingConfig,
            IServiceScopeFactory scopeFactory,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService)
        {
            _logger = logger;
            _scopeManagerService = scopeManagerService;
            _serviceFactory = serviceFactory;
            _statusTrackerService = statusTrackerService;
            _scopeFactory = scopeFactory;
            ApiExecutionTimes = new ConcurrentDictionary<string, List<(DateTime Timestamp, long Milliseconds)>>();
            _executionConfig = executionConfig.Value;
            _tradingConfig = tradingConfig.Value;
            RefreshInterval = TimeSpan.FromMinutes(_tradingConfig.RefreshIntervalMinutes);
            BrainInstance = _executionConfig.BrainInstance;
            _logger.LogInformation("PERFMON: Initialized with BrainInstance='{BrainInstance}' from config", BrainInstance);
            _queueCountSamples = new ConcurrentDictionary<string, List<(DateTime Timestamp, int Count)>>();
            _orderBookServiceMetrics = new ConcurrentDictionary<string, List<(DateTime Timestamp, double AvgTime, int TotalOps)>>();
            _databaseMetrics = null;
        }

        /// <summary>
        /// Calculates the average WebSocket events received per minute for a specific market.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to analyze.</param>
        /// <returns>The average events per minute, rounded to 2 decimal places. Returns 0.0 if data is unavailable or invalid.</returns>
        /// <remarks>
        /// This method calculates the average by:
        /// 1. Getting the total WebSocket events (sum of three event types) since market open
        /// 2. Calculating elapsed time in minutes since market open
        /// 3. Computing events per minute, handling edge cases for invalid time spans
        ///
        /// Used for performance monitoring and market activity assessment.
        /// </remarks>
        public double CalculateAverageWebsocketEventsReceived(string marketTicker)
        {
            var dataCache = _serviceFactory.GetDataCache();
            if (dataCache == null || !dataCache.Markets.ContainsKey(marketTicker)) return 0.0;

            // Retrieve the last market open time from the data cache
            DateTime lastMarketOpenTime = dataCache.Markets[marketTicker].ChangeTracker.LastMarketOpenTime;

            // Retrieve the tuple of three integers representing WebSocket event counts
            var webSocketClient = _serviceFactory.GetKalshiWebSocketClient();
            if (webSocketClient == null) return 0.0;
            (int event1, int event2, int event3) events = webSocketClient.GetEventCountsByMarket(marketTicker);

            // Calculate the timespan in minutes from last market open time to current time
            TimeSpan timeSpan = DateTime.UtcNow - lastMarketOpenTime;
            double minutesElapsed = timeSpan.TotalMinutes;

            // Handle edge cases where the timespan is zero or negative to avoid division by zero or invalid averages
            if (minutesElapsed <= 0)
            {
                return 0.0; // Return 0 if no valid timespan exists (e.g., current time is before or equal to last market open time)
            }

            // Calculate the sum of the three event counts
            int totalEvents = events.event1 + events.event2 + events.event3;

            // Calculate the average events per minute
            double averageEventsPerMinute = totalEvents / minutesElapsed;

            return Math.Round(averageEventsPerMinute, 2);
        }

        /// <summary>
        /// Records the execution time for a specific API method or operation.
        /// </summary>
        /// <param name="methodName">The name of the method or operation being timed.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        /// <remarks>
        /// Execution times are stored in a thread-safe concurrent dictionary with timestamps.
        /// This data can be used for performance analysis, bottleneck identification,
        /// and optimization efforts. Multiple executions of the same method are accumulated
        /// in a list for statistical analysis.
        /// </remarks>
        public void RecordExecutionTime(string methodName, long milliseconds)
        {
            var record = (Timestamp: DateTime.UtcNow, Milliseconds: milliseconds);
            ApiExecutionTimes.AddOrUpdate(
                methodName,
                _ => new List<(DateTime Timestamp, long Milliseconds)> { record },
                (_, list) => { list.Add(record); return list; });
        }

        /// <summary>
        /// Starts the performance monitoring timer that periodically collects system metrics.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method initiates a background timer that:
        /// - Runs every second to poll queue counts
        /// - Every minute, collects market refresh service performance metrics
        /// - Updates rolling averages for queue utilization
        /// - Monitors system performance indicators
        ///
        /// The timer continues until the cancellation token is triggered.
        /// Multiple calls to StartTimer() are ignored if already running.
        /// </remarks>
        public async Task StartTimer()
        {
            if (_timerRunning) return;
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            BrainInstanceDTO? dto = await context.GetBrainInstanceByName(BrainInstance ?? "");

            var marketRefreshService = _serviceFactory.GetMarketRefreshService();
            var kalshiWebSocketClient = _serviceFactory.GetKalshiWebSocketClient();
            var broadcastService = _serviceFactory.GetBroadcastService();
            string marketServiceName = nameof(IMarketRefreshService);
            TimeSpan frontEndRefreshInterval = TimeSpan.FromSeconds(1); // 1 second for queue polling
            TimeSpan marketRefreshInterval = TimeSpan.FromMinutes(1); // 1 minute for market refresh
            int marketCheckCounter = 0;

            _timerRunning = true;

            _ = Task.Run(async () =>
            {
                while (!(_statusTrackerService.GetCancellationToken().IsCancellationRequested))
                {
                    try
                    {
                        // Process MarketRefreshService metrics every minute
                        if (marketCheckCounter >= 60) // 60 seconds = 1 minute
                        {
                            if (marketRefreshService.IsRunning())
                            {
                                LastRefreshCycleSeconds = marketRefreshService.LastWorkDuration.TotalSeconds;
                                LastRefreshCycleInterval = RefreshInterval.TotalSeconds;
                                LastRefreshMarketCount = marketRefreshService.LastWorkMarketCount;
                                LastRefreshUsagePercentage = LastRefreshCycleInterval > 0 ? (LastRefreshCycleSeconds / LastRefreshCycleInterval) * 100 : 0;
                                LastRefreshTimeAcceptable = dto != null && LastRefreshUsagePercentage >= dto.UsageMin && LastRefreshUsagePercentage <= dto.UsageMax;
                                LastPerformanceSampleDate = DateTime.UtcNow;

                                _logger.LogInformation(
                                    "{Service} processing time: Elapsed={ElapsedSeconds:F2}s, Markets={MarketCount}, Usage={UsagePercentage:F2}%, Acceptable={IsAcceptable}",
                                    marketServiceName, LastRefreshCycleSeconds, LastRefreshMarketCount, LastRefreshUsagePercentage, LastRefreshTimeAcceptable);

                                // Check for performance alerts
                                CheckPerformanceAlerts();
                            }
                            marketCheckCounter = 0;
                        }

                        // Poll OrderBookService queue counts every second
                        var (eventQueueCount, tickerQueueCount, notificationQueueCount) = _orderbookService?.GetQueueCounts() ?? (0, 0, 0);
                        var timestamp = DateTime.UtcNow;

                        _queueCountSamples.AddOrUpdate(
                            "EventQueue",
                            _ => new List<(DateTime Timestamp, int Count)> { (timestamp, eventQueueCount) },
                            (_, list) => { list.Add((timestamp, eventQueueCount)); return list; });

                        _queueCountSamples.AddOrUpdate(
                            "TickerQueue",
                            _ => new List<(DateTime Timestamp, int Count)> { (timestamp, tickerQueueCount) },
                            (_, list) => { list.Add((timestamp, tickerQueueCount)); return list; });

                        _queueCountSamples.AddOrUpdate(
                            "NotificationQueue",
                            _ => new List<(DateTime Timestamp, int Count)> { (timestamp, notificationQueueCount) },
                            (_, list) => { list.Add((timestamp, notificationQueueCount)); return list; });

                        // Collect OrderBookService performance metrics every second
                        if (_orderbookService != null)
                        {
                            var eventMetrics = _orderbookService.GetEventQueueProcessingMetrics();
                            var tickerMetrics = _orderbookService.GetTickerQueueProcessingMetrics();
                            var notificationMetrics = _orderbookService.GetNotificationQueueProcessingMetrics();

                            _orderBookServiceMetrics.AddOrUpdate(
                                "EventQueueProcessing",
                                _ => new List<(DateTime Timestamp, double AvgTime, int TotalOps)> { (timestamp, eventMetrics.AverageProcessingTimeMs, eventMetrics.TotalOperations) },
                                (_, list) => { list.Add((timestamp, eventMetrics.AverageProcessingTimeMs, eventMetrics.TotalOperations)); return list; });

                            _orderBookServiceMetrics.AddOrUpdate(
                                "TickerQueueProcessing",
                                _ => new List<(DateTime Timestamp, double AvgTime, int TotalOps)> { (timestamp, tickerMetrics.AverageProcessingTimeMs, tickerMetrics.TotalOperations) },
                                (_, list) => { list.Add((timestamp, tickerMetrics.AverageProcessingTimeMs, tickerMetrics.TotalOperations)); return list; });

                            _orderBookServiceMetrics.AddOrUpdate(
                                "NotificationQueueProcessing",
                                _ => new List<(DateTime Timestamp, double AvgTime, int TotalOps)> { (timestamp, notificationMetrics.AverageProcessingTimeMs, notificationMetrics.TotalOperations) },
                                (_, list) => { list.Add((timestamp, notificationMetrics.AverageProcessingTimeMs, notificationMetrics.TotalOperations)); return list; });
                        }

                        marketCheckCounter++;
                        await Task.Delay(frontEndRefreshInterval, _statusTrackerService.GetCancellationToken());
                    }
                    catch (OperationCanceledException)
                    {
                        _timerRunning = false;
                        _logger.LogDebug("{Service} performance monitoring cancelled", marketServiceName);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _timerRunning = false;
                        _logger.LogError(ex, "Error monitoring services");
                    }
                }
            });
        }

        /// <summary>
        /// Calculates rolling averages for all system queue counts over the last 5 minutes.
        /// </summary>
        /// <returns>A tuple containing the average counts for EventQueue, TickerQueue, NotificationQueue, and OrderBookQueue.</returns>
        /// <remarks>
        /// This method provides a snapshot of system queue utilization by:
        /// - Filtering samples to the last 5 minutes
        /// - Computing averages for each queue type
        /// - Cleaning up old samples to prevent memory growth
        /// - Including WebSocket order book queue data
        ///
        /// Used for monitoring system performance and detecting potential bottlenecks.
        /// </remarks>
        public (double EventQueueAvg, double TickerQueueAvg, double NotificationQueueAvg, double OrderBookQueueAvg) GetQueueCountRollingAverages()
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            double GetAverage(string queueName)
            {
                if (!_queueCountSamples.TryGetValue(queueName, out var samples))
                    return 0.0;

                var recentSamples = samples
                    .Where(s => s.Timestamp >= fiveMinutesAgo)
                    .ToList();

                double avg = recentSamples.Any() ? recentSamples.Average(s => s.Count) : 0.0;

                _queueCountSamples[queueName] = recentSamples;

                return avg;
            }

            var kalshiWebSocketClient = _serviceFactory.GetKalshiWebSocketClient();
            var timestamp = DateTime.UtcNow;
            var orderBookQueueCount = kalshiWebSocketClient?.OrderBookMessageQueueCount ?? 0;

            _queueCountSamples.AddOrUpdate(
                "OrderBookQueue",
                _ => new List<(DateTime Timestamp, int Count)> { (timestamp, orderBookQueueCount) },
                (_, existing) =>
                {
                    var newList = new List<(DateTime Timestamp, int Count)>(existing);
                    newList.Add((timestamp, orderBookQueueCount));
                    return newList;
                });

            return (
                GetAverage("EventQueue"),
                GetAverage("TickerQueue"),
                GetAverage("NotificationQueue"),
                GetAverage("OrderBookQueue")
            );
        }

        /// <summary>
        /// Calculates the percentage of time the EventQueue has been at or above the target threshold over the last 5 minutes.
        /// </summary>
        /// <returns>The percentage (0-100) of samples where the queue count exceeded the target threshold.</returns>
        /// <remarks>
        /// This method monitors queue utilization by:
        /// - Analyzing EventQueue samples from the last 5 minutes
        /// - Counting samples that exceed the configured target count
        /// - Calculating the percentage of high-utilization periods
        /// - Cleaning up old samples to manage memory
        ///
        /// Used for performance monitoring and system health assessment.
        /// </remarks>
        public double GetQueueHighCountPercentage()
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            // Get samples for EventQueue within the last 5 minutes
            var eventSamples = _queueCountSamples.TryGetValue("EventQueue", out var samples)
                ? samples.Where(s => s.Timestamp >= fiveMinutesAgo).ToList()
                : new List<(DateTime Timestamp, int Count)>();

            if (!eventSamples.Any()) return 0;

            // Calculate percentage of samples where EventQueue count >= _executionConfig.QueuesTargetCount
            var highCountSamples = eventSamples.Count(s => s.Count >= _executionConfig.QueuesTargetCount);

            double percentage = (double)(highCountSamples * 100.0 / eventSamples.Count);

            // Clean up old samples after computation
            _queueCountSamples["EventQueue"] = eventSamples;

            return percentage;
        }

        /// <summary>
        /// Records database performance metrics from the KalshiBotContext.
        /// </summary>
        /// <param name="metrics">Dictionary containing database operation metrics.</param>
        /// <remarks>
        /// This method is called by the KalshiBotContext to post database performance metrics
        /// to the central performance monitor for tracking and analysis.
        /// </remarks>
        public void RecordDatabaseMetrics(Dictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> metrics)
        {
            _databaseMetrics = metrics;
            _logger.LogDebug("Database metrics recorded: {Count} operations", metrics?.Count ?? 0);
        }

        /// <summary>
        /// Gets the current database performance metrics.
        /// </summary>
        /// <returns>Dictionary containing operation names and their performance statistics.</returns>
        public IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> GetPerformanceMetrics()
        {
            return _databaseMetrics ?? new Dictionary<string, (int, int, TimeSpan, double)>();
        }

        /// <summary>
        /// Records OverseerClientService performance metrics.
        /// </summary>
        /// <param name="metrics">Dictionary containing OverseerClientService performance metrics.</param>
        /// <remarks>
        /// This method is called by OverseerClientService to post its performance metrics
        /// to the central performance monitor for tracking and analysis.
        /// </remarks>
        public void RecordOverseerClientServiceMetrics(Dictionary<string, object> metrics)
        {
            _overseerClientServiceMetrics = metrics;
            _logger.LogDebug("OverseerClientService metrics recorded: {Count} metrics", metrics?.Count ?? 0);
        }

        /// <summary>
        /// Resets all performance metrics.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            _databaseMetrics = null;
            _overseerClientServiceMetrics = null;
            _logger.LogInformation("All performance metrics reset");
        }

        /// <summary>
        /// Checks for performance alerts based on configured thresholds and logs warnings if exceeded.
        /// </summary>
        /// <summary>
        /// Calculates rolling averages for OrderBookService processing metrics over the last 5 minutes.
        /// </summary>
        /// <returns>A tuple containing the average processing times and total operations for EventQueue, TickerQueue, and NotificationQueue.</returns>
        /// <remarks>
        /// This method provides a snapshot of OrderBookService performance by:
        /// - Filtering samples to the last 5 minutes
        /// - Computing averages for each processing metric
        /// - Cleaning up old samples to prevent memory growth
        ///
        /// Used for monitoring OrderBookService performance and detecting potential bottlenecks.
        /// </remarks>
        public (double EventQueueAvgTime, double TickerQueueAvgTime, double NotificationQueueAvgTime, int EventQueueTotalOps, int TickerQueueTotalOps, int NotificationQueueTotalOps) GetOrderBookServiceProcessingMetricsRollingAverages()
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            double GetAverageTime(string metricName, out int totalOps)
            {
                totalOps = 0;
                if (!_orderBookServiceMetrics.TryGetValue(metricName, out var samples))
                    return 0.0;

                var recentSamples = samples
                    .Where(s => s.Timestamp >= fiveMinutesAgo)
                    .ToList();

                if (!recentSamples.Any())
                    return 0.0;

                double avgTime = recentSamples.Average(s => s.AvgTime);
                totalOps = recentSamples.Sum(s => s.TotalOps);

                _orderBookServiceMetrics[metricName] = recentSamples;

                return avgTime;
            }

            var eventAvg = GetAverageTime("EventQueueProcessing", out var eventTotalOps);
            var tickerAvg = GetAverageTime("TickerQueueProcessing", out var tickerTotalOps);
            var notificationAvg = GetAverageTime("NotificationQueueProcessing", out var notificationTotalOps);

            return (eventAvg, tickerAvg, notificationAvg, eventTotalOps, tickerTotalOps, notificationTotalOps);
        }

        /// <summary>
        /// Gets the latest OrderBookService processing metrics.
        /// </summary>
        /// <returns>A tuple containing the most recent processing times and total operations for EventQueue, TickerQueue, and NotificationQueue.</returns>
        public (double EventQueueTime, double TickerQueueTime, double NotificationQueueTime, int EventQueueOps, int TickerQueueOps, int NotificationQueueOps) GetLatestOrderBookServiceProcessingMetrics()
        {
            double GetLatestTime(string metricName, out int totalOps)
            {
                totalOps = 0;
                if (!_orderBookServiceMetrics.TryGetValue(metricName, out var samples) || !samples.Any())
                    return 0.0;

                var latest = samples.Last();
                totalOps = latest.TotalOps;
                return latest.AvgTime;
            }

            var eventTime = GetLatestTime("EventQueueProcessing", out var eventOps);
            var tickerTime = GetLatestTime("TickerQueueProcessing", out var tickerOps);
            var notificationTime = GetLatestTime("NotificationQueueProcessing", out var notificationOps);

            return (eventTime, tickerTime, notificationTime, eventOps, tickerOps, notificationOps);
        }

        /// <summary>
        /// Gets OrderBookService market lock wait metrics for a specific market.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get metrics for.</param>
        /// <returns>A tuple containing the average wait time and total operations for the specified market.</returns>
        public (double AverageWaitTimeMs, int TotalOperations) GetOrderBookServiceMarketLockWaitMetrics(string marketTicker)
        {
            if (_orderbookService == null)
                return (0.0, 0);

            return _orderbookService.GetMarketLockWaitMetrics(marketTicker);
        }

        /// <remarks>
        /// This method evaluates current performance metrics against configured alert thresholds:
        /// - Queue high count percentage exceeding threshold
        /// - Refresh usage percentage exceeding threshold
        /// - Individual queue counts exceeding absolute thresholds
        ///
        /// Alerts are logged as warnings to notify administrators of potential performance issues.
        /// </remarks>
        public void CheckPerformanceAlerts()
        {
            var queueHighPercentage = GetQueueHighCountPercentage();
            if (queueHighPercentage >= _executionConfig.QueueHighCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: EventQueue high count percentage {Percentage:F2}% exceeds threshold {Threshold:F2}%. System may be overloaded.",
                    queueHighPercentage, _executionConfig.QueueHighCountAlertThreshold);
            }

            if (LastRefreshUsagePercentage >= _executionConfig.RefreshUsageAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Refresh usage {Usage:F2}% exceeds threshold {Threshold:F2}%. Market refresh cycle may be too slow.",
                    LastRefreshUsagePercentage, _executionConfig.RefreshUsageAlertThreshold);
            }

            var (eventQueueAvg, tickerQueueAvg, notificationQueueAvg, orderBookQueueAvg) = GetQueueCountRollingAverages();
            if (eventQueueAvg >= _executionConfig.QueueCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average EventQueue count {Avg:F2} exceeds threshold {Threshold}. Processing may be delayed.",
                    eventQueueAvg, _executionConfig.QueueCountAlertThreshold);
            }
            if (tickerQueueAvg >= _executionConfig.QueueCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average TickerQueue count {Avg:F2} exceeds threshold {Threshold}. Processing may be delayed.",
                    tickerQueueAvg, _executionConfig.QueueCountAlertThreshold);
            }
            if (notificationQueueAvg >= _executionConfig.QueueCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average NotificationQueue count {Avg:F2} exceeds threshold {Threshold}. Processing may be delayed.",
                    notificationQueueAvg, _executionConfig.QueueCountAlertThreshold);
            }
            if (orderBookQueueAvg >= _executionConfig.QueueCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average OrderBookQueue count {Avg:F2} exceeds threshold {Threshold}. Processing may be delayed.",
                    orderBookQueueAvg, _executionConfig.QueueCountAlertThreshold);
            }
        }
    }
}