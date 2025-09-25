using BacklashDTOs;
using BacklashInterfaces.Enums;
using System.Collections.Concurrent;

namespace KalshiBotAPI.WebSockets.Interfaces
{
    /// <summary>
    /// Defines the contract for a WebSocket client that handles real-time communication
    /// with the Kalshi trading platform, including market data subscriptions and event handling.
    /// </summary>
    public interface IKalshiWebSocketClient
    {
        /// <summary>
        /// Occurs when an order book update is received.
        /// </summary>
        event EventHandler<OrderBookEventArgs> OrderBookReceived;

        /// <summary>
        /// Occurs when a ticker update is received.
        /// </summary>
        event EventHandler<TickerEventArgs> TickerReceived;

        /// <summary>
        /// Occurs when a trade event is received.
        /// </summary>
        event EventHandler<TradeEventArgs> TradeReceived;

        /// <summary>
        /// Occurs when a fill event is received.
        /// </summary>
        event EventHandler<FillEventArgs> FillReceived;

        /// <summary>
        /// Occurs when a market lifecycle event is received.
        /// </summary>
        event EventHandler<MarketLifecycleEventArgs> MarketLifecycleReceived;

        /// <summary>
        /// Occurs when an event lifecycle event is received.
        /// </summary>
        event EventHandler<EventLifecycleEventArgs> EventLifecycleReceived;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event EventHandler<DateTime> MessageReceived;

        /// <summary>
        /// Gets or sets a value indicating whether trading is currently active.
        /// </summary>
        bool IsTradingActive { get; set; }

        /// <summary>
        /// Gets the concurrent dictionary containing event counts.
        /// </summary>
        ConcurrentDictionary<string, long> EventCounts { get; }

        /// <summary>
        /// Gets the count of connection semaphore.
        /// </summary>
        int ConnectSemaphoreCount { get; }

        /// <summary>
        /// Gets the count of subscription update semaphore.
        /// </summary>
        int SubscriptionUpdateSemaphoreCount { get; }

        /// <summary>
        /// Gets the count of channel subscription semaphore.
        /// </summary>
        int ChannelSubscriptionSemaphoreCount { get; }

        /// <summary>
        /// Gets the count of queued subscription updates.
        /// </summary>
        int QueuedSubscriptionUpdatesCount { get; }

        /// <summary>
        /// Gets the count of order book message queue.
        /// </summary>
        int OrderBookMessageQueueCount { get; }

        /// <summary>
        /// Gets the count of pending confirms.
        /// </summary>
        int PendingConfirmsCount { get; }

        /// <summary>
        /// Gets or sets the hash set of watched market tickers.
        /// </summary>
        HashSet<string> WatchedMarkets { get; set; }

        /// <summary>
        /// Connects to the WebSocket asynchronously with optional retry count.
        /// </summary>
        /// <param name="retryCount">The number of retry attempts (default 0).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ConnectAsync(int retryCount = 0);
        /// <summary>GetEventCountsByMarket</summary>
        Task SubscribeToWatchedMarketsAsync();
        Task SubscribeToChannelAsync(string action, string[] marketTickers);
        Task UnsubscribeFromChannelAsync(string action);
        Task UnsubscribeFromAllAsync();
        Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction);
        Task ResetConnectionAsync();
        Task ShutdownAsync();
        Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout);
        bool IsConnected();
        bool IsSubscribed(string marketTicker, string action);
        bool CanSubscribeToMarket(string marketTicker, string channel);
        void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state);
        void ClearOrderBookQueue(string marketTicker);
        void DisableReconnect();
        void EnableReconnect();
        int GenerateNextMessageId();
        void ResetEventCounts();
        string GetChannelName(string action);
        Task SendMessageAsync(string message);
        Task ResubscribeAsync(bool force = false);

        (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker);
    }
}
