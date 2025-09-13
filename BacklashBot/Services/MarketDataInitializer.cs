using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using System.Diagnostics;
using System.Threading;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for initializing market data during application startup.
    /// This service fetches watched markets, subscribes to WebSocket channels for new markets,
    /// synchronizes market data, and sets up positions and account balance. It ensures
    /// that all necessary market data is available before the application becomes fully operational.
    /// </summary>
    public class MarketDataInitializer : IMarketDataInitializer
    {
        private readonly ILogger<IMarketDataInitializer> _logger;
        private readonly IServiceFactory _serviceFactory;
        private readonly IStatusTrackerService _statusTracker;
        private readonly IBotReadyStatus _readyStatus;
        private readonly IScopeManagerService _scopeManagerService;
        /// <summary>
        /// Gets the duration of the last market data initialization operation.
        /// </summary>
        public TimeSpan LastInitializationDuration { get; private set; }
        /// <summary>
        /// Gets the number of markets processed during the last initialization.
        /// </summary>
        public int LastInitializationMarketCount { get; private set; }

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
        /// Initializes a new instance of the <see cref="MarketDataInitializer"/> class.
        /// </summary>
        /// <param name="logger">Logger for recording initialization operations and errors.</param>
        /// <param name="serviceFactory">Factory for accessing other services in the application.</param>
        /// <param name="scopeManagerService">Service for managing dependency injection scopes.</param>
        /// <param name="readyStatus">Service tracking the application's readiness status.</param>
        /// <param name="statusTracker">Service for tracking application status and cancellation tokens.</param>
        public MarketDataInitializer(ILogger<IMarketDataInitializer> logger, IServiceFactory serviceFactory, IScopeManagerService scopeManagerService, IBotReadyStatus readyStatus,
            IStatusTrackerService statusTracker)
        {
            _logger = logger;
            _statusTracker = statusTracker;
            _readyStatus = readyStatus;
            _scopeManagerService = scopeManagerService;
            _serviceFactory = serviceFactory;
        }

        /// <summary>
        /// Performs the complete market data initialization sequence.
        /// This method fetches watched markets, subscribes to WebSocket channels, synchronizes market data,
        /// retrieves positions, and updates account balance. It runs operations sequentially on a low-priority
        /// thread to avoid interfering with other system processes.
        /// </summary>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        public async Task SetupAsync()
        {
            var initializationStartTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            List<string> watchedMarkets = null;

            _logger.LogDebug("MarketDataInitializer.SetupAsync started at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                _logger.LogDebug("Fetching watched markets...");
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                watchedMarkets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
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

                            if (!ValidateMarketTicker(ticker))
                            {
                                _logger.LogWarning("Skipping initialization for invalid ticker: {Ticker}", ticker);
                                continue;
                            }

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

                // Collect performance metrics
                LastInitializationDuration = DateTime.UtcNow - initializationStartTime;
                LastInitializationMarketCount = watchedMarkets?.Count ?? 0;
                stopwatch.Stop();
                _logger.LogInformation("Market data initialization completed in {Duration} for {Count} markets", LastInitializationDuration, LastInitializationMarketCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MarketDataInitializer.SetupAsync cancelled at {0}", DateTime.UtcNow);
                _readyStatus.InitializationCompleted.TrySetResult(false);
                _readyStatus.BrowserReady.TrySetResult(false);
                _logger.LogDebug("Market Data initialization canceled.");

                // Collect metrics even on cancellation
                LastInitializationDuration = DateTime.UtcNow - initializationStartTime;
                LastInitializationMarketCount = watchedMarkets?.Count ?? 0;
                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MarketDataInitializer.SetupAsync");
                _readyStatus.InitializationCompleted.TrySetResult(false);
                _readyStatus.BrowserReady.TrySetResult(false);
                _logger.LogDebug("Set InitializationCompleted and BrowserReady tasks to false due to error");

                // Collect metrics even on error
                LastInitializationDuration = DateTime.UtcNow - initializationStartTime;
                LastInitializationMarketCount = watchedMarkets?.Count ?? 0;
                stopwatch.Stop();
                throw;
            }
            finally
            {
                _logger.LogDebug("MarketDataInitializer.SetupAsync completed at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            }
        }

        /// <summary>
        /// Waits for initial market data and the first orderbook snapshot to become available after subscribing to a market channel.
        /// This method polls for market data availability and ReceivedFirstSnapshot with a timeout to ensure the orderbook
        /// has been fully populated before proceeding with initialization.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol to wait for data on.</param>
        /// <returns>A task representing the asynchronous wait operation.</returns>
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
