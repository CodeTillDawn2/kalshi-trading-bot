using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using BacklashInterfaces.Enums;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace KalshiBotAPI.Websockets
{
    public class KalshiWebSocketClient : IKalshiWebSocketClient
    {
        private readonly ISqlDataService _sqlDataService;
        private readonly IStatusTrackerService _statusTrackerService;
        private readonly IBotReadyStatus _readyStatus;
        private readonly ILogger<IKalshiWebSocketClient> _logger;
        private readonly KalshiConfig _kalshiConfig;
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IMessageProcessor _messageProcessor;
        private readonly IDataCache _dataCache;
        private bool _allowReconnect = true;

        // Channel enable/disable state - all enabled by default
        private readonly ConcurrentDictionary<string, bool> _enabledChannels = new();

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
        public ConcurrentDictionary<string, long> EventCounts => _subscriptionManager.EventCounts;
        public int ConnectSemaphoreCount => _connectionManager.ConnectSemaphoreCount;
        public int SubscriptionUpdateSemaphoreCount => _subscriptionManager.SubscriptionUpdateSemaphoreCount;
        public int ChannelSubscriptionSemaphoreCount => _subscriptionManager.ChannelSubscriptionSemaphoreCount;
        public int QueuedSubscriptionUpdatesCount => _subscriptionManager.QueuedSubscriptionUpdatesCount;
        public int OrderBookMessageQueueCount => _messageProcessor.OrderBookMessageQueueCount;
        public int PendingConfirmsCount => _messageProcessor.PendingConfirmsCount;

        public bool WriteToSQL { get; private set; }

        public KalshiWebSocketClient(
            IOptions<KalshiConfig> kalshiConfig,
            ILogger<IKalshiWebSocketClient> logger,
            IStatusTrackerService statusTrackerService,
            IBotReadyStatus readyStatus,
            ISqlDataService sqlDataService,
            IWebSocketConnectionManager connectionManager,
            ISubscriptionManager subscriptionManager,
            IMessageProcessor messageProcessor,
            IDataCache dataCache,
            bool writeToSql)
        {
            _kalshiConfig = kalshiConfig.Value;
            _logger = logger;
            _statusTrackerService = statusTrackerService;
            _readyStatus = readyStatus;
            _sqlDataService = sqlDataService;
            _connectionManager = connectionManager;
            _subscriptionManager = subscriptionManager;
            _messageProcessor = messageProcessor;
            // Note: MessageProcessor is injected, but we need to ensure it has the correct WriteToSQL setting
            // This might need to be passed during construction or set via a method
            _dataCache = dataCache;
            WriteToSQL = writeToSql;

            // Wire up events
            _messageProcessor.OrderBookReceived += (sender, args) => OrderBookReceived?.Invoke(sender, args);
            _messageProcessor.TickerReceived += (sender, args) => TickerReceived?.Invoke(sender, args);
            _messageProcessor.TradeReceived += (sender, args) => TradeReceived?.Invoke(sender, args);
            _messageProcessor.FillReceived += (sender, args) => FillReceived?.Invoke(sender, args);
            _messageProcessor.MarketLifecycleReceived += (sender, args) => MarketLifecycleReceived?.Invoke(sender, args);
            _messageProcessor.EventLifecycleReceived += (sender, args) => EventLifecycleReceived?.Invoke(sender, args);
            _messageProcessor.MessageReceived += (sender, timestamp) => MessageReceived?.Invoke(sender, timestamp);

            // Set WriteToSQL in MessageProcessor
            _messageProcessor.SetWriteToSql(WriteToSQL);

            // Initialize all channels as enabled by default
            InitializeChannelStates();
        }

        public long LastSequenceNumber => _messageProcessor.LastSequenceNumber;



        public async Task StopServicesAsync()
        {
            _logger.LogDebug("KalshiWebSocketClient.StopServicesAsync called at {0}, CancellationToken.IsCancellationRequested={IsRequested}"
                , DateTime.UtcNow, _globalCancellationToken.IsCancellationRequested);
            _allowReconnect = false;
            try
            {
                await _subscriptionManager.UnsubscribeFromAllAsync();
                await _connectionManager.StopAsync();
                await _messageProcessor.StopProcessingAsync();
                _logger.LogDebug("All components stopped successfully");
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
            await _subscriptionManager.UnsubscribeFromChannelAsync(action);
        }

        public HashSet<string> WatchedMarkets
        {
            get => _subscriptionManager.WatchedMarkets;
            set => _subscriptionManager.WatchedMarkets = value;
        }


        public bool IsSubscribed(string marketTicker, string action) => _subscriptionManager.IsSubscribed(marketTicker, action);

        public bool CanSubscribeToMarket(string marketTicker, string channel) => _subscriptionManager.CanSubscribeToMarket(marketTicker, channel);

        public void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state) => _subscriptionManager.SetSubscriptionState(marketTicker, channel, state);

        public async Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction)
        {
            await _subscriptionManager.UpdateSubscriptionAsync(action, marketTickers, channelAction);
        }

        public void ResetEventCounts()
        {
            _messageProcessor.ResetEventCounts();
        }

        public void ClearOrderBookQueue(string marketTicker)
        {
            _messageProcessor.ClearOrderBookQueue(marketTicker);
        }


        public async Task ConnectAsync(int retryCount = 0)
        {
            // Check if already connected to prevent duplicate connections
            if (_connectionManager.IsConnected())
            {
                _logger.LogDebug("WebSocket already connected, skipping duplicate connection attempt");
                return;
            }

            await _connectionManager.ConnectAsync(retryCount);
            if (_connectionManager.IsConnected())
            {
                // Subscribe to enabled non-market-specific channels
                var enabledChannels = new[] { "fill", "lifecycle" }.Where(IsChannelEnabled);
                foreach (var channel in enabledChannels)
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    await _subscriptionManager.SubscribeToChannelAsync(channel, Array.Empty<string>());
                    _logger.LogDebug("Subscribed to enabled channel {Channel}", channel);
                }

                // Note: Market-specific channels will be subscribed individually as each market completes initialization
                // This allows for per-market subscription timing rather than bulk subscription


                await _messageProcessor.StartProcessingAsync();
                await _subscriptionManager.StartAsync();
                await StartReceivingAsync();
            }
        }

        public (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker)
        {
            return _messageProcessor.GetEventCountsByMarket(marketTicker);
        }

        public async Task SubscribeToWatchedMarketsAsync()
        {
            if (!_connectionManager.IsConnected())
            {
                _logger.LogWarning("WebSocket not connected, cannot subscribe to watched markets");
                return;
            }

            if (!_subscriptionManager.WatchedMarkets.Any())
            {
                _logger.LogDebug("No watched markets to subscribe to");
                return;
            }

            _logger.LogInformation("Subscribing to watched markets: {Markets}", string.Join(", ", _subscriptionManager.WatchedMarkets));

            // Only subscribe to enabled channels
            var enabledChannels = new[] { "orderbook", "ticker", "trade" }.Where(IsChannelEnabled);
            foreach (var action in enabledChannels)
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                var marketsToSubscribe = _subscriptionManager.WatchedMarkets
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
            await _messageProcessor.WaitForEmptyOrderBookQueueAsync(marketTicker, timeout);
        }

        public async Task ResetConnectionAsync()
        {
            await _connectionManager.ResetConnectionAsync();
        }

        public bool IsConnected() => _connectionManager.IsConnected();

        public async Task ResubscribeAsync(bool force = false)
        {
            await _subscriptionManager.ResubscribeAsync(force);
        }

        public string GetChannelName(string action) => _subscriptionManager.GetChannelName(action);

        public async Task SendMessageAsync(string message)
        {
            await _connectionManager.SendMessageAsync(message);
        }

        public async Task UnsubscribeFromAllAsync()
        {
            await _subscriptionManager.UnsubscribeFromAllAsync();
        }










        public async Task SubscribeToChannelAsync(string action, string[] marketTickers)
        {
            await _subscriptionManager.SubscribeToChannelAsync(action, marketTickers);
        }

        public int GenerateNextMessageId()
        {
            return _subscriptionManager.GenerateNextMessageId();
        }

        /// <summary>
        /// Enable a specific WebSocket channel
        /// </summary>
        public void EnableChannel(string channel)
        {
            _enabledChannels[channel] = true;
            _logger.LogInformation("Enabled WebSocket channel: {Channel}", channel);
        }

        /// <summary>
        /// Disable a specific WebSocket channel
        /// </summary>
        public void DisableChannel(string channel)
        {
            _enabledChannels[channel] = false;
            _logger.LogInformation("Disabled WebSocket channel: {Channel}", channel);
        }

        /// <summary>
        /// Check if a channel is enabled
        /// </summary>
        public bool IsChannelEnabled(string channel)
        {
            return _enabledChannels.GetOrAdd(channel, true); // Default to true if not set
        }

        /// <summary>
        /// Get all enabled channels
        /// </summary>
        public IEnumerable<string> GetEnabledChannels()
        {
            return _enabledChannels.Where(kv => kv.Value).Select(kv => kv.Key);
        }

        /// <summary>
        /// Get all disabled channels
        /// </summary>
        public IEnumerable<string> GetDisabledChannels()
        {
            return _enabledChannels.Where(kv => !kv.Value).Select(kv => kv.Key);
        }

        /// <summary>
        /// Enable all channels
        /// </summary>
        public void EnableAllChannels()
        {
            foreach (var channel in _enabledChannels.Keys)
            {
                _enabledChannels[channel] = true;
            }
            _logger.LogInformation("Enabled all WebSocket channels");
        }

        /// <summary>
        /// Disable all channels
        /// </summary>
        public void DisableAllChannels()
        {
            foreach (var channel in _enabledChannels.Keys)
            {
                _enabledChannels[channel] = false;
            }
            _logger.LogInformation("Disabled all WebSocket channels");
        }

        /// <summary>
        /// Get current channel states for debugging
        /// </summary>
        public Dictionary<string, bool> GetChannelStates()
        {
            return _enabledChannels.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private void InitializeChannelStates()
        {
            // Initialize channels with appropriate default states
            // Non-market-specific channels that should be enabled by default
            _enabledChannels["fill"] = true;
            _enabledChannels["lifecycle"] = true;
            _enabledChannels["event_lifecycle"] = true;

            // Market-specific channels that should be enabled by default
            _enabledChannels["orderbook"] = true;
            _enabledChannels["ticker"] = true;
            _enabledChannels["trade"] = true;
        }

        public async Task StartReceivingAsync()
{
    _logger.LogDebug("Starting WebSocket message receiving");
    try
    {
        // Start the receiving loop in a background task
        _ = Task.Run(async () =>
        {
            var buffer = new byte[1024 * 16];
            var messageBuilder = new StringBuilder();

            while (!_globalCancellationToken.IsCancellationRequested)
            {
                try
                {
                    var webSocket = _connectionManager.GetWebSocket();
                    if (webSocket == null || webSocket.State != WebSocketState.Open)
                    {
                        await Task.Delay(1000, _globalCancellationToken);
                        continue;
                    }

                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogError("WebSocket closed by server: Code={Code}, Reason={Reason}", result.CloseStatus, result.CloseStatusDescription);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messagePart = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(messagePart);

                        if (result.EndOfMessage)
                        {
                            var fullMessage = messageBuilder.ToString();
                            _logger.LogDebug("Received complete WebSocket message: Length={Length}", fullMessage.Length);
                            await _messageProcessor.ProcessMessageAsync(fullMessage);
                            messageBuilder.Clear();
                        }
                    }
                    else
                    {
                        _logger.LogError("Unexpected WebSocket message type: {MessageType}", result.MessageType);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("WebSocket receiving cancelled");
                    break;
                }
                catch (WebSocketException ex) when (ex.Message.Contains("without completing the close handshake"))
                {
                    _logger.LogWarning("WebSocket connection lost, will attempt to reconnect");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in WebSocket receiving loop");
                    await Task.Delay(5000, _globalCancellationToken);
                }
            }
        }, _globalCancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error starting WebSocket message receiving");
    }
}


}


}
