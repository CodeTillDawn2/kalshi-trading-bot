using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Options;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs.Exceptions;
using System.Diagnostics;
using TradingStrategies.Configuration;

namespace SmokehouseBot.Services
{
    public class MarketRefreshService : IMarketRefreshService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceFactory _serviceFactory;
        private readonly IStatusTrackerService _statusTracker;
        private readonly ILogger<IMarketRefreshService> _logger;
        private readonly TimeSpan _updateInterval;
        private readonly TradingConfig _tradingConfig;
        private bool _hubIsReady;
        private DateTime? _lastRefreshTime;
        private bool _hasRunInitialRefresh;
        private Task _executeTask;
        public event EventHandler<string> OnMarketUpdated;
        public TimeSpan LastWorkDuration { get; private set; }
        public int LastWorkMarketCount { get; private set; }
        private TimeSpan RefreshInterval => _updateInterval;
        private readonly IScopeManagerService _scopeManagerService;

        public MarketRefreshService(
            IServiceScopeFactory scopeFactory,
            ILogger<IMarketRefreshService> logger,
            IOptions<TradingConfig> tradingConfig,
            IServiceFactory serviceFactory,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService
            )
        {
            _logger = logger;
            _scopeManagerService = scopeManagerService;
            _serviceFactory = serviceFactory;
            _statusTracker = statusTrackerService;
            _scopeFactory = scopeFactory;
            _tradingConfig = tradingConfig.Value;
            _updateInterval = TimeSpan.FromMinutes(_tradingConfig.RefreshIntervalMinutes);
            _hasRunInitialRefresh = false;
        }

        public void ExecuteServicesAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogDebug("MarketRefreshService ExecuteAsync started...");
                _executeTask = Task.Run(async () =>
                {
                    //bool isFirstExecution = true;
                    while (!_statusTracker.GetCancellationToken().IsCancellationRequested)
                    {
                        try
                        {
                            bool isTradingActive = _serviceFactory.GetMarketDataService().GetTradingStatus();
                            _logger.LogDebug("Trading status: {Status}", isTradingActive);

                            if (!isTradingActive)
                            {
                                _logger.LogInformation("Trading is inactive, waiting for trading to become active.");
                                await Task.Delay(TimeSpan.FromMinutes(1), _statusTracker.GetCancellationToken());
                                _serviceFactory.GetTradingSnapshotService().NextExpectedSnapshotTimestamp = DateTime.UtcNow.AddMinutes(1);
                                continue;
                            }

                            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                            _logger.LogDebug("Trading is active, proceeding with market refresh.");
                            var workStartTime = DateTime.UtcNow;
                            await RefreshAllWatchedMarketsAsync();
                            var workDuration = DateTime.UtcNow - workStartTime;

                            _lastRefreshTime = DateTime.UtcNow;
                            _hasRunInitialRefresh = true;

                            var timeSinceLastRefresh = _lastRefreshTime.Value - workStartTime;
                            var remainingDelay = _updateInterval - timeSinceLastRefresh;
                            if (remainingDelay > TimeSpan.Zero)
                            {
                                _logger.LogDebug("Waiting {RemainingSeconds:F2}s before next refresh cycle.", remainingDelay.TotalSeconds);
                                await Task.Delay(remainingDelay, _statusTracker.GetCancellationToken());
                            }
                            else
                            {
                                _logger.LogWarning("Work duration exceeded interval, starting next cycle immediately. Time: {0}", workDuration.TotalSeconds);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogDebug("MarketRefreshService stopped due to cancellation.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during periodic market refresh");
                            await Task.Delay(TimeSpan.FromSeconds(10), _statusTracker.GetCancellationToken());
                        }
                    }
                    _logger.LogDebug("MarketRefreshService ExecuteAsync stopped.");
                }, _statusTracker.GetCancellationToken());
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("PopulateMarketDataAsync was cancelled");
            }
        }

        public async Task TriggerImmediateRefreshAsync(string marketTicker)
        {
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                _logger.LogInformation("Triggering immediate refresh for {MarketTicker}", marketTicker);
                await _serviceFactory.GetMarketDataService().SyncMarketDataAsync(marketTicker);
                if (OnMarketUpdated != null)
                    OnMarketUpdated?.Invoke(this, marketTicker);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("TriggerImmediateRefreshAsync was cancelled for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger immediate refresh for {MarketTicker}", marketTicker);
            }
        }


        public bool IsRunning()
        {
            return _executeTask != null && !_executeTask.IsCompleted;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MarketRefreshService stopping...");
            try
            {
                if (_executeTask != null && !_executeTask.IsCompleted)
                {
                    _logger.LogDebug("Waiting for ExecuteAsync to complete...");
                    await _executeTask.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MarketRefreshService cancellation completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping MarketRefreshService");
            }
            finally
            {
                _logger.LogDebug("MarketRefreshService stopped.");
            }
        }

        private async Task RefreshAllWatchedMarketsAsync()
        {
            _logger.LogDebug("Updating all watched markets...");

            var watchedMarkets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
            _logger.LogInformation("Processing {MarketCount} watched markets.", watchedMarkets.Count);

            var workStartTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            int MarketsRefreshed = 0;

            foreach (var marketTicker in watchedMarkets)
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                var marketData = _serviceFactory.GetMarketDataService().GetMarketDetails(marketTicker);
                if (marketData == null)
                {
                    _logger.LogDebug("Market {MarketTicker} not found in cache, attempting to load", marketTicker);
                    var freshWatched = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                    if (freshWatched.Contains(marketTicker))
                    {
                        _logger.LogDebug("Market {MarketTicker} confirmed currently watched", marketTicker);
                        await _serviceFactory.GetMarketDataService().AddMarketWatch(marketTicker);
                        marketData = _serviceFactory.GetMarketDataService().GetMarketDetails(marketTicker);
                        if (marketData == null)
                        {
                            _logger.LogWarning(new NotInCacheException(marketTicker, $"Market {marketTicker} not found in cache after loading, skipping update"),
                                "Market {MarketTicker} not found in cache after loading, skipping update", marketTicker);
                            continue;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Market {MarketTicker} not found in watch list, must have been removed.", marketTicker);
                        continue;
                    }
                }

                // Determine if a sync is needed
                var lastTicker = marketData.Tickers.OrderByDescending(t => t.LoggedDate).FirstOrDefault();
                bool needsSync =
                    lastTicker == null
                    || marketData.LastSuccessfulSync <= DateTime.UtcNow.AddMinutes(-31)
                    || (lastTicker.LoggedDate > marketData.LastSuccessfulSync && (DateTime.UtcNow - lastTicker.LoggedDate) >= TimeSpan.FromMinutes(5));

                if (needsSync)
                {
                    await _serviceFactory.GetMarketDataService().SyncMarketDataAsync(marketTicker);
                    marketData.LastSuccessfulSync = DateTime.UtcNow;

                    OnMarketUpdated?.Invoke(this, marketTicker);
                    _logger.LogInformation("Refresh - {Market} refreshed successfully.", marketTicker);
                    MarketsRefreshed++; // FIX: was MarketsRefreshed = MarketsRefreshed++;
                }
                else
                {
                    _logger.LogInformation("Refresh - No market refresh needed for {Market}. Last ticker: {Last}, Last Sync {Sync}",
                        marketTicker, lastTicker?.LoggedDate, marketData.LastSuccessfulSync);
                }
            }

            _logger.LogInformation("Refresh - Refreshed {Refreshed} markets in {Minutes} minutes",
                $"{MarketsRefreshed}/{watchedMarkets.Count}", Math.Round(stopwatch.Elapsed.TotalMinutes, 2));

            // --- Additional pass: only if <25% refreshed, and stay within a time budget ---
            if (watchedMarkets.Count > 0)
            {
                double refreshRatio = (double)MarketsRefreshed / watchedMarkets.Count; // FIX: ensure double division
                if (refreshRatio < 0.25)
                {
                    int targetForceTotal = (int)Math.Round(watchedMarkets.Count * 0.25);
                    int remainingToForce = Math.Max(0, targetForceTotal - MarketsRefreshed);
                    int ForcedRefreshCount = 0;

                    // Simple time budget: stop forced pass if ≥60% of the interval has elapsed
                    TimeSpan forcedBudget = TimeSpan.FromTicks((long)(_updateInterval.Ticks * 0.60));

                    foreach (var marketTicker in ShuffleList(watchedMarkets))
                    {
                        if (ForcedRefreshCount >= remainingToForce) break;
                        if (stopwatch.Elapsed >= forcedBudget) { _logger.LogDebug("Forced pass stopped due to time budget."); break; }

                        _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                        var marketData = _serviceFactory.GetMarketDataService().GetMarketDetails(marketTicker);
                        if (marketData == null) continue;

                        var lastTicker = marketData.Tickers.OrderByDescending(t => t.LoggedDate).FirstOrDefault();
                        bool needsForce =
                            (lastTicker == null || marketData.LastSuccessfulSync <= DateTime.UtcNow.AddMinutes(-15))
                            && (lastTicker == null || (DateTime.UtcNow - lastTicker.LoggedDate) >= TimeSpan.FromMinutes(5));

                        if (needsForce)
                        {
                            await _serviceFactory.GetMarketDataService().SyncMarketDataAsync(marketTicker);
                            marketData.LastSuccessfulSync = DateTime.UtcNow;

                            OnMarketUpdated?.Invoke(this, marketTicker);
                            _logger.LogInformation("Refresh - {Market} force refreshed successfully.", marketTicker);
                            MarketsRefreshed++;      // FIX
                            ForcedRefreshCount++;    // FIX
                        }
                        else
                        {
                            _logger.LogDebug("No forced refresh needed for {Market}. Last ticker: {Last}, Last Sync {Sync}",
                                marketTicker, lastTicker?.LoggedDate, marketData.LastSuccessfulSync);
                        }
                    }

                    _logger.LogInformation("Refresh - Force refreshed {Count} markets in {Minutes} minutes",
                        ForcedRefreshCount, Math.Round(stopwatch.Elapsed.TotalMinutes, 2));
                }
            }

            await _serviceFactory.GetMarketDataService().FetchPositionsAsync();
            LastWorkDuration = DateTime.UtcNow - workStartTime;
            LastWorkMarketCount = watchedMarkets.Count;
            stopwatch.Stop();
        }



        public static List<T> ShuffleList<T>(List<T> list)
        {
            Random random = new Random();
            List<T> shuffledList = new List<T>(list);
            int n = shuffledList.Count;

            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                // Swap elements at indices i and j
                T temp = shuffledList[i];
                shuffledList[i] = shuffledList[j];
                shuffledList[j] = temp;
            }

            return shuffledList;
        }

    }


}