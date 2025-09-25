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
        /// <summary>
        /// Subscribes to all watched markets for enabled channels.
        /// </summary>
        /// <returns>A task representing the asynchronous subscription operation.</returns>
        Task SubscribeToWatchedMarketsAsync();

        /// <summary>
        /// Subscribes to a specific channel for the given market tickers.
        /// </summary>
        /// <param name="action">The channel action to subscribe to.</param>
        /// <param name="marketTickers">Array of market tickers to subscribe for this channel.</param>
        /// <returns>A task representing the asynchronous subscription operation.</returns>
        Task SubscribeToChannelAsync(string action, string[] marketTickers);

        /// <summary>
        /// Unsubscribes from a specific channel.
        /// </summary>
        /// <param name="action">The channel action to unsubscribe from.</param>
        /// <returns>A task representing the asynchronous unsubscription operation.</returns>
        Task UnsubscribeFromChannelAsync(string action);

        /// <summary>
        /// Unsubscribes from all channels.
        /// </summary>
        /// <returns>A task representing the asynchronous unsubscription operation.</returns>
        Task UnsubscribeFromAllAsync();

        /// <summary>
        /// Updates the subscription for a specific action with the given market tickers and channel action.
        /// </summary>
        /// <param name="action">The subscription action to update.</param>
        /// <param name="marketTickers">Array of market tickers to subscribe to.</param>
        /// <param name="channelAction">The channel action for the subscription.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction);

        /// <summary>
        /// Resets the WebSocket connection.
        /// </summary>
        /// <returns>A task representing the asynchronous reset operation.</returns>
        Task ResetConnectionAsync();

        /// <summary>
        /// Shuts down the WebSocket client and all associated services gracefully.
        /// </summary>
        /// <returns>A task representing the asynchronous shutdown operation.</returns>
        Task ShutdownAsync();

        /// <summary>
        /// Waits for the order book queue to be empty for a specific market ticker within the specified timeout.
        /// </summary>
        /// <param name="marketTicker">The market ticker to wait for.</param>
        /// <param name="timeout">The maximum time to wait.</param>
        /// <returns>A task representing the asynchronous wait operation.</returns>
        Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout);

        /// <summary>
        /// Checks if the WebSocket is currently connected.
        /// </summary>
        /// <returns>True if connected, false otherwise.</returns>
        bool IsConnected();

        /// <summary>
        /// Determines whether a market is currently subscribed to a specific channel.
        /// </summary>
        /// <param name="marketTicker">The market ticker to check.</param>
        /// <param name="action">The channel action to check.</param>
        /// <returns>True if the market is subscribed to the channel, false otherwise.</returns>
        bool IsSubscribed(string marketTicker, string action);

        /// <summary>
        /// Determines whether a market can be subscribed to a specific channel.
        /// </summary>
        /// <param name="marketTicker">The market ticker to check.</param>
        /// <param name="channel">The channel name to check.</param>
        /// <returns>True if the market can be subscribed to the channel, false otherwise.</returns>
        bool CanSubscribeToMarket(string marketTicker, string channel);

        /// <summary>
        /// Sets the subscription state for a market and channel combination.
        /// </summary>
        /// <param name="marketTicker">The market ticker.</param>
        /// <param name="channel">The channel name.</param>
        /// <param name="state">The new subscription state.</param>
        void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state);

        /// <summary>
        /// Clears the order book queue for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to clear the queue for.</param>
        void ClearOrderBookQueue(string marketTicker);

        /// <summary>
        /// Disables automatic WebSocket reconnection.
        /// </summary>
        void DisableReconnect();

        /// <summary>
        /// Enables automatic WebSocket reconnection.
        /// </summary>
        void EnableReconnect();

        /// <summary>
        /// Generates the next unique message ID for WebSocket messages.
        /// </summary>
        /// <returns>The next message ID.</returns>
        int GenerateNextMessageId();

        /// <summary>
        /// Resets all event counts to zero.
        /// </summary>
        void ResetEventCounts();

        /// <summary>
        /// Gets the channel name for a given action.
        /// </summary>
        /// <param name="action">The action to get the channel name for.</param>
        /// <returns>The channel name corresponding to the action.</returns>
        string GetChannelName(string action);

        /// <summary>
        /// Sends a message through the WebSocket connection.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        Task SendMessageAsync(string message);

        /// <summary>
        /// Resubscribes to all channels, optionally forcing the resubscription.
        /// </summary>
        /// <param name="force">Whether to force the resubscription even if already subscribed.</param>
        /// <returns>A task representing the asynchronous resubscription operation.</returns>
        Task ResubscribeAsync(bool force = false);

        /// <summary>
        /// Gets the event counts (orderbook, trade, ticker) for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get event counts for.</param>
        /// <returns>A tuple containing the counts of orderbook events, trade events, and ticker events.</returns>
        (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker);
    }
}
