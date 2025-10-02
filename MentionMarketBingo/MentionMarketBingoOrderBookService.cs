using BacklashDTOs;
using BacklashDTOs.KalshiAPI;
using BacklashInterfaces.Constants;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotAPI.Websockets;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MentionMarketBingo;

public class MentionMarketBingoOrderBookService
{
    private readonly KalshiWebSocketClient _webSocketClient;
    private readonly ILogger<MentionMarketBingoOrderBookService> _logger;
    private readonly ConcurrentDictionary<string, List<OrderbookData>> _orderBooks = new();

    public event EventHandler<string>? OrderBookUpdated;

    public MentionMarketBingoOrderBookService(IKalshiWebSocketClient webSocketClient, ILogger<MentionMarketBingoOrderBookService> logger)
    {
        _webSocketClient = (KalshiWebSocketClient)webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _webSocketClient.OrderBookReceived += HandleOrderBookReceived;
        _webSocketClient.FillReceived += HandleFillReceived;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting MentionMarketBingoOrderBookService");

        await _webSocketClient.ConnectAsync();
        _logger.LogInformation("WebSocket connected and channels enabled");
    }

    public async Task SubscribeToMarket(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            _logger.LogWarning("Invalid ticker provided for subscription");
            return;
        }

        if (!_webSocketClient.IsConnected())
        {
            _logger.LogWarning("WebSocket not connected, cannot subscribe to market {Ticker}", ticker);
            return;
        }

        await _webSocketClient.SubscribeToChannelAsync("orderbook", new string[] { ticker });
        _logger.LogInformation("Subscribed to orderbook for market {Ticker}", ticker);
    }

    public async Task SubscribeToMarkets(string[] tickers)
    {
        if (tickers == null || tickers.Length == 0)
        {
            _logger.LogWarning("No tickers provided for subscription");
            return;
        }

        var validTickers = tickers.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
        if (validTickers.Length == 0)
        {
            _logger.LogWarning("No valid tickers provided for subscription");
            return;
        }

        if (!_webSocketClient.IsConnected())
        {
            _logger.LogWarning("WebSocket not connected, cannot subscribe to markets");
            return;
        }

        await _webSocketClient.SubscribeToChannelAsync("orderbook", validTickers);
        _logger.LogInformation("Subscribed to orderbook for {Count} markets", validTickers.Length);
    }

    public async Task UnsubscribeFromMarket(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            _logger.LogWarning("Invalid ticker provided for unsubscription");
            return;
        }

        if (!_webSocketClient.IsConnected())
        {
            _logger.LogWarning("WebSocket not connected, cannot unsubscribe from market {Ticker}", ticker);
            return;
        }

        await _webSocketClient.UpdateSubscriptionAsync("delete_markets", new string[] { ticker }, "orderbook");
        _orderBooks.TryRemove(ticker, out _);
        _logger.LogInformation("Unsubscribed from orderbook for market {Ticker}", ticker);
    }

    public List<OrderbookData> GetOrderBook(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return new List<OrderbookData>();
        }

        return _orderBooks.TryGetValue(ticker, out var orderBook) ? orderBook : new List<OrderbookData>();
    }

    private void HandleOrderBookReceived(object? sender, OrderBookEventArgs e)
    {
        try
        {
            var marketTicker = e.Data.GetProperty("msg").GetProperty("market_ticker").GetString() ?? "Unknown";
            var offerType = e.OfferType;
            var seq = e.Data.TryGetProperty("seq", out var seqProp) ? seqProp.GetInt64() : -1;

            _logger.LogDebug("Received orderbook event for {MarketTicker}, OfferType: {OfferType}, Seq: {Seq}", marketTicker, offerType, seq);

            var message = new OrderbookMessage(e.Data, offerType);
            List<OrderbookData> updatedOrderbook;

            if (offerType == "SNP")
            {
                updatedOrderbook = ProcessSnapshot(marketTicker, message);
            }
            else if (offerType == "DEL")
            {
                updatedOrderbook = ProcessDelta(marketTicker, message);
            }
            else
            {
                _logger.LogWarning("Unknown offer type '{OfferType}' for market {MarketTicker}", offerType, marketTicker);
                return;
            }

            _orderBooks[marketTicker] = updatedOrderbook;
            OrderBookUpdated?.Invoke(this, marketTicker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling orderbook event");
        }
    }

    private void HandleFillReceived(object? sender, FillEventArgs e)
    {
        // Log fill events for monitoring
        _logger.LogInformation("Received fill event: {Data}", e.Data);
    }

    private List<OrderbookData> ProcessSnapshot(string marketTicker, OrderbookMessage message)
    {
        var orderbook = new List<OrderbookData>();

        if (message.YesOrders != null)
        {
            foreach (var order in message.YesOrders)
            {
                orderbook.Add(new OrderbookData(marketTicker, order.Price, "yes", order.RestingContracts));
            }
        }

        if (message.NoOrders != null)
        {
            foreach (var order in message.NoOrders)
            {
                orderbook.Add(new OrderbookData(marketTicker, order.Price, "no", order.RestingContracts));
            }
        }

        return orderbook.OrderBy(x => x.Price).ToList();
    }

    private List<OrderbookData> ProcessDelta(string marketTicker, OrderbookMessage message)
    {
        if (!_orderBooks.TryGetValue(marketTicker, out var orderbook))
        {
            orderbook = new List<OrderbookData>();
        }

        if (!message.Price.HasValue || string.IsNullOrEmpty(message.Side) || !message.Delta.HasValue)
        {
            _logger.LogWarning("Invalid delta message for market {MarketTicker}", marketTicker);
            return orderbook;
        }

        var existing = orderbook.FirstOrDefault(o => o.Side == message.Side && o.Price == message.Price.Value);
        if (existing != null)
        {
            orderbook.Remove(existing);
            // Calculate new resting contracts by adding the delta to existing
            var newRestingContracts = existing.RestingContracts + message.Delta.Value;
            if (newRestingContracts > 0)
            {
                orderbook.Add(new OrderbookData(marketTicker, message.Price.Value, message.Side, newRestingContracts));
            }
        }
        else
        {
            // No existing order at this price level, add new one if delta is positive
            if (message.Delta.Value > 0)
            {
                orderbook.Add(new OrderbookData(marketTicker, message.Price.Value, message.Side, message.Delta.Value));
            }
        }

        return orderbook.OrderBy(x => x.Price).ToList();
    }
}