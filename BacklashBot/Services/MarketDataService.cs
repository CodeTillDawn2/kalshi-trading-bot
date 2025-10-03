using BacklashBot.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State;
using BacklashBot.State.Interfaces;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashDTOs.Exceptions;
using BacklashDTOs.Helpers;
using BacklashInterfaces.Constants;
using BacklashInterfaces.Enums;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace BacklashBot.Services
{
    /// <summary>
    /// Core service responsible for managing market data operations, WebSocket event handling,
    /// market watchlist management, and real-time data synchronization for the Kalshi trading bot.
    /// Supports configurable timeouts and batch sizes via MarketDataConfig, data validation for WebSocket messages,
    /// and retry logic with Polly for resilient API calls.
    /// </summary>
    /// <remarks>
    /// This service handles:
    /// - WebSocket event processing (ticker, lifecycle, fill events)
    /// - Market data caching and synchronization
    /// - Market watchlist management (adding/removing markets)
    /// - Order book and position data management
    /// - Ticker data processing with deduplication
    /// - Forward filling of candlestick data
    /// - Client notification events for UI updates
    /// </remarks>
    public class MarketDataService : IMarketDataService
    {
        private readonly IServiceFactory _serviceFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IStatusTrackerService _statusTracker;
        private readonly IBotReadyStatus _readyStatus;
        private readonly IBrainStatusService _brainStatus;
        private readonly ILogger<IMarketDataService> _logger;
        private readonly Func<MarketDTO, MarketData> _marketDataFactory;
        private readonly LoggingConfig _loggingConfig;
        private readonly MarketServiceDataConfig _marketDataConfig;
        private readonly CalculationsConfig _calculationsConfig;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _marketInitializationLocks = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _marketSyncLocks = new();
        private SemaphoreSlim _watchedMarketsSemaphore = new SemaphoreSlim(1, 1);
        private readonly IPerformanceMonitor _performanceMonitor;
        private HashSet<string> _lastWatchedMarkets = new HashSet<string>();
        private readonly IScopeManagerService _scopeManagerService;

        /// <summary>
        /// Event raised when market data is updated for a specific market ticker.
        /// </summary>
        public event EventHandler<string> MarketDataUpdated;

        /// <summary>
        /// Event raised when position data is updated for a specific market ticker.
        /// </summary>
        public event EventHandler<string> PositionDataUpdated;

        /// <summary>
        /// Event raised when the watch list of markets changes.
        /// </summary>
        public event EventHandler WatchListChanged;

        /// <summary>
        /// Event raised when a new ticker is added for a specific market ticker.
        /// </summary>
        public event EventHandler<string> TickerAdded;

        /// <summary>
        /// Event raised when the account balance is updated for a specific market ticker.
        /// </summary>
        public event EventHandler<string> AccountBalanceUpdated;

        private readonly ConcurrentBag<TickerDTO> _preppedTickers = new ConcurrentBag<TickerDTO>();
        private void OnMarketInvalid(object sender, string marketTicker)
        {
            _ = UnwatchMarket(marketTicker);
        }
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly System.Timers.Timer _tickerUpdateTimer;

        /// <summary>
        /// Gets or sets the list of markets that need to be refreshed.
        /// </summary>
        public List<string> MarketsToRefresh { get; set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the MarketDataService class.
        /// </summary>
        /// <param name="serviceFactory">Factory for creating service instances.</param>
        /// <param name="logger">Logger for recording operations and errors.</param>
        /// <param name="scopeFactory">Factory for creating service scopes.</param>
        /// <param name="loggingConfig">Configuration options for logging.</param>
        /// <param name="marketDataFactory">Factory function for creating MarketData instances.</param>
        /// <param name="marketDataConfigOptions">Configuration options for market data operations.</param>
        /// <param name="scopeManagerService">Service for managing scopes.</param>
        /// <param name="statusTracker">Service for tracking bot status and cancellation tokens.</param>
        /// <param name="readyStatus">Service for tracking bot readiness state.</param>
        /// <param name="brainStatus">Service for tracking brain status.</param>
        /// <param name="calculationsConfig">Configuration options for calculation parameters.</param>
        public MarketDataService(
            IServiceFactory serviceFactory,
            ILogger<IMarketDataService> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<LoggingConfig> loggingConfig,
            Func<MarketDTO, MarketData> marketDataFactory,
            IOptions<MarketServiceDataConfig> marketDataConfigOptions,
            IOptions<CalculationsConfig> calculationsConfig,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTracker,
            IBotReadyStatus readyStatus,
            IBrainStatusService brainStatus,
            IPerformanceMonitor performanceMonitor)
        {
            _logger = logger;
            _scopeManagerService = scopeManagerService;
            _serviceFactory = serviceFactory;
            _scopeFactory = scopeFactory;
            _loggingConfig = loggingConfig.Value;
            _marketDataConfig = marketDataConfigOptions?.Value;
            _calculationsConfig = calculationsConfig?.Value ?? throw new ArgumentNullException(nameof(calculationsConfig));
            _marketDataFactory = marketDataFactory;
            _statusTracker = statusTracker;
            _readyStatus = readyStatus;

            _tickerUpdateTimer = new System.Timers.Timer(5000); // 5 seconds
            _tickerUpdateTimer.Elapsed += async (sender, e) => await MassUpdateTickers();
            _tickerUpdateTimer.AutoReset = true;
            _tickerUpdateTimer.Start();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (exception, timeSpan, retryCount, context) =>
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                _logger.LogWarning(exception, "Retry {RetryCount} for market data fetch after {TimeSpan}", retryCount, timeSpan);
            });
            _performanceMonitor = performanceMonitor;

            _serviceFactory.GetDataCache().ExchangeStatusChanged += HandleExchangeStatusChanged;
            _brainStatus = brainStatus;
        }

        /// <summary>
        /// Configures WebSocket event handlers for real-time data processing.
        /// </summary>
        /// <remarks>
        /// Sets up event handlers for:
        /// - MessageReceived: Updates last WebSocket timestamp
        /// - TickerReceived: Processes ticker price updates
        /// - MarketLifecycleReceived: Handles market status changes
        /// - EventLifecycleReceived: Processes event lifecycle updates
        /// - FillReceived: Handles order fill notifications
        /// </remarks>
        public void ConfigureWebSocketEventHandlers()
        {
            _logger.LogInformation("MDS: ConfigureWebSocketEventHandlers called");
            try
            {
                var webSocketClient = _serviceFactory.GetKalshiWebSocketClient();
                _logger.LogInformation("MDS: Got WebSocket client: {IsNull}", webSocketClient == null);
                if (webSocketClient != null)
                {
                    _logger.LogInformation("MDS: About to add MessageReceived event handler");
                    webSocketClient.MessageReceived += (sender, timestamp) => _serviceFactory.GetDataCache().LastWebSocketTimestamp = timestamp;
                    _logger.LogInformation("MDS: MessageReceived event handler added successfully");
                }
                else
                {
                    _logger.LogWarning("MDS: WebSocket client is null.");
                    throw new Exception("WebSocket client is null.");
                }

                _serviceFactory.GetKalshiWebSocketClient().TickerReceived += async (sender, args) => await ProcessTickerUpdate(
                    args.market_ticker,
                    args.market_id,
                    args.price,
                    args.yes_bid,
                    args.yes_ask,
                    args.volume,
                    args.open_interest,
                    args.dollar_volume,
                    args.dollar_open_interest,
                    args.ts,
                    args.LoggedDate,
                    args.ProcessedDate
                );
                _serviceFactory.GetKalshiWebSocketClient().MarketLifecycleReceived += ProcessMarketLifecycleEventAsync;
                _serviceFactory.GetKalshiWebSocketClient().EventLifecycleReceived += ProcessEventLifecycleEventAsync;
                _serviceFactory.GetKalshiWebSocketClient().FillReceived += (sender, args) =>
                    Task.Run(() => ProcessFillEventAsync(args));

                _serviceFactory.GetOrderBookService().MarketInvalid += OnMarketInvalid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MDS: Error setting up handlers");
                throw;
            }
        }

        /// <summary>
        /// Gets the latest orderbook timestamp for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get the timestamp for.</param>
        /// <returns>The latest orderbook timestamp, or null if not found.</returns>
        public DateTime? GetLatestOrderbookTimestamp(string marketTicker)
        {
            _logger.LogDebug("Retrieving latest orderbook timestamp for: {MarketTicker}", marketTicker);
            if (_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
            {
                var timestamp = marketData.LastOrderbookEventTimestamp;
                _logger.LogDebug("Latest orderbook timestamp for {MarketTicker}: {Timestamp}", marketTicker, timestamp);
                return timestamp;
            }
            _logger.LogWarning("Market {MarketTicker} not found for orderbook timestamp", marketTicker);
            return null;
        }

        /// <summary>
        /// Gets the current exchange status.
        /// </summary>
        /// <returns>True if the exchange is active, false otherwise.</returns>
        public bool GetExchangeStatus()
        {
            bool status = _serviceFactory.GetDataCache().ExchangeStatus;
            _logger.LogDebug("Retrieved exchange status: {Status}", status);
            return status;
        }

        /// <summary>
        /// Gets the current trading status.
        /// </summary>
        /// <returns>True if trading is active, false otherwise.</returns>
        public bool GetTradingStatus()
        {
            bool status = _serviceFactory.GetDataCache().TradingStatus;
            _logger.LogDebug("Retrieved trading status: {Status}", status);
            return status;
        }

        /// <summary>
        /// Handles changes in exchange status by updating market data and WebSocket trading status.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The status change event arguments.</param>
        public async void HandleExchangeStatusChanged(object sender, StatusChangedEventArgs args)
        {
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                _logger.LogDebug("Handling exchange status change: ExchangeActive={ExchangeStatus}, TradingActive={TradingStatus}",
                    args.ExchangeStatus, args.TradingStatus);

                _serviceFactory.GetKalshiWebSocketClient().IsTradingActive = args.TradingStatus;
                foreach (var marketData in _serviceFactory.GetDataCache().Markets.Values)
                {
                    marketData.ChangeTracker.UpdateMarketStatus(args.ExchangeStatus, args.TradingStatus);
                }

                if (!args.TradingStatus && _serviceFactory.GetKalshiWebSocketClient().IsConnected())
                {
                    _logger.LogWarning("Trading status is false and WebSocket is connected, unsubscribing from all feeds");
                    await _serviceFactory.GetKalshiWebSocketClient().UnsubscribeFromAllAsync();
                }

                _logger.LogDebug("Exchange status updated: ExchangeActive={ExchangeStatus}, TradingActive={TradingStatus}",
                    args.ExchangeStatus, args.TradingStatus);
            }
            catch (OperationCanceledException)
            {

                _logger.LogDebug("HandleExchangeStatusChanged was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle exchange status change");
            }
        }

        private void ProcessMarketLifecycleEventAsync(object sender, MarketLifecycleEventArgs args)
        {
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                if (args.Data.TryGetProperty("msg", out var msgElement) &&
                    msgElement.TryGetProperty("market_ticker", out var tickerElement) &&
                    msgElement.TryGetProperty("status", out var statusElement))
                {
                    string marketTicker = tickerElement.GetString();
                    string status = statusElement.GetString();

                    if (string.IsNullOrWhiteSpace(marketTicker) || string.IsNullOrWhiteSpace(status))
                    {
                        _logger.LogWarning("Invalid market lifecycle data: marketTicker='{MarketTicker}', status='{Status}'", marketTicker, status);
                        return;
                    }

                    _logger.LogDebug("Processing lifecycle event for market: {MarketTicker}, status={MarketStatus}",
                        marketTicker, status);

                    using var scope = _scopeFactory.CreateScope();
                    var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

                    if (_serviceFactory.GetDataCache().WatchedMarkets.Contains(marketTicker))
                    {
                        MarketsToRefresh.Add(marketTicker);
                        lock (_serviceFactory.GetDataCache().Markets)
                        {
                            _serviceFactory.GetDataCache().Markets.First(x => x.Key == marketTicker).Value.MarketStatus = status;
                        }
                        _logger.LogInformation("Lifecycle event updated market status for {MarketTicker} to {MarketStatus}", marketTicker, status);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ProcessMarketLifecycleEventAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process lifecycle event");
            }
        }

        private async void ProcessEventLifecycleEventAsync(object sender, EventLifecycleEventArgs args)
        {
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                if (args.Data.TryGetProperty("msg", out var msgElement) &&
                    msgElement.TryGetProperty("event_ticker", out var tickerElement))
                {
                    string eventTicker = tickerElement.GetString();
                    if (!string.IsNullOrEmpty(eventTicker))
                    {
                        if (_marketDataConfig != null && _marketDataConfig.EventLifecycleDelaySeconds > 0)
                        {
                            await Task.Delay(_marketDataConfig.EventLifecycleDelaySeconds * 1000, _statusTracker.GetCancellationToken());
                        }
                        _logger.LogInformation("Fetching event data for {EventTicker} after delay for server to finish setting up event.", eventTicker);

                        using var scope = _scopeFactory.CreateScope();
                        var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

                        await apiService.FetchEventAsync(eventTicker, withNestedMarkets: true);

                        _logger.LogDebug("Successfully fetched and updated event data for {EventTicker}", eventTicker);
                    }
                    else
                    {
                        _logger.LogWarning("Event ticker is missing or empty in event_lifecycle message");
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid event_lifecycle message format: missing msg or event_ticker");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HandleEventLifecycleEventAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle event_lifecycle event");
            }
        }

        private async Task ProcessFillEventAsync(FillEventArgs args)
        {
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                if (args.Data.TryGetProperty("msg", out var msgElement) &&
                    msgElement.TryGetProperty("market_ticker", out var tickerElement))
                {
                    string? marketTicker = tickerElement.GetString();
                    if (!string.IsNullOrEmpty(marketTicker))
                    {
                        _logger.LogDebug("Processing fill event for market: {MarketTicker}", marketTicker);

                        if (_serviceFactory.GetDataCache().WatchedMarkets.Contains(marketTicker))
                        {
                            _logger.LogDebug("Market {MarketTicker} is watched, updating positions", marketTicker);
                            await RetrieveAndUpdatePositionsAsync();
                            NotifyPositionDataUpdated(marketTicker);
                        }
                        else
                        {
                            _logger.LogDebug("Market {MarketTicker} not watched, skipping position and orderbook update", marketTicker);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HandleFillEventAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle fill event");
            }
        }

        /// <summary>
        /// Retrieves and updates position data for all watched markets from the API.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RetrieveAndUpdatePositionsAsync()
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            using var scope = _scopeFactory.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
            await apiService.FetchPositionsAsync();
            await apiService.FetchOrdersAsync();
            foreach (MarketData marketData in _serviceFactory.GetDataCache().Markets.Values)
            {
                List<MarketPositionDTO> marketPositionData = await context.GetMarketPositions(new HashSet<string>() { marketData.MarketTicker });
                marketData.Positions = marketPositionData;
                marketData.RestingOrders = await context.GetOrders(marketData.MarketTicker, "resting");
                marketData.RefreshPositionMetadata();
            }
        }

        /// <summary>
        /// Notifies clients that the market watch list has changed.
        /// </summary>
        public void NotifyClientsOfMarketListChange()
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Notifying clients of market list change");
            WatchListChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Adds a market to the watch list and initializes its data.
        /// </summary>
        /// <param name="marketTicker">The market ticker to add to the watch list.</param>
        /// <param name="interestScore">Optional interest score for the market.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddMarketToWatchList(string marketTicker, double? interestScore = null)
        {
            _logger.LogInformation("Adding market watch to database: {MarketTicker}", marketTicker);
            var initSemaphore = _marketInitializationLocks.GetOrAdd(marketTicker, _ => new SemaphoreSlim(1, 1));
            bool lockAcquired = false;
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                lockAcquired = await initSemaphore.WaitAsync(_marketDataConfig.SemaphoreTimeoutMs, _statusTracker.GetCancellationToken());
                if (!lockAcquired)
                {
                    _logger.LogError("Failed to acquire initialization lock for {MarketTicker} within {Timeout} ms", marketTicker, _marketDataConfig.SemaphoreTimeoutMs);
                    return;
                }
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                var marketWatch = new MarketWatchDTO
                {
                    market_ticker = marketTicker,
                    BrainLock = _brainStatus.BrainLock,
                    InterestScore = interestScore,
                    InterestScoreDate = DateTime.Now,
                    LastWatched = DateTime.Now
                };
                await context.AddOrUpdateMarketWatch(marketWatch);

                NotifyClientsOfMarketListChange();
                _logger.LogInformation("Successfully added market watch for {MarketTicker} to database", marketTicker);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("AddMarketWatchToDb was cancelled for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add market watch to database for {MarketTicker}", marketTicker);
                return;
            }
            finally
            {
                if (lockAcquired)
                {
                    _logger.LogDebug("Released initialization lock for {MarketTicker}", marketTicker);
                    initSemaphore.Release();
                    _marketInitializationLocks.TryRemove(marketTicker, out _);
                }
            }

            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                var market = await EnsureMarketDataAsync(marketTicker);
                if (market == null)
                {
                    _logger.LogWarning("Failed to initialize market {MarketTicker} after adding to watch list", marketTicker);
                    return;
                }

                _serviceFactory.GetDataCache().Markets[marketTicker] = _marketDataFactory(market);
                _serviceFactory.GetDataCache().Markets[marketTicker].OrderbookData = new List<OrderbookData>();
                _serviceFactory.GetDataCache().Markets[marketTicker].LastSuccessfulSync = DateTime.UtcNow;
                _logger.LogInformation("Initialized market cache for {MarketTicker}", marketTicker);

                await _serviceFactory.GetWebSocketHostedService().TriggerConnectionCheckAsync();
                if (!_readyStatus.InitializationCompleted.Task.IsCompleted)
                {
                    _logger.LogInformation("Skipping connection attempt as initialization is not complete.");
                }
                else if (!_serviceFactory.GetKalshiWebSocketClient().IsConnected())
                {
                    _logger.LogDebug("WebSocket not connected after check for {MarketTicker}, retrying once", marketTicker);
                    await _serviceFactory.GetKalshiWebSocketClient().ConnectAsync();
                    if (!_serviceFactory.GetKalshiWebSocketClient().IsConnected())
                    {
                        _logger.LogWarning("WebSocket connection failed for {MarketTicker}, proceeding with API fallback", marketTicker);
                    }
                    else
                    {
                        _logger.LogDebug("WebSocket connected successfully for {MarketTicker}", marketTicker);
                    }
                }
                else
                {
                    _logger.LogDebug("WebSocket already connected for {MarketTicker}", marketTicker);
                }

                foreach (var channel in KalshiConstants.AllChannels.Select(_serviceFactory.GetKalshiWebSocketClient().GetChannelName))
                {
                    _serviceFactory.GetKalshiWebSocketClient().SetSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                }

                if (!_serviceFactory.GetDataCache().WatchedMarkets.Contains(marketTicker))
                {
                    _logger.LogInformation("Market {MarketTicker} added to watch list after validation", marketTicker);
                    _serviceFactory.GetDataCache().WatchedMarkets.Add(marketTicker);
                    await UpdateWatchedMarketsAsync();
                }

                _logger.LogDebug("Initiating subscription for {MarketTicker}", marketTicker);
                await SubscribeToMarketChannelsAsync(marketTicker);

                await SyncMarketDataAsync(marketTicker);

                await _serviceFactory.GetMarketRefreshService().TriggerImmediateRefreshAsync(marketTicker);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Background initialization was cancelled for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize market subscriptions for {MarketTicker}", marketTicker);
            }
        }

        /// <summary>
        /// Subscribes to WebSocket channels for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to subscribe to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SubscribeToMarketChannelsAsync(string marketTicker)
        {
            _logger.LogDebug("Subscribing to market: {MarketTicker}", marketTicker);
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                _logger.LogDebug("Ensuring market data for {MarketTicker}", marketTicker);
                var market = await EnsureMarketDataAsync(marketTicker);
                if (market == null)
                {
                    _logger.LogWarning("Market {MarketTicker} failed to load, cannot subscribe", marketTicker);
                    return;
                }

                _logger.LogDebug("Initializing market cache for {MarketTicker}", marketTicker);
                _serviceFactory.GetDataCache().Markets[marketTicker] = _marketDataFactory(market);
                _serviceFactory.GetDataCache().Markets[marketTicker].OrderbookData = new List<OrderbookData>();
                _serviceFactory.GetDataCache().Markets[marketTicker].LastSuccessfulSync = DateTime.UtcNow;
                _logger.LogDebug("Initialized market cache for {MarketTicker}", marketTicker);

                _logger.LogDebug("Syncing market data for {MarketTicker}", marketTicker);
                await SyncMarketDataAsync(marketTicker);
                _logger.LogDebug("Synced market data for {MarketTicker}", marketTicker);

                if (!_serviceFactory.GetDataCache().WatchedMarkets.Contains(marketTicker))
                {
                    _logger.LogInformation("Stats: Adding {MarketTicker} to WatchedMarkets", marketTicker);
                    _serviceFactory.GetDataCache().WatchedMarkets.Add(marketTicker);
                    if (_serviceFactory.GetDataCache().RecentlyRemovedMarkets.Contains(marketTicker))
                        _serviceFactory.GetDataCache().RecentlyRemovedMarkets.Remove(marketTicker);
                    await UpdateWatchedMarketsAsync();
                    _logger.LogDebug("Updated WatchedMarkets with {MarketTicker}", marketTicker);
                }

                // Connect WebSocket if not already connected before subscribing to channels
                if (!_serviceFactory.GetKalshiWebSocketClient().IsConnected())
                {
                    _logger.LogDebug("WebSocket not connected, connecting before subscribing to {MarketTicker}", marketTicker);
                    await _serviceFactory.GetKalshiWebSocketClient().ConnectAsync();
                    if (!_serviceFactory.GetKalshiWebSocketClient().IsConnected())
                    {
                        _logger.LogWarning("Failed to connect WebSocket for {MarketTicker}, skipping subscription", marketTicker);
                        return;
                    }
                    _logger.LogDebug("WebSocket connected successfully for {MarketTicker}", marketTicker);
                    // Add a delay to ensure the WebSocket is stable before sending subscriptions
                    await Task.Delay(2000, _statusTracker.GetCancellationToken());
                }

                _logger.LogDebug("Subscribing to channels for {MarketTicker}", marketTicker);
                foreach (var action in KalshiConstants.MarketChannels)
                {
                    _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                    var channel = _serviceFactory.GetKalshiWebSocketClient().GetChannelName(action);
                    if (!_serviceFactory.GetKalshiWebSocketClient().IsSubscribed(marketTicker, action))
                    {
                        _logger.LogInformation("Subscribing to {Channel} for {MarketTicker}", channel, marketTicker);
                        try
                        {
                            await _serviceFactory.GetKalshiWebSocketClient().SubscribeToChannelAsync(action, new[] { marketTicker });
                            _logger.LogInformation("Subscribed to {Channel} for {MarketTicker}", channel, marketTicker);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to subscribe to {Channel} for {MarketTicker}", channel, marketTicker);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("{Channel} already subscribed for {MarketTicker}", channel, marketTicker);
                    }
                }

                _logger.LogDebug("Successfully subscribed to market {MarketTicker}", marketTicker);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("SubscribeToMarketAsync was cancelled for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to market {MarketTicker}", marketTicker);
            }
        }

        /// <summary>
        /// Removes a market from the watch list and unsubscribes from its WebSocket channels.
        /// </summary>
        /// <param name="marketTicker">The market ticker to remove from the watch list.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UnwatchMarket(string marketTicker)
        {
            _logger.LogInformation("Stats: Unwatching watch: {MarketTicker}", marketTicker);
            var initSemaphore = _marketInitializationLocks.GetOrAdd(marketTicker, _ => new SemaphoreSlim(1, 1));
            bool lockAcquired = false;
            bool NoNeedToRemove = false;
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                lockAcquired = await initSemaphore.WaitAsync(_marketDataConfig.SemaphoreTimeoutMs, _statusTracker.GetCancellationToken());
                if (!lockAcquired)
                {
                    _logger.LogError("Failed to acquire initialization lock for {MarketTicker} within {Timeout} ms", marketTicker, _marketDataConfig.SemaphoreTimeoutMs);
                    return;
                }
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                MarketWatchDTO? marketWatch = await context.GetMarketWatchByTicker(marketTicker);
                Guid brainLock = _brainStatus.BrainLock;
                if (marketWatch != null
                    //&& marketWatch.BrainLock == brainLock
                    )
                {
                    marketWatch.BrainLock = null;
                    await context.AddOrUpdateMarketWatch(marketWatch);
                    _logger.LogInformation("Market {MarketTicker} successfully removed from watch list", marketTicker);
                    if (_serviceFactory.GetDataCache().Markets.ContainsKey(marketTicker))
                    {
                        _serviceFactory.GetDataCache().Markets[marketTicker].ChangeTracker.Shutdown();
                        _serviceFactory.GetDataCache().Markets.TryRemove(marketTicker, out var _);
                    }
                    if (!_serviceFactory.GetDataCache().RecentlyRemovedMarkets.Contains(marketTicker))
                        _serviceFactory.GetDataCache().RecentlyRemovedMarkets.Add(marketTicker);
                    _serviceFactory.GetDataCache().WatchedMarkets.Remove(marketTicker);
                    _logger.LogInformation("Stats: Removed {MarketTicker} from WatchedMarkets, current WatchedMarkets: {WatchedMarkets}", marketTicker, string.Join(", ", _serviceFactory.GetDataCache().WatchedMarkets));

                    await UpdateWatchedMarketsAsync();
                    await UpdateMarketSubscriptionAsync("delete_markets", new[] { marketTicker });

                    foreach (var channel in KalshiConstants.AllChannels.Select(_serviceFactory.GetKalshiWebSocketClient().GetChannelName))
                    {
                        _serviceFactory.GetKalshiWebSocketClient().SetSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                    }

                    // Clear orderbook queue for this market
                    _serviceFactory.GetKalshiWebSocketClient().ClearOrderBookQueue(marketTicker);

                    // Clear other queues
                    _serviceFactory.GetOrderBookService().ClearQueueForMarketAsync(marketTicker);


                    if (!_serviceFactory.GetDataCache().WatchedMarkets.Any())
                    {
                        _logger.LogDebug("No watched markets remain, unsubscribing from market-specific channels");
                        foreach (var action in KalshiConstants.MarketChannels)
                        {
                            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                            _logger.LogDebug("Unsubscribing from channel {Action}", action);
                            await _serviceFactory.GetKalshiWebSocketClient().UnsubscribeFromChannelAsync(action);
                        }
                    }
                }
                else
                {
                    if (_serviceFactory.GetDataCache().Markets.ContainsKey(marketTicker) && _serviceFactory.GetDataCache().Markets[marketTicker].Positions.Count > 0)
                    {
                        _logger.LogWarning("Cannot remove {MarketTicker} from watch list due to active position", marketTicker);
                    }
                    else
                    {
                        _logger.LogWarning("Market watch for {MarketTicker} does not exist in database and no position", marketTicker);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("DeleteMarketWatch was cancelled for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete market watch for {MarketTicker}, Stack trace: {st}", marketTicker, ex.StackTrace);
            }
            finally
            {
                if (lockAcquired)
                {
                    _logger.LogDebug("Released initialization lock for {MarketTicker}", marketTicker);
                    initSemaphore.Release();
                    if (!NoNeedToRemove)
                        _marketInitializationLocks.TryRemove(marketTicker, out _);
                }
                NotifyClientsOfMarketListChange();
            }
        }

        /// <summary>
        /// Marks a market as unhealthy by setting its interest score to zero and WebSocket health to false.
        /// This effectively removes it from active watching while keeping the database record.
        /// Used when WebSocket subscriptions repeatedly fail for a market.
        /// </summary>
        /// <param name="marketTicker">The market ticker to mark as unhealthy.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task MarkMarketAsUnhealthyAsync(string marketTicker)
        {
            _logger.LogInformation("Marking market {MarketTicker} as unhealthy by setting interest score to zero and WebSocket health to false", marketTicker);
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                // Set WebSocket health to false in the MarketData instance
                if (_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
                {
                    marketData.WebSocketHealthy = false;
                    _logger.LogDebug("Set WebSocketHealthy=false for market {MarketTicker}", marketTicker);
                }
                else
                {
                    _logger.LogWarning("MarketData instance for {MarketTicker} not found in cache, cannot set WebSocket health", marketTicker);
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                MarketWatchDTO? marketWatch = await context.GetMarketWatchByTicker(marketTicker);

                if (marketWatch != null)
                {
                    marketWatch.InterestScore = 0;
                    marketWatch.InterestScoreDate = DateTime.Now;
                    await context.AddOrUpdateMarketWatch(marketWatch);
                    _logger.LogInformation("Successfully marked market {MarketTicker} as unhealthy (interest score = 0)", marketTicker);

                    // Proper unsubscription
                    await UpdateMarketSubscriptionAsync("delete_markets", new[] { marketTicker });

                    // Add to refresh for recovery
                    if (!MarketsToRefresh.Contains(marketTicker)) {
                        MarketsToRefresh.Add(marketTicker);
                        _logger.LogDebug("Added {MarketTicker} to MarketsToRefresh for recovery", marketTicker);
                    }

                    // Shutdown change tracker if in cache
                    if (_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData2)) {
                        marketData2.ChangeTracker.Shutdown();
                    }
                }
                else
                {
                    _logger.LogWarning("Market watch for {MarketTicker} not found in database, cannot mark as unhealthy", marketTicker);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MarkMarketAsUnhealthyAsync was cancelled for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark market {MarketTicker} as unhealthy", marketTicker);
            }
        }

        /// <summary>
        /// Marks a market as healthy by setting WebSocket health to true and logging the restoration of WebSocket connectivity.
        /// Used when WebSocket subscriptions are successfully restored for a market.
        /// Note: Interest score is not modified when marking healthy.
        /// </summary>
        /// <param name="marketTicker">The market ticker to mark as healthy.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task MarkMarketAsHealthyAsync(string marketTicker)
        {
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                // Set WebSocket health to true in the MarketData instance
                if (_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
                {
                    bool wasAlreadyHealthy = marketData.WebSocketHealthy;
                    marketData.WebSocketHealthy = true;

                    // Only log if the market was previously unhealthy
                    if (!wasAlreadyHealthy)
                    {
                        _logger.LogInformation("Marking market {MarketTicker} as healthy - WebSocket connectivity restored", marketTicker);
                        _logger.LogInformation("Market {MarketTicker} WebSocket connectivity has been restored", marketTicker);
                    }
                    else
                    {
                        _logger.LogDebug("Market {MarketTicker} WebSocket connectivity confirmed (already healthy)", marketTicker);
                    }
                }
                else
                {
                    _logger.LogWarning("MarketData instance for {MarketTicker} not found in cache, cannot set WebSocket health", marketTicker);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MarkMarketAsHealthyAsync was cancelled for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark market {MarketTicker} as healthy", marketTicker);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates WebSocket subscriptions for markets based on the specified action.
        /// </summary>
        /// <param name="action">The action to perform (e.g., "add_markets", "delete_markets").</param>
        /// <param name="marketTickers">The market tickers to update subscriptions for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateMarketSubscriptionAsync(string action, string[] marketTickers)
        {
            _logger.LogDebug("Initiating subscription update: action={Action}, markets={Markets}", action, string.Join(", ", marketTickers));

            if (!marketTickers.Any())
            {
                _logger.LogWarning("No markets provided for subscription update: action={Action}", action);
                return;
            }

            // Check if WebSocket is connected before attempting subscription update
            if (!_serviceFactory.GetKalshiWebSocketClient().IsConnected())
            {
                _logger.LogWarning("WebSocket not connected, skipping subscription update for action={Action}, markets={Markets}", action, string.Join(", ", marketTickers));
                return;
            }

            var channels = KalshiConstants.AllChannels;
            bool isFirstMarket = !_serviceFactory.GetDataCache().WatchedMarkets.Any() || _serviceFactory.GetDataCache().WatchedMarkets.All(t => marketTickers.Contains(t));

            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                foreach (var channel in channels)
                {
                    _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                    _logger.LogDebug("Processing subscription for channel {Channel}, action={Action}, markets={Markets}", channel, action, string.Join(", ", marketTickers));
                    try
                    {
                        if (isFirstMarket && action == "add_markets")
                        {
                            _logger.LogDebug("First market detected, subscribing to {Channel} for {Markets}", channel, string.Join(", ", marketTickers));
                            await _serviceFactory.GetKalshiWebSocketClient().SubscribeToChannelAsync(channel, marketTickers);
                        }
                        else if (!(channel.Contains("fill") || channel.Contains("lifecycle")))
                        {
                            bool needsUpdate = action == "add_markets"
                                ? marketTickers.Any(t => !_serviceFactory.GetKalshiWebSocketClient().IsSubscribed(t, channel))
                                : marketTickers.Any(t => _serviceFactory.GetKalshiWebSocketClient().IsSubscribed(t, channel));
                            if (needsUpdate)
                            {
                                _logger.LogDebug("Updating subscription for channel {Channel}, action={Action}, markets={Markets}", channel, action, string.Join(", ", marketTickers));
                                await _serviceFactory.GetKalshiWebSocketClient().UpdateSubscriptionAsync(action, marketTickers, channel);
                                _logger.LogDebug("Completed subscription update for {Channel}, action={Action}, markets={Markets}", channel, action, string.Join(", ", marketTickers));
                            }
                            else
                            {
                                _logger.LogDebug("Skipping subscription update for {Channel}: markets already in desired state", channel);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Skipping subscription update for non-market-specific channel {Channel}", channel);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process subscription for {Channel}, action={Action}, markets={Markets}", channel, action, string.Join(", ", marketTickers));
                    }
                }
                _logger.LogDebug("Completed subscription updates for action={Action}, markets={Markets}", action, string.Join(", ", marketTickers));
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("UpdateMarketSubscriptionAsync was cancelled for action {Action}, markets: {Markets}", action, string.Join(", ", marketTickers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during subscription update for action={Action}, markets={Markets}", action, string.Join(", ", marketTickers));
            }
        }

        /// <summary>
        /// Stops the market data services and cleans up resources.
        /// </summary>
        public void StopServicesAsync()
        {
            _logger.LogDebug("Stopping MarketDataService...");
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                _logger.LogDebug("Clearing market subscriptions");
                foreach (var marketTicker in _serviceFactory.GetDataCache().WatchedMarkets.ToList())
                {
                    foreach (var channel in KalshiConstants.AllChannels.Select(_serviceFactory.GetKalshiWebSocketClient().GetChannelName))
                    {
                        _serviceFactory.GetKalshiWebSocketClient().SetSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                    }
                }
                _lastWatchedMarkets.Clear();
                _serviceFactory.GetKalshiWebSocketClient().WatchedMarkets = new HashSet<string>();
                _logger.LogDebug("Cleared subscription states and WebSocket WatchedMarkets");


                foreach (var semaphore in _marketInitializationLocks.Values)
                {
                    semaphore.Dispose();
                }
                _marketInitializationLocks.Clear();
                _logger.LogDebug("Cleared and disposed all initialization locks");

                _watchedMarketsSemaphore.Dispose();
                _watchedMarketsSemaphore = new SemaphoreSlim(1, 1);
                _logger.LogDebug("Reinitialized watched markets semaphore");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("StopAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping MarketDataService");
            }
            _logger.LogDebug("MarketDataService stopped successfully");
        }

        /// <summary>
        /// Synchronizes market data including candlesticks, order book, and positions with retry logic using Polly.
        /// </summary>
        public async Task SyncMarketDataAsync(string marketTicker)
        {
            var syncSemaphore = _marketSyncLocks.GetOrAdd(marketTicker, _ => new SemaphoreSlim(1, 1));
            bool lockAcquired = false;
            try
            {
                lockAcquired = await syncSemaphore.WaitAsync(_marketDataConfig.SemaphoreTimeoutMs, _statusTracker.GetCancellationToken());
                if (!lockAcquired)
                {
                    _logger.LogWarning("Failed to acquire sync lock for {MarketTicker} within {Timeout} ms", marketTicker, _marketDataConfig.SemaphoreTimeoutMs);
                    return;
                }

                _logger.LogInformation("Sync-Sync needed for: {MarketTicker}", marketTicker);
                var stopwatch = Stopwatch.StartNew();
                var process = Process.GetCurrentProcess();
                var startCpuTime = process.TotalProcessorTime;
                try
                {
                    _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                    _logger.LogInformation("Sync-Syncing market data for: {MarketTicker}", marketTicker);
                    _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                    if (!_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
                    {
                        _logger.LogInformation("Sync-Market {MarketTicker} not found in cache for data sync", marketTicker);
                        return;
                    }
                    _logger.LogInformation("Sync-Began market data sync for {MarketTicker}", marketTicker);
                    await _retryPolicy.ExecuteAsync(() => _serviceFactory.GetCandlestickService().UpdateCandlesticksAsync(marketTicker));
                    await _retryPolicy.ExecuteAsync(() => _serviceFactory.GetCandlestickService().PopulateMarketDataAsync(marketTicker));
                    _logger.LogInformation("Sync-Populated market data for {MarketTicker}", marketTicker);

                    await _retryPolicy.ExecuteAsync(() => _serviceFactory.GetOrderBookService().SyncOrderBookAsync(marketTicker));
                    List<OrderbookData> orderbook = new List<OrderbookData>();
                    if (_serviceFactory.GetDataCache().Markets.ContainsKey(marketTicker))
                    {
                        orderbook = _serviceFactory.GetDataCache().Markets[marketTicker].OrderbookData;
                    }
                    marketData.OrderbookData = orderbook;

                    if (marketData != null && marketData.Candlesticks != null && marketData.Candlesticks.ContainsKey("minute"))
                    {
                        marketData.AllSupportResistanceLevels = _serviceFactory.GetTradingCalculator().CalculateHistoricalSupportResistance(
                            marketTicker,
                            marketData.Candlesticks["minute"],
                            minCandlestickPercentage: _calculationsConfig.ResistanceLevels_MinCandlestickPercentage,
                            maxLevels: _calculationsConfig.ResistanceLevels_MaxLevels,
                            sigma: _calculationsConfig.ResistanceLevels_Sigma,
                            minDistance: _calculationsConfig.ResistanceLevels_MinDistance);
                    }
                    else
                    {
                        _logger.LogWarning("Skipping support resistance calculation for {MarketTicker}: Candlesticks data not available", marketTicker);
                    }

                    NotifyMarketDataUpdated(marketTicker);
                    marketData.LastSuccessfulSync = DateTime.UtcNow;

                    _logger.LogDebug("Sync-Completed market data sync for {MarketTicker}", marketTicker);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("SyncMarketDataAsync was cancelled for {MarketTicker}", marketTicker);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync market data for {MarketTicker}", marketTicker);
                }
                finally
                {
                    // Record performance metrics
                    var endCpuTime = process.TotalProcessorTime;
                    var cpuTimeUsed = endCpuTime - startCpuTime;
                    var elapsed = stopwatch.Elapsed;
                    var cpuUsagePercent = (cpuTimeUsed.TotalMilliseconds / elapsed.TotalMilliseconds) * 100 / Environment.ProcessorCount;
                    var memoryUsageMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                    var cacheSize = _serviceFactory.GetDataCache().Markets.Count;
                    var watchedMarkets = _serviceFactory.GetDataCache().WatchedMarkets.Count;

                    if (_marketDataConfig.EnablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordSpeedDialMetric("MarketDataService", "SyncElapsedTime", "Sync Elapsed Time", "Time taken for market data sync", elapsed.TotalMilliseconds, "ms", "Performance", null, 5000, 10000);
                        _performanceMonitor.RecordSpeedDialMetric("MarketDataService", "SyncCpuUsage", "Sync CPU Usage", "CPU usage during sync", cpuUsagePercent, "%", "Performance", 0, 50, 80);
                        _performanceMonitor.RecordNumericDisplayMetric("MarketDataService", "SyncMemoryUsage", "Sync Memory Usage", "Memory usage after sync", memoryUsageMB, "MB", "Performance");
                        _performanceMonitor.RecordCounterMetric("MarketDataService", "MarketsCacheSize", "Markets Cache Size", "Number of markets in cache", cacheSize, "count", "Cache");
                        _performanceMonitor.RecordCounterMetric("MarketDataService", "WatchedMarketsCount", "Watched Markets Count", "Number of watched markets", watchedMarkets, "count", "Markets");
                    }
                    else
                    {
                        _performanceMonitor.RecordDisabledMetric("MarketDataService", "SyncElapsedTime", "Sync Elapsed Time", "Time taken for market data sync", elapsed.TotalMilliseconds, "ms", "Performance");
                        _performanceMonitor.RecordDisabledMetric("MarketDataService", "SyncCpuUsage", "Sync CPU Usage", "CPU usage during sync", cpuUsagePercent, "%", "Performance");
                        _performanceMonitor.RecordDisabledMetric("MarketDataService", "SyncMemoryUsage", "Sync Memory Usage", "Memory usage after sync", memoryUsageMB, "MB", "Performance");
                        _performanceMonitor.RecordDisabledMetric("MarketDataService", "MarketsCacheSize", "Markets Cache Size", "Number of markets in cache", cacheSize, "count", "Cache");
                        _performanceMonitor.RecordDisabledMetric("MarketDataService", "WatchedMarketsCount", "Watched Markets Count", "Number of watched markets", watchedMarkets, "count", "Markets");
                    }
                }
            }
            finally
            {
                if (lockAcquired)
                {
                    syncSemaphore.Release();
                    _marketSyncLocks.TryRemove(marketTicker, out _);
                }
            }
        }

        /// <summary>
        /// Ensures market data is loaded and up-to-date, fetching from API if necessary with retry logic using Polly.
        /// </summary>
        public async Task<MarketDTO?> EnsureMarketDataAsync(string marketTicker)
        {
            try
            {
                var cancellationToken = _statusTracker.GetCancellationToken();
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Ensuring market data for: {MarketTicker}", marketTicker);
                bool marketLoadedFromAPI = false;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                var marketService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

                var marketStatus = await context.GetMarketStatus(marketTicker);
                MarketDTO? thisMarket;

                if (!marketStatus.MarketFound)
                {
                    await _retryPolicy.ExecuteAsync(() => marketService.FetchMarketsAsync(marketTicker));
                }
                thisMarket = await context.GetMarketByTicker(marketTicker);

                if (thisMarket == null)
                {
                    throw new MarketInvalidException(marketTicker, $"Market is null even after an API call for {marketTicker}");
                }

                // Don't use the cached version this time
                marketStatus = await context.GetMarketStatus(marketTicker);

                if (!marketStatus.EventFound || thisMarket.category == "")
                {
                    await _retryPolicy.ExecuteAsync(() => marketService.FetchEventAsync(thisMarket.event_ticker));
                    thisMarket.Event = await context.GetEventByTicker(thisMarket.event_ticker);
                    if (thisMarket.Event != null)
                        thisMarket.category = thisMarket.Event.category;
                }

                if (!marketStatus.SeriesFound)
                {
                    await _retryPolicy.ExecuteAsync(() => marketService.FetchSeriesAsync(thisMarket.Event.series_ticker));
                    thisMarket.Event.Series = await context.GetSeriesByTicker(thisMarket.Event.series_ticker);
                }

                // Check for incomplete market data (e.g., missing title or invalid close_time)
                if (thisMarket != null && (string.IsNullOrEmpty(thisMarket.title) ||
                    (thisMarket.close_time <= DateTime.UtcNow && !KalshiConstants.IsMarketStatusEnded(thisMarket.status))))
                {
                    _logger.LogInformation("Market {MarketTicker} has incomplete data, refetching from API", marketTicker);
                    await _retryPolicy.ExecuteAsync(() => marketService.FetchMarketsAsync(tickers: new[] { marketTicker }));
                    marketLoadedFromAPI = true;
                    thisMarket = await context.GetMarketByTicker(marketTicker);
                }

                marketStatus = await context.GetMarketStatus(marketTicker);

                if (marketStatus.MarketFound == false || marketStatus.EventFound == false || marketStatus.SeriesFound == false)
                {
                    if (marketStatus.MarketFound == false)
                    {
                        _logger.LogWarning("Failed to load complete market data for {MarketTicker}. Market is null.", marketTicker);
                    }
                    else if (marketStatus.EventFound == false)
                    {
                        _logger.LogWarning("Failed to load complete market data for {MarketTicker}. Event {event} is null.", marketTicker, thisMarket.event_ticker);
                    }
                    else if (marketStatus.SeriesFound == false)
                    {
                        _logger.LogWarning("Failed to load complete market data for {MarketTicker}. Series {series} is null.", marketTicker, thisMarket.Event.series_ticker);
                    }
                    else
                    {
                        _logger.LogError("Failed to load complete market data for {MarketTicker}", marketTicker);
                    }
                    return null;
                }

                _logger.LogDebug("Market data ensured for {MarketTicker}, loaded from API={LoadedFromAPI}",
                    marketTicker, marketLoadedFromAPI);
                return thisMarket;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("EnsureMarketDataAsync was cancelled for {MarketTicker}", marketTicker);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to ensure market data for {MarketTicker}. Error {0}. {1}", marketTicker, ex.Message, ex.InnerException?.Message);
                return null;
            }
        }

        /// <summary>
        /// Updates the list of watched markets from the database and synchronizes with WebSocket client.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateWatchedMarketsAsync()
        {
            _logger.LogDebug("Starting watched markets update");
            bool semaphoreAcquired = false;
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                if (!_readyStatus.InitializationCompleted.Task.IsCompleted)
                {
                    _logger.LogDebug("MarketDataService is stopped or not initialized, skipping watched markets update");
                    return;
                }

                semaphoreAcquired = await _watchedMarketsSemaphore.WaitAsync(_marketDataConfig.SemaphoreTimeoutMs, _statusTracker.GetCancellationToken());
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire watched markets semaphore within {Timeout}ms", _marketDataConfig.SemaphoreTimeoutMs);
                    return;
                }
                _logger.LogDebug("Acquired watched markets semaphore");

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                var markets = (await context
                    .GetMarketWatchesFiltered(brainLocksIncluded: new HashSet<Guid>() { _brainStatus.BrainLock })
                    ).Select(m => m.market_ticker);


                _logger.LogDebug("Fetched {Count} market watches from database: {Markets}", markets.Count(), string.Join(", ", markets));

                HashSet<string> allWatchedMarkets = markets.ToHashSet();

                allWatchedMarkets = allWatchedMarkets.Distinct().ToHashSet();
                _logger.LogDebug("Total watched markets after union: {Count}: {Markets}", allWatchedMarkets.Count, string.Join(", ", allWatchedMarkets));

                // Always sync WatchedMarkets to the database to prevent temporary modifications from affecting cache
                var currentWatched = _serviceFactory.GetDataCache().WatchedMarkets ?? new HashSet<string>();
                _serviceFactory.GetDataCache().WatchedMarkets = new HashSet<string>(allWatchedMarkets);
                _lastWatchedMarkets = currentWatched;
                _serviceFactory.GetKalshiWebSocketClient().WatchedMarkets = allWatchedMarkets;

                var newMarkets = allWatchedMarkets.Except(_lastWatchedMarkets).ToList();
                var removedMarkets = _lastWatchedMarkets.Except(allWatchedMarkets).ToList();

                foreach (var market in removedMarkets)
                {
                    _serviceFactory.GetDataCache().Markets.TryRemove(market, out var _);
                    _serviceFactory.GetDataCache().RecentlyRemovedMarkets.Add(market);
                    _logger.LogInformation("Removed market {MarketTicker} from cache", market);
                }

                foreach (var market in newMarkets)
                {
                    // Add to cache if not already there
                    if (!_serviceFactory.GetDataCache().Markets.ContainsKey(market))
                    {
                        var marketData = await EnsureMarketDataAsync(market);
                        if (marketData != null)
                        {
                            _serviceFactory.GetDataCache().Markets[market] = _marketDataFactory(marketData);
                            _serviceFactory.GetDataCache().Markets[market].OrderbookData = new List<OrderbookData>();
                            _serviceFactory.GetDataCache().Markets[market].LastSuccessfulSync = DateTime.UtcNow;
                            _logger.LogInformation("Added market {MarketTicker} to cache", market);
                        }
                    }
                }

                if (newMarkets.Any())
                {
                    _logger.LogDebug("Triggering subscriptions for new markets: {Markets}", string.Join(", ", newMarkets));
                    await _serviceFactory.GetKalshiWebSocketClient().SubscribeToWatchedMarketsAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("UpdateWatchedMarketsAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update watched markets");
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _watchedMarketsSemaphore.Release();
                    _logger.LogDebug("Released watched markets semaphore");
                }
                else if (!_readyStatus.InitializationCompleted.Task.IsCompleted)
                {
                    _logger.LogDebug("Not yet ready");
                }
                else
                {
                    _logger.LogWarning("Semaphore was not acquired, skipping release");
                }
            }
        }

        /// <summary>
        /// Fetches the list of currently watched markets.
        /// </summary>
        /// <returns>A list of market tickers that are being watched.</returns>
        public async Task<List<string>> FetchWatchedMarketsAsync()
        {
            _logger.LogDebug("Fetching watched markets");
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                if (!_readyStatus.InitializationCompleted.Task.IsCompleted)
                {
                    _logger.LogDebug("MarketDataService is not initialized, skipping UpdateWatchedMarketsAsync");
                }
                else
                {
                    await UpdateWatchedMarketsAsync();
                }

                if (_serviceFactory.GetDataCache().WatchedMarkets.Any())
                {
                    _logger.LogDebug("Returning {Count} watched markets from cache: {Markets}", _serviceFactory.GetDataCache().WatchedMarkets.Count, string.Join(", ", _serviceFactory.GetDataCache().WatchedMarkets));
                    return _serviceFactory.GetDataCache().WatchedMarkets.ToList();
                }

                _logger.LogDebug("Cache.WatchedMarkets is empty, re-fetching from database");
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                var markets = ((await context
                    .GetMarketWatchesFiltered(brainLocksIncluded: new HashSet<Guid>() { _brainStatus.BrainLock }))
                    );

                var marketTickers = markets.Select(m => m.market_ticker).ToHashSet();

                if (_serviceFactory.GetDataCache().WatchedMarkets == null || _serviceFactory.GetDataCache().WatchedMarkets.Count == 0)
                {
                    _serviceFactory.GetDataCache().WatchedMarkets = marketTickers;
                    _lastWatchedMarkets = marketTickers;
                    _serviceFactory.GetKalshiWebSocketClient().WatchedMarkets = marketTickers;
                }
                else
                {
                    _lastWatchedMarkets = _serviceFactory.GetDataCache().WatchedMarkets;
                }

                _logger.LogInformation("Stats: Re-fetched {Count} market watches from database: {Markets}", markets.Count(), string.Join(", ", marketTickers));

                var watchedMarkets = _serviceFactory.GetDataCache().WatchedMarkets.ToList();
                _logger.LogDebug("Returning {Count} watched markets: {Markets}", watchedMarkets.Count, string.Join(", ", watchedMarkets));
                return watchedMarkets;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("FetchWatchedMarketsAsync was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch watched markets");
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets the current order book data for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get order book data for.</param>
        /// <returns>A list of order book data entries.</returns>
        public List<OrderbookData> GetCurrentOrderBook(string marketTicker)
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Retrieving current order book for: {MarketTicker}", marketTicker);
            var orderBook = _serviceFactory.GetOrderBookService().GetCurrentOrderBook(marketTicker);
            _logger.LogDebug("Retrieved {Count} order book entries for {MarketTicker}", orderBook.Count, marketTicker);
            return orderBook;
        }

        /// <summary>
        /// Gets the market details for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get details for.</param>
        /// <returns>The market data interface, or null if not found.</returns>
        public IMarketData GetMarketDetails(string marketTicker)
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Retrieving market details for: {MarketTicker}", marketTicker);
            var data = _serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData) ? marketData : null;
            _logger.LogDebug("Market details for {MarketTicker}: {Exists}", marketTicker, data != null ? "Found" : "Not found");
            return data;
        }

        /// <summary>
        /// Gets market details for multiple market tickers in a batch operation.
        /// </summary>
        /// <param name="marketTickers">The market tickers to get details for.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping market tickers to their market data.</returns>
        public async Task<Dictionary<string, IMarketData>> GetMarketDetailsBatchAsync(IEnumerable<string> marketTickers, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogDebug("Retrieving market details for {MarketCount} tickers", marketTickers.Count());

            var result = new Dictionary<string, IMarketData>();
            foreach (var ticker in marketTickers.Distinct())
            {
                if (_serviceFactory.GetDataCache().Markets.TryGetValue(ticker, out var marketData))
                {
                    result[ticker] = marketData;
                }
            }

            _logger.LogDebug("Found details for {FoundCount} of {TotalCount} tickers", result.Count, marketTickers.Count());
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Gets the current account balance.
        /// </summary>
        /// <returns>The account balance as a double.</returns>
        public double GetAccountBalance()
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Retrieving account balance");
            var balance = _serviceFactory.GetDataCache().AccountBalance;
            _logger.LogDebug("Account balance: {Balance}", balance);
            return balance;
        }

        /// <summary>
        /// Gets the current portfolio value.
        /// </summary>
        /// <returns>The portfolio value as a double.</returns>
        public double GetPortfolioValue()
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Retrieving portfolio value");
            var value = _serviceFactory.GetDataCache().PortfolioValue;
            _logger.LogDebug("Portfolio value: {Value}", value);
            return value;
        }

        /// <summary>
        /// Gets the latest WebSocket timestamp.
        /// </summary>
        /// <returns>The latest WebSocket timestamp as a DateTime.</returns>
        public DateTime GetLatestWebSocketTimestamp()
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Retrieving latest WebSocket timestamp");
            var timestamp = _serviceFactory.GetDataCache().LastWebSocketTimestamp;
            _logger.LogDebug("Latest WebSocket timestamp: {Timestamp}", timestamp);
            return timestamp;
        }

        /// <summary>
        /// Updates the account balance from the API.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateAccountBalanceAsync()
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Updating account balance");
            using var scope = _scopeFactory.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
            _serviceFactory.GetDataCache().AccountBalance = (double)(await apiService.GetBalanceAsync()) / 100.0;
            _logger.LogDebug("Updated account balance to: {Balance}", _serviceFactory.GetDataCache().AccountBalance);
            foreach (var marketTicker in _serviceFactory.GetDataCache().Markets.Keys)
            {
                NotifyAccountBalanceUpdated(marketTicker);
            }
        }

        private async Task MassUpdateTickers()
        {
            if (_preppedTickers.IsEmpty || !_loggingConfig.StoreWebSocketEvents)
                return;

            try
            {
                var tickers = new List<TickerDTO>();
                while (_preppedTickers.TryTake(out var ticker))
                {
                    tickers.Add(ticker);
                }

                if (tickers.Count > 0)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                    foreach (var batch in tickers.Chunk(_marketDataConfig.TickerBatchSize))
                    {
                        await context.AddOrUpdateTickers(batch.ToList());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error saving batch of tickers");
            }
        }

        /// <summary>
        /// Processes ticker updates from WebSocket messages, including data validation and deduplication.
        /// </summary>
        public async Task ProcessTickerUpdate(string marketTicker, Guid marketId, int price, int yesBid, int yesAsk, int volume, int openInterest, int dollarVolume, int dollarOpenInterest, long ts, DateTime loggedDate, DateTime? processedDate = null)
        {
            if (!ValidateTickerData(marketTicker, price, yesBid, yesAsk, volume, openInterest, dollarVolume, dollarOpenInterest, ts, loggedDate, processedDate))
            {
                return;
            }

            var ticker = new TickerDTO
            {
                market_id = marketId,
                market_ticker = marketTicker,
                price = price,
                yes_bid = yesBid,
                yes_ask = yesAsk,
                volume = volume,
                open_interest = openInterest,
                dollar_volume = dollarVolume,
                dollar_open_interest = dollarOpenInterest,
                ts = ts,
                LoggedDate = loggedDate,
                ProcessedDate = processedDate
            };
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Applying ticker update for: {MarketTicker}, Ask={Ask}, Bid={Bid}", marketTicker, ticker.yes_ask, ticker.yes_bid);

            if (!_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
            {
                // Check if still watched in DB
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                var marketWatch = await context.GetMarketWatchByTicker(marketTicker);

                if (marketWatch != null) // DB-watched
                {
                    // Market is watched but not in cache, re-add it
                    var market = await EnsureMarketDataAsync(marketTicker);
                    if (market != null)
                    {
                        _serviceFactory.GetDataCache().Markets[marketTicker] = _marketDataFactory(market);
                        _serviceFactory.GetDataCache().Markets[marketTicker].OrderbookData = new List<OrderbookData>();
                        _serviceFactory.GetDataCache().Markets[marketTicker].LastSuccessfulSync = DateTime.UtcNow;
                        marketData = _serviceFactory.GetDataCache().Markets[marketTicker];
                        _logger.LogInformation("Re-added market {MarketTicker} to cache for ticker update", marketTicker);

                        // Subscribe to market channels for recovery
                        await SubscribeToMarketChannelsAsync(marketTicker);

                        // Mark as healthy after recovery
                        marketData.WebSocketHealthy = true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to re-add market {MarketTicker} to cache for ticker update", marketTicker);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning(new MarketTransientFailureException(marketTicker, $"Market {marketTicker} not found for ticker update")
                        , "Market {MarketTicker} not found for ticker update", marketTicker);
                    return;
                }
            }

            try
            {

                if (ticker.LoggedDate == default)
                {
                    ticker.LoggedDate = ticker.ts > 0
                        ? UnixHelper.ConvertFromUnixTimestamp(ticker.ts).ToUniversalTime()
                        : DateTime.UtcNow;
                }
                if (ticker.ProcessedDate == default)
                {
                    ticker.ProcessedDate = DateTime.UtcNow;
                }

                // Add ticker to queue for processing
                _preppedTickers.Add(ticker);

                // Add ticker to in-memory collection
                marketData.Tickers.Add(ticker);
                _logger.LogDebug("Added ticker for {MarketTicker}. Tickers count: {Count}, Latest: {Latest}",
                    marketTicker, marketData.Tickers.Count,
                    marketData.Tickers.OrderByDescending(t => t.LoggedDate).FirstOrDefault()?.LoggedDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? "None");

                marketData.UpdateCurrentPrice(ticker.yes_ask, ticker.yes_bid, ticker.LoggedDate, "Ticker");
                NotifyTickerAdded(marketTicker);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Cancelled ApplyTickerUpdateAsync");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save ticker for {MarketTicker}", marketTicker);
            }
        }

        /// <summary>
        /// Validates ticker data parameters to ensure data integrity before processing.
        /// </summary>
        private bool ValidateTickerData(string marketTicker, int price, int yesBid, int yesAsk, int volume, int openInterest, int dollarVolume, int dollarOpenInterest, long ts, DateTime loggedDate, DateTime? processedDate)
        {
            var errors = new List<string>();

            if (price < 0) errors.Add($"price < 0 ({price})");
            if (yesBid < 0) errors.Add($"yesBid < 0 ({yesBid})");
            if (yesAsk <= yesBid) errors.Add($"yesAsk <= yesBid ({yesAsk} <= {yesBid})");
            if (volume < 0) errors.Add($"volume < 0 ({volume})");
            if (openInterest < 0) errors.Add($"openInterest < 0 ({openInterest})");
            if (dollarVolume < 0) errors.Add($"dollarVolume < 0 ({dollarVolume})");
            if (dollarOpenInterest < 0) errors.Add($"dollarOpenInterest < 0 ({dollarOpenInterest})");
            if (ts < 0) errors.Add($"ts < 0 ({ts})");
            if (loggedDate == default(DateTime)) errors.Add($"loggedDate is default ({loggedDate})");

            if (errors.Any())
            {
                _logger.LogWarning("Invalid ticker data for {MarketTicker}: {Errors}", marketTicker, string.Join(", ", errors));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Notifies listeners that market data has been updated for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker for which data was updated.</param>
        public void NotifyMarketDataUpdated(string marketTicker)
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Notifying market data updated: {MarketTicker}", marketTicker);
            if (MarketDataUpdated != null)
                MarketDataUpdated?.Invoke(this, marketTicker);
        }

        /// <summary>
        /// Notifies listeners that position data has been updated for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker for which position data was updated.</param>
        public void NotifyPositionDataUpdated(string marketTicker)
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Notifying position data updated: {MarketTicker}", marketTicker);
            if (PositionDataUpdated != null)
                PositionDataUpdated?.Invoke(this, marketTicker);
        }

        /// <summary>
        /// Notifies listeners that a new ticker has been added for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker for which a ticker was added.</param>
        public void NotifyTickerAdded(string marketTicker)
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Notifying ticker added: {MarketTicker}", marketTicker);
            if (TickerAdded != null)
                TickerAdded?.Invoke(this, marketTicker);
        }

        /// <summary>
        /// Notifies listeners that the account balance has been updated for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker for which account balance was updated.</param>
        public void NotifyAccountBalanceUpdated(string marketTicker)
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("Notifying account balance updated: {MarketTicker}", marketTicker);
            if (AccountBalanceUpdated != null)
                AccountBalanceUpdated?.Invoke(this, marketTicker);
        }

        /// <summary>
        /// Forward fills candlestick data by filling gaps with the last known values.
        /// </summary>
        /// <param name="candlesticks">The list of candlestick data to forward fill.</param>
        /// <param name="marketTicker">The market ticker for the candlesticks.</param>
        /// <returns>A list of forward-filled candlestick data.</returns>
        public List<CandlestickData> ForwardFillCandlesticks(List<CandlestickData> candlesticks, string marketTicker)
        {
            if (candlesticks.Count <= 1) return candlesticks;
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                if (!candlesticks.Any()) return new List<CandlestickData>();

                _logger.LogDebug("Forward filling candlesticks for {MarketTicker}", marketTicker);

                var sortedCandles = candlesticks
                    .Where(c => c.MarketTicker == marketTicker)
                    .OrderBy(c => c.Date)
                    .ToList();

                if (!sortedCandles.Any()) return new List<CandlestickData>();

                var intervalType = sortedCandles.First().IntervalType;
                var start = sortedCandles.First().Date;
                var end = sortedCandles.Last().Date;

                // Create lookup dictionary for O(1) access
                Dictionary<DateTime, CandlestickData> candleLookup;
                if (intervalType == 3)
                {
                    // For day interval, group by date (ignore time)
                    candleLookup = sortedCandles
                        .GroupBy(c => c.Date.Date)
                        .ToDictionary(g => g.Key, g => g.First());
                }
                else
                {
                    // For minute/hour, exact datetime match
                    candleLookup = sortedCandles.ToDictionary(c => c.Date);
                }

                List<DateTime> allTimes;
                switch (intervalType)
                {
                    case 1:
                        allTimes = Enumerable.Range(0, (int)(end - start).TotalMinutes + 2)
                            .Select(m => start.AddMinutes(m))
                            .ToList();
                        break;
                    case 2:
                        allTimes = Enumerable.Range(0, (int)(end - start).TotalHours + 2)
                            .Select(h => start.AddHours(h))
                            .ToList();
                        break;
                    case 3:
                        allTimes = Enumerable.Range(0, (end - start).Days + 2)
                            .Select(d => start.AddDays(d))
                            .ToList();
                        break;
                    default:
                        throw new ArgumentException($"Unsupported interval type: {intervalType}");
                }

                var result = new List<CandlestickData>();
                CandlestickData? lastCandle = null;

                foreach (var time in allTimes)
                {
                    _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                    CandlestickData? candle;

                    if (intervalType == 3)
                    {
                        // Use date-only lookup for day interval
                        candleLookup.TryGetValue(time.Date, out candle);
                    }
                    else
                    {
                        // Use exact datetime lookup for minute/hour
                        candleLookup.TryGetValue(time, out candle);
                    }

                    if (candle != null)
                    {
                        lastCandle = candle;
                    }
                    else if (lastCandle != null)
                    {
                        candle = new CandlestickData
                        {
                            MarketTicker = marketTicker,
                            IntervalType = intervalType,
                            Date = time,
                            OpenInterest = lastCandle.OpenInterest,
                            Volume = 0,
                            AskOpen = lastCandle.AskClose,
                            AskHigh = lastCandle.AskClose,
                            AskLow = lastCandle.AskClose,
                            AskClose = lastCandle.AskClose,
                            BidOpen = lastCandle.BidClose,
                            BidHigh = lastCandle.BidClose,
                            BidLow = lastCandle.BidClose,
                            BidClose = lastCandle.BidClose
                        };
                    }

                    if (candle != null)
                    {
                        result.Add(candle);
                    }
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ForwardFillCandlesticks was cancelled for {MarketTicker}", marketTicker);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forward fill candlesticks for {MarketTicker}", marketTicker);
                return new List<CandlestickData>();
            }
        }

    }
}
