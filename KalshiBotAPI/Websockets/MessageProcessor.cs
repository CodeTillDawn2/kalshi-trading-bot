using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using BacklashDTOs;
using BacklashDTOs.Exceptions;
using BacklashInterfaces.Constants;
using BacklashInterfaces.Enums;
using BacklashBot.State.Interfaces;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BacklashBot.Services.Interfaces;
using BacklashInterfaces.SmokehouseBot.Services;
using BacklashBot.KalshiAPI.Interfaces;

namespace KalshiBotAPI.Websockets
{
    /// <summary>
    /// Processes incoming WebSocket messages from Kalshi's trading platform, routing them to appropriate handlers
    /// based on message type. Manages event counting, order book message queuing, and integrates with data
    /// persistence and API services for comprehensive market data processing.
    /// </summary>
    public class MessageProcessor : IMessageProcessor
    {
        private readonly ILogger<MessageProcessor> _logger;
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IStatusTrackerService _statusTrackerService;
        private readonly ISqlDataService _sqlDataService;
        private readonly IKalshiAPIService _kalshiAPIService;
        private bool _isDataPersistenceEnabled;
        private readonly ConcurrentDictionary<string, long> _messageTypeCounts;
        private readonly PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long> _orderBookUpdateQueue;
        private readonly object _orderBookQueueSynchronizationLock = new object();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>> _marketChannelSubscriptionStates;
        private readonly Dictionary<string, (int Sid, HashSet<string> Markets)> _channelSubscriptions;
        private readonly object _sequenceNumberSynchronizationLock = new object();
        private long _latestProcessedSequenceNumber = 0;
        private int _orderBookSubscriptionId = 0;
        private Task _messageReceivingTask = null!;
        private CancellationToken _processingCancellationToken;
        private DateTime _lastMessageTimestamp = DateTime.UtcNow;

        public event EventHandler<OrderBookEventArgs>? OrderBookReceived;
        public event EventHandler<TickerEventArgs>? TickerReceived;
        public event EventHandler<TradeEventArgs>? TradeReceived;
        public event EventHandler<FillEventArgs>? FillReceived;
        public event EventHandler<MarketLifecycleEventArgs>? MarketLifecycleReceived;
        public event EventHandler<EventLifecycleEventArgs>? EventLifecycleReceived;
        public event EventHandler<DateTime>? MessageReceived;

        /// <summary>
        /// Initializes a new instance of the MessageProcessor with required dependencies.
        /// Sets up internal data structures for message processing, event counting, and queue management.
        /// </summary>
        /// <param name="logger">Logger for recording processing activities and errors.</param>
        /// <param name="connectionManager">Manages WebSocket connection lifecycle and communication.</param>
        /// <param name="subscriptionManager">Handles market data subscription management and state tracking.</param>
        /// <param name="statusTrackerService">Provides system status and cancellation token management.</param>
        /// <param name="sqlDataService">Handles data persistence to SQL database when enabled.</param>
        /// <param name="kalshiAPIService">Provides access to Kalshi API for market data retrieval and updates.</param>
        public MessageProcessor(
            ILogger<MessageProcessor> logger,
            IWebSocketConnectionManager connectionManager,
            ISubscriptionManager subscriptionManager,
            IStatusTrackerService statusTrackerService,
            ISqlDataService sqlDataService,
            IKalshiAPIService kalshiAPIService)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _subscriptionManager = subscriptionManager;
            _statusTrackerService = statusTrackerService;
            _sqlDataService = sqlDataService;
            _kalshiAPIService = kalshiAPIService;
            _isDataPersistenceEnabled = false; // Default to false, will be set by SetDataPersistenceEnabled method
            _processingCancellationToken = statusTrackerService.GetCancellationToken();

            // Initialize internal state
            _messageTypeCounts = new ConcurrentDictionary<string, long>();
            _orderBookUpdateQueue = new PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long>();
            _marketChannelSubscriptionStates = new ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>>();
            _channelSubscriptions = new Dictionary<string, (int Sid, HashSet<string> Markets)>();

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

            _logger.LogInformation("MessageProcessor initialized, data persistence will be configured later");
        }

        /// <summary>
        /// Starts the background message processing task that continuously receives and processes WebSocket messages.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartProcessingAsync()
        {
            _messageReceivingTask = Task.Run(() => ReceiveAsync(), _processingCancellationToken);
        }

        /// <summary>
        /// Stops the message processing task and waits for it to complete.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StopProcessingAsync()
        {
            if (_messageReceivingTask != null && !_messageReceivingTask.IsCompleted)
            {
                await _messageReceivingTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the current count of order book update messages in the processing queue.
        /// </summary>
        public int OrderBookMessageQueueCount => _orderBookUpdateQueue.Count;

        /// <summary>
        /// Gets the count of pending subscription confirmations. Always returns 0 as this is now managed by SubscriptionManager.
        /// </summary>
        public int PendingConfirmsCount => 0;

        /// <summary>
        /// Continuously receives WebSocket messages and processes them until cancellation is requested.
        /// Handles message fragmentation, connection monitoring, and error recovery.
        /// </summary>
        /// <returns>A task representing the asynchronous receive operation.</returns>
        private async Task ReceiveAsync()
        {
            _logger.LogInformation("WebSocket message receiving task started");
            var buffer = new byte[1024 * 16];
            var messageBuilder = new StringBuilder();
            try
            {
                while (!_processingCancellationToken.IsCancellationRequested)
                {
                    var webSocket = _connectionManager.GetWebSocket();
                    if (webSocket == null || webSocket.State != WebSocketState.Open)
                        throw new InvalidOperationException("WebSocket connection lost");

                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
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
                            _lastMessageTimestamp = DateTime.UtcNow;
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
                _logger.LogInformation("WebSocket message receiving task cancelled at {Timestamp}", DateTime.UtcNow);
            }
            catch (WebSocketException ex) when (ex.Message.Contains("without completing the close handshake"))
            {
                // Handle reconnection - placeholder for future implementation
            }
            catch (Exception ex)
            {
                _logger.LogError("WebSocket receiver encountered error: {Message}. Attempting to reconnect", ex.Message);
                // Handle reconnection - placeholder for future implementation
            }
            finally
            {
                _logger.LogInformation("WebSocket message receiving task completed at {Timestamp}", DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Processes a single WebSocket message by parsing its JSON content and routing it to the appropriate handler
        /// based on the message type. Updates event counts and triggers relevant events for subscribers.
        /// </summary>
        /// <param name="message">The raw JSON message string received from the WebSocket.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        public async Task ProcessMessageAsync(string message)
        {
            _logger.LogDebug("Processing WebSocket message: {Message}", message);
            try
            {
                _processingCancellationToken.ThrowIfCancellationRequested();
                var data = JsonSerializer.Deserialize<JsonElement>(message);
                var msgType = data.GetProperty("type").GetString() ?? "unknown";

                _logger.LogDebug("Received WebSocket message type: {MsgType}", msgType);

                if (MessageReceived != null)
                    MessageReceived?.Invoke(this, DateTime.UtcNow);

                switch (msgType)
                {
                    case "orderbook_snapshot":
                    case "orderbook_delta":
                        await ProcessOrderBookUpdateAsync(data, msgType);
                        break;
                    case "ticker":
                        await ProcessTickerUpdateAsync(data);
                        break;
                    case "trade":
                        await ProcessTradeUpdateAsync(data);
                        break;
                    case "fill":
                        await ProcessFillUpdateAsync(data);
                        break;
                    case "market_lifecycle_v2":
                        await ProcessMarketLifecycleUpdateAsync(data);
                        break;
                    case "event_lifecycle":
                        await ProcessEventLifecycleUpdateAsync(data);
                        break;
                    case "subscribed":
                        await ProcessSubscriptionConfirmationAsync(data);
                        break;
                    case "unsubscribed":
                        await ProcessUnsubscriptionConfirmationAsync(data);
                        break;
                    case "ok":
                        await ProcessOkConfirmationAsync(data);
                        break;
                    case "error":
                        await ProcessErrorMessageAsync(data);
                        break;
                    default:
                        _messageTypeCounts.AddOrUpdate("Unknown", 1, (_, count) => count + 1);
                        _logger.LogWarning("Received unknown message type: {MsgType}, Message: {Message}", msgType, message);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("The input does not contain any JSON tokens. Raw: {RawMessage}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
            }
        }

        /// <summary>
        /// Gets the latest sequence number processed from WebSocket messages.
        /// Used for maintaining message ordering and detecting gaps in message streams.
        /// </summary>
        public long LastSequenceNumber => _latestProcessedSequenceNumber;

        /// <summary>
        /// Resets all message type counters to zero. Used for periodic statistics reset or testing.
        /// </summary>
        public void ResetEventCounts()
        {
            _messageTypeCounts.Clear();
        }

        /// <summary>
        /// Configures whether processed market data should be persisted to the SQL database.
        /// When enabled, order book, ticker, trade, and lifecycle data will be stored for analysis.
        /// </summary>
        /// <param name="isDataPersistenceEnabled">True to enable SQL persistence, false to disable.</param>
        public void SetWriteToSql(bool isDataPersistenceEnabled)
        {
            _isDataPersistenceEnabled = isDataPersistenceEnabled;
            _logger.LogInformation("Data persistence to SQL set to {IsEnabled}", _isDataPersistenceEnabled);
        }

        /// <summary>
        /// Removes all queued order book update messages for a specific market ticker.
        /// Used when clearing stale data or resetting market state.
        /// </summary>
        /// <param name="marketTicker">The market ticker to clear messages for.</param>
        public void ClearOrderBookQueue(string marketTicker)
        {
            _logger.LogInformation("Clearing order book update queue for market: {MarketTicker}", marketTicker);
            lock (_orderBookQueueSynchronizationLock)
            {
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
                _logger.LogInformation("Cleared order book messages for {MarketTicker}. Remaining queue count: {Count}", marketTicker, _orderBookUpdateQueue.Count);
            }
        }

        /// <summary>
        /// Waits asynchronously for the order book update queue to be empty for a specific market,
        /// with a configurable timeout. Used to ensure all pending updates are processed before
        /// taking snapshots or performing market analysis.
        /// </summary>
        /// <param name="marketTicker">The market ticker to wait for queue clearance.</param>
        /// <param name="timeout">Maximum time to wait for queue clearance.</param>
        /// <returns>A task representing the asynchronous wait operation.</returns>
        public async Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            bool waited = false;

            while (!_processingCancellationToken.IsCancellationRequested)
            {
                bool hasPendingUpdates;
                lock (_orderBookQueueSynchronizationLock)
                {
                    hasPendingUpdates = _orderBookUpdateQueue.Count > 0 &&
                                _orderBookUpdateQueue.UnorderedItems.Any(item =>
                                    item.Element.Data.GetProperty("msg").GetProperty("market_ticker").GetString() == marketTicker);
                }

                if (!hasPendingUpdates)
                {
                    _logger.LogInformation("Order book queue cleared for market: {MarketTicker}", marketTicker);
                    return;
                }
                {
                    waited = true;
                    _logger.LogInformation("Waiting for order book queue to clear for market: {MarketTicker}", marketTicker);
                }

                if (DateTime.UtcNow - startTime > timeout)
                {
                    _logger.LogWarning("Timeout waiting for order book queue to clear for market: {MarketTicker} after {TimeoutSeconds}s", marketTicker, timeout.TotalSeconds);
                    return;
                }

                await Task.Delay(100, _processingCancellationToken);
            }

            if (waited)
                _logger.LogInformation("Market {MarketTicker} waited {WaitTime}s before saving snapshot", marketTicker, (DateTime.UtcNow - startTime).TotalSeconds);
        }

        /// <summary>
        /// Gets the count of different event types processed for a specific market ticker.
        /// Currently returns default values as market-specific event tracking is not yet implemented.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get event counts for.</param>
        /// <returns>A tuple containing counts for orderbook, trade, and ticker events.</returns>
        public (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker)
        {
            // TODO: Implement market-specific event counting by maintaining per-market dictionaries
            // This would require tracking events by market ticker in addition to global counts
            return (0, 0, 0);
        }

        /// <summary>
        /// Processes order book snapshot and delta messages, updating event counts and triggering events.
        /// Handles both full snapshots and incremental updates to maintain current order book state.
        /// </summary>
        /// <param name="data">The JSON data containing the order book message.</param>
        /// <param name="msgType">The message type ("orderbook_snapshot" or "orderbook_delta").</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessOrderBookUpdateAsync(JsonElement data, string msgType)
        {
            _logger.LogDebug("Processing order book update: {MsgType}, DataPersistence: {IsEnabled}", msgType, _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("OrderBook", 1, (_, count) => count + 1);

            try
            {
                var offerType = msgType == "orderbook_snapshot" ? "SNP" : "DEL";
                var eventArgs = new OrderBookEventArgs(offerType == "SNP" ? "snapshot" : "delta", data);
                OrderBookReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting order book data: {MsgType}, offerType: {OfferType}", msgType, offerType);
                    await _sqlDataService.StoreOrderBookAsync(data, offerType);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for order book: DataPersistence is disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order book update message");
            }
        }

        /// <summary>
        /// Processes ticker messages containing real-time market price and volume data.
        /// Extracts ticker information and triggers events for subscribers while optionally persisting data.
        /// </summary>
        /// <param name="data">The JSON data containing the ticker message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessTickerUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing ticker update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("Ticker", 1, (_, count) => count + 1);

            try
            {
                var msg = data.GetProperty("msg");
                var eventArgs = new TickerEventArgs();

                // Deserialize the ticker data from the "msg" property
                if (msg.TryGetProperty("market_id", out var marketIdProp) && marketIdProp.TryGetGuid(out var marketId))
                    eventArgs.market_id = marketId;
                if (msg.TryGetProperty("market_ticker", out var tickerProp))
                    eventArgs.market_ticker = tickerProp.GetString() ?? "";
                if (msg.TryGetProperty("price", out var priceProp))
                    eventArgs.price = priceProp.GetInt32();
                if (msg.TryGetProperty("yes_bid", out var yesBidProp))
                    eventArgs.yes_bid = yesBidProp.GetInt32();
                if (msg.TryGetProperty("yes_ask", out var yesAskProp))
                    eventArgs.yes_ask = yesAskProp.GetInt32();
                if (msg.TryGetProperty("volume", out var volumeProp))
                    eventArgs.volume = volumeProp.GetInt32();
                if (msg.TryGetProperty("open_interest", out var openInterestProp))
                    eventArgs.open_interest = openInterestProp.GetInt32();
                if (msg.TryGetProperty("dollar_volume", out var dollarVolumeProp))
                    eventArgs.dollar_volume = dollarVolumeProp.GetInt32();
                if (msg.TryGetProperty("dollar_open_interest", out var dollarOpenInterestProp))
                    eventArgs.dollar_open_interest = dollarOpenInterestProp.GetInt32();
                if (msg.TryGetProperty("ts", out var tsProp))
                    eventArgs.ts = tsProp.GetInt64();

                eventArgs.LoggedDate = DateTime.UtcNow;

                _logger.LogInformation("Ticker data extracted - Market: {Market}, Bid: {Bid}, Ask: {Ask}, Price: {Price}",
                    eventArgs.market_ticker, eventArgs.yes_bid, eventArgs.yes_ask, eventArgs.price);

                TickerReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting ticker data");
                    await _sqlDataService.StoreTickerAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for ticker: DataPersistence is disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ticker update message");
            }
        }

        /// <summary>
        /// Processes trade execution messages containing details of completed market transactions.
        /// Triggers events for subscribers and optionally persists trade data for analysis.
        /// </summary>
        /// <param name="data">The JSON data containing the trade message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessTradeUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing trade update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("Trade", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new TradeEventArgs(data);
                TradeReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting trade data");
                    await _sqlDataService.StoreTradeAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for trade: DataPersistence is disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing trade update message");
            }
        }

        /// <summary>
        /// Processes fill messages indicating order execution and position updates.
        /// Triggers events for subscribers and optionally persists fill data for tracking.
        /// </summary>
        /// <param name="data">The JSON data containing the fill message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessFillUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing fill update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("Fill", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new FillEventArgs(data);
                FillReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting fill data");
                    await _sqlDataService.StoreFillAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for fill: DataPersistence is disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing fill update message");
            }
        }

        /// <summary>
        /// Processes market lifecycle messages indicating changes in market status or availability.
        /// Triggers events for subscribers, optionally persists data, and refreshes market information from API.
        /// </summary>
        /// <param name="data">The JSON data containing the market lifecycle message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessMarketLifecycleUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing market lifecycle update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("MarketLifecycle", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new MarketLifecycleEventArgs(data);
                MarketLifecycleReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting market lifecycle data");
                    await _sqlDataService.StoreMarketLifecycleAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for market lifecycle: DataPersistence is disabled");
                }

                // Trigger API refresh for the affected market
                if (data.TryGetProperty("msg", out var msg) && msg.TryGetProperty("market_ticker", out var tickerProp))
                {
                    var marketTicker = tickerProp.GetString();
                    if (!string.IsNullOrEmpty(marketTicker))
                    {
                        var (processedCount, errorCount) = await _kalshiAPIService.FetchMarketsAsync(tickers: new[] { marketTicker });
                        _logger.LogDebug("Fetched API data for market {MarketTicker} due to lifecycle event: {ProcessedCount} processed, {ErrorCount} errors",
                            marketTicker, processedCount, errorCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing market lifecycle update message");
            }
        }

        /// <summary>
        /// Processes event lifecycle messages indicating changes in event status or market groupings.
        /// Triggers events for subscribers, optionally persists data, and refreshes event or market information from API.
        /// </summary>
        /// <param name="data">The JSON data containing the event lifecycle message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessEventLifecycleUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing event lifecycle update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("EventLifecycle", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new EventLifecycleEventArgs(data);
                EventLifecycleReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting event lifecycle data");
                    await _sqlDataService.StoreEventLifecycleAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for event lifecycle: DataPersistence is disabled");
                }

                // Trigger API refresh if market_ticker or event_ticker is available
                if (data.TryGetProperty("msg", out var msg))
                {
                    // First try market_ticker
                    if (msg.TryGetProperty("market_ticker", out var marketTickerProp))
                    {
                        var marketTicker = marketTickerProp.GetString();
                        if (!string.IsNullOrEmpty(marketTicker))
                        {
                            var (processedCount, errorCount) = await _kalshiAPIService.FetchMarketsAsync(tickers: new[] { marketTicker });
                            _logger.LogDebug("Fetched API data for market {MarketTicker} due to event lifecycle: {ProcessedCount} processed, {ErrorCount} errors",
                                marketTicker, processedCount, errorCount);
                        }
                    }
                    // Then try event_ticker
                    else if (msg.TryGetProperty("event_ticker", out var eventTickerProp))
                    {
                        var eventTicker = eventTickerProp.GetString();
                        if (!string.IsNullOrEmpty(eventTicker))
                        {
                            var eventResponse = await _kalshiAPIService.FetchEventAsync(eventTicker);
                            _logger.LogDebug("Fetched API data for event {EventTicker} due to event lifecycle: {Success}",
                                eventTicker, eventResponse != null ? "success" : "failed");
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Event lifecycle message does not contain market_ticker or event_ticker, skipping API refresh");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event lifecycle update message");
            }
        }

        /// <summary>
        /// Processes subscription confirmation messages indicating successful channel subscriptions.
        /// Updates subscription state in the subscription manager and removes from pending confirmations.
        /// </summary>
        /// <param name="data">The JSON data containing the subscription confirmation message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessSubscriptionConfirmationAsync(JsonElement data)
        {
            _logger.LogDebug("Processing subscription confirmation");
            _messageTypeCounts.AddOrUpdate("Subscribe", 1, (_, count) => count + 1);

            try
            {
                var msg = data.GetProperty("msg");
                if (msg.TryGetProperty("sid", out var sidProp))
                {
                    var sid = sidProp.GetInt32();
                    _logger.LogInformation("Subscription confirmed with SID: {Sid}", sid);

                    // Update subscription state in subscription manager
                    if (msg.TryGetProperty("channel", out var channelProp))
                    {
                        var channel = channelProp.GetString() ?? "";
                        await _subscriptionManager.UpdateSubscriptionStateFromConfirmationAsync(sid, channel);
                    }
                    else
                    {
                        _logger.LogWarning("Subscription confirmation message missing channel property");
                    }

                    // Remove from pending confirmations if this ID was pending
                    if (data.TryGetProperty("id", out var idProp))
                    {
                        var id = idProp.GetInt32();
                        _subscriptionManager.RemovePendingConfirmation(id);
                    }
                    else
                    {
                        _logger.LogWarning("Subscription confirmation message missing id property");
                    }
                }
                else
                {
                    _logger.LogWarning("Subscription confirmation message missing sid property");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription confirmation message");
            }
        }

        /// <summary>
        /// Processes unsubscription confirmation messages indicating successful channel unsubscriptions.
        /// Logs the confirmation and updates internal state as needed.
        /// </summary>
        /// <param name="data">The JSON data containing the unsubscription confirmation message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessUnsubscriptionConfirmationAsync(JsonElement data)
        {
            _logger.LogDebug("Processing unsubscription confirmation");
            _messageTypeCounts.AddOrUpdate("Unsubscribe", 1, (_, count) => count + 1);

            try
            {
                if (data.TryGetProperty("sid", out var sidProp))
                {
                    var sid = sidProp.GetInt32();
                    _logger.LogInformation("Unsubscription confirmed for SID: {Sid}", sid);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unsubscription confirmation message");
            }
        }

        /// <summary>
        /// Processes general confirmation messages for various operations (updates, subscriptions).
        /// Handles both subscription confirmations with SIDs and general operation confirmations.
        /// </summary>
        /// <param name="data">The JSON data containing the confirmation message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessOkConfirmationAsync(JsonElement data)
        {
            _logger.LogDebug("Processing confirmation message");
            _messageTypeCounts.AddOrUpdate("Ok", 1, (_, count) => count + 1);

            try
            {
                // Handle update confirmations and possibly subscribe confirmations
                if (data.TryGetProperty("id", out var idProp))
                {
                    var id = idProp.GetInt32();
                    _logger.LogInformation("Confirmation received for ID: {Id}", id);

                    // Check if this is a subscribe confirmation with sid
                    if (data.TryGetProperty("sid", out var sidProp))
                    {
                        var sid = sidProp.GetInt32();
                        _logger.LogInformation("Subscribe confirmed with SID: {Sid} for ID: {Id}", sid, id);

                        // Get channel from pending confirmation since it's not in the message
                        var pending = _subscriptionManager.GetPendingConfirm(id);
                        if (pending.HasValue)
                        {
                            var channel = pending.Value.Channel;
                            await _subscriptionManager.UpdateSubscriptionStateFromConfirmationAsync(sid, channel);
                        }
                        else
                        {
                            _logger.LogWarning("No pending confirmation found for subscribe confirmation ID: {Id}", id);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Update confirmed for ID: {Id}", id);
                    }

                    // Remove from pending confirmations
                    _subscriptionManager.RemovePendingConfirmation(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing confirmation message");
            }
        }

        /// <summary>
        /// Processes error messages from the WebSocket connection, extracting error codes and messages.
        /// Handles specific error conditions like "Already subscribed" and logs appropriate warnings.
        /// </summary>
        /// <param name="data">The JSON data containing the error message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessErrorMessageAsync(JsonElement data)
        {
            _logger.LogInformation("Processing error message");
            _messageTypeCounts.AddOrUpdate("Error", 1, (_, count) => count + 1);

            try
            {
                if (data.TryGetProperty("msg", out var msgProp))
                {
                    string errorMsg = "Unknown error";
                    int errorCode = -1;

                    // Handle different types of error messages
                    if (msgProp.ValueKind == JsonValueKind.String)
                    {
                        errorMsg = msgProp.GetString() ?? "Unknown error";
                    }
                    else if (msgProp.ValueKind == JsonValueKind.Object)
                    {
                        // Extract error code and message
                        if (msgProp.TryGetProperty("code", out var codeProp))
                        {
                            errorCode = codeProp.GetInt32();
                        }
                        if (msgProp.TryGetProperty("msg", out var msgTextProp))
                        {
                            errorMsg = msgTextProp.GetString() ?? "Unknown error";
                        }
                        else
                        {
                            errorMsg = $"Error object: {msgProp.ToString()}";
                        }
                    }
                    else
                    {
                        // For other types, convert to string representation
                        errorMsg = $"Error value: {msgProp.ToString()}";
                    }

                    _logger.LogWarning("WebSocket error received: Code={Code}, Message={ErrorMessage}", errorCode, errorMsg);

                    // Handle specific error codes
                    if (errorCode == 6 && errorMsg.Contains("Already subscribed"))
                    {
                        // If we get "Already subscribed", treat it as successful subscription
                        // Try to extract the ID and update subscription state
                        if (data.TryGetProperty("id", out var idProp))
                        {
                            var id = idProp.GetInt32();
                            _logger.LogInformation("Received 'Already subscribed' for ID {Id}, treating as successful subscription", id);

                            // Remove from pending confirmations since it's already subscribed
                            _subscriptionManager.RemovePendingConfirmation(id);

                            // Try to find the subscription and update its state
                            // We need to look at the original message to determine what was being subscribed to
                            // For now, just log that we handled it
                            _logger.LogDebug("Handled 'Already subscribed' error for ID {Id}", id);
                        }
                    }
                }
                else
                {
                    _logger.LogError("WebSocket error received with no message");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing error message");
            }
        }
    }
}