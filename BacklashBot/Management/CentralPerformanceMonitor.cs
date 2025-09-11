// CentralPerformanceMonitor.cs
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs.Data;
using BacklashBot.State.Interfaces;
using System.Collections.Concurrent;
using TradingStrategies.Configuration;

namespace BacklashBot.Management
{
    public class CentralPerformanceMonitor : ICentralPerformanceMonitor
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

        private bool _timerRunning = false;
        private readonly ConcurrentDictionary<string, List<(DateTime Timestamp, int Count)>> _queueCountSamples;

        public double LastRefreshCycleSeconds { get; set; }
        public double LastRefreshCycleInterval { get; set; }
        public int LastRefreshMarketCount { get; set; }
        public double LastRefreshUsagePercentage { get; set; }
        public bool LastRefreshTimeAcceptable { get; set; }
        public DateTime? LastPerformanceSampleDate { get; set; }
        private readonly IScopeManagerService _scopeManagerService;
        private IStatusTrackerService _statusTrackerService;

        public bool IsStartingUp { get; set; } = false;
        public bool IsShuttingDown { get; set; } = false;

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
            _queueCountSamples = new ConcurrentDictionary<string, List<(DateTime Timestamp, int Count)>>();
        }

        public double CalculateAverageWebsocketEventsReceived(string marketTicker)
        {
            var dataCache = _serviceFactory.GetDataCache();
            if (dataCache == null || !dataCache.Markets.ContainsKey(marketTicker)) return 0.0;

            // Retrieve the last market open time from the data cache
            DateTime lastMarketOpenTime = dataCache.Markets[marketTicker].ChangeTracker.LastMarketOpenTime;

            // Retrieve the tuple of three integers representing WebSocket event counts
            var webSocketClient = _serviceFactory.GetKalshiWebSocketClient();
            if (webSocketClient == null) return 0.0;
            (int event1, int event2, int event3) events = webSocketClient.ReturnWebSocketCountsByMarket(marketTicker);

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

        public void RecordExecutionTime(string methodName, long milliseconds)
        {
            var record = (Timestamp: DateTime.UtcNow, Milliseconds: milliseconds);
            ApiExecutionTimes.AddOrUpdate(
                methodName,
                _ => new List<(DateTime Timestamp, long Milliseconds)> { record },
                (_, list) => { list.Add(record); return list; });
        }

        public async Task StartTimer()
        {
            if (_timerRunning) return;
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            BrainInstanceDTO? dto = await context.GetBrainInstance(BrainInstance ?? "");

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

                                _logger.LogDebug(
                                    "{Service} processing time: Elapsed={ElapsedSeconds:F2}s, Markets={MarketCount}, Usage={UsagePercentage:F2}%, Acceptable={IsAcceptable}",
                                    marketServiceName, LastRefreshCycleSeconds, LastRefreshMarketCount, LastRefreshUsagePercentage, LastRefreshTimeAcceptable);
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
    }
}