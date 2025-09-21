using Microsoft.Extensions.Options;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs.Exceptions;
using System.Diagnostics;
using System.Threading;
using BacklashBot.Configuration;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for managing periodic market data refresh operations for watched markets.
    /// This service runs a background task that checks trading status, determines which markets need data synchronization,
    /// and performs refresh operations at configured intervals. It provides metrics on refresh performance and supports
    /// immediate refresh triggers for specific markets.
    /// </summary>
    public class MarketRefreshService : IMarketRefreshService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceFactory _serviceFactory;
        private readonly IStatusTrackerService _statusTracker;
        private readonly ILogger<IMarketRefreshService> _logger;
        private readonly TimeSpan _updateInterval;
        private readonly MarketRefreshServiceConfig _marketRefreshServiceConfig;
        private DateTime? _lastRefreshTime;
        private Task _executeTask;
        /// <summary>
        /// Event raised when a market's data has been updated.
        /// </summary>
        public event EventHandler<string> OnMarketUpdated;
        private TimeSpan _lastWorkDuration;
        /// <summary>
        /// Gets the duration of the last market refresh operation.
        /// </summary>
        public TimeSpan LastWorkDuration
        {
            get
            {
                if (!_marketRefreshServiceConfig.EnablePerformanceMetrics)
                    throw new InvalidOperationException("Performance metrics are not enabled for MarketRefreshService.");
                return _lastWorkDuration;
            }
            private set { _lastWorkDuration = value; }
        }
        private int _lastWorkMarketCount;
        /// <summary>
        /// Gets the number of markets processed in the last refresh operation.
        /// </summary>
        public int LastWorkMarketCount
        {
            get
            {
                if (!_marketRefreshServiceConfig.EnablePerformanceMetrics)
                    throw new InvalidOperationException("Performance metrics are not enabled for MarketRefreshService.");
                return _lastWorkMarketCount;
            }
            private set { _lastWorkMarketCount = value; }
        }
        /// <summary>
        /// Gets the total number of refresh operations performed.
        /// </summary>
        public long TotalRefreshOperations { get; private set; }
        private TimeSpan _averageRefreshTimePerMarket;
        /// <summary>
        /// Gets the average time spent per market refresh in the last operation.
        /// </summary>
        public TimeSpan AverageRefreshTimePerMarket
        {
            get
            {
                if (!_marketRefreshServiceConfig.EnablePerformanceMetrics)
                    throw new InvalidOperationException("Performance metrics are not enabled for MarketRefreshService.");
                return _averageRefreshTimePerMarket;
            }
            private set { _averageRefreshTimePerMarket = value; }
        }
        private int _lastRefreshCount;
        /// <summary>
        /// Gets the total number of markets refreshed in the last operation.
        /// </summary>
        public int LastRefreshCount
        {
            get
            {
                if (!_marketRefreshServiceConfig.EnablePerformanceMetrics)
                    throw new InvalidOperationException("Performance metrics are not enabled for MarketRefreshService.");
                return _lastRefreshCount;
            }
            private set { _lastRefreshCount = value; }
        }
        private TimeSpan _lastCpuTime;
        /// <summary>
        /// Gets the CPU time used in the last refresh operation.
        /// </summary>
        public TimeSpan LastCpuTime
        {
            get
            {
                if (!_marketRefreshServiceConfig.EnablePerformanceMetrics)
                    throw new InvalidOperationException("Performance metrics are not enabled for MarketRefreshService.");
                return _lastCpuTime;
            }
            private set { _lastCpuTime = value; }
        }
        private long _lastMemoryUsage;
        /// <summary>
        /// Gets the memory usage at the end of the last refresh operation.
        /// </summary>
        public long LastMemoryUsage
        {
            get
            {
                if (!_marketRefreshServiceConfig.EnablePerformanceMetrics)
                    throw new InvalidOperationException("Performance metrics are not enabled for MarketRefreshService.");
                return _lastMemoryUsage;
            }
            private set { _lastMemoryUsage = value; }
        }
        private double _refreshThroughput;
        /// <summary>
        /// Gets the throughput (markets refreshed per second) in the last operation.
        /// </summary>
        public double RefreshThroughput
        {
            get
            {
                if (!_marketRefreshServiceConfig.EnablePerformanceMetrics)
                    throw new InvalidOperationException("Performance metrics are not enabled for MarketRefreshService.");
                return _refreshThroughput;
            }
            private set { _refreshThroughput = value; }
        }
        private TimeSpan RefreshInterval => _updateInterval;
        private readonly IScopeManagerService _scopeManagerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketRefreshService"/> class.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency injection.</param>
        /// <param name="logger">Logger for recording service operations and errors.</param>
        /// <param name="marketRefreshServiceConfig">Configuration options for market refresh service parameters.</param>
        /// <param name="serviceFactory">Factory for accessing other services in the application.</param>
        /// <param name="scopeManagerService">Service for managing dependency injection scopes.</param>
        /// <param name="statusTrackerService">Service for tracking application status and cancellation tokens.</param>
        public MarketRefreshService(
            IServiceScopeFactory scopeFactory,
            ILogger<IMarketRefreshService> logger,
            IOptions<MarketRefreshServiceConfig> marketRefreshServiceConfig,
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
            _marketRefreshServiceConfig = marketRefreshServiceConfig.Value;
            _updateInterval = TimeSpan.FromMinutes(_marketRefreshServiceConfig.RefreshIntervalMinutes);
        }

        /// <summary>
        /// Starts the periodic market refresh service that runs in the background.
        /// This method initiates a long-running task that continuously monitors trading status and refreshes market data
        /// for watched markets at configured intervals. The service respects cancellation tokens and handles errors gracefully.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to stop the service operation.</param>
        public void ExecuteServicesAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogDebug("MarketRefreshService ExecuteAsync started...");
                _executeTask = Task.Factory.StartNew(async () =>
                {
                    // Set thread priority to BelowNormal for market refresh operations
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    _logger.LogDebug("MarketRefreshService running on low priority thread (BelowNormal)");

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
                }, _statusTracker.GetCancellationToken(), TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MarketRefreshService ExecuteAsync was cancelled");
            }
        }

        /// <summary>
        /// Triggers an immediate refresh of market data for a specific market ticker.
        /// This method performs a synchronous data update for the specified market and raises the market updated event.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol to refresh.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task TriggerImmediateRefreshAsync(string marketTicker)
        {
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                if (!ValidateMarketTicker(marketTicker))
                {
                    _logger.LogWarning("Skipping immediate refresh for invalid ticker: {MarketTicker}", marketTicker);
                    return;
                }

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


        /// <summary>
        /// Determines whether the market refresh service is currently running.
        /// </summary>
        /// <returns>True if the background refresh task is active and not completed; otherwise, false.</returns>
        public bool IsRunning()
        {
            return _executeTask != null && !_executeTask.IsCompleted;
        }

        /// <summary>
        /// Stops the market refresh service gracefully.
        /// This method waits for the background refresh task to complete and ensures proper cleanup.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the stop operation.</param>
        /// <returns>A task representing the asynchronous stop operation.</returns>
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

        /// <summary>
        /// Performs a comprehensive refresh of all watched markets.
        /// This method fetches the current list of watched markets, determines which ones need data synchronization
        /// based on sync timestamps and trading activity, and performs the necessary updates. It includes
        /// an additional pass for markets that haven't been refreshed recently if the overall refresh ratio is low.
        /// </summary>
        /// <returns>A task representing the asynchronous refresh operation.</returns>
        private async Task RefreshAllWatchedMarketsAsync()
        {
            _logger.LogDebug("Updating all watched markets...");

            var watchedMarkets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
            _logger.LogInformation("Processing {MarketCount} watched markets.", watchedMarkets.Count);

            var workStartTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var startCpu = TimeSpan.Zero;
            var startMemory = 0L;
            if (_marketRefreshServiceConfig.EnablePerformanceMetrics)
            {
                startCpu = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
                startMemory = System.GC.GetTotalMemory(false);
            }
            int MarketsRefreshed = 0;

            foreach (var marketTicker in watchedMarkets)
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                if (marketTicker == null) continue;

                if (!ValidateMarketTicker(marketTicker))
                {
                    _logger.LogWarning("Skipping refresh for invalid ticker: {MarketTicker}", marketTicker);
                    continue;
                }

                var marketData = _serviceFactory.GetMarketDataService().GetMarketDetails(marketTicker);
                if (marketData == null)
                {
                    _logger.LogDebug("Market {MarketTicker} not found in cache, attempting to load", marketTicker);
                    var freshWatched = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                    if (freshWatched.Contains(marketTicker))
                    {
                        _logger.LogDebug("Market {MarketTicker} confirmed currently watched", marketTicker);
                        await _serviceFactory.GetMarketDataService().AddMarketToWatchList(marketTicker);
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
                    (lastTicker == null && marketData.LastSuccessfulSync <= DateTime.UtcNow.AddMinutes(-5))
                    || marketData.LastSuccessfulSync <= DateTime.UtcNow.AddMinutes(-31)
                    || (lastTicker != null && lastTicker.LoggedDate > marketData.LastSuccessfulSync && (DateTime.UtcNow - lastTicker.LoggedDate) >= TimeSpan.FromMinutes(5));

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
                    _logger.LogDebug("Refresh - No market refresh needed for {Market}. Last ticker: {Last}, Last Sync {Sync}",
                        marketTicker, lastTicker?.LoggedDate, marketData.LastSuccessfulSync);
                }
            }

            _logger.LogInformation("Refresh - Refreshed {Refreshed} markets in {Minutes} minutes",
                $"{MarketsRefreshed}/{watchedMarkets.Count}", Math.Round(stopwatch.Elapsed.TotalMinutes, 2));

            // --- Additional pass: only if <threshold% refreshed, and stay within a time budget ---
            if (watchedMarkets.Count > 0)
            {
                double refreshRatio = (double)MarketsRefreshed / watchedMarkets.Count; // FIX: ensure double division
                if (refreshRatio < _marketRefreshServiceConfig.RefreshThresholdRatio)
                {
                    int targetForceTotal = (int)Math.Round(watchedMarkets.Count * _marketRefreshServiceConfig.RefreshThresholdRatio);
                    int remainingToForce = Math.Max(0, targetForceTotal - MarketsRefreshed);
                    int ForcedRefreshCount = 0;

                    // Simple time budget: stop forced pass if =timeBudgetRatio% of the interval has elapsed
                    TimeSpan forcedBudget = TimeSpan.FromTicks((long)(_updateInterval.Ticks * _marketRefreshServiceConfig.TimeBudgetRatio));

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

            await _serviceFactory.GetMarketDataService().RetrieveAndUpdatePositionsAsync();
            LastWorkDuration = DateTime.UtcNow - workStartTime;
            LastWorkMarketCount = watchedMarkets.Count;
            LastRefreshCount = MarketsRefreshed;
            TotalRefreshOperations++;
            AverageRefreshTimePerMarket = LastWorkMarketCount > 0 ? TimeSpan.FromTicks(LastWorkDuration.Ticks / LastWorkMarketCount) : TimeSpan.Zero;
            if (_marketRefreshServiceConfig.EnablePerformanceMetrics)
            {
                var endCpu = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
                var endMemory = System.GC.GetTotalMemory(false);
                LastCpuTime = endCpu - startCpu;
                LastMemoryUsage = endMemory;
            }
            if (_marketRefreshServiceConfig.EnablePerformanceMetrics)
            {
                RefreshThroughput = LastWorkDuration.TotalSeconds > 0 ? (double)LastRefreshCount / LastWorkDuration.TotalSeconds : 0;
            }
            stopwatch.Stop();
        }



        /// <summary>
        /// Validates a market ticker for basic format requirements.
        /// Logs a warning if the ticker is invalid but does not throw an exception.
        /// </summary>
        /// <param name="ticker">The market ticker to validate.</param>
        /// <returns>True if the ticker is valid; otherwise, false.</returns>
        private bool ValidateMarketTicker(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                _logger.LogWarning("Invalid market ticker: ticker is null or empty");
                return false;
            }

            // Basic validation: alphanumeric, dashes, underscores, reasonable length
            if (!System.Text.RegularExpressions.Regex.IsMatch(ticker, @"^[a-zA-Z0-9_-]{1,50}$"))
            {
                _logger.LogWarning("Invalid market ticker format: {Ticker}", ticker);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Shuffles the elements of a list using the Fisher-Yates algorithm.
        /// This method randomizes the order of elements in the provided list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <returns>A new list containing the shuffled elements.</returns>
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
