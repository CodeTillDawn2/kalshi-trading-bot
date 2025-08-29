using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using SmokehouseInterfaces.Constants;
using SmokehouseInterfaces.Enums;
using System.Collections.Concurrent;
using System.Data;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using KalshiBotAPI.Configuration;
using System.Text.Json;
using SmokehouseDTOs.Exceptions;

namespace KalshiBotAPI.WebSockets
{
    public class KalshiWebSocketClient : IKalshiWebSocketClient
    {
        private readonly ISqlDataService _sqlDataService;
        private readonly IStatusTrackerService _statusTrackerService;
        private readonly ILogger<IKalshiWebSocketClient> _logger;
        private readonly KalshiConfig _kalshiConfig;
        private readonly LoggingConfig _loggingConfig;
        private readonly RSA _privateKey;
        private ClientWebSocket? _webSocket = null!;
        private int _messageId = 1;
        private readonly object _webSocketLock = new object();
        private DateTime _lastMessageReceived = DateTime.UtcNow;
        private DateTime LastMessageReceived { get => _lastMessageReceived; set => _lastMessageReceived = value; }
        private readonly Dictionary<string, (int Sid, HashSet<string> Markets)> _subscriptions = new();
        private readonly ConcurrentDictionary<string, bool> _pendingSubscriptions = new ConcurrentDictionary<string, bool>();
        private readonly ConcurrentDictionary<int, (DateTime SentTime, string Message, string Channel, string[] MarketTickers)> _pendingConfirms = new ConcurrentDictionary<int, (DateTime, string, string, string[])>();
        private long _lastSequenceNumber = 0;
        private int _orderBookSid = 0;
        private readonly object _sequenceLock = new object();
        private HashSet<string> _watchedMarkets = new HashSet<string>();
        private readonly SemaphoreSlim _connectSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _subscriptionUpdateSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _channelSubscriptionSemaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastWatchedMarketsUpdate = DateTime.MinValue;
        private readonly ConcurrentQueue<(string Action, string[] MarketTickers, string ChannelAction)> _queuedSubscriptionUpdates = new();
        private bool _isConnected = false;
        private const int DebounceIntervalMs = 1000;
        private const int SubscriptionConfirmTimeoutSeconds = 20;
        private Task _queueProcessorTask = null!;
        private Task _confirmCheckTask = null!;
        private Dictionary<string, HashSet<string>> _subscriptionState = new Dictionary<string, HashSet<string>>();
        private bool _isReconnecting = false;

        private readonly PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long> _orderBookMessageQueue = new();
        private readonly object _orderBookQueueLock = new object();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>> _marketSubscriptionStates = new();
        private bool _allowReconnect = true;
        private readonly ConcurrentDictionary<string, long> _eventCounts = new ConcurrentDictionary<string, long>();
        private Task _orderBookProcessorTask = null!;

        private CancellationToken _globalCancellationToken => _statusTrackerService.GetCancellationToken();

        public event EventHandler<OrderBookEventArgs>? OrderBookReceived;
        public event EventHandler<TickerEventArgs>? TickerReceived;
        public event EventHandler<TradeEventArgs>? TradeReceived;
        public event EventHandler<FillEventArgs>? FillReceived;
        public event EventHandler<MarketLifecycleEventArgs>? MarketLifecycleReceived;
        public event EventHandler<EventLifecycleEventArgs>? EventLifecycleReceived;
        public event EventHandler<DateTime>? MessageReceived;

        public bool IsTradingActive { get; set; } = true;

        // Public properties for monitoring
        public ConcurrentDictionary<string, long> EventCounts => _eventCounts;
        public int ConnectSemaphoreCount => _connectSemaphore.CurrentCount;
        public int SubscriptionUpdateSemaphoreCount => _subscriptionUpdateSemaphore.CurrentCount;
        public int ChannelSubscriptionSemaphoreCount => _channelSubscriptionSemaphore.CurrentCount;
        public int QueuedSubscriptionUpdatesCount => _queuedSubscriptionUpdates.Count;
        public int OrderBookMessageQueueCount => _orderBookMessageQueue.Count;
        public int PendingConfirmsCount => _pendingConfirms.Count;

        private bool _firstCalculation = true; //Skip first warning about message queue being high

        // Public properties for last event received times
        public DateTime LastOrderBookReceived { get; private set; } = DateTime.MinValue;
        public DateTime LastTickerReceived { get; private set; } = DateTime.MinValue;
        public DateTime LastTradeReceived { get; private set; } = DateTime.MinValue;
        public DateTime LastFillReceived { get; private set; } = DateTime.MinValue;
        public DateTime LastLifecycleReceived { get; private set; } = DateTime.MinValue;

        private Dictionary<string, long> _marketTickerEventsReceived = new Dictionary<string, long>();
        private Dictionary<string, long> _marketOrderbooEventsReceived = new Dictionary<string, long>();
        private Dictionary<string, long> _marketTradeEventsReceived = new Dictionary<string, long>();

        public KalshiWebSocketClient(
            IOptions<KalshiConfig> kalshiConfig,
            IOptions<LoggingConfig> loggingConfig,
            ILogger<IKalshiWebSocketClient> logger,
            IStatusTrackerService statusTrackerService,
            ISqlDataService sqlDataService)
        {
            _kalshiConfig = kalshiConfig.Value;
            _loggingConfig = loggingConfig.Value;
            _logger = logger;
            _statusTrackerService = statusTrackerService;
            _sqlDataService = sqlDataService;
            _privateKey = RSA.Create();
            _privateKey.ImportFromPem(File.ReadAllText(_kalshiConfig.KeyFile));

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

        public long LastSequenceNumber
        {
            get
            {
                lock (_sequenceLock)
                {
                    return _lastSequenceNumber;
                }
            }
        }



        public async Task StopServicesAsync()
        {
            _logger.LogDebug("KalshiWebSocketClient.StopServicesAsync called at {0}, CancellationToken.IsCancellationRequested={IsRequested}"
                , DateTime.UtcNow, _globalCancellationToken.IsCancellationRequested);
            _allowReconnect = false;
            try
            {
                _logger.LogInformation("Unsubscribing from all channels...");
                await UnsubscribeFromAllAsync();
                _logger.LogInformation("Unsubscribed from all channels");

                await WaitForPendingUnsubscriptionConfirmsAsync();
                _logger.LogDebug("Completed waiting for unsubscription confirmations");

                _subscriptions.Clear();
                _subscriptionState.Clear();
                _pendingSubscriptions.Clear();
                _marketSubscriptionStates.Clear();
                _logger.LogDebug("Cleared all subscriptions and subscription states");

                ClientWebSocket? oldSocket = null;
                lock (_webSocketLock)
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        oldSocket = _webSocket;
                        _webSocket = null;
                        _isConnected = false;
                    }
                }
                if (oldSocket != null)
                {
                    await oldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping client", CancellationToken.None);
                    oldSocket.Dispose();
                    _logger.LogDebug("Closed and disposed WebSocket");
                }

                if (_queueProcessorTask != null && !_queueProcessorTask.IsCompleted)
                {
                    try
                    {
                        await _queueProcessorTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Queue processor task canceled as expected");
                    }
                    _logger.LogDebug("Stopped queue processor");
                }

                if (_confirmCheckTask != null && !_confirmCheckTask.IsCompleted)
                {
                    try
                    {
                        await _confirmCheckTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Confirmation check task canceled as expected");
                    }
                    _logger.LogDebug("Stopped confirmation check task");
                }
                if (_orderBookProcessorTask != null && !_orderBookProcessorTask.IsCompleted)
                {
                    try
                    {
                        await _orderBookProcessorTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Order book queue processor task canceled as expected");
                    }
                    _logger.LogDebug("Stopped order book queue processor");
                }

                lock (_sequenceLock)
                {
                    _lastSequenceNumber = 0;
                    _orderBookSid = 0;
                }
                _logger.LogDebug("Reset sequence number and orderbook SID");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping KalshiWebSocketClient");
            }
            finally
            {
                _logger.LogDebug("KalshiWebSocketClient.StopAsync completed at {0}", DateTime.UtcNow);
            }
        }


        public async Task UnsubscribeFromChannelAsync(string action)
        {
            _logger.LogDebug("Unsubscribing from channel: action={Action}", action);
            if (!IsConnected())
            {
                _logger.LogWarning("WebSocket not connected, cannot unsubscribe from {Action}", action);
                return;
            }

            bool semaphoreAcquired = false;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Acquiring channel subscription semaphore for action {Action}", action);
                semaphoreAcquired = await _channelSubscriptionSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire channel subscription semaphore for action {Action} within 60000ms", action);
                    return;
                }
                _logger.LogDebug("Acquired channel subscription semaphore for action {Action}", action);

                var channel = GetChannelName(action);
                if (!_subscriptions.TryGetValue(channel, out var subscription))
                {
                    _logger.LogWarning("No active subscription for {Channel}, skipping unsubscription", channel);
                    return;
                }

                if (subscription.Sid == 0)
                {
                    _logger.LogWarning("No SID for {Channel}, removing local subscription", channel);
                    _subscriptions.Remove(channel);
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
                await SendMessageAsync(message);

                foreach (var marketTicker in subscription.Markets)
                {
                    UpdateSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                }
                _subscriptions.Remove(channel);
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
                    _logger.LogDebug("Released channel subscription semaphore for action {Action}", action);
                    _channelSubscriptionSemaphore.Release();
                }
            }
        }

        public HashSet<string> WatchedMarkets
        {
            get => _watchedMarkets;
            set
            {
                _watchedMarkets = value.ToHashSet();
            }
        }

        public int GetNextMessageId() => Interlocked.Increment(ref _messageId);

        public bool IsSubscribed(string marketTicker, string action)
        {
            var channel = GetChannelName(action);
            bool isSubscribed = _subscriptions.TryGetValue(channel, out var subscription) && (subscription.Markets.Contains(marketTicker) || marketTicker == "");
            _logger.LogDebug("Checked subscription for {MarketTicker} on {Channel}: {IsSubscribed}", marketTicker, channel, isSubscribed);
            return isSubscribed;
        }

        public bool CanSubscribe(string marketTicker, string channel)
        {
            var marketStates = _marketSubscriptionStates.GetOrAdd(marketTicker, _ => new ConcurrentDictionary<string, SubscriptionState>());
            var state = marketStates.GetOrAdd(channel, SubscriptionState.Unsubscribed);
            bool canSubscribe = state == SubscriptionState.Unsubscribed || state == SubscriptionState.Unsubscribing;
            _logger.LogDebug("CanSubscribe check for {MarketTicker} on {Channel}: State={State}, CanSubscribe={CanSubscribe}", marketTicker, channel, state, canSubscribe);
            return canSubscribe;
        }

        public void UpdateSubscriptionState(string marketTicker, string channel, SubscriptionState state)
        {
            var marketStates = _marketSubscriptionStates.GetOrAdd(marketTicker, _ => new ConcurrentDictionary<string, SubscriptionState>());
            marketStates[channel] = state;
            _logger.LogDebug("Updated subscription state for {MarketTicker} on {Channel} to {State}", marketTicker, channel, state);
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

            if (!_isConnected)
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
                _logger.LogDebug("Acquiring subscription update semaphore for action {Action}", action);
                semaphoreAcquired = await _channelSubscriptionSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire channel subscription semaphore for update on {Channel} within 60000ms", channel);
                    return;
                }
                _logger.LogDebug("Acquired channel subscription semaphore for update on {Channel}", channel);

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

                var subscriptionId = GetNextMessageId();
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

                await SendMessageAsync(message);

                var updatedMarkets = new HashSet<string>(subscription.Markets);
                foreach (var ticker in marketsToUpdate)
                {
                    if (action == "add_markets")
                        updatedMarkets.Add(ticker);
                    else
                        updatedMarkets.Remove(ticker);
                    _pendingSubscriptions.TryRemove($"{channelAction}:{ticker}", out _);
                    if (action == "delete_markets")
                        UpdateSubscriptionState(ticker, channel, SubscriptionState.Unsubscribed);
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
                    channel, action, string.Join(", ", marketTickers));
                throw;
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _logger.LogDebug("Released channel subscription semaphore for update on {Channel}", channel);
                    _channelSubscriptionSemaphore.Release();
                }
            }
        }

        public void ResetEventCounts()
        {
            _eventCounts.Clear();
        }

        public void ClearOrderBookQueueForMarket(string marketTicker)
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


        public async Task ConnectAsync(int retryCount = 0)
        {
            if ((!_allowReconnect && !_isConnected) || !_statusTrackerService.InitializationCompleted.Task.IsCompleted)
            {
                _logger.LogDebug("Reconnection disabled, skipping connect attempt");
                return;
            }
            _logger.LogInformation("Connecting WebSocket, retry attempt: {RetryCount}", retryCount);
            bool semaphoreAcquired = false;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                semaphoreAcquired = await _connectSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire connect semaphore within 60000ms");
                    return;
                }
                _logger.LogDebug("Acquired connect semaphore");

                lock (_webSocketLock)
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        _logger.LogDebug("WebSocket already open, skipping connection attempt");
                        return;
                    }
                    if (_webSocket != null)
                    {
                        _webSocket.Dispose();
                        _webSocket = null;
                        _isConnected = false;
                        _logger.LogDebug("Disposed closed or stale WebSocket");
                    }
                }

                var uri = new Uri(_kalshiConfig.Environment == "demo"
                    ? "wss://demo-api.kalshi.co/trade-api/ws/v2"
                    : "wss://api.elections.kalshi.com/trade-api/ws/v2");

                var newWebSocket = new ClientWebSocket();
                newWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                var (timestamp, signature) = GenerateAuthHeaders("GET", "/trade-api/ws/v2");
                newWebSocket.Options.SetRequestHeader("KALSHI-ACCESS-KEY", _kalshiConfig.KeyId);
                newWebSocket.Options.SetRequestHeader("KALSHI-ACCESS-SIGNATURE", signature);
                newWebSocket.Options.SetRequestHeader("KALSHI-ACCESS-TIMESTAMP", timestamp);

                await newWebSocket.ConnectAsync(uri, _globalCancellationToken);
                _logger.LogDebug("WebSocket connection established");
                _lastMessageReceived = DateTime.UtcNow;
                _isConnected = true;

                lock (_webSocketLock)
                {
                    _webSocket = newWebSocket;
                }

                // Subscribe to non-market-specific channels
                foreach (var channel in new[] { "fill", "lifecycle" 
                })
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    await SubscribeToChannelAsync(channel, Array.Empty<string>());
                    _logger.LogDebug("Subscribed to non-market-specific channel {Channel}", channel);
                }

                await RestoreSubscriptionsAsync();
                await ResubscribeAsync();
                _logger.LogDebug("Restored and resubscribed to existing subscriptions");

                //Clear pending subscription updates when connecting
                _queuedSubscriptionUpdates.Clear();

                _ = Task.Run(() => ReceiveAsync());

                if (_queueProcessorTask == null || _queueProcessorTask.IsCompleted)
                {
                    _queueProcessorTask = Task.Run(() => ProcessQueuePeriodicallyAsync(), _globalCancellationToken);
                    _logger.LogDebug("Started background queue processor");
                }

                if (_confirmCheckTask == null || _confirmCheckTask.IsCompleted)
                {
                    _confirmCheckTask = Task.Run(() => CheckPendingConfirmsAsync(), _globalCancellationToken);
                    _logger.LogDebug("Started confirmation check task");
                }
                if (_orderBookProcessorTask == null || _orderBookProcessorTask.IsCompleted)
                {
                    _orderBookProcessorTask = Task.Run(() => ProcessOrderBookQueuePeriodicallyAsync(), _globalCancellationToken);
                    _logger.LogDebug("Started background order book queue processor");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ConnectAsync was cancelled on retry {RetryCount}", retryCount);
                _isConnected = false;
            }
            catch (WebSocketException ex) when (ex.Message.Contains("Failed to connect WebSocket on retry"))
            {
                _logger.LogWarning(new WebSocketRetryFailedException(ex.Message, ex), "Failed to connect to a websocket");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect WebSocket on retry {RetryCount}", retryCount);
                if (retryCount < 5 && _allowReconnect)
                {
                    int delay = (int)Math.Pow(2, retryCount) * 1000;
                    _logger.LogInformation("Retrying connection in {Delay}ms", delay);
                    await Task.Delay(delay, _globalCancellationToken);
                    await ConnectAsync(retryCount + 1);
                }
                else
                {
                    _isConnected = false;
                    throw new InvalidOperationException("Failed to establish WebSocket connection after maximum retries", ex);
                }
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _connectSemaphore.Release();
                    _logger.LogDebug("Released connect semaphore");
                }
            }
        }

        public (int orderbookEvents, int tradeEvents, int tickerEvents) ReturnWebSocketCountsByMarket(string marketTicker)
        {
            int orderbookEvents = _marketOrderbooEventsReceived.TryGetValue(marketTicker, out var orderbookCount) ? (int)orderbookCount : 0;
            int tradeEvents = _marketTradeEventsReceived.TryGetValue(marketTicker, out var tradeCount) ? (int)tradeCount : 0;
            int tickerEvents = _marketTickerEventsReceived.TryGetValue(marketTicker, out var tickerCount) ? (int)tickerCount : 0;

            return (orderbookEvents, tradeEvents, tickerEvents);
        }

        public async Task SubscribeToWatchedMarketsAsync()
        {
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                if (!_isConnected)
                {
                    _logger.LogWarning("WebSocket not connected, cannot subscribe to watched markets");
                    return;
                }

                if (!_statusTrackerService.InitializationCompleted.Task.IsCompleted)
                {
                    _logger.LogWarning("Initialization not complete, delaying watched markets subscription");
                    return;
                }

                if (!_watchedMarkets.Any())
                {
                    _logger.LogDebug("No watched markets to subscribe to");
                    return;
                }

                _logger.LogInformation("Subscribing to watched markets: {Markets}", string.Join(", ", _watchedMarkets));
                foreach (var action in new[] { "orderbook", "ticker", "trade" })
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    var marketsToSubscribe = _watchedMarkets
                        .Where(m => !IsSubscribed(m, action) && CanSubscribe(m, GetChannelName(action)))
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

        public void DisableReconnect()
        {
            _logger.LogDebug("Disabling WebSocket reconnection.");
            _allowReconnect = false;
        }

        public void EnableReconnect()
        {
            _logger.LogDebug("Enabling WebSocket reconnection.");
            _allowReconnect = true;
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
                    _logger.LogDebug("SUB-Order book queue cleared for market: {MarketTicker}", marketTicker);
                    return;
                }
                {
                    waited = true;
                    _logger.LogInformation("SUB-Waiting for order book queue to clear for market: {MarketTicker}", marketTicker);
                }

                if (DateTime.UtcNow - startTime > timeout)
                {
                    _logger.LogWarning("SUB-Timeout waiting for order book queue to clear for market: {MarketTicker} after {TimeoutSeconds}s", marketTicker, timeout.TotalSeconds);
                    return;
                }

                await Task.Delay(100, _globalCancellationToken);
            }

            if (waited)
                _logger.LogInformation("SUB-Market {0} waited {1}s before saving snapshot", marketTicker, (DateTime.UtcNow - startTime).TotalSeconds);
        }

        public async Task ResetConnectionAsync()
        {
            if (_isReconnecting)
            {
                _logger.LogDebug("Reconnection already in progress, skipping");
                return;
            }

            _isReconnecting = true;
            _logger.LogDebug("Resetting WebSocket connection");
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                ClientWebSocket? oldSocket = null;
                lock (_webSocketLock)
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        _logger.LogWarning("Closing existing WebSocket connection");
                        oldSocket = _webSocket;
                    }
                    _webSocket = null;
                    _isConnected = false;
                }

                _subscriptionState.Clear();
                foreach (var sub in _subscriptions)
                {
                    _subscriptionState[sub.Key] = new HashSet<string>(sub.Value.Markets);
                }
                _logger.LogInformation("SUB-Captured subscription snapshot: {Count} channels", _subscriptionState.Count);

                if (oldSocket != null)
                {
                    await oldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Resetting socket", _globalCancellationToken);
                    oldSocket.Dispose();
                    _logger.LogDebug("Closed and disposed old WebSocket");
                }

                lock (_sequenceLock)
                {
                    _lastSequenceNumber = 0;
                    _orderBookSid = 0;
                }

                _marketSubscriptionStates.Clear();
                _subscriptions.Clear();
                _logger.LogInformation("SUB-Cleared subscriptions");
                _logger.LogDebug("Preserving {Count} pending subscriptions", _pendingSubscriptions.Count);

                if (_allowReconnect)
                {
                    _logger.LogInformation("SUB-Waiting 5 seconds");
                    await Task.Delay(5000, _globalCancellationToken);
                    await ConnectAsync();
                }
                else
                {
                    _logger.LogDebug("Reconnection disabled, skipping connect attempt.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ResetConnectionAsync was cancelled");
                _isConnected = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset WebSocket connection");
                _isConnected = false;
            }
            finally
            {
                _isReconnecting = false;
                _logger.LogDebug("Reconnection attempt completed");
            }
        }

        public bool IsConnected()
        {
            lock (_webSocketLock)
            {
                bool connected = _webSocket != null && _webSocket.State == WebSocketState.Open;
                _logger.LogDebug("WebSocket connection status: {Connected}", connected);
                return connected;
            }
        }


        public async Task SubscribeToChannelAsync(string action, string[] marketTickers)
        {
            _logger.LogInformation("SUB-Subscribing to channel: action={Action}, markets={Markets}", action, string.Join(", ", marketTickers));
            if (!IsConnected())
            {
                _logger.LogDebug("WebSocket not connected, queuing subscription: action={Action}, markets={Markets}", action, string.Join(", ", marketTickers));
                _queuedSubscriptionUpdates.Enqueue(("add_markets", marketTickers, action));
                return;
            }

            bool semaphoreAcquired = false;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Acquiring channel subscription semaphore: action={Action}, markets={Markets}", action, string.Join(", ", marketTickers));
                semaphoreAcquired = await _channelSubscriptionSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire channel subscription semaphore for action {Action} within 60000ms", action);
                    return;
                }
                _logger.LogDebug("Acquired channel subscription semaphore for action {Action}", action);

                var channel = GetChannelName(action);
                if (!_subscriptions.TryGetValue(channel, out var subscription))
                {
                    subscription = (0, new HashSet<string>());
                    _subscriptions[channel] = subscription;
                }
                var newSubscriptions = marketTickers
                    .Where(ticker => CanSubscribe(ticker, channel) && !subscription.Markets.Contains(ticker)
                    ).ToArray();

                _logger.LogInformation("SUB-New subscriptions for {Channel}: {Markets}. Current Subscription: {0}"
                    , channel, string.Join(", ", newSubscriptions), string.Join(", ", subscription.Markets));

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
                    _logger.LogDebug("No new markets to subscribe {Channel}: {Markets}. Existing: {Existing}", channel, string.Join(", ", marketTickers), string.Join(", ", subscription.Markets));
                    return;
                }

                foreach (var ticker in newSubscriptions)
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    UpdateSubscriptionState(ticker, channel, SubscriptionState.Subscribing);
                    _logger.LogDebug("Set subscription state to Subscribing for {MarketTicker} on {Channel}", ticker, channel);
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
                    _logger.LogDebug("Sending update_subscription for existing {Channel}, ID={Id}, SID={Sid}, markets={Markets}", channel, subscriptionId, subscription.Sid, string.Join(", ", newSubscriptions));
                }
                else
                {
                    string[] marketsToSubscribeTo = (new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel)) ? newSubscriptions : Array.Empty<string>();

                    if (new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel) && newSubscriptions.Length == 0)
                    {
                        _logger.LogWarning(message: "No markets to subscribe for {Channel}, but channel requires market tickers: {Markets}. Skipping subscription."
                            , channel, string.Join(", ", newSubscriptions));
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
                    _logger.LogDebug("Sending subscribe for new {Channel}, ID={Id}, markets={Markets}", channel, subscriptionId, string.Join(", ", newSubscriptions));
                }

                if (new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel) && !SkipMessage)
                {
                    _pendingConfirms.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel, newSubscriptions));
                    _logger.LogInformation("SUB-Added to pending {action} confirms for {Channel}, ID={Id}, expecting 'ok' or 'subscribed', tickers: {tickers}", action, channel, subscriptionId, newSubscriptions);
                }
                else
                {
                    _logger.LogDebug("Skipping pending confirms for {Channel}, ID={Id}, non-market-specific channel", channel, subscriptionId);
                }

                if (!SkipMessage)
                {
                    await SendMessageAsync(message);

                    var currentMarkets = subscription.Markets;
                    foreach (var ticker in newSubscriptions)
                    {
                        currentMarkets.Add(ticker);
                        _pendingSubscriptions.TryAdd($"{action}:{ticker}", true);
                    }
                    _subscriptions[channel] = (subscription.Sid, currentMarkets);
                    _logger.LogInformation("SUB-Updated subscription for {Channel}: markets={Markets}, SID={Sid}", channel, string.Join(", ", currentMarkets), subscription.Sid);
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
                    UpdateSubscriptionState(ticker, GetChannelName(action), SubscriptionState.Unsubscribed);
                    _logger.LogWarning("Reset subscription state to Unsubscribed for {MarketTicker} on {Channel} due to error", ticker, GetChannelName(action));
                }
                _logger.LogError(ex, "Failed to subscribe to {Channel} for markets: {Markets}", action, string.Join(", ", marketTickers));
                throw;
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _channelSubscriptionSemaphore.Release();
                    _logger.LogDebug("Released channel subscription semaphore for action {Action}", action);
                }
            }
        }

        public async Task ResubscribeAsync(bool force = false)
        {
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation("SUB-Resubscribing with force={Force}", force);
                if (!IsConnected())
                {
                    _logger.LogWarning("Cannot resubscribe: WebSocket is not connected");
                    throw new InvalidOperationException("Cannot resubscribe: WebSocket is not connected");
                }
                if (_watchedMarkets == null || !_watchedMarkets.Any())
                {
                    _logger.LogDebug("No watched markets set, skipping resubscription");
                    return;
                }

                string[] marketTickers = _watchedMarkets.ToArray();
                foreach (var action in KalshiConstants.MarketChannels)
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    var channel = GetChannelName(action);
                    if (force || !_subscriptions.ContainsKey(channel))
                    {
                        _subscriptions[channel] = (0, new HashSet<string>());
                        _logger.LogInformation("SUB-Initialized subscription for {Channel} during resubscribe", channel);
                    }

                    var marketsToSubscribe = marketTickers
                        .Where(m => !IsSubscribed(m, action) && CanSubscribe(m, channel))
                        .ToArray();

                    if (marketsToSubscribe.Any())
                    {
                        _logger.LogInformation("SUB-Resubscribing {Action} for markets: {Markets}", action, string.Join(", ", marketsToSubscribe));
                        await SubscribeToChannelAsync(action, marketsToSubscribe);
                    }
                    else
                    {
                        _logger.LogDebug("No markets to resubscribe for {Action}: {Markets}", action, string.Join(", ", marketTickers));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ResubscribeAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resubscribe with force={Force}", force);
                throw;
            }
        }

        public string GetChannelName(string action) => action switch
        {
            "orderbook" => "orderbook_delta",
            "ticker" => "ticker",
            "trade" => "trade",
            "fill" => "fill",
            "lifecycle" => "market_lifecycle_v2",
            _ => throw new ArgumentException($"Invalid action: {action}")
        };

        public async Task SendMessageAsync(string message)
        {
            _logger.LogInformation("SUB-Sending WebSocket message: {Message}", message);
            ClientWebSocket currentSocket;
            lock (_webSocketLock)
            {
                if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                {
                    _logger.LogError("Cannot send message: WebSocket is not connected");
                    throw new InvalidOperationException("Cannot send message: WebSocket is not connected");
                }
                currentSocket = _webSocket;
            }

            var buffer = Encoding.UTF8.GetBytes(message);
            await currentSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                _globalCancellationToken);
            _logger.LogDebug("Sent WebSocket message successfully");
        }

        public async Task UnsubscribeFromAllAsync()
        {
            _logger.LogDebug("Acquiring channel subscription semaphore for unsubscribe");
            bool semaphoreAcquired = false;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                await WaitSemaphoreWithTimeoutWarningAsync(_channelSubscriptionSemaphore, "unsubscribe from all feeds");
                semaphoreAcquired = true;
                _logger.LogDebug("Acquired channel subscription semaphore for unsubscription");

                lock (_webSocketLock)
                {
                    if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                    {
                        _logger.LogWarning("WebSocket is not connected, skipping unsubscription");
                        return;
                    }
                }
                foreach (var subscription in _subscriptions.ToList())
                {
                    var channel = subscription.Key;
                    var sid = subscription.Value.Sid;
                    var markets = subscription.Value.Markets.ToArray();
                    if (sid != 0)
                    {
                        var subscriptionId = GetNextMessageId();
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
                        await SendMessageAsync(message);
                        foreach (var marketTicker in markets)
                        {
                            UpdateSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                        }
                        _logger.LogDebug("Marked local subscription as unsubscribed for channel {Channel}", channel);
                    }
                    else
                    {
                        _logger.LogWarning("No SID for channel {Channel}, skipping unsubscribe command", channel);
                    }
                }
                lock (_sequenceLock)
                {
                    _lastSequenceNumber = 0;
                    _orderBookSid = 0;
                }
                _logger.LogDebug("Reset sequence number and orderbook SID");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("KalshiWebSocketClient.UnsubscribeFromAllAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from all feeds");
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _logger.LogDebug("Released channel subscription semaphore for unsubscription");
                    _channelSubscriptionSemaphore.Release();
                }
                else
                {
                    _logger.LogDebug("Semaphore not acquired, skipping release for unsubscription");
                }
            }
        }



        private async Task WaitSemaphoreWithTimeoutWarningAsync(SemaphoreSlim semaphore, string context)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var timeoutTask = Task.Delay(5000);
                var waitTask = semaphore.WaitAsync(_globalCancellationToken);

                var completedTask = await Task.WhenAny(waitTask, timeoutTask);
                if (completedTask == timeoutTask && !waitTask.IsCompleted)
                {
                    _logger.LogWarning("Semaphore wait for {Context} exceeded 5 seconds; still waiting", context);
                    await waitTask;
                }
                else
                {
                    await waitTask;
                }

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                if (elapsed > 5000)
                {
                    _logger.LogWarning("Semaphore wait for {Context} took {ElapsedMs}ms", context, elapsed);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("WaitSemaphoreWithTimeoutWarningAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to wait for semaphore for {Context}", context);
            }
        }

        private async Task RestoreSubscriptionsAsync()
        {
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation("SUB-Restoring subscriptions from snapshot: {Count} channels", _subscriptionState.Count);
                foreach (var channel in _subscriptionState)
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    var markets = channel.Value.ToArray();
                    if (markets.Any())
                    {
                        _logger.LogInformation("SUB-Restoring subscription for channel {Channel} with markets: {Markets}", channel.Key, string.Join(", ", markets));
                        try
                        {
                            await SubscribeToChannelAsync(GetActionFromChannel(channel.Key), markets);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to restore subscription for channel {Channel}", channel.Key);
                        }
                    }
                }
                _logger.LogDebug("Completed restoring subscriptions");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("RestoreSubscriptionsAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while restoring subscriptions");
            }
        }

        private async Task ProcessQueuedSubscriptionsAsync()
        {
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Processing queued subscriptions, queue count: {Count}", _queuedSubscriptionUpdates.Count);
                while (_queuedSubscriptionUpdates.TryDequeue(out var update))
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    _logger.LogDebug("Processing queued subscription update: action={Action}, channel={Channel}, markets={Markets}", update.Action, update.ChannelAction, string.Join(", ", update.MarketTickers));
                    await UpdateSubscriptionAsync(update.Action, update.MarketTickers, update.ChannelAction);
                }
                _logger.LogDebug("Completed processing queued subscriptions");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ProcessQueuedSubscriptionsAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process queued subscriptions");
            }
        }

        private async Task ProcessQueuePeriodicallyAsync()
        {
            _logger.LogDebug("Starting periodic queue processor");
            while (!_globalCancellationToken.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;
                try
                {
                    if (_isConnected && _queuedSubscriptionUpdates.Count > 0)
                    {
                        await ProcessQueuedSubscriptionsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing queued subscriptions");
                }

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                if (elapsed > 5000)
                {
                    _logger.LogWarning("Queue processing took {ElapsedMs}ms, exceeding 5000ms", elapsed);
                }

                var delay = Math.Max(0, 1000 - (int)elapsed);
                await Task.Delay(delay, _globalCancellationToken);
            }
            _logger.LogDebug("Stopped periodic queue processor");
        }

        private async Task CheckPendingConfirmsAsync()
        {
            _logger.LogDebug("Starting subscription confirmation check");
            var retryCounts = new ConcurrentDictionary<int, int>();
            const int maxRetries = 3;

            while (!_globalCancellationToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var key in _pendingConfirms.Keys.ToArray())
                    {
                        if (_globalCancellationToken.IsCancellationRequested)
                        {
                            _logger.LogDebug("Cancellation requested, exiting confirmation check");
                            return;
                        }
                        if (_pendingConfirms.TryGetValue(key, out var confirm) &&
                            (DateTime.UtcNow - confirm.SentTime).TotalSeconds > SubscriptionConfirmTimeoutSeconds)
                        {
                            int retryCount = retryCounts.GetOrAdd(key, 0);
                            _logger.LogWarning("Subscription command ID={Id} for channel '{Channel}' timed out after {Timeout}s, retry {RetryCount}/{MaxRetries}, markets={Markets}",
                                key, confirm.Channel, SubscriptionConfirmTimeoutSeconds, string.Join(", ", confirm.MarketTickers), retryCount, maxRetries);

                            if (retryCount < maxRetries)
                            {
                                bool isInitialSubscription = confirm.Message.Contains("\"cmd\": \"subscribe\"");
                                _logger.LogInformation("Retrying subscription ID={Id} for channel {Channel}, markets={Markets}, initial = {isInitialSubscription}",
                                    key, confirm.Channel, string.Join(", ", confirm.MarketTickers), isInitialSubscription);
                                if (isInitialSubscription)
                                {
                                    await SubscribeToChannelAsync(GetActionFromChannel(confirm.Channel), confirm.MarketTickers);
                                }
                                else
                                {
                                    await UpdateSubscriptionAsync("add_markets", confirm.MarketTickers, GetActionFromChannel(confirm.Channel));
                                }
                                retryCounts[key] = retryCount + 1;
                                if (_pendingConfirms.TryGetValue(key, out var currentValue))
                                {
                                    var updatedValue = (DateTime.UtcNow, currentValue.Message, currentValue.Channel, currentValue.MarketTickers);
                                    _pendingConfirms.TryUpdate(key, updatedValue, currentValue);
                                }
                            }
                            else
                            {
                                _logger.LogError("Subscription ID={Id} for channel {Channel} failed after {MaxRetries} retries, marking as failed. Markets = '{market}'",
                                    key, confirm.Channel, maxRetries, confirm.MarketTickers);
                                _pendingConfirms.TryRemove(key, out _);
                                retryCounts.TryRemove(key, out _);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Confirmation check task canceled as expected");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in confirmation check task");
                }
            }
            _logger.LogDebug("Stopped subscription confirmation check");
        }

        private async Task ProcessOrderBookQueuePeriodicallyAsync()
        {
            _logger.LogDebug("Starting periodic order book queue processor");
            while (!_globalCancellationToken.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;
                try
                {
                    if (_isConnected && _orderBookMessageQueue.Count > 0)
                    {
                        ProcessOrderBookMessagesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order book queue");
                }

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var delay = Math.Max(0, 100 - (int)elapsed); // 100ms interval
                await Task.Delay(delay, _globalCancellationToken);
            }
            _logger.LogDebug("Stopped periodic order book queue processor");
        }

        private (string timestamp, string signature) GenerateAuthHeaders(string method, string path)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var message = $"{timestamp}{method}{path.Split('?')[0]}";
            var signature = Convert.ToBase64String(_privateKey.SignData(
                Encoding.UTF8.GetBytes(message),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss));
            return (timestamp, signature);
        }



        private string GetActionFromChannel(string channel) => channel switch
        {
            "orderbook_delta" => "orderbook",
            "ticker" => "ticker",
            "trade" => "trade",
            "fill" => "fill",
            "market_lifecycle_v2" => "lifecycle",
            _ => throw new ArgumentException($"Invalid channel: {channel}")
        };

        private async Task ReceiveAsync()
        {
            _logger.LogDebug("ReceiveAsync started");
            var buffer = new byte[1024 * 16];
            var messageBuilder = new StringBuilder();
            try
            {
                while (!_globalCancellationToken.IsCancellationRequested)
                {
                    ClientWebSocket currentSocket;
                    lock (_webSocketLock)
                    {
                        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                            throw new InvalidOperationException("WebSocket connection lost");
                        currentSocket = _webSocket;
                    }

                    var result = await currentSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _globalCancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogError("WebSocket closed by server: Code={Code}, Reason={Reason}", result.CloseStatus, result.CloseStatusDescription);
                        throw new InvalidOperationException($"WebSocket closed: {result.CloseStatusDescription}");
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messagePart = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(messagePart);

                        if (result.EndOfMessage)
                        {
                            var fullMessage = messageBuilder.ToString();
                            _lastMessageReceived = DateTime.UtcNow;
                            _logger.LogDebug("Received complete WebSocket message: Length={Length}", fullMessage.Length);
                            await ProcessMessageAsync(fullMessage);
                            messageBuilder.Clear();
                        }
                    }
                    else
                    {
                        _logger.LogError("Unexpected WebSocket message type: {MessageType}", result.MessageType);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ReceiveAsync cancelled at {0}", DateTime.UtcNow);
            }
            catch (WebSocketException ex) when (ex.Message.Contains("without completing the close handshake"))
            {
                if (IsTradingActive && _allowReconnect && !_globalCancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(new ConnectionDisruptionException("The exchange didn't complete its handshake."),
                        "The exchange didn't complete its handshake. Exchange should be active, attempting to reconnect");
                }
                else
                {
                    _logger.LogWarning("Exchange is inactive or reconnection disabled, skipping reconnection attempt");
                    lock (_webSocketLock)
                    {
                        _isConnected = false;
                        if (_webSocket != null)
                        {
                            _webSocket.Dispose();
                            _webSocket = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_allowReconnect && !_globalCancellationToken.IsCancellationRequested)
                {
                    _logger.LogError("WebSocket receiver encountered error: {Message}. Attempting to reconnect", ex.Message);
                    await ResetConnectionAsync();
                }
                else
                {
                    _logger.LogDebug("Reconnection disabled or canceled, stopping WebSocket receiver: {Message}", ex.Message);
                }
            }
            finally
            {
                _logger.LogDebug("ReceiveAsync completed at {0}, CancellationToken.IsCancellationRequested={IsRequested}"
                    , DateTime.UtcNow, _globalCancellationToken.IsCancellationRequested);
            }
        }

        private async Task WaitForPendingUnsubscriptionConfirmsAsync()
        {
            _logger.LogDebug("Waiting for pending unsubscription confirmations, total pending count: {Count}", _pendingConfirms.Count);
            var startTime = DateTime.UtcNow;
            const int timeoutSeconds = 10;

            bool HasPendingUnsubscribes() => _pendingConfirms.Any(kvp => kvp.Value.Message.Contains("unsubscribe"));

            while (HasPendingUnsubscribes() && !_globalCancellationToken.IsCancellationRequested)
            {
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeoutSeconds)
                {
                    var remainingUnsubscribes = _pendingConfirms.Count(kvp => kvp.Value.Message.Contains("unsubscribe"));
                    _logger.LogWarning("Timeout waiting for unsubscription confirmations after {Timeout}s, remaining unconfirms: {Count}", timeoutSeconds, remainingUnsubscribes);
                    break;
                }
                await Task.Delay(100, _globalCancellationToken);
            }

            if (!HasPendingUnsubscribes())
            {
                _logger.LogDebug("All unsubscription confirmations received");
            }
            else
            {
                var remainingUnsubscribes = _pendingConfirms.Count(kvp => kvp.Value.Message.Contains("unsubscribe"));
                _logger.LogWarning("Proceeding with {Count} unconfirmed unsubscriptions", remainingUnsubscribes);
            }
        }



        private async Task ProcessMessageAsync(string message)
        {
            _logger.LogDebug("Stats: Processing WebSocket message: {Message}", message);
            var startTime = DateTime.UtcNow;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                var data = JsonSerializer.Deserialize<JsonElement>(message);
                var msgType = data.GetProperty("type").GetString() ?? "unknown";

                if (MessageReceived != null)
                    MessageReceived?.Invoke(this, DateTime.UtcNow);

                void CheckMarketSubscription(string channel, JsonElement msgData)
                {
                    if (msgData.TryGetProperty("market_ticker", out var tickerProp))
                    {
                        var marketTicker = tickerProp.GetString();
                        if (!string.IsNullOrEmpty(marketTicker) &&
                            _subscriptions.TryGetValue(channel, out var subscription) &&
                            !subscription.Markets.Contains(marketTicker))
                        {
                            _logger.LogDebug("Received {Channel} message for unsubscribed market: {MarketTicker}", channel, marketTicker);
                        }
                    }
                }
                switch (msgType)
                {
                    case "orderbook_snapshot":
                    case "orderbook_delta":
                        LastOrderBookReceived = DateTime.UtcNow;
                        _eventCounts.AddOrUpdate("OrderBook", 1, (_, count) => count + 1);
                        var orderbookTicker = data.GetProperty("msg").GetProperty("market_ticker").GetString();
                        _logger.LogDebug("Processing orderbook message: Type={Type}, QueueCount={QueueCount}, MarketTicker={Ticker}",
                            msgType, _orderBookMessageQueue.Count, orderbookTicker);
                        var offerType = msgType.Contains("snapshot") ? "SNP" : "DEL";
                        var seq = data.TryGetProperty("seq", out var seqProp) ? seqProp.GetInt64() : -1;

                        lock (_sequenceLock)
                        {
                            if (seq < _lastSequenceNumber)
                                _logger.LogDebug("Out-of-order sequence detected: Last={LastSeq}, Current={CurrentSeq}", _lastSequenceNumber, seq);
                            else if (seq > _lastSequenceNumber + 1)
                                _logger.LogDebug("Sequence gap detected: Last={LastSeq}, Current={CurrentSeq}", _lastSequenceNumber, seq);
                            _lastSequenceNumber = seq;
                        }

                        lock (_orderBookQueueLock)
                        {
                            _orderBookMessageQueue.Enqueue((data, offerType, seq, Guid.NewGuid()), seq);
                            _logger.LogDebug("Enqueued orderbook message for seq={Seq}, queue count={Count}", seq, _orderBookMessageQueue.Count);
                        }

                        if (!_marketOrderbooEventsReceived.ContainsKey(orderbookTicker))
                        {
                            _marketOrderbooEventsReceived[orderbookTicker] = 1;
                        }
                        else
                        {
                            _marketOrderbooEventsReceived[orderbookTicker] = _marketOrderbooEventsReceived[orderbookTicker] + 1;
                        }

                        CheckMarketSubscription("orderbook_delta", data.GetProperty("msg"));

                        if (_loggingConfig.StoreWebSocketEvents)
                        {
                            await _sqlDataService.StoreOrderBookAsync(data, offerType);
                            _logger.LogDebug("Stored orderbook: Type={Type}, MarketTicker={Ticker}, QueueCount={QueueCount}",
                                msgType, data.GetProperty("msg").GetProperty("market_ticker").GetString(), _orderBookMessageQueue.Count);
                        }
                        break;
                    case "ticker":
                        LastTickerReceived = DateTime.UtcNow;
                        _eventCounts.AddOrUpdate("Ticker", 1, (_, count) => count + 1);
                        var tickermsg = data.GetProperty("msg");
                        var market_ticker = tickermsg.GetProperty("market_ticker").GetString();
                        var tickerArgs = new TickerEventArgs
                        {
                            market_ticker = market_ticker,
                            market_id = Guid.Parse(tickermsg.GetProperty("market_id").GetString()),
                            yes_ask = tickermsg.GetProperty("yes_ask").GetInt32(),
                            yes_bid = tickermsg.GetProperty("yes_bid").GetInt32(),
                            price = tickermsg.GetProperty("price").GetInt32(),
                            volume = tickermsg.GetProperty("volume").GetInt32(),
                            open_interest = tickermsg.GetProperty("open_interest").GetInt32(),
                            dollar_volume = tickermsg.GetProperty("dollar_volume").GetInt32(),
                            dollar_open_interest = tickermsg.GetProperty("dollar_open_interest").GetInt32(),
                            ts = tickermsg.GetProperty("ts").GetInt64(),
                            LoggedDate = DateTime.UtcNow,
                            ProcessedDate = null
                        };

                        CheckMarketSubscription("ticker", tickermsg);

                        if (!_marketTickerEventsReceived.ContainsKey(market_ticker))
                        {
                            _marketTickerEventsReceived[market_ticker] = 1;
                        }
                        else
                        {
                            _marketTickerEventsReceived[market_ticker] = _marketTickerEventsReceived[market_ticker] + 1;
                        }

                        _logger.LogDebug("Received ticker for market: {MarketTicker}, ask={Ask}, bid={Bid}", tickerArgs.market_ticker, tickerArgs.yes_ask, tickerArgs.yes_bid);
                        TickerReceived?.Invoke(this, tickerArgs);
                        break;
                    case "trade":
                        LastTradeReceived = DateTime.UtcNow;
                        _eventCounts.AddOrUpdate("Trade", 1, (_, count) => count + 1);
                        CheckMarketSubscription("trade", data.GetProperty("msg"));
                        _logger.LogDebug("Received trade message");
                        var trademsg = data.GetProperty("msg");
                        var tradeTicker = trademsg.GetProperty("market_ticker").GetString() ?? string.Empty;

                        if (!_marketTradeEventsReceived.ContainsKey(tradeTicker))
                        {
                            _marketTradeEventsReceived[tradeTicker] = 1;
                        }
                        else
                        {
                            _marketTradeEventsReceived[tradeTicker] = _marketTradeEventsReceived[tradeTicker] + 1;
                        }

                        if (TradeReceived != null)
                            TradeReceived?.Invoke(this, new TradeEventArgs(data));

                        if (_loggingConfig.StoreWebSocketEvents) 
                            await _sqlDataService.StoreTradeAsync(data);
                        break;

                    case "fill":
                        LastFillReceived = DateTime.UtcNow;
                        _eventCounts.AddOrUpdate("Fill", 1, (_, count) => count + 1);
                        _logger.LogDebug("Received fill message");
                        if (FillReceived != null)
                            FillReceived?.Invoke(this, new FillEventArgs(data));
                        if (_loggingConfig.StoreWebSocketEvents)
                            await _sqlDataService.StoreFillAsync(data);
                        break;

                    case "market_lifecycle_v2":
                        LastLifecycleReceived = DateTime.UtcNow;
                        _eventCounts.AddOrUpdate("MarketLifecycle", 1, (_, count) => count + 1);
                        _logger.LogDebug("Received market lifecycle message");
                        if (MarketLifecycleReceived != null)
                            MarketLifecycleReceived?.Invoke(this, new MarketLifecycleEventArgs(data));
                        if (_loggingConfig.StoreWebSocketEvents)
                            await _sqlDataService.StoreMarketLifecycleAsync(data);
                        break;

                    case "event_lifecycle":
                        _eventCounts.AddOrUpdate("EventLifecycle", 1, (_, count) => count + 1);
                        _logger.LogDebug("Received event lifecycle message");
                        if (EventLifecycleReceived != null)
                            EventLifecycleReceived?.Invoke(this, new EventLifecycleEventArgs(data));
                        if (_loggingConfig.StoreWebSocketEvents)
                            await _sqlDataService.StoreEventLifecycleAsync(data);
                        break;

                    case "subscribed":
                        _eventCounts.AddOrUpdate("Subscribed", 1, (_, count) => count + 1);
                        _logger.LogInformation("SUB-Received subscription confirmation: {Message}", message);
                        if (data.TryGetProperty("msg", out var msgProp) &&
                            msgProp.TryGetProperty("channel", out var channelProp) &&
                            msgProp.TryGetProperty("sid", out var sidProp))
                        {
                            var channel = channelProp.GetString();
                            var sid = sidProp.GetInt32();
                            if (data.TryGetProperty("id", out var idProp))
                            {
                                int id = idProp.GetInt32();
                                if (_pendingConfirms.TryGetValue(id, out var confirm) &&
                                    new[] { "orderbook_delta", "ticker", "trade" }.Contains(channel))
                                {
                                    foreach (var marketTicker in confirm.MarketTickers)
                                    {
                                        UpdateSubscriptionState(marketTicker, channel, SubscriptionState.Subscribed);
                                    }
                                    _pendingConfirms.TryRemove(id, out _);
                                    _logger.LogDebug("Confirmed subscription ID={Id} for channel {Channel} via 'subscribed'", id, channel);
                                }
                            }
                            if (!string.IsNullOrEmpty(channel) && _subscriptions.ContainsKey(channel))
                            {
                                var markets = _subscriptions[channel].Markets;
                                _subscriptions[channel] = (sid, markets);
                                if (channel == "orderbook_delta")
                                {
                                    lock (_sequenceLock)
                                    {
                                        _orderBookSid = sid;
                                        _logger.LogInformation("SUB-Set orderbook_delta SID to {Sid}", sid);
                                    }
                                }
                                _logger.LogInformation("SUB-Updated SID for channel {Channel} to {Sid}, markets={Markets}", channel, sid, string.Join(", ", markets));
                            }
                            else
                            {
                                _logger.LogWarning("Received SID {Sid} for unknown or untracked channel {Channel}", sid, channel ?? "null");
                            }
                        }
                        break;

                    case "unsubscribed":
                        _eventCounts.AddOrUpdate("Unsubscribed", 1, (_, count) => count + 1);
                        _logger.LogInformation("SUB-Received unsubscription confirmation: {Message}", message);
                        if (data.TryGetProperty("sid", out var unsubSidProp))
                        {
                            int sid = unsubSidProp.GetInt32();
                            var channelEntry = _subscriptions.FirstOrDefault(kv => kv.Value.Sid == sid);
                            if (channelEntry.Key != null)
                            {
                                var channel = channelEntry.Key;
                                _logger.LogInformation("SUB-Confirmed unsubscription for channel {Channel}, SID={Sid}", channel, sid);
                                _subscriptions.Remove(channel);
                                foreach (var marketTicker in channelEntry.Value.Markets)
                                {
                                    UpdateSubscriptionState(marketTicker, channel, SubscriptionState.Unsubscribed);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Received unsubscription for unknown SID {Sid}", sid);
                            }
                        }
                        if (data.TryGetProperty("id", out var unsubIdProp))
                        {
                            int id = unsubIdProp.GetInt32();
                            _pendingConfirms.TryRemove(id, out _);
                            _logger.LogDebug("Confirmed unsubscription ID={Id}", id);
                        }
                        break;

                    case "ok":
                        _eventCounts.AddOrUpdate("Ok", 1, (_, count) => count + 1);
                        _logger.LogInformation("SUB-Received update subscription confirmation: {Message}", message);
                        if (data.TryGetProperty("id", out var okIdProp))
                        {
                            int id = okIdProp.GetInt32();
                            if (_pendingConfirms.TryGetValue(id, out var confirm) &&
                                new[] { "orderbook_delta", "ticker", "trade" }.Contains(confirm.Channel))
                            {
                                foreach (var marketTicker in confirm.MarketTickers)
                                {
                                    UpdateSubscriptionState(marketTicker, confirm.Channel, SubscriptionState.Subscribed);
                                }
                                _pendingConfirms.TryRemove(id, out _);
                                _logger.LogDebug("Confirmed subscription ID={Id} for channel {Channel} via 'ok'", id, confirm.Channel);
                            }
                        }
                        if (data.TryGetProperty("msg", out var messageContents))
                        {
                            if (data.TryGetProperty("sid", out var okSidProp) &&
                                messageContents.TryGetProperty("market_tickers", out var tickersProp))
                            {
                                var sid = okSidProp.GetInt32();
                                var confirmedTickers = tickersProp.EnumerateArray().Select(t => t.GetString()).Where(t => t != null).ToHashSet();
                                var channel = _subscriptions.FirstOrDefault(kv => kv.Value.Sid == sid).Key;
                                if (channel != null)
                                {
                                    _subscriptions[channel] = (sid, confirmedTickers);
                                    _logger.LogInformation("SUB-Confirmed subscription update for channel {Channel}, SID={Sid}, markets={Markets}", channel, sid, string.Join(", ", confirmedTickers));
                                }
                                else
                                {
                                    _logger.LogWarning("SUB-Received update confirmation for unknown SID {Sid}", sid);
                                }
                            }
                        }
                        break;

                    case "error":
                        _eventCounts.AddOrUpdate("Error", 1, (_, count) => count + 1);
                        var errorCode = data.GetProperty("msg").GetProperty("code").GetInt32();
                        var errorMsg = data.GetProperty("msg").GetProperty("msg").GetString();
                        _logger.LogDebug("WebSocket error: Code={Code}, Message={Error}", errorCode, errorMsg);
                        if (errorCode == 6 && errorMsg == "Already subscribed")
                        {
                            _logger.LogDebug("Ignoring 'Already subscribed' error, verifying subscription state");
                            if (data.TryGetProperty("id", out var errIdProp))
                            {
                                int id = errIdProp.GetInt32();
                                if (_pendingConfirms.TryGetValue(id, out var confirm))
                                {
                                    var channel = confirm.Channel;
                                    var markets = confirm.MarketTickers;
                                    if (_subscriptions.TryGetValue(channel, out var subscription))
                                    {
                                        foreach (var marketTicker in markets)
                                        {
                                            subscription.Markets.Add(marketTicker);
                                            _pendingSubscriptions.TryRemove($"{GetActionFromChannel(channel)}:{marketTicker}", out _);
                                            UpdateSubscriptionState(marketTicker, channel, SubscriptionState.Subscribed);
                                        }
                                        _subscriptions[channel] = (subscription.Sid, subscription.Markets);
                                        _logger.LogInformation("SUB-Updated subscription state for channel {Channel}: markets={Markets}", channel, string.Join(", ", subscription.Markets));
                                    }
                                    _pendingConfirms.TryRemove(id, out _);
                                    _logger.LogDebug("Removed ID={Id} from pending confirms due to 'Already subscribed' error", id);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogError("Received error from web socket {0}. Message: {1}", errorCode, errorMsg);
                        }
                        break;
                    default:
                        _eventCounts.AddOrUpdate("Unknown", 1, (_, count) => count + 1);
                        _logger.LogWarning("Received unknown message type: {MsgType}, Message: {Message}", msgType, message);
                        break;
                }

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogDebug("Processed message type {MsgType}, ProcessingTime: {ElapsedMs}ms", msgType, elapsedMs);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ProcessMessageAsync was cancelled");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("The input does not contain any JSON tokens. Raw: {0}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message");
            }
        }

        private void ProcessOrderBookMessagesAsync()
        {
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                lock (_orderBookQueueLock)
                {
                    int queueCount = _orderBookMessageQueue.Count;
                    _logger.LogDebug("Processing orderbook messages, queue count: {Count}", queueCount);
                    if (queueCount > Math.Max(_watchedMarkets.Count * .5, 25) && !_firstCalculation)
                    {
                        _logger.LogWarning("Orderbook message queue length is high: {Count}", queueCount);
                    }
                    _firstCalculation = false;

                    while (_orderBookMessageQueue.Count > 0 && _orderBookMessageQueue.TryPeek(out var item, out var seq))
                    {
                        _globalCancellationToken.ThrowIfCancellationRequested();
                        if (seq <= _lastSequenceNumber)
                        {
                            if (_orderBookMessageQueue.TryDequeue(out (JsonElement data, string offerType, long seq, Guid eventId) message, out _))
                            {
                                _logger.LogDebug("Dispatching orderbook message: seq={Seq}, eventId={EventId}", message.seq, message.eventId);
                                // Capture current prices at dequeue
                                var marketTicker = message.data.GetProperty("msg").GetProperty("market_ticker").GetString() ?? "Unknown";

                                if (OrderBookReceived != null)
                                    OrderBookReceived?.Invoke(this, new OrderBookEventArgs(message.offerType, message.data));
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Waiting for sequence {ExpectedSeq}, current head={CurrentSeq}", _lastSequenceNumber + 1, seq);
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ProcessOrderBookMessagesAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process order book messages");
            }
        }

    }


}