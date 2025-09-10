using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using BacklashBot.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using BacklashBot.State.Interfaces;
using TradingStrategies.Configuration;

namespace BacklashBot.Management
{
    public abstract class BaseMarketManagerService : IMarketManagerService
    {
        protected readonly IServiceFactory _serviceFactory;
        protected readonly IScopeManagerService _scopeManagerService;
        protected readonly ILogger<IMarketManagerService> _logger;
        protected readonly IServiceScopeFactory _scopeFactory;
        protected readonly IBrainStatusService _brainStatus;
        protected readonly ICentralPerformanceMonitor _performanceMonitor;
        protected List<string> MarketsToReset = new List<string>();
        protected List<string> MarketsToAddAfterReset = new List<string>();
        protected bool _recentMarketAdjustment = false;
        protected bool _firstWatchUpdate = true;
        protected readonly ExecutionConfig _executionConfig;
        protected readonly TradingConfig _tradingConfig;
        protected IStatusTrackerService _statusTrackerService;
        protected bool MonitoringWatchList = false;
        protected readonly object _resetLock = new();

        protected BaseMarketManagerService(IServiceFactory serviceFactory,
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
            lock (_resetLock)
            {
                MarketsToAddAfterReset.Clear();
                MarketsToReset.Clear();
            }
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
            lock (_resetLock)
            {
                MarketsToReset.Add(marketTicker);
            }
        }

        public abstract Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics metrics);

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

        protected async Task RefreshMarkets()
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

        protected async Task ResetMarkets()
        {
            List<string> marketsToAdd;
            lock (_resetLock)
            {
                marketsToAdd = MarketsToAddAfterReset.Distinct().ToList();
            }

            foreach (var market in marketsToAdd)
            {
                if (_statusTrackerService.GetCancellationToken().IsCancellationRequested)
                {
                    lock (_resetLock) { MarketsToAddAfterReset.Clear(); }
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
                    if (mkt != null && !KalshiConstants.MarketIsEnded(mkt.status))
                    {
                        _logger.LogInformation("Stats: Adding back {market} after reset, with status {status}", market, mkt.status);
                        await _serviceFactory.GetMarketDataService().AddMarketWatch(market);
                    }
                }
                else
                {
                    _logger.LogInformation("Stats: Skipped readding {Market} after reset because already watched. Watched: {Watched}",
                        market, string.Join(",", _serviceFactory.GetDataCache().WatchedMarkets));
                }
            }

            lock (_resetLock) { MarketsToAddAfterReset.Clear(); }

            List<string> marketsToReset;
            lock (_resetLock)
            {
                marketsToReset = MarketsToReset.Distinct().ToList();
            }

            foreach (var market in marketsToReset)
            {
                if (_statusTrackerService.GetCancellationToken().IsCancellationRequested)
                {
                    lock (_resetLock) { MarketsToReset.Clear(); }
                    return;
                }

                if (_serviceFactory.GetDataCache().WatchedMarkets.Contains(market))
                {
                    _logger.LogInformation("Stats: Removing {market} for reset", market);
                    await _serviceFactory.GetMarketDataService().UnwatchMarket(market);
                    lock (_resetLock) { MarketsToAddAfterReset.Add(market); }
                }
                lock (_resetLock) { MarketsToReset.RemoveAll(x => x == market); }
            }
        }

        protected async Task<int> RemoveLowestInterestMarkets(IKalshiBotContext context, IKalshiAPIService apiService, BrainInstanceDTO brain,
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

        protected async Task<int> RemoveEndedMarkets(IKalshiBotContext context)
        {
            int marketsRemoved = 0;

            var finalizedWatches = await context.GetFinalizedMarketWatches(_brainStatus.BrainLock);

            foreach (MarketWatchDTO watch in finalizedWatches)
            {
                if (!_serviceFactory.GetDataCache().WatchedMarkets.Contains(watch.market_ticker))
                {
                    _logger.LogInformation("Stats: Skipped removing {0} because it wasn't actually still being watched", watch.market_ticker);
                    watch.BrainLock = null;
                    await context.AddOrUpdateMarketWatch(watch);
                    continue;
                }
                _logger.LogInformation("Stats: Removing ended market {market}", watch.market_ticker);
                await _serviceFactory.GetMarketDataService().UnwatchMarket(watch.market_ticker);
                marketsRemoved = marketsRemoved + 1;

            }

            await context.RemoveMarketWatches(finalizedWatches);
            return marketsRemoved;
        }

        protected async Task<List<string>> AddHighInterestMarkets(IKalshiBotContext context, IKalshiAPIService apiService, int marketsToAddCount, double minimumInterest)
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

                int marketsToAdd = Math.Min(marketsToAddCount - marketsAdded, _executionConfig.MaxMarketsPerSubscriptionAction);
                if (marketsToAdd > 0)
                {
                    List<MarketDTO> candidates = await context.GetMarkets(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active },
                        hasMarketWatch: false);
                    List<string> candidateMarkets = candidates.OrderByDescending(x => x.volume_24h).Take(Math.Max(marketsToAdd * 2, 20)).Select(x => x.market_ticker).ToList();

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

                            if (existingDoubleCheck.Count == 0) // Strict check
                            {
                                MarketWatchDTO newWatch = new MarketWatchDTO
                                {
                                    market_ticker = market.Ticker,
                                    InterestScore = market.Score,
                                    InterestScoreDate = DateTime.Now
                                };

                                if (market.Score >= minimumInterest)
                                {
                                    newWatch.BrainLock = _brainStatus.BrainLock;
                                    newWatch.LastWatched = DateTime.Now;
                                }

                                await context.AddOrUpdateMarketWatch(newWatch);

                                if (market.Score < minimumInterest)
                                {
                                    _logger.LogInformation("BRAIN: Added market {market} to market watches but score too low to watch. Interest: {score}", market.Ticker, market.Score);
                                    continue;
                                }

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

        protected async Task<int> RemoveUninterestingMarkets(IKalshiBotContext context, IKalshiAPIService apiService, BrainInstanceDTO brain, double minimumInterest)
        {
            int removed = 0;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var token = cts.Token;

                var candidates = await context.GetMarketWatches_cached(
                    brainLocksIncluded: new HashSet<Guid> { _brainStatus.BrainLock },
                    maxInterestScore: minimumInterest
                );

                var interestScoreCutoff = DateTime.Now.AddHours(-3);
                var tickersToScore = candidates
                    .Where(x => x.InterestScore == null || x.InterestScoreDate <= interestScoreCutoff)
                    .Select(x => x.market_ticker)
                    .Distinct()
                    .ToList();

                if (tickersToScore.Any())
                {
                    _logger.LogDebug("API: Getting market interest score for {@Tickers} in {Context}", tickersToScore, "RemoveUninterestingMarkets");
                    var scores = await _serviceFactory.GetMarketInterestScoreHelper()
                        .GetMarketInterestScores(_scopeFactory, tickersToScore);

                    foreach (var w in candidates)
                    {
                        var s = scores.FirstOrDefault(x => x.Ticker == w.market_ticker);
                        if (s.Ticker != null)
                        {
                            w.InterestScore = s.Score;
                            w.InterestScoreDate = DateTime.Now;
                        }
                    }
                }

                if (brain.WatchPositions || brain.WatchOrders)
                {
                    var marketPositions = await context.GetMarketPositions_cached(
                        marketTickers: candidates.Select(x => x.market_ticker).ToHashSet());

                    if (brain.WatchPositions)
                    {
                        var protectedByPosition = marketPositions
                            .Where(p => p.Position != 0)
                            .Select(p => p.Ticker)
                            .ToHashSet();
                        candidates = candidates.Where(x => !protectedByPosition.Contains(x.market_ticker)).ToHashSet();
                    }

                    if (brain.WatchOrders)
                    {
                        var protectedByOrders = marketPositions
                            .Where(p => p.RestingOrdersCount != 0)
                            .Select(p => p.Ticker)
                            .ToHashSet();
                        candidates = candidates.Where(x => !protectedByOrders.Contains(x.market_ticker)).ToHashSet();
                    }
                }

                foreach (var watch in candidates)
                {
                    if ((watch.InterestScore ?? double.MinValue) <= minimumInterest)
                    {
                        _logger.LogInformation("Stats: Removing market {MarketTicker} due to low interest. Interest={InterestScore}",
                            watch.market_ticker, watch.InterestScore);
                        await _serviceFactory.GetMarketDataService().UnwatchMarket(watch.market_ticker);
                        removed++;
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
            return removed;
        }
    }
}