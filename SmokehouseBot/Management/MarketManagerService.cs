using KalshiBotData.Data.Interfaces;
using KalshiBotData.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmokehouseBot.Configuration;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using SmokehouseInterfaces.Constants;
using TradingStrategies.Configuration;

namespace SmokehouseBot.Management
{
    public class MarketManagerService : IMarketManagerService
    {
        private readonly IServiceFactory _serviceFactory;
        private readonly IScopeManagerService _scopeManagerService;
        private readonly ILogger<IMarketManagerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBrainStatusService _brainStatus;
        private readonly ICentralPerformanceMonitor _performanceMonitor;
        private List<string> MarketsToReset = new List<string>();
        private List<string> MarketsToAddAfterReset = new List<string>();
        private bool _recentMarketAdjustment = false;
        private bool _firstWatchUpdate = true;
        private readonly ExecutionConfig _executionConfig;
        private readonly TradingConfig _tradingConfig;
        private IStatusTrackerService _statusTrackerService;
        private bool MonitoringWatchList = false;

        public MarketManagerService(IServiceFactory serviceFactory,
            ILogger<IMarketManagerService> logger,
            IServiceScopeFactory scopeFactory,
            ICentralPerformanceMonitor performanceMonitor,
            IOptions<ExecutionConfig> executionConfig,
            IOptions<TradingConfig> tradingConfig,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService,
            IBrainStatusService brainStatus)
        {
            _serviceFactory = serviceFactory;
            _scopeManagerService = scopeManagerService;
            _statusTrackerService = statusTrackerService;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _tradingConfig = tradingConfig.Value;
            _executionConfig = executionConfig.Value;
            _performanceMonitor = performanceMonitor;
            _brainStatus = brainStatus;
        }

        public void ClearMarketsToReset()
        {
            MarketsToAddAfterReset.Clear();
            MarketsToReset.Clear();
        }

        public async Task HandleMarketResets()
        {
            try
            {
                await RefreshMarkets();
                await ResetMarkets();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HandleMarketResets was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to handle market resets: {Message}", ex.Message);
            }
        }

        public void TriggerMarketReset(string marketTicker)
        {
            MarketsToReset.Add(marketTicker);
        }

        public async Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics metrics)
        {
            if (MonitoringWatchList) return;
            MonitoringWatchList = true;
            try
            {
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                int removedCount = await RemoveEndedMarkets(context);
                if (removedCount > 0)
                    _logger.LogInformation("BRAIN: Removed {count} markets due to them being ended.", removedCount);

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var token = cts.Token;
                if (_performanceMonitor.LastPerformanceSampleDate == null)
                {
                    _logger.LogInformation("Stats: No performance metric available for MarketRefreshService.");
                    MonitoringWatchList = false;
                    return;
                }

                var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                var marketDataService = _serviceFactory.GetMarketDataService();

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                var watchedMarkets = await marketDataService.FetchWatchedMarketsAsync();
                int actualMarketCount = watchedMarkets.Count();

                double maxDif = watchedMarkets.Count() * .05;

                if ((Math.Abs(metrics.CurrentCount - watchedMarkets.Count()) <= maxDif) && metrics.CurrentUsage > 0)
                {
                    _recentMarketAdjustment = false;
                    _logger.LogDebug("Stats: Markets settled, reset adjustment flag.");
                }

                if (_recentMarketAdjustment
                    || (Math.Abs(metrics.CurrentCount - watchedMarkets.Count()) > maxDif)
                    || MarketsToAddAfterReset.Count() > 0)
                {
                    _logger.LogDebug("Stats: Waiting for markets to settle. Percentage={percentage:F2}%, CurrentCount={currentCount}, ActualCount={actualCount}, Refresh Usage: {usage}, Queue Usage {queue}, Markets to add after reset: {0}, Recent Adjustment? {1}",
                        metrics.CurrentUsage, metrics.CurrentCount, actualMarketCount, Math.Round(metrics.CurrentUsage, 2), _performanceMonitor.GetQueueHighCountPercentage(), MarketsToAddAfterReset.Count(), _recentMarketAdjustment);
                    MonitoringWatchList = false;
                    return;
                }

                int actualTarget;

                if (brain.ManagedWatchList)
                {
                    actualTarget = CalculateTarget(metrics, brain);
                    brain.TargetWatches = actualTarget;
                }
                else
                {
                    actualTarget = brain.TargetWatches;
                    brain.TargetWatches = actualTarget;
                }
                await context.AddOrUpdateBrainInstance(brain);

                _recentMarketAdjustment = true;
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                if (actualTarget < actualMarketCount && (!brain.ManagedWatchList || metrics.CurrentUsage > brain.UsageMax))
                {
                    _logger.LogInformation("BRAIN: Usage is too high - attempting to remove up to {0} markets.", actualMarketCount - actualTarget);

                    int removed = await RemoveLowestInterestMarkets(context, apiService, brain, actualMarketCount - actualTarget, token);
                    _logger.LogInformation("BRAIN: Usage too high: {percentage:F2}%. Removed {count} markets to target {actualTarget} markets",
                        metrics.CurrentUsage, removed, actualTarget);
                }
                else if (actualTarget > actualMarketCount && (!brain.ManagedWatchList || metrics.CurrentUsage < brain.UsageMin))
                {
                    BrainInstanceDTO dto = await context.GetBrainInstance(_executionConfig.BrainInstance);
                    _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                    List<string> addedMarkets = await AddHighInterestMarkets(context, apiService, actualTarget - actualMarketCount, dto.MinimumInterest);
                    if (addedMarkets.Count > 0 && _serviceFactory.GetDataCache().WatchedMarkets != null)
                    {
                        _logger.LogInformation("BRAIN: Usage too low: {percentage:F2}% with {actualMarketCount} markets. Added up to {count} markets.",
                            metrics.CurrentUsage, actualMarketCount, actualTarget - actualMarketCount);
                        foreach (var ticker in addedMarkets)
                        {
                            _logger.LogInformation("BRAIN: Added {market} to watch list during Monitor Watch List", ticker);
                        }
                    }
                }
                else if (brain.ManagedWatchList)
                {
                    BrainInstanceDTO? dto = await context.GetBrainInstance(_executionConfig.BrainInstance);
                    _logger.LogInformation("BRAIN: Usage within acceptable range: {percentage:F2}%. Checking for uninteresting markets.",
                        metrics.CurrentUsage);
                    await RemoveUninterestingMarkets(context, apiService, brain, dto.MinimumInterest);
                }

                _firstWatchUpdate = false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MonitorWatchList was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to monitor watch list");
            }
            MonitoringWatchList = false;
        }

        public int CalculateTarget(PerformanceMetrics metrics, BrainInstanceDTO brain)
        {
            const int MaxValidValue = int.MaxValue;
            const int MinValidValue = 0; // Assuming non-negative targets

            // Helper function to calculate target and validate result
            int CalculateAndValidateTarget(double limit, double avg, int count)
            {
                if (count == 0 || avg == 0) return MaxValidValue; // Skip invalid cases
                double perEachUsage = avg / count;
                if (perEachUsage == 0) return MaxValidValue; // Avoid division by zero
                double result = limit / perEachUsage;
                int target = (int)Math.Floor(result);
                if (target <= MinValidValue || target == int.MinValue) return MaxValidValue; // Skip overflow/invalid
                return target;
            }

            // Target count by usage
            int targetCountUsage = CalculateAndValidateTarget(brain.UsageTarget, metrics.CurrentUsage, metrics.CurrentCount);

            // Target count by Notification Queue
            int notificationQueueLimit = 50;
            int targetCountNotificationQueue = CalculateAndValidateTarget(notificationQueueLimit, metrics.NotificationQueueAvg, metrics.CurrentCount);

            // Target count by Orderbook Queue
            int orderbookQueueLimit = 50;
            int targetCountOrderbookQueue = CalculateAndValidateTarget(orderbookQueueLimit, metrics.OrderbookQueueAvg, metrics.CurrentCount);

            // Target count by Event Queue
            int eventQueueLimit = 50;
            int targetCountEventQueue = CalculateAndValidateTarget(eventQueueLimit, metrics.EventQueueAvg, metrics.CurrentCount);

            // Target count by Ticker Queue
            int tickerQueueLimit = 50;
            int targetCountTickerQueue = CalculateAndValidateTarget(tickerQueueLimit, metrics.TickerQueueAvg, metrics.CurrentCount);

            // Collect valid targets
            var validTargets = new List<int>();
            if (targetCountUsage < MaxValidValue) validTargets.Add(targetCountUsage);
            if (targetCountNotificationQueue < MaxValidValue) validTargets.Add(targetCountNotificationQueue);
            if (targetCountOrderbookQueue < MaxValidValue) validTargets.Add(targetCountOrderbookQueue);
            if (targetCountEventQueue < MaxValidValue) validTargets.Add(targetCountEventQueue);
            if (targetCountTickerQueue < MaxValidValue) validTargets.Add(targetCountTickerQueue);

            // Final target: Use minimum of valid targets, or start with 10 if none are valid
            int actualTarget = validTargets.Any() ? validTargets.Min() : 10;

            _logger.LogInformation("Final target: Usage={Usage}, Notification={Notification}, Orderbook={Orderbook}, Event={Event}, Ticker={Ticker}, Selected={Selected}",
                targetCountUsage, targetCountNotificationQueue, targetCountOrderbookQueue, targetCountEventQueue, targetCountTickerQueue, actualTarget);

            return actualTarget;
        }


        private async Task RefreshMarkets()
        {
            using var scope = _scopeFactory.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
            var marketsToRefresh = _serviceFactory.GetMarketDataService().MarketsToRefresh.Distinct().ToList();

            foreach (var market in marketsToRefresh)
            {
                if (_statusTrackerService.GetCancellationToken().IsCancellationRequested)
                {
                    _serviceFactory.GetMarketDataService().MarketsToRefresh.Clear();
                    break;
                }
                _logger.LogDebug("API: refreshing market {0}... refetching from API", market);
                await apiService.FetchMarketsAsync(tickers: new string[] { market });
                _logger.LogDebug("BRAIN: Resetting market {0} due to RefreshMarkets", market);
                TriggerMarketReset(market);
                _serviceFactory.GetMarketDataService().MarketsToRefresh.RemoveAll(x => x == market);
            }
        }

        private async Task ResetMarkets()
        {
            var marketsToAdd = MarketsToAddAfterReset.Distinct().ToList();
            foreach (var market in marketsToAdd)
            {
                if (_statusTrackerService.GetCancellationToken().IsCancellationRequested)
                {
                    MarketsToAddAfterReset.Clear();
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                await apiService.FetchMarketsAsync(tickers: new string[] { market });

                if (!_serviceFactory.GetDataCache().WatchedMarkets.Contains(market))
                {
                    List<MarketDTO> mkts = await context.GetMarkets(includedMarkets: new HashSet<string>() { market });
                    MarketDTO? mkt = mkts.FirstOrDefault();
                    if (mkt != null)
                    {
                        if (!KalshiConstants.MarketIsEnded(mkt.status))
                        {
                            _logger.LogInformation("Stats: Adding back {market} after reset, with status {status}", market, mkt.status);
                            await _serviceFactory.GetMarketDataService().AddMarketWatch(market);
                        }
                    }
                    
                }
                else
                {
                    _logger.LogInformation("Stats: Skipped readding {0} after reset because already watched. Watched: {0}", market,
                        String.Join(",",_serviceFactory.GetDataCache().WatchedMarkets));
                }
            }

            MarketsToAddAfterReset.Clear();

            var marketsToReset = MarketsToReset.Distinct().ToList();
            foreach (var market in marketsToReset)
            {
                if (_statusTrackerService.GetCancellationToken().IsCancellationRequested)
                {
                    MarketsToReset.Clear();
                    return;
                }
                if (_serviceFactory.GetDataCache().WatchedMarkets.Contains(market))
                {
                    _logger.LogInformation("Stats: Removing {market} for reset", market);
                    await _serviceFactory.GetMarketDataService().UnwatchMarket(market);
                    MarketsToAddAfterReset.Add(market);
                    
                }
                MarketsToReset.RemoveAll(x => x == market);
            }

        }

        private async Task<int> RemoveLowestInterestMarkets(IKalshiBotContext context, IKalshiAPIService apiService, BrainInstanceDTO brain,
            int marketsToRemoveCount, CancellationToken token)
        {
            int removed = 0;
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                var myWatches = await context.GetMarketWatches_cached(brainLocksIncluded: new HashSet<Guid>() { _brainStatus.BrainLock });

                _logger.LogInformation("BRAIN: Found {0} markets to consider for removal.", myWatches.Count());

                if (myWatches.Count() == 0) return 0;

                foreach (MarketWatchDTO mw in myWatches)
                {
                    if (mw.InterestScore == null)
                    {
                        var score = await _serviceFactory.GetMarketInterestScoreHelper().CalculateMarketInterestScoreAsync(context, mw.market_ticker);
                        mw.InterestScore = score.score;
                        mw.InterestScoreDate = DateTime.Now;
                        await context.AddOrUpdateMarketWatch(mw);
                    }
                }

                var myMarketPositions = await context.GetMarketPositions_cached(myWatches.Select(x => x.market_ticker).ToHashSet());

                List<MarketWatchDTO> marketsToRemove = myWatches.OrderBy(m => m.InterestScore).ToList();

                if (brain.WatchPositions)
                {
                    marketsToRemove.RemoveAll(x => myMarketPositions.Where(x => x.Position != 0).Select(x => x.Ticker).ToList()
                    .Contains(x.market_ticker));
                }
                if (brain.WatchOrders)
                {
                    marketsToRemove.RemoveAll(x => myMarketPositions.Where(x => x.RestingOrdersCount != 0).Select(x => x.Ticker).ToList()
                    .Contains(x.market_ticker));
                }

                var marketsToRemoveTickers = marketsToRemove.Take(marketsToRemoveCount).Select(x => x.market_ticker).ToList();

                _logger.LogInformation("BRAIN: Found {0} markets to remove.", marketsToRemove.Count());
                foreach (var ticker in marketsToRemoveTickers)
                {
                    await marketDataService.UnwatchMarket(ticker);
                    removed++;
                    _logger.LogDebug("Stats: Removed market {MarketTicker}.", ticker);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("RemoveLowInterestMarkets was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove low interest markets");
            }
            return removed;
        }

        private async Task<int> RemoveEndedMarkets(IKalshiBotContext context)
        {
            int marketsRemoved = 0;

            var finalizedWatches = await context.GetFinalizedMarketWatches(_brainStatus.BrainLock);

            foreach (MarketWatchDTO watch in finalizedWatches)
            {
                if (_serviceFactory.GetDataCache().WatchedMarkets.Contains(watch.market_ticker))
                {
                    _logger.LogInformation("Stats: Skipped removing {0} because it wasn't actually still being watched", watch.market_ticker);
                    watch.BrainLock = null;
                    await context.AddOrUpdateMarketWatch(watch);
                    continue;
                }
                _logger.LogInformation("Stats: Removing ended market {market}", watch.market_ticker);
                await _serviceFactory.GetMarketDataService().UnwatchMarket(watch.market_ticker);
                marketsRemoved = marketsRemoved - 1;

            }
            await context.RemoveMarketWatches(finalizedWatches);


            return marketsRemoved;
        }

        private async Task<List<string>> AddHighInterestMarkets(IKalshiBotContext context, IKalshiAPIService apiService, int marketsToAddCount, double minimumInterest)
        {
            if (marketsToAddCount == 0) return new List<string>();
            int marketsAdded = 0;
            List<string> marketsAddedList = new List<string>();
            try
            {
                var allMarketWatches = await context.GetMarketWatches(brainLockIsNull: true);

                // Prioritize existing high-interest markets
                List<MarketWatchDTO> newMarketWatches = allMarketWatches
                    .Where(x => (x.InterestScore >= minimumInterest
                    || x.InterestScore == null) && !MarketsToAddAfterReset.Contains(x.market_ticker)
                    && !MarketsToReset.Contains(x.market_ticker))
                    .Take(50)
                    .ToList();
                if (newMarketWatches.Any())
                {
                    _logger.LogDebug("API: Getting market interest score for {0} in {1}", newMarketWatches.Select(x => x.market_ticker).ToList(), "AddHighInterestMarkets");
                    var marketScores = await _serviceFactory.GetMarketInterestScoreHelper().GetMarketInterestScores(_scopeFactory, newMarketWatches.Select(x => x.market_ticker).ToList());

                    foreach (var watch in newMarketWatches.ToList())
                    {
                        if (marketsAdded >= marketsToAddCount) break;

                        double score = marketScores.FirstOrDefault(x => x.Ticker == watch.market_ticker).Score;
                        watch.InterestScore = score;
                        watch.InterestScoreDate = DateTime.Now;
                        if (score >= minimumInterest)
                        {
                            watch.BrainLock = _brainStatus.BrainLock;
                            watch.LastWatched = DateTime.Now;
                            await context.AddOrUpdateMarketWatch(watch);
                            await _serviceFactory.GetMarketDataService().AddMarketWatch(watch.market_ticker);
                            _logger.LogDebug("BRAIN: Locked existing high-interest market {MarketTicker}. Interest: {interest}", watch.market_ticker, watch.InterestScore);
                            marketsAdded++;
                            marketsAddedList.Add(watch.market_ticker);
                            _serviceFactory.GetDataCache().WatchedMarkets.Add(watch.market_ticker);
                        }
                        else
                        {
                            await context.AddOrUpdateMarketWatch(watch);
                        }

                    }
                }

                int marketsToAdd = marketsToAddCount - marketsAdded;
                if (marketsToAdd > 0)
                {
                    List<MarketDTO> candidates = await context.GetMarkets(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active },
                        hasMarketWatch: false);
                    List<string> candidateMarkets = candidates.OrderByDescending(x => x.volume_24h).Take(marketsToAdd).Select(x => x.market_ticker).ToList();

                    if (candidateMarkets.Any())
                    {
                        _logger.LogDebug("API: Getting market interest score for {0} in {1}", candidateMarkets, "AddHighInterestMarkets2");
                        var marketScores = await _serviceFactory.GetMarketInterestScoreHelper().GetMarketInterestScores(_scopeFactory, candidateMarkets);

                        foreach (var market in marketScores)
                        {
                            if (marketsAdded >= marketsToAddCount) break;

                            var existingWatch = newMarketWatches.FirstOrDefault(x => x.market_ticker == market.Ticker);
                            if (existingWatch != null)
                            {
                                existingWatch.InterestScore = market.Score;
                                existingWatch.InterestScoreDate = DateTime.Now;
                                if (existingWatch.BrainLock == null && market.Score >= minimumInterest)
                                {
                                    existingWatch.BrainLock = _brainStatus.BrainLock;
                                    existingWatch.LastWatched = DateTime.Now;
                                    await context.AddOrUpdateMarketWatch(existingWatch);
                                    marketsAdded++;
                                    marketsAddedList.Add(existingWatch.market_ticker);
                                    _serviceFactory.GetDataCache().WatchedMarkets.Add(existingWatch.market_ticker);
                                    _logger.LogInformation("BRAIN: Locked existing market {MarketTicker} after score update. Interest: {score}", market.Ticker, existingWatch.InterestScore);
                                }
                                else
                                {
                                    await context.AddOrUpdateMarketWatch(existingWatch);
                                    _logger.LogInformation("BRAIN: Updated score for existing market {MarketTicker}. Interest: {score}", market.Ticker, existingWatch.InterestScore);
                                }
                                continue;
                            }

                            var existingDoubleCheck = await context.GetMarketWatches_cached(marketTickers: new HashSet<string> { market.Ticker });

                            if (existingDoubleCheck == null && market.Score >= minimumInterest) // Strict check
                            {
                                MarketWatchDTO newWatch = new MarketWatchDTO
                                {
                                    market_ticker = market.Ticker,
                                    InterestScore = market.Score,
                                    InterestScoreDate = DateTime.Now,
                                    BrainLock = _brainStatus.BrainLock,
                                    LastWatched = DateTime.Now
                                };

                                await context.AddOrUpdateMarketWatch(newWatch);

                                marketsAddedList.Add(market.Ticker);
                                _serviceFactory.GetDataCache().WatchedMarkets.Add(newWatch.market_ticker);
                                marketsAdded++;
                                _logger.LogInformation("BRAIN: Added and locked market {MarketTicker} due to high interest. Interest: {score}", market.Ticker, market.Score);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("AddHighInterestMarkets canceled adding high interest markets");
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogWarning("SqlException while adding high interest markets. Message: {0}, stack {1}", ex.Message, ex.StackTrace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add high interest markets");
            }
            return marketsAddedList;
        }

        private async Task RemoveUninterestingMarkets(IKalshiBotContext context, IKalshiAPIService apiService, BrainInstanceDTO brain, double minimumInterest)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var token = cts.Token;
                var marketDataService = _serviceFactory.GetMarketDataService();
                var marketsToRemove = await context.GetMarketWatches_cached(
                    brainLocksIncluded: new HashSet<Guid>() { _brainStatus.BrainLock },
                    maxInterestScore: minimumInterest
                    );

                var interestScoreCutoff = DateTime.Now.AddHours(-3);
                var tickersToScore = marketsToRemove
                    .Where(x => x.InterestScore == null || x.InterestScoreDate <= interestScoreCutoff)
                    .Select(x => x.market_ticker)
                    .Distinct()
                    .ToList();

                _logger.LogDebug("API: Getting market interest score for {0} in {1}", tickersToScore, "RemoveUninterestingMarkets");
                var marketScores = tickersToScore.Any()
                    ? await _serviceFactory.GetMarketInterestScoreHelper().GetMarketInterestScores(_scopeFactory, tickersToScore)
                    : new List<(string Ticker, double Score)>();

                foreach (var watch in marketsToRemove.ToList())
                {

                    var score = marketScores.FirstOrDefault(x => x.Ticker == watch.market_ticker);
                    if (score.Ticker != null)
                    {
                        watch.InterestScore = score.Score;
                        watch.InterestScoreDate = DateTime.Now;
                    }
                }

                // Ensure that if these are toggled on, they don't get unwatched
                if (brain.WatchPositions || brain.WatchOrders)
                {
                    var MarketPositions = await context.GetMarketPositions_cached(
                        marketTickers: marketsToRemove.Select(x => x.market_ticker).ToHashSet());

                    if (brain.WatchPositions)
                    {
                        marketsToRemove.RemoveWhere(x => MarketPositions.Where(x => x.Position != 0)
                            .Select(x => x.Ticker).Contains(x.market_ticker));
                    }

                    if (brain.WatchOrders)
                    {
                        marketsToRemove.RemoveWhere(x => MarketPositions.Where(x => x.RestingOrdersCount != 0)
                            .Select(x => x.Ticker).Contains(x.market_ticker));
                    }
                }

                foreach (var watch in marketsToRemove.ToList())
                {
                    if (watch.InterestScore > minimumInterest)
                    {
                        watch.LastWatched = DateTime.Now;
                    }
                    else
                    {
                        _logger.LogInformation("Stats: Removing market {MarketTicker} due to low interest. {0}", watch.market_ticker, watch.InterestScore);
                        await _serviceFactory.GetMarketDataService().UnwatchMarket(watch.market_ticker);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("RemoveUninterestingMarkets was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove uninteresting markets");
            }
        }

    }
}
