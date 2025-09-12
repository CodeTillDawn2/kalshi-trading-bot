using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using BacklashInterfaces.Enums;
using BacklashInterfaces.Constants;
using BacklashBot.State.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;
using BacklashDTOs;

namespace KalshiBotAPI.Websockets
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly ILogger<SubscriptionManager> _logger;
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly IDataCache _dataCache;
        private readonly IStatusTrackerService _statusTrackerService;
        private readonly ConcurrentDictionary<string, (int Sid, HashSet<string> Markets)> _subscriptions = new();
        private readonly ConcurrentDictionary<string, bool> _pendingSubscriptions = new ConcurrentDictionary<string, bool>();
        private readonly ConcurrentDictionary<int, (DateTime SentTime, string Message, string Channel, string[] MarketTickers)> _pendingConfirms = new ConcurrentDictionary<int, (DateTime, string, string, string[])>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>> _marketSubscriptionStates = new();
        private readonly SemaphoreSlim _subscriptionUpdateSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _channelSubscriptionSemaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentQueue<(string Action, string[] MarketTickers, string ChannelAction)> _queuedSubscriptionUpdates = new();
        private int _messageId = 1;
        private Task _queueProcessorTask = null!;
        private Task _confirmCheckTask = null!;
        private readonly object _sequenceLock = new object();
        private long _lastSequenceNumber = 0;
        private int _orderBookSid = 0;
        private readonly PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long> _orderBookMessageQueue = new();
        private readonly object _orderBookQueueLock = new object();
        private readonly ConcurrentDictionary<string, long> _eventCounts = new ConcurrentDictionary<string, long>();
        private Task _orderBookProcessorTask = null!;
        private CancellationToken _globalCancellationToken;

        public SubscriptionManager(
            ILogger<SubscriptionManager> logger,
            IWebSocketConnectionManager connectionManager,
            IDataCache dataCache,
            IStatusTrackerService statusTrackerService)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _dataCache = dataCache;
            _statusTrackerService = statusTrackerService;
            _globalCancellationToken = statusTrackerService.GetCancellationToken();

            // Initialize event counts
            _eventCounts.TryAdd("OrderBook", 0);
            _eventCounts.TryAdd("Ticker", 0);
            _eventCounts.TryAdd("Trade", 0);
            _eventCounts.TryAdd("Fill", 0);
            _eventCounts.TryAdd("MarketLifecycle", 0);
            _eventCounts.TryAdd("EventLifecycle", 0);
            _eventCounts.TryAdd("Subscribe", 0);
            _eventCounts.TryAdd("Unsubscribe", 0);
            _eventCounts.TryAdd("Ok", 0);
            _eventCounts.TryAdd("Error", 0);
            _eventCounts.TryAdd("Unknown", 0);
        }

        public async Task StartAsync()
        {
            _queueProcessorTask = Task.Run(() => ProcessQueuePeriodicallyAsync(), _globalCancellationToken);
            _confirmCheckTask = Task.Run(() => CheckPendingConfirmsAsync(), _globalCancellationToken);
            _orderBookProcessorTask = Task.Run(() => ProcessOrderBookQueuePeriodicallyAsync(), _globalCancellationToken);
        }

        public async Task StopAsync()
        {
            if (_queueProcessorTask != null && !_queueProcessorTask.IsCompleted)
            {
                await _queueProcessorTask.ConfigureAwait(false);
            }
            if (_confirmCheckTask != null && !_confirmCheckTask.IsCompleted)
            {
                await _confirmCheckTask.ConfigureAwait(false);
            }
            if (_orderBookProcessorTask != null && !_orderBookProcessorTask.IsCompleted)
            {
                await _orderBookProcessorTask.ConfigureAwait(false);
            }
        }

        public HashSet<string> WatchedMarkets
        {
            get => _dataCache.WatchedMarkets ?? new HashSet<string>();
            set => _dataCache.WatchedMarkets = value;
        }

        public ConcurrentDictionary<string, long> EventCounts => _eventCounts;
        public int SubscriptionUpdateSemaphoreCount => _subscriptionUpdateSemaphore.CurrentCount;
        public int ChannelSubscriptionSemaphoreCount => _channelSubscriptionSemaphore.CurrentCount;
        public int QueuedSubscriptionUpdatesCount => _queuedSubscriptionUpdates.Count;

        public async Task SubscribeToChannelAsync(string action, string[] marketTickers)
        {
            _logger.LogInformation("SUB-Subscribing to channel: action={Action}, markets={Markets}, current subscriptions count: {Count}", action, string.Join(", ", marketTickers), _subscriptions.Count);
            if (!_connectionManager.IsConnected())
            {
                _logger.LogWarning("WebSocket not connected, queuing subscription: action={Action}, markets={Markets}", action, string.Join(", ", marketTickers));
                _queuedSubscriptionUpdates.Enqueue(("add_markets", marketTickers, action));
                return;
            }

            bool semaphoreAcquired = false;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                semaphoreAcquired = await _channelSubscriptionSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire channel subscription semaphore for action {Action} within 60000ms", action);
                    return;
                }

                var channel = GetChannelName(action);
                if (!_subscriptions.TryGetValue(channel, out var subscription))
                {
                    subscription = (0, new HashSet<string>());
                    _subscriptions[channel] = subscription;
                }

                // For market-specific channels, check if ALL requested markets are already subscribed
                if (new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel) && subscription.Sid != 0)
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
                if (new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel) && subscription.Sid != 0 && marketTickers.Any())
                {
                    _logger.LogDebug("Channel {Channel} already has SID {Sid}, using update_subscription for new markets", channel, subscription.Sid);
                    // Release semaphore before calling UpdateSubscriptionAsync to avoid deadlock
                    if (semaphoreAcquired)
                    {
                        _channelSubscriptionSemaphore.Release();
                        semaphoreAcquired = false;
                    }
                    await UpdateSubscriptionAsync("add_markets", marketTickers, action);
                    return;
                }

                var newSubscriptions = marketTickers
                    .Where(ticker => CanSubscribeToMarket(ticker, channel) && !subscription.Markets.Contains(ticker))
                    .ToArray();

                _logger.LogDebug("SUB-New subscriptions for {Channel}: {NewMarkets}, existing: {Existing}", channel, string.Join(", ", newSubscriptions), string.Join(", ", subscription.Markets));

                if (new[] { "fill", "market_lifecycle_v2" }.Contains(channel))
                {
                    if (newSubscriptions.Any())
                    {
                        _logger.LogDebug("Ignoring market tickers for {Channel} as it operates in 'All markets' mode: {Markets}", channel, string.Join(", ", newSubscriptions));
                        newSubscriptions = Array.Empty<string>();
                    }
                }

                if (!newSubscriptions.Any() && !new[] { "fill", "market_lifecycle" }.Contains(channel) && subscription.Markets.Any())
                {
                    _logger.LogWarning("No new markets to subscribe for {Channel}: requested {Markets}, existing {Existing}", channel, string.Join(", ", marketTickers), string.Join(", ", subscription.Markets));
                    return;
                }

                foreach (var ticker in newSubscriptions)
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    SetSubscriptionState(ticker, channel, SubscriptionState.Subscribing);
                }

                var subscriptionId = Interlocked.Increment(ref _messageId);
                string message;
                bool SkipMessage = false;

                if (subscription.Sid != 0 && subscription.Markets.Any() && !new[] { "fill", "market_lifecycle_v2" }.Contains(channel))
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
                    string[] marketsToSubscribeTo = new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel) ? newSubscriptions : Array.Empty<string>();

                    if (new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel) && newSubscriptions.Length == 0)
                    {
                        _logger.LogWarning("No markets to subscribe for {Channel}, but channel requires market tickers: {Markets}. Skipping subscription.", channel, string.Join(", ", newSubscriptions));
                        SkipMessage = true;
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

                if (new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel) && !SkipMessage)
                {
                    _pendingConfirms.TryAdd(subscriptionId, (SentTime: DateTime.UtcNow, Message: message, Channel: channel, MarketTickers: newSubscriptions));
                }

                if (!SkipMessage)
                {
                    await _connectionManager.SendMessageAsync(message);

                    // Log the full message for debugging
                    _logger.LogInformation("SUB-Full subscribe message: {Message}", message);

                    var currentMarkets = subscription.Markets;
                    foreach (var ticker in newSubscriptions)
                    {
                        currentMarkets.Add(ticker);
                        _pendingSubscriptions.TryAdd($"{action}:{ticker}", true);

                        // Update WatchedMarkets in DataCache
                        if (!WatchedMarkets.Contains(ticker))
                        {
                            WatchedMarkets.Add(ticker);
                            _logger.LogDebug("Added {MarketTicker} to WatchedMarkets in DataCache", ticker);
                        }
                    }
                    _subscriptions[channel] = (subscription.Sid, currentMarkets);
                }
                else
                {
                    _logger.LogWarning("Skipping subscription message for {Channel} as no markets to subscribe", channel);
                }
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
                if (semaphoreAcquired)
                {
                    _channelSubscriptionSemaphore.Release();
                }
            }
        }

        public async Task SubscribeToWatchedMarketsAsync()
        {
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
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
                foreach (var action in new[] { "orderbook", "ticker", "trade" })
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
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

        public async Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction)
        {
            _logger.LogInformation("SUB-UpdateSubscriptionAsync called: action={Action}, channel={Channel}, markets={Markets}",
                action, channelAction, string.Join(", ", marketTickers));

            if (!new[] { "add_markets", "delete_markets" }.Contains(action))
            {
                _logger.LogError("Invalid action: {Action}", action);
                throw new Exception($"Invalid action: {action}");
            }

            if (!_connectionManager.IsConnected())
            {
                _logger.LogWarning("WebSocket not connected, queuing subscription update: action={Action}, channel={Channel}, markets={Markets}",
                    action, channelAction, string.Join(", ", marketTickers));
                _queuedSubscriptionUpdates.Enqueue((action, marketTickers, channelAction));
                return;
            }

            var channel = GetChannelName(channelAction);
            bool semaphoreAcquired = false;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                semaphoreAcquired = await _channelSubscriptionSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire channel subscription semaphore for update on {Channel} within 60000ms", channel);
                    return;
                }

                if (!_subscriptions.TryGetValue(channel, out var subscription))
                {
                    subscription = (0, new HashSet<string>());
                    _subscriptions[channel] = subscription;
                    _logger.LogInformation("SUB-Initialized new subscription for {Channel}", channel);
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
                _logger.LogInformation("SUB-Sending update_subscription command: channel={Channel}, SID={Sid}, ID={Id}, action={Action}, markets={Markets}",
                    channel, subscription.Sid, subscriptionId, action, string.Join(", ", marketsToUpdate));

                // Log the full message for debugging
                _logger.LogDebug("SUB-Full update_subscription message: {Message}", message);

                if (new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel))
                {
                    _pendingConfirms.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel, marketsToUpdate));
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
                        // Update WatchedMarkets in DataCache
                        if (!WatchedMarkets.Contains(ticker))
                        {
                            WatchedMarkets.Add(ticker);
                            _logger.LogDebug("Added {MarketTicker} to WatchedMarkets in DataCache during update", ticker);
                        }
                    }
                    else
                    {
                        updatedMarkets.Remove(ticker);
                        // Check if market is still subscribed to other channels
                        bool stillSubscribed = _subscriptions.Any(s => s.Key != channel && s.Value.Markets.Contains(ticker));
                        if (!stillSubscribed && WatchedMarkets.Contains(ticker))
                        {
                            WatchedMarkets.Remove(ticker);
                            _logger.LogDebug("Removed {MarketTicker} from WatchedMarkets in DataCache as no longer subscribed to any channels", ticker);
                        }
                    }
                    _pendingSubscriptions.TryRemove($"{channelAction}:{ticker}", out bool _);
                }
                _subscriptions[channel] = (subscription.Sid, updatedMarkets);

                _logger.LogInformation("SUB-Updated subscription locally: channel={Channel}, SID={Sid}, action={Action}, new markets={Markets}",
                    channel, subscription.Sid, action, string.Join(", ", updatedMarkets));
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("UpdateSubscriptionAsync was cancelled for channel {Channel}, action={Action}", channel, action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update subscription for {Channel}, action={Action}, markets={Markets}",
                    channel, string.Join(", ", marketTickers));
                throw;
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _channelSubscriptionSemaphore.Release();
                }
            }
        }

        public async Task UnsubscribeFromChannelAsync(string action)
        {
            _logger.LogDebug("Unsubscribing from channel: action={Action}", action);
            if (!_connectionManager.IsConnected())
            {
                _logger.LogWarning("WebSocket not connected, cannot unsubscribe from {Action}", action);
                return;
            }

            bool semaphoreAcquired = false;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                semaphoreAcquired = await _channelSubscriptionSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire channel subscription semaphore for action {Action} within 60000ms", action);
                    return;
                }

                var channel = GetChannelName(action);
                if (!_subscriptions.TryGetValue(channel, out var subscription))
                {
                    _logger.LogWarning("No active subscription for {Channel}, skipping unsubscription", channel);
                    return;
                }

                if (subscription.Sid == 0)
                {
                    _logger.LogWarning("No SID for {Channel}, removing local subscription", channel);
                    ((IDictionary<string, (int Sid, HashSet<string> Markets)>)_subscriptions).Remove(channel);
                    return;
                }

                var subscriptionId = Interlocked.Increment(ref _messageId);
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
                _logger.LogInformation("SUB-Sending unsubscription request for {Channel}, ID={Id}, SID={Sid}, message={Message}", channel, subscriptionId, subscription.Sid, message);
                _pendingConfirms.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel, Array.Empty<string>()));
                await _connectionManager.SendMessageAsync(message);

                foreach (var marketTicker in subscription.Markets)
                {
                    SetSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                    // Check if market is still subscribed to other channels
                    bool stillSubscribed = _subscriptions.Any(s => s.Key != channel && s.Value.Markets.Contains(marketTicker));
                    if (!stillSubscribed && WatchedMarkets.Contains(marketTicker))
                    {
                        WatchedMarkets.Remove(marketTicker);
                        _logger.LogDebug("Removed {MarketTicker} from WatchedMarkets in DataCache as no longer subscribed to any channels", marketTicker);
                    }
                }
                ((IDictionary<string, (int Sid, HashSet<string> Markets)>)_subscriptions).Remove(channel);
                _logger.LogInformation("SUB-Removed local subscription for {Channel}", channel);
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
                if (semaphoreAcquired)
                {
                    _channelSubscriptionSemaphore.Release();
                }
            }
        }

        public async Task UnsubscribeFromAllAsync()
        {
            _logger.LogDebug("Acquiring channel subscription semaphore for unsubscribe");
            bool semaphoreAcquired = false;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                semaphoreAcquired = await _channelSubscriptionSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire channel subscription semaphore for unsubscription within 60000ms");
                    return;
                }

                foreach (var subscription in _subscriptions.ToList())
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
                        _logger.LogInformation("SUB-Sending unsubscribe command: channel={Channel}, SID={Sid}, ID={Id}, markets={Markets}",
                            channel, sid, subscriptionId, string.Join(", ", markets));
                        _pendingConfirms.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel, markets));
                        await _connectionManager.SendMessageAsync(message);
                        foreach (var marketTicker in markets)
                        {
                            SetSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                            // Remove from WatchedMarkets since unsubscribing from all
                            if (WatchedMarkets.Contains(marketTicker))
                            {
                                WatchedMarkets.Remove(marketTicker);
                                _logger.LogDebug("Removed {MarketTicker} from WatchedMarkets in DataCache during full unsubscription", marketTicker);
                            }
                        }
                        _logger.LogDebug("Marked local subscription as unsubscribed for channel {Channel}", channel);
                    }
                    else
                    {
                        _logger.LogWarning("No SID for channel {Channel}, skipping unsubscribe command", channel);
                    }
                }
                _subscriptions.Clear();
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
                if (semaphoreAcquired)
                {
                    _channelSubscriptionSemaphore.Release();
                }
            }
        }

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
                ? _subscriptions.Where(s => s.Value.Sid != 0).ToList()
                : _subscriptions.Where(s => s.Value.Sid == 0).ToList();

            if (!subscriptionsToResubscribe.Any())
            {
                _logger.LogDebug("No subscriptions need resubscribing");
                return;
            }

            foreach (var subscription in subscriptionsToResubscribe)
            {
                var channel = subscription.Key;
                var markets = subscription.Value.Markets.ToArray();

                if (markets.Any() || new[] { "fill", "market_lifecycle_v2" }.Contains(channel))
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

        public bool IsSubscribed(string marketTicker, string action)
        {
            var channel = GetChannelName(action);
            bool isSubscribed = _subscriptions.TryGetValue(channel, out var subscription) && (subscription.Markets.Contains(marketTicker) || marketTicker == "");
            return isSubscribed;
        }

        public bool CanSubscribeToMarket(string marketTicker, string channel)
        {
            var marketStates = _marketSubscriptionStates.GetOrAdd(marketTicker, _ => new ConcurrentDictionary<string, SubscriptionState>());
            var state = marketStates.GetOrAdd(channel, SubscriptionState.Unsubscribed);
            bool canSubscribe = state == SubscriptionState.Unsubscribed || state == SubscriptionState.Unsubscribing;
            return canSubscribe;
        }

        public void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state)
        {
            var marketStates = _marketSubscriptionStates.GetOrAdd(marketTicker, _ => new ConcurrentDictionary<string, SubscriptionState>());
            marketStates[channel] = state;
        }

        public void ClearOrderBookQueue(string marketTicker)
        {
            _logger.LogDebug("Clearing orderbook message queue for market: {MarketTicker}", marketTicker);
            lock (_orderBookQueueLock)
            {
                var tempQueue = new PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long>();
                while (_orderBookMessageQueue.TryDequeue(out var message, out var seq))
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
                    _orderBookMessageQueue.Enqueue(message, seq);
                }
                _logger.LogDebug("Cleared orderbook messages for {MarketTicker}. Remaining queue count: {Count}", marketTicker, _orderBookMessageQueue.Count);
            }
        }

        public async Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            bool waited = false;

            while (!_globalCancellationToken.IsCancellationRequested)
            {
                bool hasPendingUpdates;
                lock (_orderBookQueueLock)
                {
                    hasPendingUpdates = _orderBookMessageQueue.Count > 0 &&
                                _orderBookMessageQueue.UnorderedItems.Any(item =>
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

                await Task.Delay(100, _globalCancellationToken);
            }

            if (waited)
                _logger.LogDebug("Market {0} waited {1}s before saving snapshot", marketTicker, (DateTime.UtcNow - startTime).TotalSeconds);
        }

        public void ResetEventCounts()
        {
            _eventCounts.Clear();
        }

        public (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker)
        {
            // Implementation
            return (0, 0, 0);
        }

        private string GetChannelNameInternal(string action) => action switch
        {
            "orderbook" => "orderbook_delta",
            "ticker" => "ticker",
            "trade" => "trade",
            "fill" => "fill",
            "lifecycle" => "market_lifecycle_v2",
            _ => throw new ArgumentException($"Invalid action: {action}")
        };

        private async Task ProcessQueuePeriodicallyAsync()
        {
            _logger.LogDebug("Starting queue processor");
            while (!_globalCancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_connectionManager.IsConnected() && _queuedSubscriptionUpdates.TryDequeue(out var update))
                    {
                        var (action, marketTickers, channelAction) = update;
                        _logger.LogDebug("Processing queued subscription update: action={Action}, markets={Markets}, channel={Channel}",
                            action, string.Join(", ", marketTickers), channelAction);

                        await UpdateSubscriptionAsync(action, marketTickers, channelAction);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing queued subscription update");
                }

                await Task.Delay(1000, _globalCancellationToken);
            }
            _logger.LogDebug("Queue processor stopped");
        }

        private async Task CheckPendingConfirmsAsync()
        {
            _logger.LogDebug("Starting pending confirmations checker");
            while (!_globalCancellationToken.IsCancellationRequested)
            {
                try
                {
                    var expiredConfirms = _pendingConfirms
                        .Where(kvp => DateTime.UtcNow - kvp.Value.SentTime > TimeSpan.FromSeconds(60))
                        .ToList();

                    foreach (var confirm in expiredConfirms)
                    {
                        _logger.LogWarning("Pending confirmation expired for ID {Id}, channel {Channel}, markets {Markets} after 60 seconds",
                            confirm.Key, confirm.Value.Channel, string.Join(", ", confirm.Value.MarketTickers));
                        _pendingConfirms.TryRemove(confirm.Key, out var _);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking pending confirmations");
                }

                await Task.Delay(5000, _globalCancellationToken);
            }
            _logger.LogDebug("Pending confirmations checker stopped");
        }

        private async Task ProcessOrderBookQueuePeriodicallyAsync()
        {
            _logger.LogDebug("Starting order book queue processor");
            while (!_globalCancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_orderBookMessageQueue.TryDequeue(out var message, out var seq))
                    {
                        _logger.LogDebug("Processing order book message with seq {Seq}", seq);

                        // Update last sequence number
                        lock (_sequenceLock)
                        {
                            if (seq > _lastSequenceNumber)
                            {
                                _lastSequenceNumber = seq;
                            }
                        }

                        // Process the message
                        var eventArgs = new OrderBookEventArgs(message.OfferType, message.Data);
                        // Note: In the refactored design, this should trigger the OrderBookReceived event
                        // but since we're in SubscriptionManager, we need to coordinate with MessageProcessor
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order book queue message");
                }

                await Task.Delay(10, _globalCancellationToken);
            }
            _logger.LogDebug("Order book queue processor stopped");
        }

        public string GetChannelName(string action) => GetChannelNameInternal(action);

        public int GenerateNextMessageId() => Interlocked.Increment(ref _messageId);

        public async Task UpdateSubscriptionStateFromConfirmationAsync(int sid, string channel)
        {
            _logger.LogDebug("Updating subscription state for SID {Sid} on channel {Channel}", sid, channel);

            // Find the subscription and update its SID
            if (_subscriptions.TryGetValue(channel, out var subscription))
            {
                var updatedSubscription = (Sid: sid, subscription.Markets);
                _subscriptions[channel] = updatedSubscription;
                _logger.LogDebug("Updated SID for channel {Channel} to {Sid}", channel, sid);

                // Update subscription states for all markets in this channel
                foreach (var market in subscription.Markets)
                {
                    SetSubscriptionState(market, channel, SubscriptionState.Subscribed);
                }
            }
            else
            {
                _logger.LogWarning("Received confirmation for unknown channel {Channel}", channel);
            }
        }

        public bool RemovePendingConfirmation(int id)
        {
            bool removed = _pendingConfirms.TryRemove(id, out var _);
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

        public (string Channel, string[] MarketTickers)? GetPendingConfirm(int id)
        {
            if (_pendingConfirms.TryGetValue(id, out var confirm))
            {
                return (confirm.Channel, confirm.MarketTickers);
            }
            return null;
        }
    }
}