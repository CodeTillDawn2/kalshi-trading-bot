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
    public class MessageProcessor : IMessageProcessor
    {
        private readonly ILogger<MessageProcessor> _logger;
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IStatusTrackerService _statusTrackerService;
        private readonly ISqlDataService _sqlDataService;
        private readonly IKalshiAPIService _kalshiAPIService;
        private bool _writeToSql;
        // Note: Pending confirms are now managed by SubscriptionManager
        private readonly ConcurrentDictionary<string, long> _eventCounts;
        private readonly PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long> _orderBookMessageQueue;
        private readonly object _orderBookQueueLock = new object();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>> _marketSubscriptionStates;
        private readonly Dictionary<string, (int Sid, HashSet<string> Markets)> _subscriptions;
        private readonly object _sequenceLock = new object();
        private long _lastSequenceNumber = 0;
        private int _orderBookSid = 0;
        private Task _receiveTask = null!;
        private CancellationToken _globalCancellationToken;
        private DateTime _lastMessageReceived = DateTime.UtcNow;

        public event EventHandler<OrderBookEventArgs>? OrderBookReceived;
        public event EventHandler<TickerEventArgs>? TickerReceived;
        public event EventHandler<TradeEventArgs>? TradeReceived;
        public event EventHandler<FillEventArgs>? FillReceived;
        public event EventHandler<MarketLifecycleEventArgs>? MarketLifecycleReceived;
        public event EventHandler<EventLifecycleEventArgs>? EventLifecycleReceived;
        public event EventHandler<DateTime>? MessageReceived;

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
            _writeToSql = false; // Default to false, will be set by SetWriteToSql method
            _globalCancellationToken = statusTrackerService.GetCancellationToken();

            // Initialize internal state
            _eventCounts = new ConcurrentDictionary<string, long>();
            _orderBookMessageQueue = new PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long>();
            _marketSubscriptionStates = new ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>>();
            _subscriptions = new Dictionary<string, (int Sid, HashSet<string> Markets)>();

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

            _logger.LogInformation("MessageProcessor initialized, WriteToSQL will be set later");
        }

        public async Task StartProcessingAsync()
        {
            _receiveTask = Task.Run(() => ReceiveAsync(), _globalCancellationToken);
        }

        public async Task StopProcessingAsync()
        {
            if (_receiveTask != null && !_receiveTask.IsCompleted)
            {
                await _receiveTask.ConfigureAwait(false);
            }
        }

        public int OrderBookMessageQueueCount => _orderBookMessageQueue.Count;

        public int PendingConfirmsCount => 0; // Pending confirms are now managed by SubscriptionManager

        private async Task ReceiveAsync()
        {
            _logger.LogDebug("ReceiveAsync started");
            var buffer = new byte[1024 * 16];
            var messageBuilder = new StringBuilder();
            try
            {
                while (!_globalCancellationToken.IsCancellationRequested)
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
                            _lastMessageReceived = DateTime.UtcNow;
                            _logger.LogInformation("Received complete WebSocket message: Length={Length}", fullMessage.Length);
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
                // Handle reconnection
            }
            catch (Exception ex)
            {
                _logger.LogError("WebSocket receiver encountered error: {Message}. Attempting to reconnect", ex.Message);
                // Handle reconnection
            }
            finally
            {
                _logger.LogDebug("ReceiveAsync completed at {0}", DateTime.UtcNow);
            }
        }

        public async Task ProcessMessageAsync(string message)
        {
            _logger.LogInformation("Processing WebSocket message: {Message}", message);
            try
            {
                _globalCancellationToken.ThrowIfCancellationRequested();
                var data = JsonSerializer.Deserialize<JsonElement>(message);
                var msgType = data.GetProperty("type").GetString() ?? "unknown";

                _logger.LogInformation("Received WebSocket message type: {MsgType}", msgType);

                if (MessageReceived != null)
                    MessageReceived?.Invoke(this, DateTime.UtcNow);

                switch (msgType)
                {
                    case "orderbook_snapshot":
                    case "orderbook_delta":
                        await HandleOrderBookMessageAsync(data, msgType);
                        break;
                    case "ticker":
                        await HandleTickerMessageAsync(data);
                        break;
                    case "trade":
                        await HandleTradeMessageAsync(data);
                        break;
                    case "fill":
                        await HandleFillMessageAsync(data);
                        break;
                    case "market_lifecycle_v2":
                        await HandleMarketLifecycleMessageAsync(data);
                        break;
                    case "event_lifecycle":
                        await HandleEventLifecycleMessageAsync(data);
                        break;
                    case "subscribed":
                        await HandleSubscribedMessageAsync(data);
                        break;
                    case "unsubscribed":
                        await HandleUnsubscribedMessageAsync(data);
                        break;
                    case "ok":
                        await HandleOkMessageAsync(data);
                        break;
                    case "error":
                        await HandleErrorMessageAsync(data);
                        break;
                    default:
                        _eventCounts.AddOrUpdate("Unknown", 1, (_, count) => count + 1);
                        _logger.LogWarning("Received unknown message type: {MsgType}, Message: {Message}", msgType, message);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("The input does not contain any JSON tokens. Raw: {0}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
            }
        }

        public long LastSequenceNumber => _lastSequenceNumber;

        public void ResetEventCounts()
        {
            _eventCounts.Clear();
        }

        public void SetWriteToSql(bool writeToSql)
        {
            _writeToSql = writeToSql;
            _logger.LogInformation("Set WriteToSQL to {WriteToSql}", _writeToSql);
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
                    _logger.LogInformation("Waiting for order book queue to clear for market: {MarketTicker}", marketTicker);
                }

                if (DateTime.UtcNow - startTime > timeout)
                {
                    _logger.LogWarning("Timeout waiting for order book queue to clear for market: {MarketTicker} after {TimeoutSeconds}s", marketTicker, timeout.TotalSeconds);
                    return;
                }

                await Task.Delay(100, _globalCancellationToken);
            }

            if (waited)
                _logger.LogInformation("Market {0} waited {1}s before saving snapshot", marketTicker, (DateTime.UtcNow - startTime).TotalSeconds);
        }

        public (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker)
        {
            // Note: These dictionaries need to be added to MessageProcessor
            // For now, return default values
            return (0, 0, 0);
        }

        private async Task HandleOrderBookMessageAsync(JsonElement data, string msgType)
        {
            _logger.LogDebug("Processing orderbook message: {MsgType}, WriteToSQL: {WriteToSql}", msgType, _writeToSql);
            _eventCounts.AddOrUpdate("OrderBook", 1, (_, count) => count + 1);

            try
            {
                var offerType = msgType == "orderbook_snapshot" ? "SNP" : "DEL";
                var eventArgs = new OrderBookEventArgs(offerType == "SNP" ? "snapshot" : "delta", data);
                OrderBookReceived?.Invoke(this, eventArgs);

                if (_writeToSql)
                {
                    _logger.LogDebug("Storing orderbook to SQL: {MsgType}, offerType: {OfferType}", msgType, offerType);
                    await _sqlDataService.StoreOrderBookAsync(data, offerType);
                }
                else
                {
                    _logger.LogDebug("Skipping SQL storage for orderbook: WriteToSQL is false");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orderbook message");
            }
        }

        private async Task HandleTickerMessageAsync(JsonElement data)
        {
            _logger.LogDebug("Processing ticker message, WriteToSQL: {WriteToSql}", _writeToSql);
            _eventCounts.AddOrUpdate("Ticker", 1, (_, count) => count + 1);

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

                if (_writeToSql)
                {
                    _logger.LogDebug("Storing ticker to SQL");
                    await _sqlDataService.StoreTickerAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping SQL storage for ticker: WriteToSQL is false");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ticker message");
            }
        }

        private async Task HandleTradeMessageAsync(JsonElement data)
        {
            _logger.LogDebug("Processing trade message, WriteToSQL: {WriteToSql}", _writeToSql);
            _eventCounts.AddOrUpdate("Trade", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new TradeEventArgs(data);
                TradeReceived?.Invoke(this, eventArgs);

                if (_writeToSql)
                {
                    _logger.LogDebug("Storing trade to SQL");
                    await _sqlDataService.StoreTradeAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping SQL storage for trade: WriteToSQL is false");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing trade message");
            }
        }

        private async Task HandleFillMessageAsync(JsonElement data)
        {
            _logger.LogDebug("Processing fill message, WriteToSQL: {WriteToSql}", _writeToSql);
            _eventCounts.AddOrUpdate("Fill", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new FillEventArgs(data);
                FillReceived?.Invoke(this, eventArgs);

                if (_writeToSql)
                {
                    _logger.LogDebug("Storing fill to SQL");
                    await _sqlDataService.StoreFillAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping SQL storage for fill: WriteToSQL is false");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing fill message");
            }
        }

        private async Task HandleMarketLifecycleMessageAsync(JsonElement data)
        {
            _logger.LogDebug("Processing market lifecycle message, WriteToSQL: {WriteToSql}", _writeToSql);
            _eventCounts.AddOrUpdate("MarketLifecycle", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new MarketLifecycleEventArgs(data);
                MarketLifecycleReceived?.Invoke(this, eventArgs);

                if (_writeToSql)
                {
                    _logger.LogDebug("Storing market lifecycle to SQL");
                    await _sqlDataService.StoreMarketLifecycleAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping SQL storage for market lifecycle: WriteToSQL is false");
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
                _logger.LogError(ex, "Error processing market lifecycle message");
            }
        }

        private async Task HandleEventLifecycleMessageAsync(JsonElement data)
        {
            _logger.LogDebug("Processing event lifecycle message, WriteToSQL: {WriteToSql}", _writeToSql);
            _eventCounts.AddOrUpdate("EventLifecycle", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new EventLifecycleEventArgs(data);
                EventLifecycleReceived?.Invoke(this, eventArgs);

                if (_writeToSql)
                {
                    _logger.LogDebug("Storing event lifecycle to SQL");
                    await _sqlDataService.StoreEventLifecycleAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping SQL storage for event lifecycle: WriteToSQL is false");
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
                _logger.LogError(ex, "Error processing event lifecycle message");
            }
        }

        private async Task HandleSubscribedMessageAsync(JsonElement data)
        {
            _logger.LogDebug("Processing subscribed confirmation");
            _eventCounts.AddOrUpdate("Subscribe", 1, (_, count) => count + 1);

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
                        _logger.LogWarning("Subscribed message missing channel property");
                    }

                    // Remove from pending confirms if this ID was pending
                    if (data.TryGetProperty("id", out var idProp))
                    {
                        var id = idProp.GetInt32();
                        _subscriptionManager.RemovePendingConfirmation(id);
                    }
                    else
                    {
                        _logger.LogWarning("Subscribed message missing id property");
                    }
                }
                else
                {
                    _logger.LogWarning("Subscribed message missing sid property");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscribed message");
            }
        }

        private async Task HandleUnsubscribedMessageAsync(JsonElement data)
        {
            _logger.LogDebug("Processing unsubscribed confirmation");
            _eventCounts.AddOrUpdate("Unsubscribe", 1, (_, count) => count + 1);

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
                _logger.LogError(ex, "Error processing unsubscribed message");
            }
        }

        private async Task HandleOkMessageAsync(JsonElement data)
        {
            _logger.LogDebug("Processing ok confirmation");
            _eventCounts.AddOrUpdate("Ok", 1, (_, count) => count + 1);

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

                        // Get channel from pending confirm since it's not in the message
                        var pending = _subscriptionManager.GetPendingConfirm(id);
                        if (pending.HasValue)
                        {
                            var channel = pending.Value.Channel;
                            await _subscriptionManager.UpdateSubscriptionStateFromConfirmationAsync(sid, channel);
                        }
                        else
                        {
                            _logger.LogWarning("No pending confirm found for subscribe confirmation ID: {Id}", id);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Update confirmed for ID: {Id}", id);
                    }

                    // Remove from pending confirms
                    _subscriptionManager.RemovePendingConfirmation(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ok message");
            }
        }

        private async Task HandleErrorMessageAsync(JsonElement data)
        {
            _logger.LogInformation("Processing error message");
            _eventCounts.AddOrUpdate("Error", 1, (_, count) => count + 1);

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

                            // Remove from pending confirms since it's already subscribed
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