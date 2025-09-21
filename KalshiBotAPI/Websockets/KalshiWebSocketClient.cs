using BacklashBot.Services.Interfaces;
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
using System.Net.WebSockets;
using System.Text;

namespace KalshiBotAPI.Websockets
{
    /// <summary>
    /// Main WebSocket client for connecting to Kalshi's trading platform.
    /// Manages the complete lifecycle of WebSocket connections, subscriptions to market data channels,
    /// and real-time message processing. Acts as the central orchestrator between connection management,
    /// subscription handling, and message processing components.
    /// </summary>
    /// <remarks>
    /// This class implements the IKalshiWebSocketClient interface and coordinates with:
    /// - IWebSocketConnectionManager: Handles low-level WebSocket connection and reconnection
    /// - ISubscriptionManager: Manages channel subscriptions and subscription states
    /// - IMessageProcessor: Processes incoming WebSocket messages and raises events
    /// - IDataCache: Provides access to watched markets and other cached data
    /// - ISqlDataService: Handles database operations for market data persistence
    /// - IStatusTrackerService: Provides cancellation tokens and status tracking
    /// - IBotReadyStatus: Tracks bot readiness state
    /// </remarks>
    public class KalshiWebSocketClient : IKalshiWebSocketClient, IWebSocketPerformanceMetrics
    {
        private readonly ISqlDataService _sqlDataService;
        private readonly IStatusTrackerService _statusTrackerService;
        private readonly IBotReadyStatus _readyStatus;
        private readonly ILogger<IKalshiWebSocketClient> _logger;
        private readonly KalshiConfig _kalshiConfig;
        private readonly KalshiWebSocketClientConfig _websocketConfig;
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IMessageProcessor _messageProcessor;
        private readonly IDataCache _dataCache;
        private readonly IWebSocketPerformanceMetrics? _performanceMetrics;
        private bool _allowReconnect = true;

        /// <summary>
        /// Configurable WebSocket buffer size in bytes. Default is 16KB.
        /// </summary>
        private readonly int _webSocketBufferSize;

        // Channel enable/disable state - all enabled by default
        private readonly ConcurrentDictionary<string, bool> _enabledChannels = new();

        // Performance metrics enhancements
        private readonly ConcurrentDictionary<string, long> _messageProcessingTimeTicks = new();
        private readonly ConcurrentDictionary<string, int> _messageProcessingCount = new();
        private readonly ConcurrentDictionary<string, long> _bufferUsageBytes = new();
        private readonly ConcurrentDictionary<string, TimeSpan> _asyncOperationTimes = new();
        private readonly ConcurrentDictionary<string, int> _semaphoreWaitCount = new();

        /// <summary>
        /// Enables or disables performance metrics collection for this WebSocket client.
        /// When disabled, metrics collection is skipped to improve performance.
        /// Can be configured via appsettings.json: KalshiWebSocketClient:EnablePerformanceMetrics
        /// or set at runtime.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;

        private CancellationToken _globalCancellationToken => _statusTrackerService.GetCancellationToken();

        /// <summary>
        /// Event raised when order book data is received from the WebSocket.
        /// </summary>
        public event EventHandler<OrderBookEventArgs>? OrderBookReceived;

        /// <summary>
        /// Event raised when ticker data is received from the WebSocket.
        /// </summary>
        public event EventHandler<TickerEventArgs>? TickerReceived;

        /// <summary>
        /// Event raised when trade data is received from the WebSocket.
        /// </summary>
        public event EventHandler<TradeEventArgs>? TradeReceived;

        /// <summary>
        /// Event raised when fill data is received from the WebSocket.
        /// </summary>
        public event EventHandler<FillEventArgs>? FillReceived;

        /// <summary>
        /// Event raised when market lifecycle events are received from the WebSocket.
        /// </summary>
        public event EventHandler<MarketLifecycleEventArgs>? MarketLifecycleReceived;

        /// <summary>
        /// Event raised when event lifecycle events are received from the WebSocket.
        /// </summary>
        public event EventHandler<EventLifecycleEventArgs>? EventLifecycleReceived;

        /// <summary>
        /// Event raised when any WebSocket message is received, providing the timestamp.
        /// </summary>
        public event EventHandler<DateTime>? MessageReceived;

        /// <summary>
        /// Gets or sets whether trading operations are currently active.
        /// When false, the client may still receive data but trading actions are disabled.
        /// </summary>
        public bool IsTradingActive { get; set; } = true;

        /// <summary>
        /// Gets the current event counts for different message types processed by the subscription manager.
        /// </summary>
        public ConcurrentDictionary<string, long> EventCounts => _subscriptionManager.EventCounts;

        /// <summary>
        /// Gets the current count of the connection semaphore, indicating connection operation status.
        /// </summary>
        public int ConnectSemaphoreCount => _connectionManager.ConnectSemaphoreCount;

        /// <summary>
        /// Gets the current count of the subscription update semaphore.
        /// </summary>
        public int SubscriptionUpdateSemaphoreCount => _subscriptionManager.SubscriptionUpdateSemaphoreCount;

        /// <summary>
        /// Gets the current count of the channel subscription semaphore.
        /// </summary>
        public int ChannelSubscriptionSemaphoreCount => _subscriptionManager.ChannelSubscriptionSemaphoreCount;

        /// <summary>
        /// Gets the count of queued subscription updates waiting to be processed.
        /// </summary>
        public int QueuedSubscriptionUpdatesCount => _subscriptionManager.QueuedSubscriptionUpdatesCount;

        /// <summary>
        /// Gets the count of order book messages currently in the processing queue.
        /// </summary>
        public int OrderBookMessageQueueCount => _messageProcessor.OrderBookMessageQueueCount;

        /// <summary>
        /// Gets the count of pending subscription confirmations.
        /// </summary>
        public int PendingConfirmsCount => _messageProcessor.PendingConfirmsCount;

        /// <summary>
        /// Gets whether market data should be written to SQL database.
        /// </summary>
        public bool WriteToSQL { get; private set; }

        /// <summary>
        /// Gets the average processing time per message type in milliseconds.
        /// </summary>
        public ConcurrentDictionary<string, double> AverageProcessingTimesMs => new(
            _messageProcessingTimeTicks.ToDictionary(
                kv => kv.Key,
                kv => _messageProcessingCount.TryGetValue(kv.Key, out var count) && count > 0
                    ? TimeSpan.FromTicks(kv.Value / count).TotalMilliseconds
                    : 0.0
            )
        );

        /// <summary>
        /// Gets the total bytes received per message type.
        /// </summary>
        public ConcurrentDictionary<string, long> BufferUsageBytes => new(_bufferUsageBytes);

        /// <summary>
        /// Gets the average time for async operations in milliseconds.
        /// </summary>
        public ConcurrentDictionary<string, double> AsyncOperationTimesMs => new(
            _asyncOperationTimes.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.TotalMilliseconds
            )
        );

        /// <summary>
        /// Gets the count of semaphore waits per operation type.
        /// </summary>
        public ConcurrentDictionary<string, int> SemaphoreWaitCounts => new(_semaphoreWaitCount);

        /// <summary>
        /// Initializes a new instance of the KalshiWebSocketClient class.
        /// </summary>
        /// <param name="kalshiConfig">Configuration options for Kalshi core settings.</param>
        /// <param name="websocketConfig">Configuration options for WebSocket operations.</param>
        /// <param name="logger">Logger instance for recording operations and errors.</param>
        /// <param name="statusTrackerService">Service for tracking bot status and cancellation tokens.</param>
        /// <param name="readyStatus">Service for tracking bot readiness state.</param>
        /// <param name="sqlDataService">Service for SQL database operations.</param>
        /// <param name="connectionManager">Manager for WebSocket connection lifecycle.</param>
        /// <param name="subscriptionManager">Manager for channel subscriptions and states.</param>
        /// <param name="messageProcessor">Processor for incoming WebSocket messages.</param>
        /// <param name="performanceMetrics">Optional performance metrics service for recording WebSocket metrics.</param>
        /// <param name="writeToSql">Whether to write market data to SQL database.</param>
        /// <param name="webSocketBufferSize">Size of the WebSocket buffer in bytes. Default is 16KB.</param>
        /// <param name="enablePerformanceMetrics">Whether to enable performance metrics collection. Default is true.</param>
        public KalshiWebSocketClient(
            IOptions<KalshiConfig> kalshiConfig,
            IOptions<KalshiWebSocketClientConfig> websocketConfig,
            ILogger<IKalshiWebSocketClient> logger,
            IStatusTrackerService statusTrackerService,
            IBotReadyStatus readyStatus,
            ISqlDataService sqlDataService,
            IWebSocketConnectionManager connectionManager,
            ISubscriptionManager subscriptionManager,
            IMessageProcessor messageProcessor,
            IWebSocketPerformanceMetrics? performanceMetrics = null,
            bool writeToSql = false,
            int webSocketBufferSize = 16384,
            bool? enablePerformanceMetrics = null)
        {
            _kalshiConfig = kalshiConfig.Value;
            _websocketConfig = websocketConfig.Value;
            _logger = logger;
            _statusTrackerService = statusTrackerService;
            _readyStatus = readyStatus;
            _sqlDataService = sqlDataService;
            _connectionManager = connectionManager;
            _subscriptionManager = subscriptionManager;
            _messageProcessor = messageProcessor;
            _performanceMetrics = performanceMetrics;
            WriteToSQL = writeToSql;
            _webSocketBufferSize = webSocketBufferSize;
            EnablePerformanceMetrics = enablePerformanceMetrics ?? _websocketConfig.EnablePerformanceMetrics;

            // Wire up events from MessageProcessor to expose them publicly
            _messageProcessor.OrderBookReceived += (sender, args) => OrderBookReceived?.Invoke(sender, args);
            _messageProcessor.TickerReceived += (sender, args) => TickerReceived?.Invoke(sender, args);
            _messageProcessor.TradeReceived += (sender, args) => TradeReceived?.Invoke(sender, args);
            _messageProcessor.FillReceived += (sender, args) => FillReceived?.Invoke(sender, args);
            _messageProcessor.MarketLifecycleReceived += (sender, args) => MarketLifecycleReceived?.Invoke(sender, args);
            _messageProcessor.EventLifecycleReceived += (sender, args) => EventLifecycleReceived?.Invoke(sender, args);
            _messageProcessor.MessageReceived += (sender, timestamp) => MessageReceived?.Invoke(sender, timestamp);

            // Configure MessageProcessor with SQL writing setting
            _messageProcessor.SetWriteToSql(WriteToSQL);

            // Initialize all channels as enabled by default
            InitializeChannelStates();
        }

        /// <summary>
        /// Gets the last sequence number processed from WebSocket messages.
        /// Used for tracking message ordering and detecting missed messages.
        /// </summary>
        public long LastSequenceNumber => _messageProcessor.LastSequenceNumber;

        /// <summary>
        /// Shuts down the WebSocket client and all associated services gracefully.
        /// This includes unsubscribing from all channels, stopping message processing,
        /// and closing the WebSocket connection.
        /// </summary>
        /// <returns>A task representing the asynchronous shutdown operation.</returns>
        public async Task ShutdownAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Shutting down KalshiWebSocketClient at {Timestamp}, CancellationToken.IsCancellationRequested={IsRequested}",
                DateTime.UtcNow, _globalCancellationToken.IsCancellationRequested);
            _allowReconnect = false;
            try
            {
                await _subscriptionManager.UnsubscribeFromAllAsync();
                await _connectionManager.StopAsync();
                await _messageProcessor.StopProcessingAsync();
                _logger.LogInformation("All WebSocket components stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down KalshiWebSocketClient");
            }
            finally
            {
                stopwatch.Stop();
                if (EnablePerformanceMetrics)
                {
                    _asyncOperationTimes["Shutdown"] = stopwatch.Elapsed;
                    _performanceMetrics?.RecordWebSocketOperation("Shutdown", stopwatch.Elapsed);
                }
                _logger.LogDebug("KalshiWebSocketClient.ShutdownAsync completed at {Timestamp}", DateTime.UtcNow);
            }
        }


        /// <summary>
        /// Unsubscribes from a specific WebSocket channel.
        /// </summary>
        /// <param name="action">The channel action to unsubscribe from (e.g., "orderbook", "ticker").</param>
        /// <returns>A task representing the asynchronous unsubscription operation.</returns>
        public async Task UnsubscribeFromChannelAsync(string action)
        {
            await _subscriptionManager.UnsubscribeFromChannelAsync(action);
        }

        /// <summary>
        /// Gets or sets the collection of market tickers that are being watched.
        /// Markets in this collection will receive WebSocket subscriptions for enabled channels.
        /// </summary>
        public HashSet<string> WatchedMarkets
        {
            get => _subscriptionManager.WatchedMarkets;
            set => _subscriptionManager.WatchedMarkets = value;
        }


        /// <summary>
        /// Determines whether a market is currently subscribed to a specific channel.
        /// </summary>
        /// <param name="marketTicker">The market ticker to check.</param>
        /// <param name="action">The channel action to check (e.g., "orderbook", "ticker").</param>
        /// <returns>True if the market is subscribed to the channel, false otherwise.</returns>
        public bool IsSubscribed(string marketTicker, string action) => _subscriptionManager.IsSubscribed(marketTicker, action);

        /// <summary>
        /// Determines whether a market can be subscribed to a specific channel.
        /// </summary>
        /// <param name="marketTicker">The market ticker to check.</param>
        /// <param name="channel">The channel name to check.</param>
        /// <returns>True if the market can be subscribed to the channel, false otherwise.</returns>
        public bool CanSubscribeToMarket(string marketTicker, string channel) => _subscriptionManager.CanSubscribeToMarket(marketTicker, channel);

        /// <summary>
        /// Sets the subscription state for a market and channel combination.
        /// </summary>
        /// <param name="marketTicker">The market ticker.</param>
        /// <param name="channel">The channel name.</param>
        /// <param name="state">The new subscription state.</param>
        public void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state) => _subscriptionManager.SetSubscriptionState(marketTicker, channel, state);

        public async Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _subscriptionManager.UpdateSubscriptionAsync(action, marketTickers, channelAction);
            stopwatch.Stop();
            if (EnablePerformanceMetrics)
            {
                _asyncOperationTimes[$"UpdateSubscription_{action}"] = stopwatch.Elapsed;
                _semaphoreWaitCount.AddOrUpdate($"UpdateSubscription_{action}", 0, (k, v) => v + 1); // Assuming one wait per call
                _performanceMetrics?.RecordWebSocketOperation($"UpdateSubscription_{action}", stopwatch.Elapsed);
                _performanceMetrics?.RecordSemaphoreWait($"UpdateSubscription_{action}", 1);
            }
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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Check if already connected to prevent duplicate connections
            if (_connectionManager.IsConnected())
            {
                _logger.LogDebug("WebSocket already connected, skipping duplicate connection attempt");
                return;
            }

            await _connectionManager.ConnectAsync(retryCount);
            stopwatch.Stop();
            if (EnablePerformanceMetrics)
            {
                _asyncOperationTimes["Connect"] = stopwatch.Elapsed;
                _performanceMetrics?.RecordWebSocketOperation("Connect", stopwatch.Elapsed);
            }
            if (_connectionManager.IsConnected())
            {
                // Subscribe to enabled non-market-specific channels
                var enabledChannels = new[] { KalshiConstants.ScriptType_Feed_Fill, KalshiConstants.ScriptType_Feed_Lifecycle }.Where(IsChannelEnabled);
                foreach (var channel in enabledChannels)
                {
                    _globalCancellationToken.ThrowIfCancellationRequested();
                    await _subscriptionManager.SubscribeToChannelAsync(channel, Array.Empty<string>());
                    _logger.LogDebug("Subscribed to enabled channel {Channel}", channel);
                }

                // Market-specific channels will be subscribed individually as each market completes initialization
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
            var enabledChannels = KalshiConstants.MarketChannels.Where(IsChannelEnabled);
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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _subscriptionManager.SubscribeToChannelAsync(action, marketTickers);
            stopwatch.Stop();
            if (EnablePerformanceMetrics)
            {
                _asyncOperationTimes[$"SubscribeToChannel_{action}"] = stopwatch.Elapsed;
                _performanceMetrics?.RecordWebSocketOperation($"SubscribeToChannel_{action}", stopwatch.Elapsed);
            }
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
            _enabledChannels[KalshiConstants.ScriptType_Feed_Fill] = true;
            _enabledChannels[KalshiConstants.ScriptType_Feed_Lifecycle] = true;
            _enabledChannels[KalshiConstants.ScriptType_Feed_Event_Lifecycle] = true;

            // Market-specific channels that should be enabled by default
            _enabledChannels[KalshiConstants.ScriptType_Feed_Orderbook] = true;
            _enabledChannels[KalshiConstants.ScriptType_Feed_Ticker] = true;
            _enabledChannels[KalshiConstants.ScriptType_Feed_Trade] = true;
        }

        public async Task StartReceivingAsync()
        {
            _logger.LogInformation("Starting WebSocket message receiving loop");
            try
            {
                // Start the receiving loop in a background task
                _ = Task.Run(async () =>
                {
                    var buffer = new byte[_webSocketBufferSize];
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
                                    _logger.LogInformation("Received WebSocket message: {Message}", fullMessage);
                                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                                    await _messageProcessor.ProcessMessageAsync(fullMessage);
                                    stopwatch.Stop();

                                    if (EnablePerformanceMetrics)
                                    {
                                        // Track processing time and buffer usage
                                        _messageProcessingTimeTicks.AddOrUpdate("WebSocketMessage", 0, (k, v) => v + stopwatch.ElapsedTicks);
                                        _messageProcessingCount.AddOrUpdate("WebSocketMessage", 0, (k, v) => v + 1);
                                        _bufferUsageBytes.AddOrUpdate("WebSocketMessage", 0, (k, v) => v + fullMessage.Length);

                                        // Post to performance service
                                        _performanceMetrics?.RecordWebSocketMessageProcessing("WebSocketMessage", stopwatch.ElapsedTicks, 1, fullMessage.Length, EnablePerformanceMetrics);
                                    }

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

            await Task.CompletedTask;
        }

        #region IWebSocketPerformanceMetrics Implementation

        /// <summary>
        /// Records WebSocket message processing performance with enablement status.
        /// </summary>
        public void RecordWebSocketMessageProcessing(string messageType, long processingTimeTicks, int messageCount, long bufferSizeBytes, bool metricsEnabled)
        {
            if (!EnablePerformanceMetrics || !metricsEnabled) return;

            _messageProcessingTimeTicks.AddOrUpdate(messageType, 0, (k, v) => v + processingTimeTicks);
            _messageProcessingCount.AddOrUpdate(messageType, 0, (k, v) => v + messageCount);
            _bufferUsageBytes.AddOrUpdate(messageType, 0, (k, v) => v + bufferSizeBytes);
        }

        /// <summary>
        /// Records WebSocket connection performance.
        /// </summary>
        public void RecordWebSocketOperation(string operation, TimeSpan duration)
        {
            if (!EnablePerformanceMetrics) return;

            _asyncOperationTimes[operation] = duration;
        }

        /// <summary>
        /// Records semaphore wait counts for WebSocket operations.
        /// </summary>
        public void RecordSemaphoreWait(string operation, int waitCount)
        {
            if (!EnablePerformanceMetrics) return;

            _semaphoreWaitCount.AddOrUpdate(operation, 0, (k, v) => v + waitCount);
        }

        /// <summary>
        /// Gets the average processing times for WebSocket messages.
        /// </summary>
        public ConcurrentDictionary<string, double> GetAverageProcessingTimesMs()
        {
            return new ConcurrentDictionary<string, double>(
                _messageProcessingTimeTicks.ToDictionary(
                    kv => kv.Key,
                    kv => _messageProcessingCount.TryGetValue(kv.Key, out var count) && count > 0
                        ? TimeSpan.FromTicks(kv.Value / count).TotalMilliseconds
                        : 0.0
                )
            );
        }

        /// <summary>
        /// Gets the total buffer usage for WebSocket messages.
        /// </summary>
        public ConcurrentDictionary<string, long> GetBufferUsageBytes()
        {
            return new ConcurrentDictionary<string, long>(_bufferUsageBytes);
        }

        /// <summary>
        /// Gets the average times for WebSocket operations.
        /// </summary>
        public ConcurrentDictionary<string, double> GetAsyncOperationTimesMs()
        {
            return new ConcurrentDictionary<string, double>(
                _asyncOperationTimes.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.TotalMilliseconds
                )
            );
        }

        /// <summary>
        /// Gets the semaphore wait counts for WebSocket operations.
        /// </summary>
        public ConcurrentDictionary<string, int> GetSemaphoreWaitCounts()
        {
            return new ConcurrentDictionary<string, int>(_semaphoreWaitCount);
        }

        /// <summary>
        /// Resets all WebSocket performance metrics.
        /// </summary>
        public void ResetWebSocketMetrics()
        {
            _messageProcessingTimeTicks.Clear();
            _messageProcessingCount.Clear();
            _bufferUsageBytes.Clear();
            _asyncOperationTimes.Clear();
            _semaphoreWaitCount.Clear();
        }

        #endregion

    }


}
