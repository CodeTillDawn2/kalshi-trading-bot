// CentralPerformanceMonitor.cs
using KalshiBotAPI.WebSockets.Interfaces;
using BacklashBotData.Data.Interfaces;
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
    public class CentralPerformanceMonitor : ICentralPerformanceMonitor, IKalshiBotContextPerformanceMetrics, INightActivitiesPerformanceMetrics, IWebSocketPerformanceMetrics, ISqlDataServicePerformanceMetrics, ISubscriptionManagerPerformanceMetrics, IMessageProcessorPerformanceMetrics
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
        public TimeSpan LastRefreshCpuTime { get; set; }
        public long LastRefreshMemoryUsage { get; set; }
        public double LastRefreshThroughput { get; set; }
        public TimeSpan LastRefreshAverageTimePerMarket { get; set; }
        public int LastRefreshCount { get; set; }
        private readonly IScopeManagerService _scopeManagerService;
        private IStatusTrackerService _statusTrackerService;
        private IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)>? _databaseMetrics;

        // WebSocket performance metrics
        private readonly ConcurrentDictionary<string, long> _webSocketProcessingTimeTicks = new();
        private readonly ConcurrentDictionary<string, int> _webSocketProcessingCount = new();
        private readonly ConcurrentDictionary<string, long> _webSocketBufferUsageBytes = new();
        private readonly ConcurrentDictionary<string, TimeSpan> _webSocketOperationTimes = new();
        private readonly ConcurrentDictionary<string, int> _webSocketSemaphoreWaitCount = new();
        private IReadOnlyDictionary<string, object>? _overseerClientServiceMetrics;

        // SubscriptionManager performance metrics
        private IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)>? _subscriptionManagerOperationMetrics;
        private IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)>? _subscriptionManagerLockMetrics;

        // MessageProcessor performance metrics
        private long _messageProcessorTotalMessagesProcessed;
        private long _messageProcessorTotalProcessingTimeMs;
        private double _messageProcessorAverageProcessingTimeMs;
        private double _messageProcessorMessagesPerSecond;
        private int _messageProcessorOrderBookQueueDepth;
        private int _messageProcessorDuplicateMessageCount;
        private int _messageProcessorDuplicatesInWindow;
        private DateTime _messageProcessorLastDuplicateWarningTime;
        private IReadOnlyDictionary<string, long>? _messageProcessorMessageTypeCounts;

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
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
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

                                // Collect additional metrics if enabled
                                try
                                {
                                    LastRefreshCpuTime = marketRefreshService.LastCpuTime;
                                    LastRefreshMemoryUsage = marketRefreshService.LastMemoryUsage;
                                    LastRefreshThroughput = marketRefreshService.RefreshThroughput;
                                    LastRefreshAverageTimePerMarket = marketRefreshService.AverageRefreshTimePerMarket;
                                    LastRefreshCount = marketRefreshService.LastRefreshCount;
                                }
                                catch (InvalidOperationException ex)
                                {
                                    _logger.LogDebug("MarketRefreshService performance metrics are disabled: {Message}", ex.Message);
                                    // Reset to defaults
                                    LastRefreshCpuTime = TimeSpan.Zero;
                                    LastRefreshMemoryUsage = 0;
                                    LastRefreshThroughput = 0;
                                    LastRefreshAverageTimePerMarket = TimeSpan.Zero;
                                    LastRefreshCount = 0;
                                }

                                _logger.LogInformation(
                                    "{Service} processing time: Elapsed={ElapsedSeconds:F2}s, Markets={MarketCount}, Usage={UsagePercentage:F2}%, Acceptable={IsAcceptable}, CpuTime={CpuTime}, Memory={Memory}MB, Throughput={Throughput:F2}/s",
                                    marketServiceName, LastRefreshCycleSeconds, LastRefreshMarketCount, LastRefreshUsagePercentage, LastRefreshTimeAcceptable,
                                    LastRefreshCpuTime.TotalSeconds, LastRefreshMemoryUsage / (1024 * 1024), LastRefreshThroughput);

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
        /// Records broadcast service performance metrics.
        /// </summary>
        /// <param name="successfulBroadcasts">Number of successful broadcasts.</param>
        /// <param name="failedBroadcasts">Number of failed broadcasts.</param>
        /// <param name="totalBroadcastTimeMs">Total time spent on broadcasts in milliseconds.</param>
        /// <param name="averageBroadcastTimeMs">Average broadcast time in milliseconds.</param>
        /// <param name="broadcastSuccessRate">Success rate percentage.</param>
        /// <param name="totalDataSize">Total size of broadcast data in bytes.</param>
        /// <param name="broadcastsPerMinute">Average broadcasts per minute.</param>
        /// <param name="totalMemoryUsed">Total memory used during broadcasts in bytes.</param>
        /// <param name="averageIntervalDeviationMs">Average interval deviation in milliseconds.</param>
        /// <remarks>
        /// This method receives broadcast performance metrics from BroadcastService
        /// and integrates them with the central performance monitoring system.
        /// </remarks>
        public void RecordBroadcastMetrics(
            long successfulBroadcasts,
            long failedBroadcasts,
            double totalBroadcastTimeMs,
            double averageBroadcastTimeMs,
            double broadcastSuccessRate,
            long totalDataSize,
            double broadcastsPerMinute,
            long totalMemoryUsed,
            double averageIntervalDeviationMs)
        {
            // Record execution time for broadcast operations
            RecordExecutionTime("BroadcastService", (long)totalBroadcastTimeMs);

            // Log broadcast performance summary
            _logger.LogInformation(
                "BROADCAST PERFORMANCE: Success={Successful}/{Total}, SuccessRate={SuccessRate:F2}%, AvgTime={AvgTime:F2}ms, DataSize={DataSize} bytes, Throughput={Throughput:F2}/min, Memory={Memory} bytes, IntervalDeviation={Deviation:F2}ms",
                successfulBroadcasts, successfulBroadcasts + failedBroadcasts, broadcastSuccessRate, averageBroadcastTimeMs, totalDataSize, broadcastsPerMinute, totalMemoryUsed, averageIntervalDeviationMs);

            // Check for broadcast performance alerts
            if (broadcastSuccessRate < 95.0)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Broadcast success rate {SuccessRate:F2}% is below 95%", broadcastSuccessRate);
            }

            if (averageBroadcastTimeMs > 1000) // 1 second
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average broadcast time {AvgTime:F2}ms exceeds 1 second", averageBroadcastTimeMs);
            }

            if (averageIntervalDeviationMs > 5000) // 5 seconds
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average interval deviation {Deviation:F2}ms exceeds 5 seconds", averageIntervalDeviationMs);
            }
        }

        /// <summary>
        /// Records MarketDataInitializer performance metrics.
        /// </summary>
        /// <param name="totalDuration">Total initialization duration.</param>
        /// <param name="marketCount">Number of markets processed.</param>
        /// <param name="averageMarketTime">Average time per market.</param>
        /// <param name="memoryDelta">Memory usage change in bytes.</param>
        /// <param name="cpuTime">CPU time used.</param>
        /// <param name="successfulMarkets">Number of successfully initialized markets.</param>
        /// <param name="failedMarkets">Number of failed market initializations.</param>
        /// <param name="totalWaitTime">Total time spent waiting.</param>
        /// <remarks>
        /// This method receives performance data from MarketDataInitializer
        /// and integrates it with the central performance monitoring system.
        /// </remarks>
        public void RecordMarketDataInitializerMetrics(
            TimeSpan totalDuration,
            int marketCount,
            TimeSpan averageMarketTime,
            long memoryDelta,
            TimeSpan cpuTime,
            int successfulMarkets,
            int failedMarkets,
            TimeSpan totalWaitTime)
        {
            // Record execution time
            RecordExecutionTime("MarketDataInitializer", (long)totalDuration.TotalMilliseconds);

            // Log performance summary
            _logger.LogInformation(
                "MARKET DATA INITIALIZER PERFORMANCE: Duration={Duration}, Markets={Count}, AvgTime={AvgTime}, MemoryDelta={Memory} bytes, CpuTime={Cpu}, Success={Success}, Fail={Fail}, WaitTime={Wait}",
                totalDuration, marketCount, averageMarketTime, memoryDelta, cpuTime, successfulMarkets, failedMarkets, totalWaitTime);

            // Check for performance alerts
            if (totalDuration.TotalSeconds > 300) // 5 minutes
            {
                _logger.LogWarning("PERFORMANCE ALERT: Market data initialization took {Duration} (>5 minutes)", totalDuration);
            }

            if (failedMarkets > marketCount * 0.1) // More than 10% failures
            {
                _logger.LogWarning("PERFORMANCE ALERT: {FailCount} market initialization failures (>10% of {TotalCount})", failedMarkets, marketCount);
            }

            if (memoryDelta > 100 * 1024 * 1024) // 100MB
            {
                _logger.LogWarning("PERFORMANCE ALERT: Memory usage increased by {Memory}MB (>100MB)", memoryDelta / (1024 * 1024));
            }
        }

        /// <summary>
        /// Records overnight activities performance metrics from the common OvernightActivitiesHelper.
        /// </summary>
        /// <param name="metrics">The performance metrics from overnight activities.</param>
        /// <remarks>
        /// This method receives comprehensive performance data from the OvernightActivitiesHelper
        /// and integrates it with the central performance monitoring system for analysis and alerting.
        /// </remarks>
        public void RecordOvernightActivitiesMetrics(INightActivitiesPerformanceMetrics metrics)
        {
            var (totalTime, marketsProcessed, apiCalls, errors, peakMemory, startTime, endTime, taskDurations) = metrics.GetOvernightPerformanceMetrics();

            // Record execution time
            RecordExecutionTime("OvernightActivities", totalTime);

            // Log comprehensive overnight performance summary
            _logger.LogInformation("OVERNIGHT PERFORMANCE: Total={TotalTime}ms, Markets={Markets}, API Calls={ApiCalls}, Errors={Errors}, Peak Memory={PeakMemory}MB",
                totalTime, marketsProcessed, apiCalls, errors, peakMemory);

            // Log individual task performances
            foreach (var task in taskDurations)
            {
                _logger.LogInformation("OVERNIGHT TASK: {TaskName}={Duration}ms", task.Key, task.Value);
            }

            // Check for overnight performance alerts
            if (totalTime > 300000) // 5 minutes
            {
                _logger.LogWarning("PERFORMANCE ALERT: Overnight activities took {TotalTime}ms (>5 minutes)", totalTime);
            }

            if (errors > 10)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Overnight activities had {ErrorCount} errors", errors);
            }

            if (peakMemory > 1000) // 1GB
            {
                _logger.LogWarning("PERFORMANCE ALERT: Overnight activities used {PeakMemory}MB peak memory (>1GB)", peakMemory);
            }
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

        #region INightActivitiesPerformanceMetrics Implementation

        /// <summary>
        /// Gets the current overnight activities performance metrics.
        /// </summary>
        /// <returns>Tuple containing comprehensive performance data.</returns>
        public (long TotalExecutionTimeMs, int MarketsProcessed, int ApiCallsMade, int ErrorsEncountered,
                long PeakMemoryUsageMB, DateTime StartTime, DateTime EndTime,
                Dictionary<string, long> TaskDurations) GetOvernightPerformanceMetrics()
        {
            // This is a placeholder implementation since CentralPerformanceMonitor doesn't track these specific metrics
            // The actual metrics are tracked by the OvernightActivitiesHelper itself
            return (0, 0, 0, 0, 0, DateTime.MinValue, DateTime.MinValue, new Dictionary<string, long>());
        }

        /// <summary>
        /// Records an overnight task execution with performance data.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        /// <param name="duration">The execution duration in milliseconds.</param>
        /// <param name="success">Whether the task was successful.</param>
        public void RecordOvernightTask(string taskName, long duration, bool success)
        {
            RecordExecutionTime(taskName, duration);
            if (!success)
            {
                _logger.LogWarning("Overnight task '{TaskName}' failed after {Duration}ms", taskName, duration);
            }
        }

        /// <summary>
        /// Records an API call made during overnight processing.
        /// </summary>
        public void RecordApiCall()
        {
            // API calls are tracked through the existing RecordExecutionTime method
            // This is a no-op since we don't have a separate counter for overnight API calls
        }

        /// <summary>
        /// Records an error that occurred during overnight processing.
        /// </summary>
        public void RecordError()
        {
            // Errors are logged through the existing logging mechanism
            // This is a no-op since we don't have a separate error counter for overnight activities
        }

        /// <summary>
        /// Records the number of markets processed.
        /// </summary>
        /// <param name="count">The number of markets processed.</param>
        public void RecordMarketsProcessed(int count)
        {
            // Market processing counts are tracked through the existing market refresh metrics
            // This is a no-op since we don't have a separate counter for overnight market processing
        }

        /// <summary>
        /// Records memory usage during overnight processing.
        /// </summary>
        /// <param name="memoryMB">Current memory usage in MB.</param>
        public void RecordMemoryUsage(long memoryMB)
        {
            // Memory usage is not specifically tracked in CentralPerformanceMonitor
            // This is a no-op since we don't have memory tracking for overnight activities
        }

        /// <summary>
        /// Records MarketAnalysisHelper performance metrics.
        /// </summary>
        /// <param name="totalMarkets">Total number of markets processed.</param>
        /// <param name="totalTimeMs">Total processing time in milliseconds.</param>
        /// <param name="averageTimeMs">Average time per market in milliseconds.</param>
        /// <param name="errorCount">Number of errors encountered.</param>
        /// <remarks>
        /// This method receives performance data from MarketAnalysisHelper
        /// and integrates it with the central performance monitoring system.
        /// </remarks>
        public void RecordMarketAnalysisHelperMetrics(int totalMarkets, long totalTimeMs, double averageTimeMs, int errorCount)
        {
            // Record execution time
            RecordExecutionTime("MarketAnalysisHelper.GenerateSnapshotGroups", totalTimeMs);

            // Log performance summary
            _logger.LogInformation(
                "MARKET ANALYSIS HELPER PERFORMANCE: TotalMarkets={TotalMarkets}, TotalTime={TotalTime}ms, AvgTime={AvgTime:F2}ms, Errors={Errors}",
                totalMarkets, totalTimeMs, averageTimeMs, errorCount);

            // Check for performance alerts
            if (errorCount > 0)
            {
                _logger.LogWarning("PERFORMANCE ALERT: MarketAnalysisHelper encountered {ErrorCount} errors", errorCount);
            }

            if (averageTimeMs > 5000) // 5 seconds per market
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average market processing time {AvgTime:F2}ms exceeds 5 seconds", averageTimeMs);
            }

            if (totalTimeMs > 300000) // 5 minutes total
            {
                _logger.LogWarning("PERFORMANCE ALERT: Total processing time {TotalTime}ms exceeds 5 minutes", totalTimeMs);
            }
        }

        /// <summary>
        /// Gets a formatted performance summary string.
        /// </summary>
        /// <returns>Formatted performance summary.</returns>
        public string GetPerformanceSummary()
        {
            var summary = "Central Performance Monitor - Overnight Activities Summary";
            return summary;
        }

        #endregion

        #region IWebSocketPerformanceMetrics Implementation

        /// <summary>
        /// Records WebSocket message processing performance.
        /// </summary>
        public void RecordWebSocketMessageProcessing(string messageType, long processingTimeTicks, int messageCount, long bufferSizeBytes)
        {
            _webSocketProcessingTimeTicks.AddOrUpdate(messageType, 0, (k, v) => v + processingTimeTicks);
            _webSocketProcessingCount.AddOrUpdate(messageType, 0, (k, v) => v + messageCount);
            _webSocketBufferUsageBytes.AddOrUpdate(messageType, 0, (k, v) => v + bufferSizeBytes);
            _logger.LogDebug("WebSocket message processing recorded: Type={Type}, TimeTicks={TimeTicks}, Count={Count}, BufferBytes={BufferBytes}",
                messageType, processingTimeTicks, messageCount, bufferSizeBytes);
        }

        /// <summary>
        /// Records WebSocket connection performance.
        /// </summary>
        public void RecordWebSocketOperation(string operation, TimeSpan duration)
        {
            _webSocketOperationTimes[operation] = duration;
            _logger.LogDebug("WebSocket operation recorded: {Operation}={Duration}ms", operation, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Records semaphore wait counts for WebSocket operations.
        /// </summary>
        public void RecordSemaphoreWait(string operation, int waitCount)
        {
            _webSocketSemaphoreWaitCount.AddOrUpdate(operation, 0, (k, v) => v + waitCount);
            _logger.LogDebug("Semaphore wait recorded: {Operation}={WaitCount}", operation, waitCount);
        }

        /// <summary>
        /// Gets the average processing times for WebSocket messages.
        /// </summary>
        public ConcurrentDictionary<string, double> GetAverageProcessingTimesMs()
        {
            return new ConcurrentDictionary<string, double>(
                _webSocketProcessingTimeTicks.ToDictionary(
                    kv => kv.Key,
                    kv => _webSocketProcessingCount.TryGetValue(kv.Key, out var count) && count > 0
                        ? TimeSpan.FromTicks(kv.Value / count).TotalMilliseconds
                        : 0.0
                )
            );
        }

        /// <summary>
        /// Gets the total buffer usage for WebSocket messages.
        /// </summary>
        public ConcurrentDictionary<string, long> GetBufferUsageBytes()
        {
            return new ConcurrentDictionary<string, long>(_webSocketBufferUsageBytes);
        }

        /// <summary>
        /// Gets the average times for WebSocket operations.
        /// </summary>
        public ConcurrentDictionary<string, double> GetAsyncOperationTimesMs()
        {
            return new ConcurrentDictionary<string, double>(
                _webSocketOperationTimes.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.TotalMilliseconds
                )
            );
        }

        /// <summary>
        /// Gets the semaphore wait counts for WebSocket operations.
        /// </summary>
        public ConcurrentDictionary<string, int> GetSemaphoreWaitCounts()
        {
            return new ConcurrentDictionary<string, int>(_webSocketSemaphoreWaitCount);
        }

        /// <summary>
        /// Resets all WebSocket performance metrics.
        /// </summary>
        public void ResetWebSocketMetrics()
        {
            _webSocketProcessingTimeTicks.Clear();
            _webSocketProcessingCount.Clear();
            _webSocketBufferUsageBytes.Clear();
            _webSocketOperationTimes.Clear();
            _webSocketSemaphoreWaitCount.Clear();
            _logger.LogInformation("WebSocket performance metrics reset");
        }

        #endregion

        #region ISqlDataServicePerformanceMetrics Implementation

        /// <summary>
        /// Receives throughput metrics from SqlDataService.
        /// </summary>
        /// <param name="operationsPerSecond">Current operations per second rate.</param>
        /// <param name="totalProcessed">Total operations processed successfully.</param>
        /// <param name="totalFailed">Total operations that failed.</param>
        public void ReceiveThroughputMetrics(double operationsPerSecond, long totalProcessed, long totalFailed)
        {
            _logger.LogDebug("SqlDataService Throughput: {OpsPerSec:F2} ops/sec, Processed: {Processed}, Failed: {Failed}",
                operationsPerSecond, totalProcessed, totalFailed);

            // Store the metrics for monitoring
            // Could be extended to store in a time-series database or expose via API
        }

        /// <summary>
        /// Receives latency metrics from SqlDataService.
        /// </summary>
        /// <param name="averageLatencyMs">Average latency in milliseconds for processed operations.</param>
        /// <param name="sampleCount">Number of latency samples collected.</param>
        public void ReceiveLatencyMetrics(double averageLatencyMs, long sampleCount)
        {
            _logger.LogDebug("SqlDataService Latency: {AvgLatency:F2}ms over {SampleCount} samples", averageLatencyMs, sampleCount);

            // Check for performance alerts
            if (averageLatencyMs > 1000) // 1 second
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService average latency {AvgLatency:F2}ms exceeds 1 second", averageLatencyMs);
            }
        }

        /// <summary>
        /// Receives resource utilization metrics from SqlDataService.
        /// </summary>
        /// <param name="cpuUsagePercent">Current CPU usage percentage.</param>
        /// <param name="memoryUsageMB">Current memory usage in MB.</param>
        public void ReceiveResourceMetrics(double cpuUsagePercent, double memoryUsageMB)
        {
            _logger.LogDebug("SqlDataService Resources: CPU {CpuUsage:F2}%, Memory {MemoryUsage:F2}MB", cpuUsagePercent, memoryUsageMB);

            // Check for resource alerts
            if (cpuUsagePercent > 80)
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService CPU usage {CpuUsage:F2}% exceeds 80%", cpuUsagePercent);
            }

            if (memoryUsageMB > 1000) // 1GB
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService memory usage {MemoryUsage:F2}MB exceeds 1GB", memoryUsageMB);
            }
        }

        /// <summary>
        /// Receives queue depth metrics from SqlDataService.
        /// </summary>
        /// <param name="orderBookQueueDepth">Current depth of order book queue.</param>
        /// <param name="tradeQueueDepth">Current depth of trade queue.</param>
        /// <param name="fillQueueDepth">Current depth of fill queue.</param>
        /// <param name="eventLifecycleQueueDepth">Current depth of event lifecycle queue.</param>
        /// <param name="marketLifecycleQueueDepth">Current depth of market lifecycle queue.</param>
        /// <param name="totalQueuedOperations">Total operations across all queues.</param>
        public void ReceiveQueueMetrics(int orderBookQueueDepth, int tradeQueueDepth, int fillQueueDepth,
                                       int eventLifecycleQueueDepth, int marketLifecycleQueueDepth, int totalQueuedOperations)
        {
            _logger.LogDebug("SqlDataService Queues: OrderBook={OrderBook}, Trade={Trade}, Fill={Fill}, Event={Event}, Market={Market}, Total={Total}",
                orderBookQueueDepth, tradeQueueDepth, fillQueueDepth, eventLifecycleQueueDepth, marketLifecycleQueueDepth, totalQueuedOperations);

            // Check for queue alerts
            if (totalQueuedOperations > 5000)
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService total queued operations {Total} exceeds 5000", totalQueuedOperations);
            }
        }

        /// <summary>
        /// Receives success rate metrics from SqlDataService.
        /// </summary>
        /// <param name="successRatePercent">Success rate as a percentage (0-100).</param>
        public void ReceiveSuccessRateMetrics(double successRatePercent)
        {
            _logger.LogDebug("SqlDataService Success Rate: {SuccessRate:F2}%", successRatePercent);

            // Check for success rate alerts
            if (successRatePercent < 95.0)
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService success rate {SuccessRate:F2}% is below 95%", successRatePercent);
            }
        }

        #endregion

        #region ISubscriptionManagerPerformanceMetrics Implementation

        /// <summary>
        /// Posts operation performance metrics from SubscriptionManager.
        /// </summary>
        /// <param name="metrics">Dictionary containing operation names and their performance statistics.</param>
        public void PostOperationMetrics(IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)> metrics)
        {
            _subscriptionManagerOperationMetrics = metrics;
            _logger.LogDebug("SubscriptionManager operation metrics posted: {Count} operations", metrics?.Count ?? 0);
        }

        /// <summary>
        /// Posts lock contention metrics from SubscriptionManager.
        /// </summary>
        /// <param name="metrics">Dictionary containing lock names and their contention statistics.</param>
        public void PostLockContentionMetrics(IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)> metrics)
        {
            _subscriptionManagerLockMetrics = metrics;
            _logger.LogDebug("SubscriptionManager lock contention metrics posted: {Count} locks", metrics?.Count ?? 0);
        }

        /// <summary>
        /// Gets the current operation performance metrics.
        /// </summary>
        /// <returns>Dictionary containing operation names and their performance statistics.</returns>
        public IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)> GetOperationMetrics()
        {
            return _subscriptionManagerOperationMetrics ?? new Dictionary<string, (long, long, long)>();
        }

        /// <summary>
        /// Gets the current lock contention metrics.
        /// </summary>
        /// <returns>Dictionary containing lock names and their contention statistics.</returns>
        public IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)> GetLockContentionMetrics()
        {
            return _subscriptionManagerLockMetrics ?? new Dictionary<string, (long, long, long)>();
        }

        /// <summary>
        /// Resets all SubscriptionManager performance metrics.
        /// </summary>
        public void ResetSubscriptionManagerMetrics()
        {
            _subscriptionManagerOperationMetrics = null;
            _subscriptionManagerLockMetrics = null;
            _logger.LogInformation("SubscriptionManager performance metrics reset");
        }

        #endregion

        #region IMessageProcessorPerformanceMetrics Implementation

        /// <summary>
        /// Posts message processing performance metrics from MessageProcessor.
        /// </summary>
        /// <param name="totalMessagesProcessed">Total number of messages processed since last reset.</param>
        /// <param name="totalProcessingTimeMs">Total processing time in milliseconds since last reset.</param>
        /// <param name="averageProcessingTimeMs">Average processing time per message in milliseconds.</param>
        /// <param name="messagesPerSecond">Current messages per second rate.</param>
        /// <param name="orderBookQueueDepth">Current depth of the order book update queue.</param>
        public void PostMessageProcessingMetrics(long totalMessagesProcessed, long totalProcessingTimeMs,
            double averageProcessingTimeMs, double messagesPerSecond, int orderBookQueueDepth)
        {
            _messageProcessorTotalMessagesProcessed = totalMessagesProcessed;
            _messageProcessorTotalProcessingTimeMs = totalProcessingTimeMs;
            _messageProcessorAverageProcessingTimeMs = averageProcessingTimeMs;
            _messageProcessorMessagesPerSecond = messagesPerSecond;
            _messageProcessorOrderBookQueueDepth = orderBookQueueDepth;
            _logger.LogDebug("MessageProcessor metrics posted: {TotalMessages} messages, {AvgTime:F2}ms avg, {MsgsPerSec:F2} msg/sec, QueueDepth={QueueDepth}",
                totalMessagesProcessed, averageProcessingTimeMs, messagesPerSecond, orderBookQueueDepth);
        }

        /// <summary>
        /// Posts duplicate message detection metrics from MessageProcessor.
        /// </summary>
        /// <param name="duplicateMessageCount">Total number of duplicate messages detected.</param>
        /// <param name="duplicatesInWindow">Number of duplicates detected in the current time window.</param>
        /// <param name="lastDuplicateWarningTime">Timestamp of the last duplicate message warning.</param>
        public void PostDuplicateMessageMetrics(int duplicateMessageCount, int duplicatesInWindow, DateTime lastDuplicateWarningTime)
        {
            _messageProcessorDuplicateMessageCount = duplicateMessageCount;
            _messageProcessorDuplicatesInWindow = duplicatesInWindow;
            _messageProcessorLastDuplicateWarningTime = lastDuplicateWarningTime;
            _logger.LogDebug("MessageProcessor duplicate metrics posted: {DuplicateCount} total, {DuplicatesInWindow} in window",
                duplicateMessageCount, duplicatesInWindow);
        }

        /// <summary>
        /// Posts message type distribution metrics from MessageProcessor.
        /// </summary>
        /// <param name="messageTypeCounts">Dictionary containing counts for each message type processed.</param>
        public void PostMessageTypeMetrics(IReadOnlyDictionary<string, long> messageTypeCounts)
        {
            _messageProcessorMessageTypeCounts = messageTypeCounts;
            _logger.LogDebug("MessageProcessor message type metrics posted: {Count} types", messageTypeCounts?.Count ?? 0);
        }

        /// <summary>
        /// Gets the current message processing performance metrics.
        /// </summary>
        /// <returns>Tuple containing current performance metrics.</returns>
        public (long TotalMessagesProcessed, long TotalProcessingTimeMs, double AverageProcessingTimeMs,
            double MessagesPerSecond, int OrderBookQueueDepth) GetMessageProcessingMetrics()
        {
            return (_messageProcessorTotalMessagesProcessed, _messageProcessorTotalProcessingTimeMs,
                _messageProcessorAverageProcessingTimeMs, _messageProcessorMessagesPerSecond, _messageProcessorOrderBookQueueDepth);
        }

        /// <summary>
        /// Gets the current duplicate message metrics.
        /// </summary>
        /// <returns>Tuple containing duplicate message statistics.</returns>
        public (int DuplicateMessageCount, int DuplicatesInWindow, DateTime LastDuplicateWarningTime) GetDuplicateMessageMetrics()
        {
            return (_messageProcessorDuplicateMessageCount, _messageProcessorDuplicatesInWindow, _messageProcessorLastDuplicateWarningTime);
        }

        /// <summary>
        /// Gets the current message type distribution metrics.
        /// </summary>
        /// <returns>Dictionary containing message type counts.</returns>
        public IReadOnlyDictionary<string, long> GetMessageTypeMetrics()
        {
            return _messageProcessorMessageTypeCounts ?? new Dictionary<string, long>();
        }

        /// <summary>
        /// Resets all MessageProcessor performance metrics.
        /// </summary>
        public void ResetMessageProcessorMetrics()
        {
            _messageProcessorTotalMessagesProcessed = 0;
            _messageProcessorTotalProcessingTimeMs = 0;
            _messageProcessorAverageProcessingTimeMs = 0;
            _messageProcessorMessagesPerSecond = 0;
            _messageProcessorOrderBookQueueDepth = 0;
            _messageProcessorDuplicateMessageCount = 0;
            _messageProcessorDuplicatesInWindow = 0;
            _messageProcessorLastDuplicateWarningTime = DateTime.MinValue;
            _messageProcessorMessageTypeCounts = null;
            _logger.LogInformation("MessageProcessor performance metrics reset");
        }

        #endregion
    }
}