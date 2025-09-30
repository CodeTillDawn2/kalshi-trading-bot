using BacklashBot.State.Interfaces;
using BacklashDTOs;
using BacklashInterfaces.Constants;
using BacklashInterfaces.Enums;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace KalshiBotAPI.Websockets
{
    /// <summary>
    /// Base class for managing WebSocket channel subscriptions for real-time market data from Kalshi's trading platform.
    /// Handles subscription lifecycle, state tracking, confirmation processing, and queue management
    /// for reliable market data streaming with proper error handling and recovery mechanisms.
    /// </summary>
    public class BaseSubscriptionManager : ISubscriptionManager
    {
        /// <summary>
        /// Logger for recording subscription activities and errors.
        /// </summary>
        protected readonly ILogger<BaseSubscriptionManager> _logger;
        /// <summary>
        /// Manages WebSocket connection lifecycle and communication.
        /// </summary>
        protected readonly IWebSocketConnectionManager _connectionManager;
        /// <summary>
        /// Provides system status and cancellation token management.
        /// </summary>
        protected readonly IStatusTrackerService _statusTrackerService;
        /// <summary>
        /// Optional service for posting performance metrics.
        /// </summary>
        protected readonly IPerformanceMonitor _performanceMetrics;
        private readonly ConcurrentDictionary<string, (int Sid, HashSet<string> Markets)> _channelSubscriptions = new();
        private readonly ConcurrentDictionary<string, bool> _pendingMarketSubscriptions = new ConcurrentDictionary<string, bool>();
        private readonly ConcurrentDictionary<int, (DateTime SentTime, string Message, string Channel, string[] MarketTickers)> _pendingSubscriptionConfirmations = new ConcurrentDictionary<int, (DateTime, string, string, string[])>();
        private readonly ConcurrentDictionary<string, (int RetryCount, DateTime FirstRetryTime)> _subscriptionRetryInfo = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>> _marketChannelSubscriptionStates = new();
        private readonly SemaphoreSlim _subscriptionUpdateSynchronizationSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _channelSubscriptionSynchronizationSemaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentQueue<(string Action, string[] MarketTickers, string ChannelAction)> _queuedSubscriptionUpdateRequests = new();
        private int _nextMessageId = 1;
        private Task _subscriptionQueueProcessorTask = null!;
        private Task _pendingConfirmationMonitorTask = null!;
        private readonly object _sequenceNumberSynchronizationLock = new object();
        private long _latestProcessedSequenceNumber = 0;
        // private int _orderBookSubscriptionId = 0; // Currently unused, reserved for future subscription ID tracking
        private readonly PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long> _orderBookUpdateQueue = new();
        private readonly object _orderBookQueueSynchronizationLock = new object();
        private readonly ConcurrentDictionary<string, long> _messageTypeCounts = new ConcurrentDictionary<string, long>();
        private Task _orderBookQueueProcessorTask = null!;
        private CancellationToken _processingCancellationToken;

        // Configuration options
        private readonly int _subscriptionTimeoutMs;
        private readonly int _confirmationTimeoutSeconds;
        private readonly int _retryDelayMs;
        private readonly int _maxQueueSize;
        private readonly int _batchSize;
        private readonly int _healthCheckIntervalMs;
        private readonly bool _enableMetrics;

        // Exponential backoff configuration for subscription retries
        private readonly int _maxSubscriptionRetries = 5;
        private readonly int _baseRetryDelayMs = 1000;
        private readonly int _maxRetryDelayMs = 30000;

        // Performance metrics
        private readonly ConcurrentDictionary<string, long> _operationTimings = new();
        private readonly ConcurrentDictionary<string, long> _operationCounts = new();
        private readonly ConcurrentDictionary<string, long> _successCounts = new();

        // Lock contention metrics
        private readonly ConcurrentDictionary<string, long> _lockAcquisitionTimes = new();
        private readonly ConcurrentDictionary<string, long> _lockContentionCounts = new();
        private readonly ConcurrentDictionary<string, long> _lockWaitTimes = new();

        // Subscription deduplication
        private readonly ConcurrentDictionary<string, DateTime> _recentSubscriptions = new();

        /// <summary>
        /// Event raised when WebSocket health becomes unhealthy for specific markets.
        /// </summary>
        public event EventHandler<string[]>? MarketWebSocketUnhealthy;

        /// <summary>
        /// Event raised when WebSocket health is restored for specific markets.
        /// </summary>
        public event EventHandler<string[]>? MarketWebSocketHealthy;

        // Health monitoring
        private Task _healthMonitorTask = null!;
        private readonly ReaderWriterLockSlim _subscriptionLock = new();
        private readonly ReaderWriterLockSlim _queueLock = new();

        /// <summary>
        /// Initializes a new instance of the BaseSubscriptionManager with required dependencies.
        /// Sets up internal data structures for subscription management, state tracking, and queue processing.
        /// </summary>
        /// <param name="logger">Logger for recording subscription activities and errors.</param>
        /// <param name="connectionManager">Manages WebSocket connection lifecycle and communication.</param>
        /// <param name="statusTrackerService">Provides system status and cancellation token management.</param>
        /// <param name="config">Configuration options for subscription manager settings.</param>
        /// <param name="performanceMetrics">Optional service for posting performance metrics.</param>
        public BaseSubscriptionManager(
            ILogger<BaseSubscriptionManager> logger,
            IWebSocketConnectionManager connectionManager,
            IStatusTrackerService statusTrackerService,
            IOptions<SubscriptionManagerConfig> config,
            IPerformanceMonitor performanceMetrics)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _statusTrackerService = statusTrackerService;
            _performanceMetrics = performanceMetrics;
            _processingCancellationToken = statusTrackerService.GetCancellationToken();

            // Load configuration values from injected options
            var subscriptionConfig = config.Value;
            _subscriptionTimeoutMs = subscriptionConfig.SubscriptionTimeoutMs;
            _confirmationTimeoutSeconds = subscriptionConfig.ConfirmationTimeoutSeconds;
            _retryDelayMs = subscriptionConfig.SubscriptionManagerRetryDelayMs;
            _maxQueueSize = subscriptionConfig.MaxQueueSize;
            _batchSize = subscriptionConfig.BatchSize;
            _healthCheckIntervalMs = subscriptionConfig.HealthCheckIntervalMs;
            _enableMetrics = subscriptionConfig.EnableSubscriptionManagerMetrics;

            // Initialize message type counts
            _messageTypeCounts.TryAdd("OrderBook", 0);
            _messageTypeCounts.TryAdd("Ticker", 0);
            _messageTypeCounts.TryAdd("Trade", 0);
            _messageTypeCounts.TryAdd("Fill", 0);
            _messageTypeCounts.TryAdd("MarketLifecycle", 0);
            _messageTypeCounts.TryAdd("EventLifecycle", 0);
            _messageTypeCounts.TryAdd("Subscribe", 0);
            _messageTypeCounts.TryAdd("Unsubscribe", 0);
            _messageTypeCounts.TryAdd("Ok", 0);
            _messageTypeCounts.TryAdd("Error", 0);
            _messageTypeCounts.TryAdd("Unknown", 0);

            // Initialize performance metrics
            _operationTimings.TryAdd("Subscribe", 0);
            _operationTimings.TryAdd("Update", 0);
            _operationTimings.TryAdd("Unsubscribe", 0);
            _operationCounts.TryAdd("Subscribe", 0);
            _operationCounts.TryAdd("Update", 0);
            _operationCounts.TryAdd("Unsubscribe", 0);
            _successCounts.TryAdd("Subscribe", 0);
            _successCounts.TryAdd("Update", 0);
            _successCounts.TryAdd("Unsubscribe", 0);
        }

        /// <summary>
        /// Starts the background tasks for processing subscription queues and monitoring confirmations.
        /// Initializes the subscription queue processor, pending confirmation monitor, and order book queue processor.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartAsync()
        {
            _subscriptionQueueProcessorTask = Task.Run(() => ProcessQueuePeriodicallyAsync(), _processingCancellationToken);
            _pendingConfirmationMonitorTask = Task.Run(() => CheckPendingConfirmationsAsync(), _processingCancellationToken);
            _orderBookQueueProcessorTask = Task.Run(() => ProcessOrderBookQueuePeriodicallyAsync(), _processingCancellationToken);
            _healthMonitorTask = Task.Run(() => MonitorSubscriptionHealthAsync(), _processingCancellationToken);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Stops all background tasks gracefully, waiting for completion of ongoing operations.
        /// Ensures proper cleanup of subscription processing tasks.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StopAsync()
        {
            if (_subscriptionQueueProcessorTask != null && !_subscriptionQueueProcessorTask.IsCompleted)
            {
                await _subscriptionQueueProcessorTask.ConfigureAwait(false);
            }
            if (_pendingConfirmationMonitorTask != null && !_pendingConfirmationMonitorTask.IsCompleted)
            {
                await _pendingConfirmationMonitorTask.ConfigureAwait(false);
            }
            if (_orderBookQueueProcessorTask != null && !_orderBookQueueProcessorTask.IsCompleted)
            {
                await _orderBookQueueProcessorTask.ConfigureAwait(false);
            }
            if (_healthMonitorTask != null && !_healthMonitorTask.IsCompleted)
            {
                await _healthMonitorTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets or sets the collection of market tickers that are being watched for real-time data.
        /// This property is synchronized with the data cache for persistence across application restarts.
        /// </summary>
        public virtual HashSet<string> WatchedMarkets { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets the dictionary containing counts of different message types processed by the subscription manager.
        /// Used for monitoring and diagnostics of WebSocket message traffic.
        /// </summary>
        public ConcurrentDictionary<string, long> EventCounts => _messageTypeCounts;

        /// <summary>
        /// Gets the current count of the subscription update synchronization semaphore.
        /// Indicates whether subscription updates are currently being processed.
        /// </summary>
        public int SubscriptionUpdateSemaphoreCount => _subscriptionUpdateSynchronizationSemaphore.CurrentCount;

        /// <summary>
        /// Gets the current count of the channel subscription synchronization semaphore.
        /// Indicates whether channel subscription operations are currently being processed.
        /// </summary>
        public int ChannelSubscriptionSemaphoreCount => _channelSubscriptionSynchronizationSemaphore.CurrentCount;

        /// <summary>
        /// Gets the number of queued subscription update requests waiting to be processed.
        /// Used for monitoring the backlog of subscription operations.
        /// </summary>
        public int QueuedSubscriptionUpdatesCount => _queuedSubscriptionUpdateRequests.Count;

        /// <summary>
        /// Subscribes to a specific WebSocket channel for the given market tickers.
        /// Handles both new subscriptions and updates to existing subscriptions, with proper
        /// state management and error handling for connection issues.
        /// </summary>
        /// <param name="action">The action type (e.g., "orderbook", "ticker", "trade") to subscribe to.</param>
        /// <param name="marketTickers">Array of market tickers to subscribe to for this channel.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SubscribeToChannelAsync(string action, string[] marketTickers)
        {
            var startTime = DateTime.UtcNow;
            bool success = false;
            bool lockExitedEarly = false;
            _logger.LogInformation("Subscribing to channel: action={Action}, markets={Markets}, current subscriptions count: {Count}", action, string.Join(", ", marketTickers), _channelSubscriptions.Count);

            // Check for subscription deduplication
            var deduplicationKey = $"{action}:{string.Join(",", marketTickers.OrderBy(m => m))}";
            if (_recentSubscriptions.TryGetValue(deduplicationKey, out var lastSubscriptionTime) &&
                DateTime.UtcNow - lastSubscriptionTime < TimeSpan.FromSeconds(5))
            {
                _logger.LogWarning("Duplicate subscription attempt for {Action} with markets {Markets}, skipping", action, string.Join(", ", marketTickers));
                return;
            }
            _recentSubscriptions[deduplicationKey] = DateTime.UtcNow;

            if (!_connectionManager.IsConnected())
            {
                if (_queuedSubscriptionUpdateRequests.Count >= _maxQueueSize)
                {
                    _logger.LogWarning("Subscription queue full ({MaxSize}), dropping subscription: action={Action}, markets={Markets}", _maxQueueSize, action, string.Join(", ", marketTickers));
                    return;
                }
                _logger.LogWarning("WebSocket not connected, queuing subscription: action={Action}, markets={Markets}", action, string.Join(", ", marketTickers));
                _queuedSubscriptionUpdateRequests.Enqueue(("add_markets", marketTickers, action));
                return;
            }

            // Track semaphore contention
            var semaphoreWaitStart = DateTime.UtcNow;
            await _channelSubscriptionSynchronizationSemaphore.WaitAsync(_processingCancellationToken);
            var semaphoreWaitTime = DateTime.UtcNow - semaphoreWaitStart;
            RecordLockMetrics("ChannelSubscriptionSemaphore", semaphoreWaitTime, semaphoreWaitTime > TimeSpan.Zero);

            // Track lock contention
            var lockWaitStart = DateTime.UtcNow;
            _subscriptionLock.EnterWriteLock();
            var lockWaitTime = DateTime.UtcNow - lockWaitStart;
            RecordLockMetrics("SubscriptionLock", lockWaitTime, lockWaitTime > TimeSpan.Zero);
            try
            {
                _processingCancellationToken.ThrowIfCancellationRequested();

                var channel = GetChannelName(action);
                if (!_channelSubscriptions.TryGetValue(channel, out var subscription))
                {
                    subscription = (0, new HashSet<string>());
                    _channelSubscriptions[channel] = subscription;
                }

                // For market-specific channels, check if ALL requested markets are already subscribed
                if (KalshiConstants.MarketChannelsDelta.Contains(channel) && subscription.Sid != 0)
                {
                    var alreadySubscribedMarkets = marketTickers.Where(ticker => subscription.Markets.Contains(ticker)).ToArray();
                    var notSubscribedMarkets = marketTickers.Where(ticker => !subscription.Markets.Contains(ticker)).ToArray();

                    if (alreadySubscribedMarkets.Any() && notSubscribedMarkets.Any())
                    {
                        _logger.LogDebug("Channel {Channel} has SID {Sid}, some markets already subscribed {Subscribed}, adding new markets {NewMarkets}",
                            channel, subscription.Sid, string.Join(", ", alreadySubscribedMarkets), string.Join(", ", notSubscribedMarkets));
                        // Continue to use update_subscription logic below
                    }
                    else if (alreadySubscribedMarkets.Length == marketTickers.Length)
                    {
                        _logger.LogWarning("All requested markets {Markets} already subscribed to channel {Channel} with SID {Sid}, skipping duplicate subscription attempt",
                            string.Join(", ", marketTickers), channel, subscription.Sid);
                        return;
                    }
                    // If no markets are subscribed yet, continue with normal subscription
                }

                // For market-specific channels, if we have a SID, use update_subscription instead of subscribe
                if (KalshiConstants.MarketChannelsDelta.Contains(channel) && subscription.Sid != 0 && marketTickers.Any())
                {
                    _logger.LogDebug("Channel {Channel} already has SID {Sid}, using update_subscription for new markets", channel, subscription.Sid);
                    // Release lock before calling UpdateSubscriptionAsync to avoid deadlock
                    _subscriptionLock.ExitWriteLock();
                    lockExitedEarly = true;
                    await UpdateSubscriptionAsync("add_markets", marketTickers, action);
                    return;
                }

                var newSubscriptions = marketTickers
                    .Where(ticker => CanSubscribeToMarket(ticker, channel) && !subscription.Markets.Contains(ticker))
                    .ToArray();

                _logger.LogDebug("New subscriptions for {Channel}: {NewMarkets}, existing: {Existing}", channel, string.Join(", ", newSubscriptions), string.Join(", ", subscription.Markets));

                if (new[] { KalshiConstants.ScriptType_Feed_Fill, KalshiConstants.Channel_Market_Lifecycle_V2 }.Contains(channel))
                {
                    if (newSubscriptions.Any())
                    {
                        _logger.LogDebug("Ignoring market tickers for {Channel} as it operates in 'All markets' mode: {Markets}", channel, string.Join(", ", newSubscriptions));
                        newSubscriptions = Array.Empty<string>();
                    }
                }

                if (!newSubscriptions.Any() && !new[] { KalshiConstants.ScriptType_Feed_Fill, KalshiConstants.Channel_Market_Lifecycle_V2 }.Contains(channel) && subscription.Markets.Any())
                {
                    _logger.LogWarning("No new markets to subscribe for {Channel}: requested {Markets}, existing {Existing}", channel, string.Join(", ", marketTickers), string.Join(", ", subscription.Markets));
                    return;
                }

                foreach (var ticker in newSubscriptions)
                {
                    _processingCancellationToken.ThrowIfCancellationRequested();
                    SetSubscriptionState(ticker, channel, SubscriptionState.Subscribing);
                }

                var subscriptionId = Interlocked.Increment(ref _nextMessageId);
                string message;
                bool skipMessage = false;

                if (subscription.Sid != 0 && subscription.Markets.Any() && !new[] { KalshiConstants.ScriptType_Feed_Fill, KalshiConstants.Channel_Market_Lifecycle_V2 }.Contains(channel))
                {
                    var updateCommand = new
                    {
                        id = subscriptionId,
                        cmd = "update_subscription",
                        @params = new
                        {
                            sids = new[] { subscription.Sid },
                            market_tickers = newSubscriptions,
                            action = "add_markets"
                        }
                    };
                    message = JsonSerializer.Serialize(updateCommand);
                }
                else
                {
                    string[] marketsToSubscribeTo = KalshiConstants.MarketChannelsDelta.Contains(channel) ? newSubscriptions : Array.Empty<string>();

                    if (KalshiConstants.MarketChannelsDelta.Contains(channel) && newSubscriptions.Length == 0)
                    {
                        _logger.LogWarning("No markets to subscribe for {Channel}, but channel requires market tickers: {Markets}. Skipping subscription.", channel, string.Join(", ", newSubscriptions));
                        skipMessage = true;
                    }

                    var subscribeCommand = new
                    {
                        id = subscriptionId,
                        cmd = "subscribe",
                        @params = new
                        {
                            channels = new[] { channel },
                            market_tickers = marketsToSubscribeTo
                        }
                    };
                    message = JsonSerializer.Serialize(subscribeCommand);
                }

                // Only add pending confirmations for update_subscription, not for initial subscribe
                // Subscribe confirmations may not include ID, so we don't track them as pending
                if (KalshiConstants.MarketChannelsDelta.Contains(channel) && !skipMessage && message.Contains("\"cmd\": \"update_subscription\""))
                {
                    _pendingSubscriptionConfirmations.TryAdd(subscriptionId, (SentTime: DateTime.UtcNow, Message: message, Channel: channel, MarketTickers: newSubscriptions));
                }

                if (!skipMessage)
                {
                    await _connectionManager.SendMessageAsync(message);

                    // Log the full message for debugging
                    _logger.LogInformation("Full subscribe message: {Message}", message);

                    var currentMarkets = subscription.Markets;
                    foreach (var ticker in newSubscriptions)
                    {
                        currentMarkets.Add(ticker);
                        _pendingMarketSubscriptions.TryAdd($"{action}:{ticker}", true);

                        // Update WatchedMarkets
                        if (!WatchedMarkets.Contains(ticker))
                        {
                            WatchedMarkets.Add(ticker);
                            _logger.LogDebug("Added {MarketTicker} to WatchedMarkets", ticker);
                        }
                    }
                    _channelSubscriptions[channel] = (subscription.Sid, currentMarkets);
                }
                else
                {
                    _logger.LogWarning("Skipping subscription message for {Channel} as no markets to subscribe", channel);
                }
                success = true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("SubscribeToChannelAsync was cancelled for action {Action}, markets: {Markets}", action, string.Join(", ", marketTickers));
            }
            catch (Exception ex)
            {
                foreach (var ticker in marketTickers)
                {
                    SetSubscriptionState(ticker, GetChannelName(action), SubscriptionState.Unsubscribed);
                }
                _logger.LogError(ex, "Failed to subscribe to {Channel} for markets: {Markets}", action, string.Join(", ", marketTickers));
                throw;
            }
            finally
            {
                // Only exit the lock if it wasn't already exited early
                if (!lockExitedEarly)
                {
                    _subscriptionLock.ExitWriteLock();
                }
                _channelSubscriptionSynchronizationSemaphore.Release();
                var duration = DateTime.UtcNow - startTime;
                RecordOperationMetrics("Subscribe", duration, success);
            }
        }

        /// <summary>
        /// Subscribes to all watched markets for all available market data channels.
        /// Ensures that all markets in the WatchedMarkets collection are subscribed to
        /// receive real-time data for all supported channel types.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SubscribeToWatchedMarketsAsync()
        {
            try
            {
                _processingCancellationToken.ThrowIfCancellationRequested();
                if (!_connectionManager.IsConnected())
                {
                    _logger.LogWarning("WebSocket not connected, cannot subscribe to watched markets");
                    return;
                }

                if (!WatchedMarkets.Any())
                {
                    _logger.LogDebug("No watched markets to subscribe to");
                    return;
                }

                _logger.LogInformation("Subscribing to watched markets: {Markets}", string.Join(", ", WatchedMarkets));
                foreach (var action in KalshiConstants.MarketChannels)
                {
                    _processingCancellationToken.ThrowIfCancellationRequested();
                    var marketsToSubscribe = WatchedMarkets
                        .Where(m => !IsSubscribed(m, action) && CanSubscribeToMarket(m, GetChannelName(action)))
                        .ToArray();
                    if (marketsToSubscribe.Any())
                    {
                        _logger.LogDebug("Subscribing to {Action} for markets: {Markets}", action, string.Join(", ", marketsToSubscribe));
                        await SubscribeToChannelAsync(action, marketsToSubscribe);
                    }
                    else
                    {
                        _logger.LogDebug("No new markets to subscribe for {Action}", action);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("SubscribeToWatchedMarketsAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to watched markets");
            }
        }

        /// <summary>
        /// Updates an existing subscription by adding or removing markets from a specific channel.
        /// Handles both adding new markets to an existing subscription and removing markets from it.
        /// </summary>
        /// <param name="action">The action to perform: "add_markets" or "delete_markets".</param>
        /// <param name="marketTickers">Array of market tickers to add or remove.</param>
        /// <param name="channelAction">The channel action (e.g., "orderbook", "ticker") to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction)
        {
            var startTime = DateTime.UtcNow;
            bool success = false;
            _logger.LogInformation("UpdateSubscriptionAsync called: action={Action}, channel={Channel}, markets={Markets}",
                action, channelAction, string.Join(", ", marketTickers));

            if (!new[] { "add_markets", "delete_markets" }.Contains(action))
            {
                _logger.LogError("Invalid action: {Action}", action);
                throw new Exception($"Invalid action: {action}");
            }

            if (!_connectionManager.IsConnected())
            {
                if (_queuedSubscriptionUpdateRequests.Count >= _maxQueueSize)
                {
                    _logger.LogWarning("Subscription queue full ({MaxSize}), dropping update: action={Action}, channel={Channel}, markets={Markets}", _maxQueueSize, action, channelAction, string.Join(", ", marketTickers));
                    return;
                }
                _logger.LogWarning("WebSocket not connected, queuing subscription update: action={Action}, channel={Channel}, markets={Markets}",
                    action, channelAction, string.Join(", ", marketTickers));
                _queuedSubscriptionUpdateRequests.Enqueue((action, marketTickers, channelAction));
                return;
            }

            var channel = GetChannelName(channelAction);

            // Track semaphore contention
            var semaphoreWaitStart = DateTime.UtcNow;
            await _subscriptionUpdateSynchronizationSemaphore.WaitAsync(_processingCancellationToken);
            var semaphoreWaitTime = DateTime.UtcNow - semaphoreWaitStart;
            RecordLockMetrics("SubscriptionUpdateSemaphore", semaphoreWaitTime, semaphoreWaitTime > TimeSpan.Zero);

            _subscriptionLock.EnterWriteLock();
            try
            {
                _processingCancellationToken.ThrowIfCancellationRequested();

                if (!_channelSubscriptions.TryGetValue(channel, out var subscription))
                {
                    subscription = (0, new HashSet<string>());
                    _channelSubscriptions[channel] = subscription;
                    _logger.LogInformation("Initialized new subscription for {Channel}", channel);
                }

                var marketsToUpdate = action == "add_markets"
                    ? marketTickers.Where(t => !subscription.Markets.Contains(t)).ToArray()
                    : marketTickers.Where(t => subscription.Markets.Contains(t)).ToArray();


                if (!marketsToUpdate.Any())
                {
                    _logger.LogDebug("No markets to update for {Action} on {Channel}: {Markets}", action, channel, string.Join(", ", marketTickers));
                    return;
                }

                if (subscription.Sid == 0 && action == "delete_markets")
                {
                    _logger.LogWarning("No SID exists for {Channel}, no active subscription to delete for markets: {Markets}",
                        channel, string.Join(", ", marketsToUpdate));
                    return;
                }

                var subscriptionId = GenerateNextMessageId();
                var updateCommand = new
                {
                    id = subscriptionId,
                    cmd = "update_subscription",
                    @params = new
                    {
                        sids = new[] { subscription.Sid },
                        market_tickers = marketsToUpdate,
                        action
                    }
                };

                var message = JsonSerializer.Serialize(updateCommand);
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                _logger.LogInformation("[{Timestamp}] SENDING update_subscription command: channel={Channel}, SID={Sid}, ID={Id}, action={Action}, markets={Markets}",
                    timestamp, channel, subscription.Sid, subscriptionId, action, string.Join(", ", marketsToUpdate));

                // Log the full message for debugging
                _logger.LogDebug("[{Timestamp}] Full update_subscription message ID={Id}: {Message}", timestamp, subscriptionId, message);

                if (KalshiConstants.MarketChannelsDelta.Contains(channel))
                {
                    _pendingSubscriptionConfirmations.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel, marketsToUpdate));
                    if (action == "delete_markets")
                    {
                        _logger.LogDebug("Added to pending {action} confirms for {Channel}, ID={Id}, expecting 'ok' or 'unsubscribed', for markets {markets}", action, channel, subscriptionId, marketsToUpdate);
                    }
                    else
                    {
                        _logger.LogDebug("Added to pending {action} confirms for {Channel}, ID={Id}, expecting 'ok' or 'subscribed', for markets {markets}", action, channel, subscriptionId, marketsToUpdate);
                    }
                }
                else
                {
                    _logger.LogDebug("Skipping pending confirms for {Channel}, ID={Id}, non-market-specific channel", channel, subscriptionId);
                }

                await _connectionManager.SendMessageAsync(message);

                var updatedMarkets = new HashSet<string>(subscription.Markets);
                foreach (var ticker in marketsToUpdate)
                {
                    if (action == "add_markets")
                    {
                        updatedMarkets.Add(ticker);
                        // Update WatchedMarkets
                        if (!WatchedMarkets.Contains(ticker))
                        {
                            WatchedMarkets.Add(ticker);
                            _logger.LogDebug("Added {MarketTicker} to WatchedMarkets during update", ticker);
                        }
                    }
                    else
                    {
                        updatedMarkets.Remove(ticker);
                        // Check if market is still subscribed to other channels
                        bool stillSubscribed = _channelSubscriptions.Any(s => s.Key != channel && s.Value.Markets.Contains(ticker));
                        if (!stillSubscribed && WatchedMarkets.Contains(ticker))
                        {
                            WatchedMarkets.Remove(ticker);
                            _logger.LogDebug("Removed {MarketTicker} from WatchedMarkets as no longer subscribed to any channels", ticker);
                        }
                    }
                    _pendingMarketSubscriptions.TryRemove($"{channelAction}:{ticker}", out bool _);
                }
                _channelSubscriptions[channel] = (subscription.Sid, updatedMarkets);

                _logger.LogDebug("Updated subscription locally: channel={Channel}, SID={Sid}, action={Action}, new markets={Markets}",
                    channel, subscription.Sid, action, string.Join(", ", updatedMarkets));
                success = true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("UpdateSubscriptionAsync was cancelled for channel {Channel}, action={Action}", channel, action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update subscription for {Channel}, action={Action}, markets={Markets}",
                    channel, action, string.Join(", ", marketTickers));
                throw;
            }
            finally
            {
                _subscriptionLock.ExitWriteLock();
                _subscriptionUpdateSynchronizationSemaphore.Release();
                var duration = DateTime.UtcNow - startTime;
                RecordOperationMetrics("Update", duration, success);
            }
        }

        /// <summary>
        /// Unsubscribes from a specific channel, removing all markets from that subscription.
        /// Sends an unsubscription request to the server and cleans up local state.
        /// </summary>
        /// <param name="action">The action type (e.g., "orderbook", "ticker") to unsubscribe from.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UnsubscribeFromChannelAsync(string action)
        {
            _logger.LogDebug("Unsubscribing from channel: action={Action}", action);
            if (!_connectionManager.IsConnected())
            {
                _logger.LogWarning("WebSocket not connected, cannot unsubscribe from {Action}", action);
                return;
            }

            _subscriptionLock.EnterWriteLock();
            try
            {
                _processingCancellationToken.ThrowIfCancellationRequested();

                var channel = GetChannelName(action);
                if (!_channelSubscriptions.TryGetValue(channel, out var subscription))
                {
                    _logger.LogWarning("No active subscription for {Channel}, skipping unsubscription", channel);
                    return;
                }

                if (subscription.Sid == 0)
                {
                    _logger.LogWarning("No SID for {Channel}, removing local subscription", channel);
                    ((IDictionary<string, (int Sid, HashSet<string> Markets)>)_channelSubscriptions).Remove(channel);
                    return;
                }

                var subscriptionId = Interlocked.Increment(ref _nextMessageId);
                var unsubscribeCommand = new
                {
                    id = subscriptionId,
                    cmd = "unsubscribe",
                    @params = new
                    {
                        sids = new[] { subscription.Sid }
                    }
                };

                var message = JsonSerializer.Serialize(unsubscribeCommand);
                _logger.LogInformation("Sending unsubscription request for {Channel}, ID={Id}, SID={Sid}, message={Message}", channel, subscriptionId, subscription.Sid, message);
                _pendingSubscriptionConfirmations.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel, Array.Empty<string>()));
                await _connectionManager.SendMessageAsync(message);

                foreach (var marketTicker in subscription.Markets)
                {
                    SetSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                    // Check if market is still subscribed to other channels
                    bool stillSubscribed = _channelSubscriptions.Any(s => s.Key != channel && s.Value.Markets.Contains(marketTicker));
                    if (!stillSubscribed && WatchedMarkets.Contains(marketTicker))
                    {
                        WatchedMarkets.Remove(marketTicker);
                        _logger.LogDebug("Removed {MarketTicker} from WatchedMarkets as no longer subscribed to any channels", marketTicker);
                    }
                }
                ((IDictionary<string, (int Sid, HashSet<string> Markets)>)_channelSubscriptions).Remove(channel);
                _logger.LogInformation("Removed local subscription for {Channel}", channel);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("UnsubscribeFromChannelAsync was cancelled for action {Action}", action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from {Action}", action);
                throw;
            }
            finally
            {
                _subscriptionLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Unsubscribes from all active channels, removing all market subscriptions.
        /// Sends unsubscription requests for all active subscriptions and cleans up local state.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UnsubscribeFromAllAsync()
        {
            _logger.LogDebug("Acquiring subscription lock for unsubscribe");
            _subscriptionLock.EnterWriteLock();
            try
            {
                _processingCancellationToken.ThrowIfCancellationRequested();

                foreach (var subscription in _channelSubscriptions.ToList())
                {
                    var channel = subscription.Key;
                    var sid = subscription.Value.Sid;
                    var markets = subscription.Value.Markets.ToArray();
                    if (sid != 0)
                    {
                        var subscriptionId = GenerateNextMessageId();
                        var unsubscribeCommand = new
                        {
                            id = subscriptionId,
                            cmd = "unsubscribe",
                            @params = new
                            {
                                sids = new[] { sid }
                            }
                        };
                        var message = JsonSerializer.Serialize(unsubscribeCommand);
                        _logger.LogInformation("Sending unsubscribe command: channel={Channel}, SID={Sid}, ID={Id}, markets={Markets}",
                            channel, sid, subscriptionId, string.Join(", ", markets));
                        _pendingSubscriptionConfirmations.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel, markets));
                        await _connectionManager.SendMessageAsync(message);
                        foreach (var marketTicker in markets)
                        {
                            SetSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                            // Remove from WatchedMarkets since unsubscribing from all
                            if (WatchedMarkets.Contains(marketTicker))
                            {
                                WatchedMarkets.Remove(marketTicker);
                                _logger.LogDebug("Removed {MarketTicker} from WatchedMarkets during full unsubscription", marketTicker);
                            }
                        }
                        _logger.LogDebug("Marked local subscription as unsubscribed for channel {Channel}", channel);
                    }
                    else
                    {
                        _logger.LogWarning("No SID for channel {Channel}, skipping unsubscribe command", channel);
                    }
                }
                _channelSubscriptions.Clear();
                _logger.LogDebug("Cleared all subscriptions");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("UnsubscribeFromAllAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from all feeds");
            }
            finally
            {
                _subscriptionLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Resubscribes to existing channel subscriptions, either selectively or forcefully.
        /// Used to restore subscriptions after connection recovery or when forcing a complete resubscription.
        /// </summary>
        /// <param name="force">If true, resubscribes to all channels regardless of current state. If false, only resubscribes to channels without active subscriptions.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ResubscribeAsync(bool force = false)
        {
            _logger.LogInformation("Resubscribing to existing subscriptions, force={Force}", force);

            if (!_connectionManager.IsConnected())
            {
                _logger.LogWarning("WebSocket not connected, cannot resubscribe");
                return;
            }

            // Only resubscribe to channels that don't already have active subscriptions (SID == 0)
            // or if force is true, resubscribe to all channels
            var subscriptionsToResubscribe = force
                ? _channelSubscriptions.Where(s => s.Value.Sid != 0).ToList()
                : _channelSubscriptions.Where(s => s.Value.Sid == 0).ToList();

            if (!subscriptionsToResubscribe.Any())
            {
                _logger.LogDebug("No subscriptions need resubscribing");
                return;
            }

            foreach (var subscription in subscriptionsToResubscribe)
            {
                var channel = subscription.Key;
                var markets = subscription.Value.Markets.ToArray();

                if (markets.Any() || new[] { KalshiConstants.ScriptType_Feed_Fill, KalshiConstants.Channel_Market_Lifecycle_V2 }.Contains(channel))
                {
                    _logger.LogDebug("Resubscribing to {Channel} for markets: {Markets}", channel, string.Join(", ", markets));
                    await SubscribeToChannelAsync(GetActionFromChannel(channel), markets);
                }
            }

            _logger.LogInformation("Resubscription completed");
        }

        private string GetActionFromChannel(string channel) => channel switch
        {
            "orderbook_delta" => "orderbook",
            "ticker" => "ticker",
            "trade" => "trade",
            "fill" => "fill",
            "market_lifecycle_v2" => "lifecycle",
            _ => throw new ArgumentException($"Unknown channel: {channel}")
        };

        /// <summary>
        /// Calculates the exponential backoff delay for subscription retries.
        /// Uses exponential backoff with jitter to avoid thundering herd problems.
        /// </summary>
        /// <param name="retryCount">The current retry attempt number (0-based).</param>
        /// <returns>The delay in milliseconds before the next retry attempt.</returns>
        private int CalculateRetryDelayMs(int retryCount)
        {
            if (retryCount >= _maxSubscriptionRetries)
            {
                return _maxRetryDelayMs; // Cap at maximum delay
            }

            // Exponential backoff: baseDelay * 2^retryCount
            var exponentialDelay = _baseRetryDelayMs * Math.Pow(2, retryCount);

            // Add jitter (±25% randomization) to avoid thundering herd
            var random = new Random();
            var jitter = random.NextDouble() * 0.5 - 0.25; // -25% to +25%
            var delayWithJitter = exponentialDelay * (1 + jitter);

            // Cap at maximum delay
            return Math.Min((int)delayWithJitter, _maxRetryDelayMs);
        }

        /// <summary>
        /// Cleans up subscription state for markets that failed to receive confirmation within the timeout period.
        /// Marks markets as unsubscribed and removes them from the channel subscription tracking.
        /// </summary>
        /// <param name="channel">The channel name where the subscription failed.</param>
        /// <param name="marketTickers">Array of market tickers that failed to subscribe.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CleanupExpiredSubscriptionAsync(string channel, string[] marketTickers)
        {
            _subscriptionLock.EnterWriteLock();
            try
            {
                _processingCancellationToken.ThrowIfCancellationRequested();

                // Mark each market as unsubscribed
                foreach (var ticker in marketTickers)
                {
                    SetSubscriptionState(ticker, channel, SubscriptionState.Unsubscribed);
                }

                // Remove markets from channel subscriptions
                if (_channelSubscriptions.TryGetValue(channel, out var subscription))
                {
                    var updatedMarkets = new HashSet<string>(subscription.Markets);
                    foreach (var ticker in marketTickers)
                    {
                        updatedMarkets.Remove(ticker);
                    }

                    if (updatedMarkets.Any())
                    {
                        _channelSubscriptions[channel] = (subscription.Sid, updatedMarkets);
                    }
                    else
                    {
                        // No markets left, remove the entire channel subscription
                        ((IDictionary<string, (int Sid, HashSet<string> Markets)>)_channelSubscriptions).Remove(channel);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("CleanupExpiredSubscriptionAsync was cancelled for channel {Channel}", channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired subscription for channel {Channel}, markets {Markets}",
                    channel, string.Join(", ", marketTickers));
            }
            finally
            {
                _subscriptionLock.ExitWriteLock();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Determines if a market is currently subscribed to a specific channel.
        /// </summary>
        /// <param name="marketTicker">The market ticker to check, or empty string for channel-level subscription.</param>
        /// <param name="action">The action type (e.g., "orderbook", "ticker") to check subscription for.</param>
        /// <returns>True if the market is subscribed to the channel, false otherwise.</returns>
        public bool IsSubscribed(string marketTicker, string action)
        {
            var channel = GetChannelName(action);
            bool isSubscribed = _channelSubscriptions.TryGetValue(channel, out var subscription) && (subscription.Markets.Contains(marketTicker) || marketTicker == "");
            return isSubscribed;
        }

        /// <summary>
        /// Determines if a market can be subscribed to a specific channel based on its current state.
        /// </summary>
        /// <param name="marketTicker">The market ticker to check.</param>
        /// <param name="channel">The channel name to check subscription capability for.</param>
        /// <returns>True if the market can be subscribed to the channel, false otherwise.</returns>
        public bool CanSubscribeToMarket(string marketTicker, string channel)
        {
            var marketStates = _marketChannelSubscriptionStates.GetOrAdd(marketTicker, _ => new ConcurrentDictionary<string, SubscriptionState>());
            var state = marketStates.GetOrAdd(channel, SubscriptionState.Unsubscribed);
            bool canSubscribe = state == SubscriptionState.Unsubscribed || state == SubscriptionState.Unsubscribing;
            return canSubscribe;
        }

        /// <summary>
        /// Sets the subscription state for a market on a specific channel.
        /// Used to track the lifecycle of subscription operations.
        /// </summary>
        /// <param name="marketTicker">The market ticker to update state for.</param>
        /// <param name="channel">The channel name to update state for.</param>
        /// <param name="state">The new subscription state.</param>
        public void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state)
        {
            var marketStates = _marketChannelSubscriptionStates.GetOrAdd(marketTicker, _ => new ConcurrentDictionary<string, SubscriptionState>());
            marketStates[channel] = state;
        }

        /// <summary>
        /// Clears all order book update messages for a specific market from the processing queue.
        /// Used when resetting market data or handling market-specific cleanup operations.
        /// </summary>
        /// <param name="marketTicker">The market ticker to clear messages for.</param>
        public void ClearOrderBookQueue(string marketTicker)
        {
            _logger.LogDebug("Clearing orderbook message queue for market: {MarketTicker}", marketTicker);

            // Track lock contention
            var lockWaitStart = DateTime.UtcNow;
            lock (_orderBookQueueSynchronizationLock)
            {
                var lockWaitTime = DateTime.UtcNow - lockWaitStart;
                RecordLockMetrics("OrderBookQueueLock", lockWaitTime, lockWaitTime > TimeSpan.Zero);

                var tempQueue = new PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long>();
                while (_orderBookUpdateQueue.TryDequeue(out var message, out var seq))
                {
                    if (message.Data.TryGetProperty("msg", out var msg) &&
                        msg.TryGetProperty("market_ticker", out var tickerProp) &&
                        tickerProp.GetString() != marketTicker)
                    {
                        tempQueue.Enqueue(message, seq);
                    }
                }
                while (tempQueue.TryDequeue(out var message, out var seq))
                {
                    _orderBookUpdateQueue.Enqueue(message, seq);
                }
                _logger.LogDebug("Cleared orderbook messages for {MarketTicker}. Remaining queue count: {Count}", marketTicker, _orderBookUpdateQueue.Count);
            }
        }

        /// <summary>
        /// Waits for the order book update queue to be empty for a specific market within a timeout period.
        /// Used to ensure all pending updates are processed before proceeding with operations like snapshot creation.
        /// </summary>
        /// <param name="marketTicker">The market ticker to wait for queue clearance.</param>
        /// <param name="timeout">The maximum time to wait for the queue to clear.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            bool waited = false;

            while (!_processingCancellationToken.IsCancellationRequested)
            {
                bool hasPendingUpdates;
                var lockWaitStart = DateTime.UtcNow;
                lock (_orderBookQueueSynchronizationLock)
                {
                    var lockWaitTime = DateTime.UtcNow - lockWaitStart;
                    RecordLockMetrics("OrderBookQueueLock", lockWaitTime, lockWaitTime > TimeSpan.Zero);

                    hasPendingUpdates = _orderBookUpdateQueue.Count > 0 &&
                                _orderBookUpdateQueue.UnorderedItems.Any(item =>
                                    item.Element.Data.GetProperty("msg").GetProperty("market_ticker").GetString() == marketTicker);
                }

                if (!hasPendingUpdates)
                {
                    _logger.LogDebug("Order book queue cleared for market: {MarketTicker}", marketTicker);
                    return;
                }
                {
                    waited = true;
                    _logger.LogDebug("Waiting for order book queue to clear for market: {MarketTicker}", marketTicker);
                }

                if (DateTime.UtcNow - startTime > timeout)
                {
                    _logger.LogWarning("Timeout waiting for order book queue to clear for market: {MarketTicker} after {TimeoutSeconds}s", marketTicker, timeout.TotalSeconds);
                    return;
                }

                await Task.Delay(100, _processingCancellationToken);
            }

            if (waited)
                _logger.LogDebug("Market {0} waited {1}s before saving snapshot", marketTicker, (DateTime.UtcNow - startTime).TotalSeconds);
        }

        /// <summary>
        /// Resets all message type event counts to zero.
        /// Used for resetting monitoring statistics or starting fresh counting periods.
        /// </summary>
        public void ResetEventCounts()
        {
            _messageTypeCounts.Clear();
        }

        /// <summary>
        /// Gets the event counts for different message types for a specific market.
        /// Currently returns default values as market-specific counting is not implemented.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get event counts for.</param>
        /// <returns>A tuple containing counts for orderbook, trade, and ticker events.</returns>
        public (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker)
        {
            // TODO: Implement market-specific event counting
            return (0, 0, 0);
        }

        private string GetChannelNameInternal(string action) => action switch
        {
            "orderbook" => "orderbook_delta",
            "ticker" => "ticker",
            "trade" => "trade",
            "fill" => "fill",
            "lifecycle" => "market_lifecycle_v2",
            "event_lifecycle" => "market_lifecycle_v2",
            _ => throw new ArgumentException($"Invalid action: {action}")
        };

        /// <summary>
        /// Background task that periodically processes queued subscription updates.
        /// Runs continuously while the service is active, processing any subscription
        /// requests that were queued due to connection issues.
        /// </summary>
        /// <returns>A task representing the background operation.</returns>
        private async Task ProcessQueuePeriodicallyAsync()
        {
            _logger.LogDebug("Starting subscription queue processor");
            while (!_processingCancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_connectionManager.IsConnected())
                    {
                        // Implement batching: collect multiple requests
                        var batch = new List<(string Action, string[] MarketTickers, string ChannelAction)>();
                        while (_queuedSubscriptionUpdateRequests.TryDequeue(out var update) && batch.Count < _batchSize)
                        {
                            batch.Add(update);
                        }

                        if (batch.Any())
                        {
                            _logger.LogDebug("Processing batched subscription updates: {Count} requests", batch.Count);

                            // Group by action and channel for efficient processing
                            var grouped = batch.GroupBy(u => (u.Action, u.ChannelAction));
                            foreach (var group in grouped)
                            {
                                var combinedMarkets = group.SelectMany(g => g.MarketTickers).Distinct().ToArray();
                                var (action, channelAction) = group.Key;

                                _logger.LogDebug("Processing batched update: action={Action}, channel={Channel}, combined markets={Markets}",
                                    action, channelAction, string.Join(", ", combinedMarkets));

                                await UpdateSubscriptionAsync(action, combinedMarkets, channelAction);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing queued subscription update");
                }

                await Task.Delay(_retryDelayMs, _processingCancellationToken);
            }
            _logger.LogDebug("Subscription queue processor stopped");
        }

        /// <summary>
        /// Background task that monitors pending subscription confirmations and handles timeouts.
        /// Removes stale pending confirmations and retries failed subscriptions to maintain connection reliability.
        /// </summary>
        /// <returns>A task representing the background operation.</returns>
        private async Task CheckPendingConfirmationsAsync()
        {
            _logger.LogDebug("Starting pending subscription confirmations monitor");
            while (!_processingCancellationToken.IsCancellationRequested)
            {
                try
                {
                    var expiredConfirms = _pendingSubscriptionConfirmations
                        .Where(kvp => DateTime.UtcNow - kvp.Value.SentTime > TimeSpan.FromSeconds(_confirmationTimeoutSeconds))
                        .ToList();

                    foreach (var confirm in expiredConfirms)
                    {
                        var subscriptionKey = $"{confirm.Value.Channel}:{string.Join(",", confirm.Value.MarketTickers.OrderBy(m => m))}";
                        var retryInfo = _subscriptionRetryInfo.GetOrAdd(subscriptionKey, _ => (0, DateTime.UtcNow));
                        var currentRetryCount = retryInfo.RetryCount;
                        var firstRetryTime = retryInfo.FirstRetryTime;

                        // Check if the channel already has a SID set, indicating the subscription was successful despite no confirmation
                        if (_channelSubscriptions.TryGetValue(confirm.Value.Channel, out var existingSubscription) && existingSubscription.Sid != 0)
                        {
                            _logger.LogInformation("Channel {Channel} has SID {Sid}, assuming subscription was successful despite no confirmation received. Removing pending confirmation for ID {Id}",
                                confirm.Value.Channel, existingSubscription.Sid, confirm.Key);
                            _pendingSubscriptionConfirmations.TryRemove(confirm.Key, out var _);
                            _subscriptionRetryInfo.TryRemove(subscriptionKey, out var _);
                            continue;
                        }

                        // Check if we've been retrying for more than 15 minutes
                        var totalRetryTime = DateTime.UtcNow - firstRetryTime;
                        if (totalRetryTime > TimeSpan.FromMinutes(15))
                        {
                            _logger.LogError("Subscription retry timeout exceeded (15 minutes) for channel {Channel}, markets {Markets}. Unwatching markets.",
                                confirm.Value.Channel, string.Join(", ", confirm.Value.MarketTickers));

                            // Unwatch the markets - the MarketWebSocketUnhealthy event will be raised to handle this
                            _logger.LogInformation("Persistent subscription failures detected for markets {Markets}, raising unhealthy event",
                                string.Join(", ", confirm.Value.MarketTickers));

                            _subscriptionRetryInfo.TryRemove(subscriptionKey, out var _);
                            _pendingSubscriptionConfirmations.TryRemove(confirm.Key, out var _);
                            continue;
                        }

                        if (currentRetryCount >= _maxSubscriptionRetries)
                        {
                            _logger.LogError("Max retries ({MaxRetries}) exceeded for subscription to channel {Channel}, markets {Markets}. Giving up.",
                                _maxSubscriptionRetries, confirm.Value.Channel, string.Join(", ", confirm.Value.MarketTickers));
                            _subscriptionRetryInfo.TryRemove(subscriptionKey, out var _);
                            _pendingSubscriptionConfirmations.TryRemove(confirm.Key, out var _);
                            continue;
                        }

                        _logger.LogWarning("Pending confirmation expired for ID {Id}, channel {Channel}, markets {Markets} after {_confirmationTimeoutSeconds} seconds. Retry {RetryCount}/{MaxRetries} with exponential backoff (total retry time: {TotalTime:F1}min).",
                            confirm.Key, confirm.Value.Channel, string.Join(", ", confirm.Value.MarketTickers), _confirmationTimeoutSeconds, currentRetryCount + 1, _maxSubscriptionRetries, totalRetryTime.TotalMinutes);

                        // Raise unhealthy event for affected markets
                        RaiseMarketWebSocketUnhealthy(confirm.Value.MarketTickers);

                        // Clean up the expired subscription state
                        await CleanupExpiredSubscriptionAsync(confirm.Value.Channel, confirm.Value.MarketTickers);

                        // Calculate exponential backoff delay
                        var retryDelayMs = CalculateRetryDelayMs(currentRetryCount);
                        _logger.LogDebug("Waiting {DelayMs}ms before retrying subscription for channel {Channel}, markets {Markets}",
                            retryDelayMs, confirm.Value.Channel, string.Join(", ", confirm.Value.MarketTickers));

                        await Task.Delay(retryDelayMs, _processingCancellationToken);

                        // Retry the subscription if still connected
                        if (_connectionManager.IsConnected())
                        {
                            try
                            {
                                // Map channel back to action for retry
                                var action = GetActionFromChannel(confirm.Value.Channel);
                                await SubscribeToChannelAsync(action, confirm.Value.MarketTickers);

                                // Reset retry info on successful retry initiation
                                _subscriptionRetryInfo[subscriptionKey] = (0, DateTime.UtcNow);

                                _logger.LogInformation("Retried subscription for channel {Channel}, markets {Markets} after confirmation timeout (retry {RetryCount}/{MaxRetries})",
                                    confirm.Value.Channel, string.Join(", ", confirm.Value.MarketTickers), currentRetryCount + 1, _maxSubscriptionRetries);
                            }
                            catch (Exception retryEx)
                            {
                                // Increment retry count on failure
                                _subscriptionRetryInfo[subscriptionKey] = (currentRetryCount + 1, firstRetryTime);
                                _logger.LogError(retryEx, "Failed to retry subscription for channel {Channel}, markets {Markets} after confirmation timeout (retry {RetryCount}/{MaxRetries})",
                                    confirm.Value.Channel, string.Join(", ", confirm.Value.MarketTickers), currentRetryCount + 1, _maxSubscriptionRetries);
                            }
                        }
                        else
                        {
                            // Increment retry count when not connected
                            _subscriptionRetryInfo[subscriptionKey] = (currentRetryCount + 1, firstRetryTime);
                            _logger.LogWarning("WebSocket not connected, cannot retry subscription for channel {Channel}, markets {Markets} (retry {RetryCount}/{MaxRetries})",
                                confirm.Value.Channel, string.Join(", ", confirm.Value.MarketTickers), currentRetryCount + 1, _maxSubscriptionRetries);
                        }

                        _pendingSubscriptionConfirmations.TryRemove(confirm.Key, out var _);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking pending subscription confirmations");
                }

                await Task.Delay(5000, _processingCancellationToken);
            }
            _logger.LogDebug("Pending subscription confirmations monitor stopped");
        }

        /// <summary>
        /// Background task that periodically processes the order book update queue.
        /// Handles order book messages in sequence order, updating sequence numbers and processing updates.
        /// </summary>
        /// <returns>A task representing the background operation.</returns>
        private async Task ProcessOrderBookQueuePeriodicallyAsync()
        {
            _logger.LogDebug("Starting order book queue processor");
            while (!_processingCancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_orderBookUpdateQueue.TryDequeue(out var message, out var seq))
                    {
                        _logger.LogDebug("Processing order book message with seq {Seq}", seq);

                        // Check for sequence gap (global across all markets)
                        var lockWaitStart = DateTime.UtcNow;
                        lock (_sequenceNumberSynchronizationLock)
                        {
                            var lockWaitTime = DateTime.UtcNow - lockWaitStart;
                            RecordLockMetrics("SequenceNumberLock", lockWaitTime, lockWaitTime > TimeSpan.Zero);

                            if (_latestProcessedSequenceNumber > 0 && seq != _latestProcessedSequenceNumber + 1)
                            {
                                _logger.LogWarning("Sequence gap detected: expected {Expected}, got {Seq}. Triggering full orderbook channel resubscribe for new snapshot.",
                                    _latestProcessedSequenceNumber + 1, seq);
                                // Trigger resubscribe of entire orderbook channel to get new snapshot
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await UnsubscribeFromChannelAsync("orderbook");
                                        await SubscribeToChannelAsync("orderbook", WatchedMarkets.ToArray());
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to resubscribe orderbook channel after sequence gap");
                                    }
                                }, _processingCancellationToken);
                            }

                            if (seq > _latestProcessedSequenceNumber)
                            {
                                _latestProcessedSequenceNumber = seq;
                            }
                        }

                        // Process the message
                        var eventArgs = new OrderBookEventArgs(message.OfferType, message.Data);
                        // TODO: Coordinate with MessageProcessor to trigger OrderBookReceived event
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order book queue message");
                }

                await Task.Delay(10, _processingCancellationToken);
            }
            _logger.LogDebug("Order book queue processor stopped");
        }

        /// <summary>
        /// Background task that monitors subscription health and detects stale subscriptions.
        /// Only runs health checks infrequently and only when there are genuine issues.
        /// </summary>
        /// <returns>A task representing the background operation.</returns>
        private async Task MonitorSubscriptionHealthAsync()
        {
            _logger.LogDebug("Starting subscription health monitor");
            while (!_processingCancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Only check health every 10 minutes instead of every 30 seconds
                    // The API doesn't require frequent health checks for active subscriptions
                    await Task.Delay(TimeSpan.FromMinutes(10), _processingCancellationToken);

                    var now = DateTime.UtcNow;
                    var staleThreshold = TimeSpan.FromMinutes(30); // Much longer threshold
                    bool hasStaleSubscriptions = false;

                    _subscriptionLock.EnterReadLock();
                    try
                    {
                        foreach (var subscription in _channelSubscriptions)
                        {
                            var channel = subscription.Key;
                            var sid = subscription.Value.Sid;
                            if (sid != 0)
                            {
                                // Check if this subscription has been active recently
                                // Look for recent subscription activity (not pending confirmations since they're removed after confirmation)
                                var hasRecentActivity = _recentSubscriptions.Any(r => r.Key.StartsWith($"{GetActionFromChannel(channel)}:") && (now - r.Value) < staleThreshold);

                                if (!hasRecentActivity)
                                {
                                    _logger.LogWarning("Detected potentially stale subscription for channel {Channel} with SID {Sid}, will resubscribe all", channel, sid);
                                    hasStaleSubscriptions = true;
                                    break; // No need to check further, we'll resubscribe all
                                }
                            }
                        }
                    }
                    finally
                    {
                        _subscriptionLock.ExitReadLock();
                    }

                    if (hasStaleSubscriptions)
                    {
                        _logger.LogInformation("Resubscribing to all subscriptions due to detected stale subscriptions");
                        await ResubscribeAsync(true); // Force resubscribe to all
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Subscription health monitor cancelled during shutdown");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during subscription health monitoring");
                }
            }
            _logger.LogDebug("Subscription health monitor stopped");
        }

        /// <summary>
        /// Gets the WebSocket channel name corresponding to a given action.
        /// Maps user-friendly action names to the actual channel names used by Kalshi's WebSocket API.
        /// </summary>
        /// <param name="action">The action name (e.g., "orderbook", "ticker") to convert to a channel name.</param>
        /// <returns>The corresponding WebSocket channel name.</returns>
        public string GetChannelName(string action) => GetChannelNameInternal(action);

        /// <summary>
        /// Generates the next unique message ID for WebSocket communication.
        /// Uses thread-safe increment to ensure unique IDs across concurrent operations.
        /// </summary>
        /// <returns>The next available message ID.</returns>
        public int GenerateNextMessageId() => Interlocked.Increment(ref _nextMessageId);

        /// <summary>
        /// Records performance metrics for subscription operations.
        /// </summary>
        /// <param name="operation">The operation type (e.g., "Subscribe", "Update", "Unsubscribe").</param>
        /// <param name="duration">The duration of the operation.</param>
        /// <param name="success">Whether the operation was successful.</param>
        private void RecordOperationMetrics(string operation, TimeSpan duration, bool success)
        {
            if (!_enableMetrics) return;

            _operationTimings.AddOrUpdate(operation, duration.Ticks, (key, oldValue) => (oldValue + duration.Ticks) / 2);
            _operationCounts.AddOrUpdate(operation, 1, (key, oldValue) => oldValue + 1);
            if (success)
            {
                _successCounts.AddOrUpdate(operation, 1, (key, oldValue) => oldValue + 1);
            }
        }

        /// <summary>
        /// Records lock contention metrics.
        /// </summary>
        /// <param name="lockName">The name of the lock (e.g., "SubscriptionLock", "QueueLock").</param>
        /// <param name="waitTime">The time spent waiting for the lock.</param>
        /// <param name="contended">Whether the lock was contended (wait time > 0).</param>
        private void RecordLockMetrics(string lockName, TimeSpan waitTime, bool contended)
        {
            if (!_enableMetrics) return;

            _lockAcquisitionTimes.AddOrUpdate(lockName, 1, (key, oldValue) => oldValue + 1);
            _lockWaitTimes.AddOrUpdate(lockName, waitTime.Ticks, (key, oldValue) => (oldValue + waitTime.Ticks) / 2);
            if (contended)
            {
                _lockContentionCounts.AddOrUpdate(lockName, 1, (key, oldValue) => oldValue + 1);
            }
        }

        /// <summary>
        /// Gets performance metrics for subscription operations.
        /// </summary>
        /// <returns>A dictionary containing operation metrics, or empty if metrics are disabled.</returns>
        public ConcurrentDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)> GetPerformanceMetrics()
        {
            if (!_enableMetrics)
            {
                return new ConcurrentDictionary<string, (long, long, long)>();
            }

            var metrics = new ConcurrentDictionary<string, (long, long, long)>();
            foreach (var operation in _operationTimings.Keys)
            {
                var avgTicks = _operationTimings.GetOrAdd(operation, 0);
                var totalOps = _operationCounts.GetOrAdd(operation, 0);
                var successOps = _successCounts.GetOrAdd(operation, 0);
                metrics[operation] = (avgTicks, totalOps, successOps);
            }

            // Post metrics to performance monitoring service if available
            if (_performanceMetrics != null)
            {
                PostOperationMetrics(metrics);
            }

            return metrics;
        }

        /// <summary>
        /// Gets lock contention metrics.
        /// </summary>
        /// <returns>A dictionary containing lock metrics, or empty if metrics are disabled.</returns>
        public ConcurrentDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)> GetLockMetrics()
        {
            if (!_enableMetrics)
            {
                return new ConcurrentDictionary<string, (long, long, long)>();
            }

            var metrics = new ConcurrentDictionary<string, (long, long, long)>();
            foreach (var lockName in _lockAcquisitionTimes.Keys)
            {
                var acquisitionCount = _lockAcquisitionTimes.GetOrAdd(lockName, 0);
                var avgWaitTicks = _lockWaitTimes.GetOrAdd(lockName, 0);
                var contentionCount = _lockContentionCounts.GetOrAdd(lockName, 0);
                metrics[lockName] = (acquisitionCount, avgWaitTicks, contentionCount);
            }

            // Post metrics to performance monitoring service if available
            if (_performanceMetrics != null)
            {
                PostLockContentionMetrics(metrics);
            }

            return metrics;
        }

        /// <summary>
        /// Posts operation performance metrics to the performance monitor.
        /// </summary>
        /// <param name="metrics">The operation metrics to post.</param>
        private void PostOperationMetrics(ConcurrentDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)> metrics)
        {
            if (_performanceMetrics == null) return;

            foreach (var kvp in metrics)
            {
                string operation = kvp.Key;
                var (avgTicks, totalOps, successOps) = kvp.Value;
                string id = $"SubscriptionManager_Operation_{operation}";
                string name = $"{operation} Operations";
                string description = $"Performance metrics for {operation} operations";
                string category = "SubscriptionManager";

                if (!_enableMetrics)
                {
                    _performanceMetrics.RecordDisabledMetric("BaseSubscriptionManager", id, name, description, 0, "", category);
                }
                else
                {
                    // Record total operations
                    _performanceMetrics.RecordCounterMetric("BaseSubscriptionManager", $"{id}_Total", $"{name} Total", $"Total {operation} operations", totalOps, "count", category);

                    // Record successful operations
                    _performanceMetrics.RecordCounterMetric("BaseSubscriptionManager", $"{id}_Success", $"{name} Success", $"Successful {operation} operations", successOps, "count", category);

                    // Record average time in ms
                    double avgMs = avgTicks / (double)TimeSpan.TicksPerMillisecond;
                    _performanceMetrics.RecordSpeedDialMetric("BaseSubscriptionManager", $"{id}_AvgTime", $"{name} Avg Time", $"Average time for {operation} operations", avgMs, "ms", category, 0, 1000, 5000);
                }
            }
        }

        /// <summary>
        /// Posts lock contention metrics to the performance monitor.
        /// </summary>
        /// <param name="metrics">The lock metrics to post.</param>
        private void PostLockContentionMetrics(ConcurrentDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)> metrics)
        {
            if (_performanceMetrics == null) return;

            foreach (var kvp in metrics)
            {
                string lockName = kvp.Key;
                var (acqCount, avgWaitTicks, contCount) = kvp.Value;
                string id = $"SubscriptionManager_Lock_{lockName}";
                string name = $"{lockName} Lock";
                string description = $"Lock contention metrics for {lockName}";
                string category = "SubscriptionManager";

                if (!_enableMetrics)
                {
                    _performanceMetrics.RecordDisabledMetric("BaseSubscriptionManager", id, name, description, 0, "", category);
                }
                else
                {
                    // Record acquisition count
                    _performanceMetrics.RecordCounterMetric("BaseSubscriptionManager", $"{id}_Acquisitions", $"{name} Acquisitions", $"Total acquisitions for {lockName}", acqCount, "count", category);

                    // Record contention count
                    _performanceMetrics.RecordCounterMetric("BaseSubscriptionManager", $"{id}_Contentions", $"{name} Contentions", $"Total contentions for {lockName}", contCount, "count", category);

                    // Record average wait time in ms
                    double avgWaitMs = avgWaitTicks / (double)TimeSpan.TicksPerMillisecond;
                    _performanceMetrics.RecordSpeedDialMetric("BaseSubscriptionManager", $"{id}_AvgWait", $"{name} Avg Wait", $"Average wait time for {lockName}", avgWaitMs, "ms", category, 0, 100, 500);
                }
            }
        }

        /// <summary>
        /// Updates the subscription state when a confirmation is received from the server.
        /// Sets the subscription ID and marks all markets in the channel as subscribed.
        /// </summary>
        /// <param name="sid">The subscription ID received from the server.</param>
        /// <param name="channel">The channel name for which the confirmation was received.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateSubscriptionStateFromConfirmationAsync(int sid, string channel)
        {
            _logger.LogDebug("Updating subscription state for SID {Sid} on channel {Channel}", sid, channel);

            // Find the subscription and update its SID
            if (_channelSubscriptions.TryGetValue(channel, out var subscription))
            {
                var updatedSubscription = (Sid: sid, subscription.Markets);
                _channelSubscriptions[channel] = updatedSubscription;
                _logger.LogDebug("Updated SID for channel {Channel} to {Sid}", channel, sid);

                // Update subscription states for all markets in this channel
                foreach (var market in subscription.Markets)
                {
                    SetSubscriptionState(market, channel, SubscriptionState.Subscribed);
                }

                // Raise healthy event only for markets that were previously unhealthy
                RaiseMarketWebSocketHealthy(subscription.Markets.ToArray());
            }
            else
            {
                _logger.LogWarning("Received confirmation for unknown channel {Channel}", channel);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Removes a pending confirmation from the tracking collection.
        /// Called when a confirmation response is received or when a timeout occurs.
        /// </summary>
        /// <param name="id">The message ID of the pending confirmation to remove.</param>
        /// <returns>True if the confirmation was found and removed, false otherwise.</returns>
        public bool RemovePendingConfirmation(int id)
        {
            bool removed = _pendingSubscriptionConfirmations.TryRemove(id, out var _);
            if (removed)
            {
                _logger.LogDebug("Removed pending confirmation for ID {Id}", id);
            }
            else
            {
                _logger.LogDebug("No pending confirmation found for ID {Id}", id);
            }
            return removed;
        }

        /// <summary>
        /// Retrieves information about a pending confirmation by its message ID.
        /// Used to get details about outstanding subscription requests.
        /// </summary>
        /// <param name="id">The message ID of the pending confirmation to retrieve.</param>
        /// <returns>A tuple containing the channel and market tickers if found, null otherwise.</returns>
        public (string Channel, string[] MarketTickers)? GetPendingConfirm(int id)
        {
            if (_pendingSubscriptionConfirmations.TryGetValue(id, out var confirm))
            {
                return (confirm.Channel, confirm.MarketTickers);
            }
            return null;
        }

        /// <summary>
        /// Raises the MarketWebSocketUnhealthy event for the specified markets.
        /// </summary>
        /// <param name="markets">Array of market tickers that have unhealthy WebSocket connections.</param>
        public void RaiseMarketWebSocketUnhealthy(string[] markets)
        {
            MarketWebSocketUnhealthy?.Invoke(this, markets);
        }

        /// <summary>
        /// Raises the MarketWebSocketHealthy event for the specified markets.
        /// </summary>
        /// <param name="markets">Array of market tickers that have restored WebSocket connections.</param>
        public void RaiseMarketWebSocketHealthy(string[] markets)
        {
            MarketWebSocketHealthy?.Invoke(this, markets);
        }

        /// <summary>
        /// Records that a message was received on the specified channel.
        /// Used for stale subscription detection.
        /// </summary>
        /// <param name="channel">The channel name where the message was received.</param>
        public void RecordChannelActivity(string channel)
        {
            var action = GetActionFromChannel(channel);
            var deduplicationKey = $"{action}:";
            _recentSubscriptions[deduplicationKey] = DateTime.UtcNow;
        }

        /// <summary>
        /// Handles WebSocket disconnection by clearing local subscription state.
        /// This ensures clean reconnection without stale state assumptions.
        /// </summary>
        public void HandleDisconnection()
        {
            _logger.LogInformation("Handling WebSocket disconnection - clearing local subscription state");

            // Clear all channel subscriptions
            _channelSubscriptions.Clear();

            // Clear all market channel subscription states
            _marketChannelSubscriptionStates.Clear();

            // Clear pending confirmations
            _pendingSubscriptionConfirmations.Clear();

            // Clear pending market subscriptions
            _pendingMarketSubscriptions.Clear();

            // Clear recent subscriptions to force fresh subscriptions on reconnect
            _recentSubscriptions.Clear();

            _logger.LogInformation("Local subscription state cleared due to disconnection");
        }
    }
}
