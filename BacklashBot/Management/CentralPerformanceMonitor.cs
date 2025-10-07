// CentralPerformanceMonitor.cs
using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashBotData.Data.Interfaces;
using BacklashDTOs.Data;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Options;
using OverseerBotShared;
using System.Collections.Concurrent;

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
    public class CentralPerformanceMonitor : BasePerformanceMonitor, ICentralPerformanceMonitor
    {
        private readonly ILogger<ICentralPerformanceMonitor> _logger;
        /// <summary>
        /// Gets the concurrent dictionary storing API execution times for performance monitoring.
        /// Key is the method name, value is a list of timestamp and milliseconds.
        /// </summary>
        public readonly ConcurrentDictionary<string, List<(DateTime Timestamp, long Milliseconds)>> ApiExecutionTimes;
        private readonly GeneralExecutionConfig _generalExecutionConfig;
        private readonly QueueMonitoringConfig _queueMonitoringConfig;
        private readonly CentralPerformanceMonitorConfig _centralPerformanceMonitorConfig;
        private readonly IServiceScopeFactory _scopeFactory;
        private IOrderBookService _orderbookService
        {
            get
            {
                using var scope = _scopeFactory.CreateScope();
                var serviceFactory = scope.ServiceProvider.GetRequiredService<IServiceFactory>();
                return serviceFactory.GetOrderBookService();
            }
        }
        /// <summary>
        /// Gets the brain instance identifier used for configuration and logging.
        /// </summary>
        public string? BrainInstance { get; private set; }
        /// <summary>
        /// Gets the refresh interval for market data updates.
        /// </summary>
        public TimeSpan RefreshInterval { get; private set; }
        /// <summary>
        /// Gets the current database performance metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)>? DatabaseMetrics => _databaseMetrics;

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        public double GetCurrentCpuUsage()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var cpuTime = process.TotalProcessorTime.TotalMilliseconds;
                var elapsed = (DateTime.Now - process.StartTime).TotalMilliseconds;
                return elapsed > 0 ? (cpuTime / elapsed) * 100.0 : 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Gets the average event queue depth.
        /// </summary>
        public double GetEventQueueAvg()
        {
            var (eventQueueAvg, _, _, _) = GetQueueCountRollingAverages();
            return eventQueueAvg;
        }

        /// <summary>
        /// Gets the average ticker queue depth.
        /// </summary>
        public double GetTickerQueueAvg()
        {
            var (_, tickerQueueAvg, _, _) = GetQueueCountRollingAverages();
            return tickerQueueAvg;
        }

        /// <summary>
        /// Gets the average notification queue depth.
        /// </summary>
        public double GetNotificationQueueAvg()
        {
            var (_, _, notificationQueueAvg, _) = GetQueueCountRollingAverages();
            return notificationQueueAvg;
        }

        /// <summary>
        /// Gets the average order book queue depth.
        /// </summary>
        public double GetOrderbookQueueAvg()
        {
            var (_, _, _, orderBookQueueAvg) = GetQueueCountRollingAverages();
            return orderBookQueueAvg;
        }

        /// <summary>
        /// Gets the duration of the last refresh cycle in seconds.
        /// </summary>
        public double GetLastRefreshCycleSeconds()
        {
            return LastRefreshCycleSeconds;
        }

        /// <summary>
        /// Gets the interval between the last two refresh cycles.
        /// </summary>
        public double GetLastRefreshCycleInterval()
        {
            return LastRefreshCycleInterval;
        }

        /// <summary>
        /// Gets the number of markets processed in the last refresh cycle.
        /// </summary>
        public double GetLastRefreshMarketCount()
        {
            return LastRefreshMarketCount;
        }

        /// <summary>
        /// Gets the CPU usage percentage during the last refresh cycle.
        /// </summary>
        public double GetLastRefreshUsagePercentage()
        {
            return LastRefreshUsagePercentage;
        }

        /// <summary>
        /// Gets whether the last refresh cycle completed within acceptable time limits.
        /// </summary>
        public bool GetLastRefreshTimeAcceptable()
        {
            return LastRefreshTimeAcceptable;
        }

        /// <summary>
        /// Gets the timestamp of the last performance sample.
        /// </summary>
        public DateTime? GetLastPerformanceSampleDate()
        {
            return LastPerformanceSampleDate;
        }

        /// <summary>
        /// Gets a value indicating whether the WebSocket connection is currently active.
        /// </summary>
        public bool IsWebSocketConnected
        {
            get
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var serviceFactory = scope.ServiceProvider.GetRequiredService<IServiceFactory>();
                    var kalshiWebSocketClient = serviceFactory.GetKalshiWebSocketClient();
                    return kalshiWebSocketClient?.IsConnected() ?? false;
                }
                catch
                {
                    return false;
                }
            }
        }
        // Configurable metrics data structure for GUI consumption
        private Dictionary<string, object> _configurableMetrics;

        private bool _timerRunning = false;
        private readonly ConcurrentDictionary<string, List<(DateTime Timestamp, int Count)>> _queueCountSamples;
        private readonly ConcurrentDictionary<string, List<(DateTime Timestamp, double AvgTime, int TotalOps)>> _orderBookServiceMetrics;

        /// <summary>
        /// Gets or sets the duration of the last market refresh cycle in seconds.
        /// </summary>
        public double LastRefreshCycleSeconds { get; set; }
        /// <summary>
        /// Gets or sets the interval for the last market refresh cycle in seconds.
        /// </summary>
        public double LastRefreshCycleInterval { get; set; }
        /// <summary>
        /// Gets or sets the number of markets processed in the last refresh cycle.
        /// </summary>
        public int LastRefreshMarketCount { get; set; }
        /// <summary>
        /// Gets or sets the usage percentage of the last refresh cycle.
        /// </summary>
        public double LastRefreshUsagePercentage { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the last refresh time was acceptable.
        /// </summary>
        public bool LastRefreshTimeAcceptable { get; set; }
        /// <summary>
        /// Gets or sets the date and time of the last performance sample.
        /// </summary>
        public DateTime? LastPerformanceSampleDate { get; set; }
        /// <summary>
        /// Gets or sets the CPU time used in the last refresh cycle.
        /// </summary>
        public TimeSpan LastRefreshCpuTime { get; set; }
        /// <summary>
        /// Gets or sets the memory usage in the last refresh cycle.
        /// </summary>
        public long LastRefreshMemoryUsage { get; set; }
        /// <summary>
        /// Gets or sets the throughput of the last refresh cycle.
        /// </summary>
        public double LastRefreshThroughput { get; set; }
        /// <summary>
        /// Gets or sets the average time per market in the last refresh cycle.
        /// </summary>
        public TimeSpan LastRefreshAverageTimePerMarket { get; set; }
        /// <summary>
        /// Gets or sets the count of refreshes in the last cycle.
        /// </summary>
        public int LastRefreshCount { get; set; }
        private IStatusTrackerService _statusTrackerService;
        private IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)>? _databaseMetrics;



        /// <summary>
        /// Gets or sets a value indicating whether the system is currently starting up.
        /// </summary>
        public bool IsStartingUp { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether the system is currently shutting down.
        /// </summary>
        public bool IsShuttingDown { get; set; } = false;

        /// <summary>
        /// Gets or sets whether WebSocketConnectionManager performance metrics are being recorded.
        /// This flag indicates if the WebSocketConnectionManager is actively collecting and posting metrics.
        /// </summary>
        public bool WebSocketConnectionManagerMetricsRecording { get; private set; } = false;



        /// <summary>
        /// Initializes a new instance of the CentralPerformanceMonitor class.
        /// </summary>
        /// <param name="logger">Logger instance for recording performance monitoring operations.</param>
        /// <param name="generalExecutionConfig">Configuration settings for general execution parameters.</param>
        /// <param name="queueMonitoringConfig">Configuration settings for queue monitoring parameters.</param>
        /// <param name="centralPerformanceMonitorConfig">Configuration settings for CentralPerformanceMonitor parameters.</param>
        /// <param name="scopeFactory">Factory for creating service scopes.</param>
        /// <param name="statusTrackerService">Service for tracking system status and cancellation tokens.</param>
        public CentralPerformanceMonitor(
            ILogger<ICentralPerformanceMonitor> logger,
            IOptions<GeneralExecutionConfig> generalExecutionConfig,
            string instanceName,
            IOptions<QueueMonitoringConfig> queueMonitoringConfig,
            IOptions<CentralPerformanceMonitorConfig> centralPerformanceMonitorConfig,
            IServiceScopeFactory scopeFactory,
            IStatusTrackerService statusTrackerService)
            : base(logger)
        {
            _logger = logger;
            _statusTrackerService = statusTrackerService;
            _scopeFactory = scopeFactory;
            ApiExecutionTimes = new ConcurrentDictionary<string, List<(DateTime Timestamp, long Milliseconds)>>();
            _generalExecutionConfig = generalExecutionConfig.Value;
            _queueMonitoringConfig = queueMonitoringConfig.Value;
            _centralPerformanceMonitorConfig = centralPerformanceMonitorConfig.Value;
            RefreshInterval = TimeSpan.FromMinutes(_generalExecutionConfig.RefreshIntervalMinutes);
            BrainInstance = instanceName;
            _logger.LogInformation("PERFMON: Initialized with BrainInstance='{BrainInstance}' from config", BrainInstance);
            _queueCountSamples = new ConcurrentDictionary<string, List<(DateTime Timestamp, int Count)>>();
            _orderBookServiceMetrics = new ConcurrentDictionary<string, List<(DateTime Timestamp, double AvgTime, int TotalOps)>>();
            _databaseMetrics = null;
            _configurableMetrics = new Dictionary<string, object>();
            InitializeConfigurableMetrics();
        }

        /// <summary>
        /// Initializes the configurable metrics data structure with default values.
        /// </summary>
        private void InitializeConfigurableMetrics()
        {
            _configurableMetrics = new Dictionary<string, object>
            {
                // Only include whether performance metrics are enabled for this class
                ["EnablePerformanceMetrics"] = true
            };
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
            using var scope = _scopeFactory.CreateScope();
            var serviceFactory = scope.ServiceProvider.GetRequiredService<IServiceFactory>();
            var dataCache = serviceFactory.GetDataCache();
            if (dataCache == null || !dataCache.Markets.ContainsKey(marketTicker)) return 0.0;

            // Retrieve the last market open time from the data cache
            DateTime lastMarketOpenTime = dataCache.Markets[marketTicker].ChangeTracker.LastMarketOpenTime;

            // Retrieve the tuple of three integers representing WebSocket event counts
            var webSocketClient = serviceFactory.GetKalshiWebSocketClient();
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

            using var serviceScope = _scopeFactory.CreateScope();
            var serviceFactory = serviceScope.ServiceProvider.GetRequiredService<IServiceFactory>();
            var marketRefreshService = serviceFactory.GetMarketRefreshService();
            var kalshiWebSocketClient = serviceFactory.GetKalshiWebSocketClient();
            var broadcastService = serviceFactory.GetBroadcastService();
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

                                // Record CPU usage
                                var cpuUsage = GetCurrentCpuUsage();
                                RecordSpeedDialMetric(nameof(CentralPerformanceMonitor), "CPU_Usage", "CPU Usage", "Current CPU usage percentage", cpuUsage, "%", "System", 0, 80, 100);

                                // Record performance metrics through interface
                                RecordSpeedDialMetric(nameof(CentralPerformanceMonitor), "RefreshCycleSeconds", "Refresh Cycle Seconds", "Last refresh cycle duration", LastRefreshCycleSeconds, "s", "Market Refresh", 0, RefreshInterval.TotalSeconds, RefreshInterval.TotalSeconds * 2);
                                RecordProgressBarMetric(nameof(CentralPerformanceMonitor), "RefreshUsagePercentage", "Refresh Usage Percentage", "Refresh usage percentage", LastRefreshUsagePercentage, "%", "Market Refresh", 0, 100);
                                RecordNumericDisplayMetric(nameof(CentralPerformanceMonitor), "RefreshMarketCount", "Refresh Market Count", "Markets processed in last refresh", LastRefreshMarketCount, "markets", "Market Refresh");
                                RecordSpeedDialMetric(nameof(CentralPerformanceMonitor), "RefreshThroughput", "Refresh Throughput", "Refresh throughput", LastRefreshThroughput, "markets/s", "Market Refresh");
                                RecordSpeedDialMetric(nameof(CentralPerformanceMonitor), "RefreshCpuTime", "Refresh CPU Time", "Refresh CPU time", LastRefreshCpuTime.TotalSeconds, "s", "Market Refresh");
                                RecordProgressBarMetric(nameof(CentralPerformanceMonitor), "RefreshMemoryUsage", "Refresh Memory Usage", "Refresh memory usage", LastRefreshMemoryUsage / (1024.0 * 1024.0), "MB", "Market Refresh", 0, 1000);

                                // Check for performance alerts
                                CheckPerformanceAlerts();
                            }

                            // Send comprehensive performance metrics to Overseer every minute
                            try
                            {
                                await SendPerformanceMetricsToOverseerAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error sending performance metrics to Overseer");
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

            using var serviceScope = _scopeFactory.CreateScope();
            var serviceFactory = serviceScope.ServiceProvider.GetRequiredService<IServiceFactory>();
            var kalshiWebSocketClient = serviceFactory.GetKalshiWebSocketClient();
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
            var highCountSamples = eventSamples.Count(s => s.Count >= _generalExecutionConfig.QueuesTargetCount);

            double percentage = (double)(highCountSamples * 100.0 / eventSamples.Count);

            // Clean up old samples after computation
            _queueCountSamples["EventQueue"] = eventSamples;

            return percentage;
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
        /// Resets all performance metrics.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            _databaseMetrics = null;
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
            if (queueHighPercentage >= _queueMonitoringConfig.QueueHighCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: EventQueue high count percentage {Percentage:F2}% exceeds threshold {Threshold:F2}%. System may be overloaded.",
                    queueHighPercentage, _queueMonitoringConfig.QueueHighCountAlertThreshold);
            }

            if (LastRefreshUsagePercentage >= _queueMonitoringConfig.RefreshUsageAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Refresh usage {Usage:F2}% exceeds threshold {Threshold:F2}%. Market refresh cycle may be too slow.",
                    LastRefreshUsagePercentage, _queueMonitoringConfig.RefreshUsageAlertThreshold);
            }

            var (eventQueueAvg, tickerQueueAvg, notificationQueueAvg, orderBookQueueAvg) = GetQueueCountRollingAverages();

            // Record queue metrics
            RecordCounterMetric(nameof(CentralPerformanceMonitor), "EventQueueAvg", "Event Queue Average", "Average event queue depth", eventQueueAvg, "items", "Queues");
            RecordCounterMetric(nameof(CentralPerformanceMonitor), "TickerQueueAvg", "Ticker Queue Average", "Average ticker queue depth", tickerQueueAvg, "items", "Queues");
            RecordCounterMetric(nameof(CentralPerformanceMonitor), "NotificationQueueAvg", "Notification Queue Average", "Average notification queue depth", notificationQueueAvg, "items", "Queues");
            RecordCounterMetric(nameof(CentralPerformanceMonitor), "OrderBookQueueAvg", "Order Book Queue Average", "Average order book queue depth", orderBookQueueAvg, "items", "Queues");
            RecordProgressBarMetric(nameof(CentralPerformanceMonitor), "QueueHighCountPercentage", "Queue High Count Percentage", "Percentage of time queue exceeds threshold", queueHighPercentage, "%", "Queues", 0, 100);

            if (eventQueueAvg >= _queueMonitoringConfig.QueueCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average EventQueue count {Avg:F2} exceeds threshold {Threshold}. Processing may be delayed.",
                    eventQueueAvg, _queueMonitoringConfig.QueueCountAlertThreshold);
            }
            if (tickerQueueAvg >= _queueMonitoringConfig.QueueCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average TickerQueue count {Avg:F2} exceeds threshold {Threshold}. Processing may be delayed.",
                    tickerQueueAvg, _queueMonitoringConfig.QueueCountAlertThreshold);
            }
            if (notificationQueueAvg >= _queueMonitoringConfig.QueueCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average NotificationQueue count {Avg:F2} exceeds threshold {Threshold}. Processing may be delayed.",
                    notificationQueueAvg, _queueMonitoringConfig.QueueCountAlertThreshold);
            }
            if (orderBookQueueAvg >= _queueMonitoringConfig.QueueCountAlertThreshold)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Average OrderBookQueue count {Avg:F2} exceeds threshold {Threshold}. Processing may be delayed.",
                    orderBookQueueAvg, _queueMonitoringConfig.QueueCountAlertThreshold);
            }
        }




        /// <summary>
        /// Gets all configurable performance metrics for GUI consumption.
        /// </summary>
        /// <returns>Dictionary containing all configurable metrics.</returns>
        public IReadOnlyDictionary<string, object> GetConfigurableMetrics()
        {
            return _configurableMetrics;
        }





        /// <summary>
        /// Collects and sends comprehensive performance metrics to the Overseer via SignalR.
        /// This method gathers all available performance data and transmits it for monitoring and analysis.
        /// </summary>
        /// <remarks>
        /// This method should be called periodically to send performance metrics to the Overseer.
        /// It collects data from all performance monitoring components including database operations,
        /// WebSocket metrics, message processing, and API execution times.
        /// </remarks>
        public async Task SendPerformanceMetricsToOverseerAsync()
        {
            try
            {
                // Create the performance metrics data structure
                var performanceMetrics = new PerformanceMetricsData
                {
                    BrainInstanceName = BrainInstance,
                    Timestamp = DateTime.UtcNow,

                    // Unified collection of all metrics from all sources
                    AllMetrics = base.RecordedMetrics.Select(m => new PerformanceMetricEntry
                    {
                        ClassName = m.ClassName,
                        Metric = (GeneralPerformanceMetric)m.Metric
                    }).ToList()








                };

                // Get the broadcast service and send the metrics
                using var serviceScope = _scopeFactory.CreateScope();
                var serviceFactory = serviceScope.ServiceProvider.GetRequiredService<IServiceFactory>();
                var overseerClientService = serviceFactory.GetOverseerClientService();
                if (!overseerClientService.IsConnected)
                {
                    _logger.LogDebug("No Overseer connected, skipping performance metrics broadcast.");
                    return;
                }
                var broadcastService = serviceFactory.GetBroadcastService() as BroadcastService;
                if (broadcastService != null)
                {
                    await broadcastService.BroadcastPerformanceMetricsAsync(performanceMetrics);
                    _logger.LogInformation("Sent unified performance metrics collection ({Count} metrics) to Overseer for brain {BrainInstance}", base.RecordedMetrics.Count, BrainInstance);
                }
                else
                {
                    _logger.LogDebug("BroadcastService not available - no Overseer connection detected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending performance metrics to Overseer");
            }
        }


    }
}
