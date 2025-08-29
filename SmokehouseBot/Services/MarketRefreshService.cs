using Microsoft.Extensions.Options;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services.Interfaces;
using SmokehouseBot.State;
using SmokehouseDTOs.Exceptions;
using System.Collections.Generic;
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
                                _logger.LogDebug("Trading is inactive, waiting for trading to become active.");
                                await Task.Delay(TimeSpan.FromMinutes(1), _statusTracker.GetCancellationToken());
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
                    }
                }

                // Determine if a sync is needed
                if (marketData != null)
                {
                    var lastTicker = marketData?.Tickers.OrderByDescending(t => t.LoggedDate).FirstOrDefault();
                    if (lastTicker == null || marketData.LastSuccessfulSync <= DateTime.UtcNow.AddMinutes(-31) ||
                         (lastTicker.LoggedDate > marketData.LastSuccessfulSync
                        && (DateTime.UtcNow - lastTicker.LoggedDate) >= TimeSpan.FromMinutes(5)))
                    {
                        await _serviceFactory.GetMarketDataService().SyncMarketDataAsync(marketTicker);

                        marketData.LastSuccessfulSync = DateTime.UtcNow;

                        if (OnMarketUpdated != null)
                            OnMarketUpdated?.Invoke(this, marketTicker);
                        _logger.LogInformation($"Refresh - {marketTicker} refreshed successfully.");
                        MarketsRefreshed = MarketsRefreshed++;
                    }
                    else
                    {
                        _logger.LogInformation("Refresh - No market refresh needed for {marketTicker}. Last ticker: {last}, Last Sync {sync}", marketTicker, lastTicker.LoggedDate, marketData.LastSuccessfulSync);
                    }
                }
                    
            }
            _logger.LogInformation("Refresh - Refreshed {} markets in {ChangeWindowDuration.TotalMinutes} minutes", $"{MarketsRefreshed}/{watchedMarkets.Count}", Math.Round((double)stopwatch.ElapsedMilliseconds / 1000 / 60, 2));
            
            
            //Additional refresh loop to utilize extra time so we aren't refreshing ALL of the low activity ones at once
            if (watchedMarkets.Count > 0 && MarketsRefreshed / watchedMarkets.Count < .25)
            {
                int ForceRefreshMarkets = (int)Math.Round(watchedMarkets.Count * .25) - MarketsRefreshed;
                int ForcedRefreshCount = 0;
                foreach (var marketTicker in ShuffleList(watchedMarkets))
                {
                    if (ForcedRefreshCount >= ForceRefreshMarkets) break;
                    _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                    var marketData = _serviceFactory.GetMarketDataService().GetMarketDetails(marketTicker);
                    if (marketData != null)
                    {
                        var lastTicker = marketData.Tickers.OrderByDescending(t => t.LoggedDate).FirstOrDefault();
                        if ((lastTicker == null || marketData.LastSuccessfulSync <= DateTime.UtcNow.AddMinutes(-15)) &&
                             (DateTime.UtcNow - lastTicker.LoggedDate) >= TimeSpan.FromMinutes(5))
                        {
                            await _serviceFactory.GetMarketDataService().SyncMarketDataAsync(marketTicker);

                            marketData.LastSuccessfulSync = DateTime.UtcNow;

                            if (OnMarketUpdated != null)
                                OnMarketUpdated?.Invoke(this, marketTicker);
                            _logger.LogInformation($"Refresh - {marketTicker} force refreshed successfully.");
                            MarketsRefreshed = MarketsRefreshed++;
                            ForcedRefreshCount = ForcedRefreshCount++;
                        }
                        else
                        {
                            _logger.LogDebug("No market refresh needed for {marketTicker}. Last ticker: {last}, Last Sync {sync}", marketTicker, lastTicker.LoggedDate, marketData.LastSuccessfulSync);
                        }
                    }
                }
                _logger.LogInformation("Refresh - Force Refreshed {0} markets in {ChangeWindowDuration.TotalMinutes} minutes", ForcedRefreshCount, Math.Round((double)stopwatch.ElapsedMilliseconds / 1000 / 60, 2));
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