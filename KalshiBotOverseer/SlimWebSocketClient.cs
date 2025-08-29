using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using SmokehouseInterfaces.Constants;
using SmokehouseInterfaces.Enums;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KalshiBotOverseer.Interfaces;

namespace KalshiBotOverseer
{
    public class SlimWebSocketClient : ISlimWebSocketClient
    {
        private readonly IStatusTrackerService _statusTrackerService;
        private readonly ILogger<IKalshiWebSocketClient> _logger;
        private readonly KalshiConfig _kalshiConfig;
        private readonly LoggingConfig _loggingConfig;
        private readonly RSA _privateKey;
        private ClientWebSocket? _webSocket = null!;
        private int _messageId = 1;
        private readonly object _webSocketLock = new object();
        private DateTime _lastMessageReceived = DateTime.UtcNow;
        private readonly Dictionary<string, int> _subscriptions = new();
        private readonly ConcurrentDictionary<int, (DateTime SentTime, string Message, string Channel)> _pendingConfirms = new();
        private readonly SemaphoreSlim _connectSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _channelSubscriptionSemaphore = new SemaphoreSlim(1, 1);
        private bool _isConnected = false;
        private const int SubscriptionConfirmTimeoutSeconds = 20;
        private Task _confirmCheckTask = null!;
        private bool _allowReconnect = true;
        private Task _receiveTask = null!;
        private bool _isReconnecting = false;

        private CancellationToken _globalCancellationToken => _statusTrackerService.GetCancellationToken();

        public event EventHandler<FillEventArgs>? FillReceived;
        public event EventHandler<MarketLifecycleEventArgs>? MarketLifecycleReceived;
        public event EventHandler<EventLifecycleEventArgs>? EventLifecycleReceived;
        public event EventHandler<DateTime>? MessageReceived;

        public bool IsTradingActive { get; set; } = true;

        // Public properties for monitoring
        public int ConnectSemaphoreCount => _connectSemaphore.CurrentCount;
        public int ChannelSubscriptionSemaphoreCount => _channelSubscriptionSemaphore.CurrentCount;
        public int PendingConfirmsCount => _pendingConfirms.Count;

        // Public properties for last event received times
        public DateTime LastFillReceived { get; private set; } = DateTime.MinValue;
        public DateTime LastLifecycleReceived { get; private set; } = DateTime.MinValue;

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
        }

        public async Task StopServicesAsync()
        {
            _logger.LogDebug("KalshiWebSocketClient.StopServicesAsync called at {0}, CancellationToken.IsCancellationRequested={IsRequested}",
                DateTime.UtcNow, _globalCancellationToken.IsCancellationRequested);
            _allowReconnect = false;
            try
            {
                _logger.LogInformation("Unsubscribing from all channels...");
                await UnsubscribeFromAllAsync();
                _logger.LogInformation("Unsubscribed from all channels");

                await WaitForPendingUnsubscriptionConfirmsAsync();
                _logger.LogDebug("Completed waiting for unsubscription confirmations");

                _subscriptions.Clear();
                _pendingConfirms.Clear();
                _logger.LogDebug("Cleared all subscriptions and pending confirms");

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
                if (!_subscriptions.TryGetValue(channel, out var sid))
                {
                    _logger.LogWarning("No active subscription for {Channel}, skipping unsubscription", channel);
                    return;
                }

                if (sid == 0)
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
                        sids = new[] { sid }
                    }
                };

                var message = JsonSerializer.Serialize(unsubscribeCommand);
                _logger.LogInformation("Sending unsubscription request for {Channel}, ID={Id}, SID={Sid}, message={Message}", channel, subscriptionId, sid, message);
                _pendingConfirms.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel));
                await SendMessageAsync(message);

                _subscriptions.Remove(channel);
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
                if (semaphoreAcquired)
                {
                    _logger.LogDebug("Released channel subscription semaphore for action {Action}", action);
                    _channelSubscriptionSemaphore.Release();
                }
            }
        }

        public int GetNextMessageId() => Interlocked.Increment(ref _messageId);

        public bool IsSubscribed(string action)
        {
            var channel = GetChannelName(action);
            bool isSubscribed = _subscriptions.ContainsKey(channel);
            _logger.LogDebug("Checked subscription for {Channel}: {IsSubscribed}", channel, isSubscribed);
            return isSubscribed;
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

                // Subscribe to fill and lifecycle channels
                foreach (var action in new[] { "fill", "lifecycle" })
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    await SubscribeToChannelAsync(action);
                    _logger.LogDebug("Subscribed to channel {Action}", action);
                }

                _receiveTask = Task.Run(() => ReceiveAsync());

                if (_confirmCheckTask == null || _confirmCheckTask.IsCompleted)
                {
                    _confirmCheckTask = Task.Run(() => CheckPendingConfirmsAsync(), _globalCancellationToken);
                    _logger.LogDebug("Started confirmation check task");
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

                if (oldSocket != null)
                {
                    await oldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Resetting socket", _globalCancellationToken);
                    oldSocket.Dispose();
                    _logger.LogDebug("Closed and disposed old WebSocket");
                }

                if (_allowReconnect)
                {
                    _logger.LogInformation("Waiting 5 seconds before reconnect");
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

        public async Task SubscribeToChannelAsync(string action)
        {
            _logger.LogInformation("Subscribing to channel: action={Action}", action);
            if (!IsConnected())
            {
                _logger.LogWarning("WebSocket not connected, cannot subscribe to {Action}", action);
                return;
            }

            bool semaphoreAcquired = false;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Acquiring channel subscription semaphore: action={Action}", action);
                semaphoreAcquired = await _channelSubscriptionSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire channel subscription semaphore for action {Action} within 60000ms", action);
                    return;
                }
                _logger.LogDebug("Acquired channel subscription semaphore for action {Action}", action);

                var channel = GetChannelName(action);
                if (_subscriptions.ContainsKey(channel))
                {
                    _logger.LogDebug("Already subscribed to {Channel}", channel);
                    return;
                }

                var subscriptionId = Interlocked.Increment(ref _messageId);
                var subscribeCommand = new
                {
                    id = subscriptionId,
                    cmd = "subscribe",
                    @params = new
                    {
                        channels = new[] { channel }
                    }
                };
                var message = JsonSerializer.Serialize(subscribeCommand);
                _logger.LogDebug("Sending subscribe for {Channel}, ID={Id}", channel, subscriptionId);

                _pendingConfirms.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel));
                await SendMessageAsync(message);

                _subscriptions[channel] = 0; // SID will be updated on confirmation
                _logger.LogInformation("Updated subscription for {Channel}", channel);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("SubscribeToChannelAsync was cancelled for action {Action}", action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to {Action}", action);
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

        public string GetChannelName(string action) => action switch
        {
            "fill" => "fill",
            "lifecycle" => "market_lifecycle_v2",
            _ => throw new ArgumentException($"Invalid action: {action}")
        };

        public async Task SendMessageAsync(string message)
        {
            _logger.LogInformation("Sending WebSocket message: {Message}", message);
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
                semaphoreAcquired = await _channelSubscriptionSemaphore.WaitAsync(60000, _globalCancellationToken);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire channel subscription semaphore for unsubscribe within 60000ms");
                    return;
                }
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
                    var sid = subscription.Value;
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
                        _logger.LogInformation("Sending unsubscribe command: channel={Channel}, SID={Sid}, ID={Id}",
                            channel, sid, subscriptionId);
                        _pendingConfirms.TryAdd(subscriptionId, (DateTime.UtcNow, message, channel));
                        await SendMessageAsync(message);
                        _logger.LogDebug("Marked local subscription as unsubscribed for channel {Channel}", channel);
                    }
                    else
                    {
                        _logger.LogWarning("No SID for channel {Channel}, skipping unsubscribe command", channel);
                    }
                    _subscriptions.Remove(channel);
                }
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
                    _logger.LogDebug("Released channel subscription semaphore for unsubscription");
                    _channelSubscriptionSemaphore.Release();
                }
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
                            _logger.LogWarning("Subscription command ID={Id} for channel '{Channel}' timed out after {Timeout}s, retry {RetryCount}/{MaxRetries}",
                                key, confirm.Channel, SubscriptionConfirmTimeoutSeconds, retryCount, maxRetries);

                            if (retryCount < maxRetries)
                            {
                                bool isInitialSubscription = confirm.Message.Contains("\"cmd\": \"subscribe\"");
                                _logger.LogInformation("Retrying subscription ID={Id} for channel {Channel}, initial = {isInitialSubscription}",
                                    key, confirm.Channel, isInitialSubscription);
                                if (isInitialSubscription)
                                {
                                    await SubscribeToChannelAsync(GetActionFromChannel(confirm.Channel));
                                }
                                retryCounts[key] = retryCount + 1;
                                if (_pendingConfirms.TryGetValue(key, out var currentValue))
                                {
                                    var updatedValue = (DateTime.UtcNow, currentValue.Message, currentValue.Channel);
                                    _pendingConfirms.TryUpdate(key, updatedValue, currentValue);
                                }
                            }
                            else
                            {
                                _logger.LogError("Subscription ID={Id} for channel {Channel} failed after {MaxRetries} retries, marking as failed",
                                    key, confirm.Channel, maxRetries);
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
                await Task.Delay(1000, _globalCancellationToken);
            }
            _logger.LogDebug("Stopped subscription confirmation check");
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
                _logger.LogDebug("ReceiveAsync completed at {0}, CancellationToken.IsCancellationRequested={IsRequested}",
                    DateTime.UtcNow, _globalCancellationToken.IsCancellationRequested);
            }
        }

        private async Task ProcessMessageAsync(string message)
        {
            _logger.LogDebug("Processing WebSocket message: {Message}", message);
            var startTime = DateTime.UtcNow;
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                var data = JsonSerializer.Deserialize<JsonElement>(message);
                var msgType = data.GetProperty("type").GetString() ?? "unknown";

                if (MessageReceived != null)
                    MessageReceived?.Invoke(this, DateTime.UtcNow);

                switch (msgType)
                {
                    case "fill":
                        LastFillReceived = DateTime.UtcNow;
                        _logger.LogDebug("Received fill message");
                        if (FillReceived != null)
                            FillReceived?.Invoke(this, new FillEventArgs(data));
                        break;

                    case "market_lifecycle_v2":
                        LastLifecycleReceived = DateTime.UtcNow;
                        _logger.LogDebug("Received market lifecycle message");
                        if (MarketLifecycleReceived != null)
                            MarketLifecycleReceived?.Invoke(this, new MarketLifecycleEventArgs(data));
                        break;

                    case "event_lifecycle":
                        _logger.LogDebug("Received event lifecycle message");
                        if (EventLifecycleReceived != null)
                            EventLifecycleReceived?.Invoke(this, new EventLifecycleEventArgs(data));
                        break;
                    case "subscribed":
                        _logger.LogInformation("Received subscription confirmation: {Message}", message);
                        if (data.TryGetProperty("msg", out var msgProp) &&
                            msgProp.TryGetProperty("channel", out var channelProp) &&
                            msgProp.TryGetProperty("sid", out var sidProp))
                        {
                            var channel = channelProp.GetString();
                            var sid = sidProp.GetInt32();
                            if (data.TryGetProperty("id", out var idProp))
                            {
                                int id = idProp.GetInt32();
                                if (_pendingConfirms.TryRemove(id, out _))
                                {
                                    _logger.LogDebug("Confirmed subscription ID={Id} for channel {Channel} via 'subscribed'", id, channel);
                                }
                            }
                            if (!string.IsNullOrEmpty(channel) && _subscriptions.ContainsKey(channel))
                            {
                                _subscriptions[channel] = sid;
                                _logger.LogInformation("Updated SID for channel {Channel} to {Sid}", channel, sid);
                            }
                            else
                            {
                                _logger.LogWarning("Received SID {Sid} for unknown or untracked channel {Channel}", sid, channel ?? "null");
                            }
                        }
                        break;

                    case "unsubscribed":
                        _logger.LogInformation("Received unsubscription confirmation: {Message}", message);
                        if (data.TryGetProperty("sid", out var unsubSidProp))
                        {
                            int sid = unsubSidProp.GetInt32();
                            var channelEntry = _subscriptions.FirstOrDefault(kv => kv.Value == sid).Key;
                            if (channelEntry != null)
                            {
                                _logger.LogInformation("Confirmed unsubscription for channel {Channel}, SID={Sid}", channelEntry, sid);
                                _subscriptions.Remove(channelEntry);
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
                        _logger.LogInformation("Received ok confirmation: {Message}", message);
                        if (data.TryGetProperty("id", out var okIdProp))
                        {
                            int id = okIdProp.GetInt32();
                            if (_pendingConfirms.TryRemove(id, out _))
                            {
                                _logger.LogDebug("Confirmed ID={Id} via 'ok'", id);
                            }
                        }
                        break;

                    case "error":
                        var errorCode = data.GetProperty("msg").GetProperty("code").GetInt32();
                        var errorMsg = data.GetProperty("msg").GetProperty("msg").GetString();
                        _logger.LogError("Received error from web socket {Code}. Message: {Error}", errorCode, errorMsg);
                        break;

                    default:
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
    }
}