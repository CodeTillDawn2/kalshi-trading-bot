using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using System.Threading;

namespace BacklashBot.Services
{
    public class MarketDataInitializer : IMarketDataInitializer
    {
        private readonly ILogger<IMarketDataInitializer> _logger;
        private readonly IServiceFactory _serviceFactory;
        private readonly IStatusTrackerService _statusTracker;
        private readonly IBotReadyStatus _readyStatus;
        private readonly IScopeManagerService _scopeManagerService;
        public MarketDataInitializer(ILogger<IMarketDataInitializer> logger, IServiceFactory serviceFactory, IScopeManagerService scopeManagerService, IBotReadyStatus readyStatus,
            IStatusTrackerService statusTracker)
        {
            _logger = logger;
            _statusTracker = statusTracker;
            _readyStatus = readyStatus;
            _scopeManagerService = scopeManagerService;
            _serviceFactory = serviceFactory;
        }

        public async Task SetupAsync()
        {
            _logger.LogDebug("MarketDataInitializer.SetupAsync started at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                _logger.LogDebug("Fetching watched markets...");
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                var watchedMarkets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                _logger.LogDebug("Found {Count} watched markets: {Markets}", watchedMarkets.Count, string.Join(", ", watchedMarkets));

                if (!watchedMarkets.Any())
                {
                    _logger.LogDebug("No watched markets found, updating...");
                    await _serviceFactory.GetMarketDataService().UpdateWatchedMarketsAsync();
                    watchedMarkets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                    _logger.LogDebug("After forced refresh, found {Count} watched markets: {Markets}", watchedMarkets.Count, string.Join(", ", watchedMarkets));
                }

                // Run market initialization sequentially on a single background thread with lower priority
                await Task.Run(async () =>
                {
                    try
                    {
                        // Set thread priority to BelowNormal to reduce resource competition with snapshots
                        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                        _logger.LogDebug("Starting market initialization on low-priority thread");

                        // Process markets sequentially to avoid rate limiting
                        foreach (var ticker in watchedMarkets)
                        {
                            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                            _logger.LogDebug("Initializing market {MarketTicker} on low-priority thread", ticker);
                            if (!_serviceFactory.GetDataCache().Markets.ContainsKey(ticker))
                            {
                                _logger.LogDebug("Adding market subscription for {MarketTicker}", ticker);
                                await _serviceFactory.GetMarketDataService().SubscribeToMarketChannelsAsync(ticker);
                                _logger.LogDebug("Subscribed to market {MarketTicker}", ticker);
                                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                                _logger.LogDebug("Waiting for initial WebSocket data for {MarketTicker}", ticker);
                                await WaitForInitialDataAsync(ticker);
                                _logger.LogDebug("Received initial WebSocket data for {MarketTicker}", ticker);
                            }
                            else
                            {
                                _logger.LogDebug("Syncing all market data for {MarketTicker}", ticker);
                                await _serviceFactory.GetMarketDataService().SyncMarketDataAsync(ticker);
                                _logger.LogDebug("Synced market data for {MarketTicker}", ticker);
                            }
                            _logger.LogDebug("Completed initialization for {MarketTicker}", ticker);

                            // Add 100ms delay between market initializations to prevent rate limiting
                            await Task.Delay(100, _statusTracker.GetCancellationToken());
                        }

                        _logger.LogDebug("Market initialization completed on low-priority thread");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during market initialization on low-priority thread");
                        throw;
                    }
                }, _statusTracker.GetCancellationToken());
                _logger.LogInformation("All market initializations completed");
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                _logger.LogDebug("Fetching positions...");
                await _serviceFactory.GetMarketDataService().RetrieveAndUpdatePositionsAsync();
                _logger.LogDebug("Fetched positions");

                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                _logger.LogDebug("Updating account balance...");
                await _serviceFactory.GetMarketDataService().UpdateAccountBalanceAsync();
                _logger.LogDebug("Account balance updated");

                _readyStatus.InitializationCompleted.SetResult(true);
                _logger.LogInformation("Initialization set to completed");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MarketDataInitializer.SetupAsync cancelled at {0}", DateTime.UtcNow);
                _readyStatus.InitializationCompleted.TrySetResult(false);
                _readyStatus.BrowserReady.TrySetResult(false);
                _logger.LogDebug("Market Data initialization canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MarketDataInitializer.SetupAsync");
                _readyStatus.InitializationCompleted.TrySetResult(false);
                _readyStatus.BrowserReady.TrySetResult(false);
                _logger.LogDebug("Set InitializationCompleted and BrowserReady tasks to false due to error");
                throw;
            }
            finally
            {
                _logger.LogDebug("MarketDataInitializer.SetupAsync completed at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            }
        }

        private async Task WaitForInitialDataAsync(string marketTicker)
        {
            const int maxWaitSeconds = 3;
            const int pollIntervalMs = 500;
            var startTime = DateTime.UtcNow;

            _logger.LogDebug("WaitForInitialDataAsync started for {MarketTicker} at {0}, CancellationToken.IsCancellationRequested={IsRequested}", marketTicker, DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            try
            {
                while ((DateTime.UtcNow - startTime).TotalSeconds < maxWaitSeconds)
                {
                    _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                    var marketData = _serviceFactory.GetMarketDataService().GetMarketDetails(marketTicker);
                    var lastWebSocketTimestamp = _serviceFactory.GetMarketDataService().GetLatestWebSocketTimestamp();
                    _logger.LogDebug("Waiting for {MarketTicker}: MarketData={Exists}, Timestamp={Timestamp}, Elapsed={Seconds}s",
                        marketTicker, marketData != null, lastWebSocketTimestamp.ToString("yyyy-MM-ddTHH:mm:ss"), (DateTime.UtcNow - startTime).TotalSeconds);
                    if (marketData != null)
                    {
                        _logger.LogDebug("Initial data received for {MarketTicker}", marketTicker);
                        return;
                    }
                    await Task.Delay(pollIntervalMs, _statusTracker.GetCancellationToken());
                }
                _logger.LogWarning("Timeout for {MarketTicker} after {MaxWaitSeconds}s. Proceeding with available data.", marketTicker, maxWaitSeconds);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("WaitForInitialDataAsync cancelled for {MarketTicker} at {0}", marketTicker, DateTime.UtcNow);
            }
            finally
            {
                _logger.LogDebug("WaitForInitialDataAsync completed for {MarketTicker} at {0}, CancellationToken.IsCancellationRequested={IsRequested}", marketTicker, DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            }
        }
    }
}
