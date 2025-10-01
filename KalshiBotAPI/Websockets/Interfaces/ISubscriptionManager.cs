using BacklashInterfaces.Enums;
using System.Collections.Concurrent;
using System.Text.Json;

namespace KalshiBotAPI.WebSockets.Interfaces
{
    /// <summary>
    /// Defines the contract for managing WebSocket channel subscriptions to Kalshi's trading platform.
    /// Handles subscription lifecycle, state tracking, and market-specific channel management.
    /// </summary>
    public interface ISubscriptionManager
    {
        /// <summary>
        /// Starts the subscription manager and initializes necessary resources.
        /// </summary>
        /// <returns>A task representing the asynchronous start operation.</returns>
        Task StartAsync();

        /// <summary>
        /// Subscribes to a specific WebSocket channel for the given market tickers.
        /// </summary>
        /// <param name="action">The channel action (e.g., "orderbook", "ticker").</param>
        /// <param name="marketTickers">Array of market tickers to subscribe to.</param>
        /// <returns>A task representing the asynchronous subscription operation.</returns>
        Task SubscribeToChannelAsync(string action, string[] marketTickers);

        /// <summary>
        /// Subscribes to all enabled channels for the currently watched markets.
        /// </summary>
        /// <returns>A task representing the asynchronous subscription operation.</returns>
        Task SubscribeToWatchedMarketsAsync();

        /// <summary>
        /// Updates an existing subscription for a channel with new market tickers.
        /// </summary>
        /// <param name="action">The channel action to update.</param>
        /// <param name="marketTickers">Array of market tickers for the update.</param>
        /// <param name="channelAction">The type of update action to perform.</param>
        /// <returns>A task representing the asynchronous update operation.</returns>
        Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction);

        /// <summary>
        /// Unsubscribes from a specific WebSocket channel.
        /// </summary>
        /// <param name="action">The channel action to unsubscribe from.</param>
        /// <returns>A task representing the asynchronous unsubscription operation.</returns>
        Task UnsubscribeFromChannelAsync(string action);

        /// <summary>
        /// Unsubscribes from all WebSocket channels.
        /// </summary>
        /// <returns>A task representing the asynchronous unsubscription operation.</returns>
        Task UnsubscribeFromAllAsync();

        /// <summary>
        /// Resubscribes to all channels, optionally forcing a complete resubscription.
        /// </summary>
        /// <param name="force">True to force resubscription even if already subscribed.</param>
        /// <returns>A task representing the asynchronous resubscription operation.</returns>
        Task ResubscribeAsync(bool force = false);

        /// <summary>
        /// Gets the full channel name for a given action.
        /// </summary>
        /// <param name="action">The channel action.</param>
        /// <returns>The full channel name string.</returns>
        string GetChannelName(string action);

        /// <summary>
        /// Generates the next unique message ID for WebSocket messages.
        /// </summary>
        /// <returns>The next message ID.</returns>
        int GenerateNextMessageId();

        /// <summary>
        /// Determines whether a market is currently subscribed to a specific channel.
        /// </summary>
        /// <param name="marketTicker">The market ticker to check.</param>
        /// <param name="action">The channel action to check.</param>
        /// <returns>True if subscribed, false otherwise.</returns>
        bool IsSubscribed(string marketTicker, string action);

        /// <summary>
        /// Determines whether a market can be subscribed to a specific channel.
        /// </summary>
        /// <param name="marketTicker">The market ticker to check.</param>
        /// <param name="channel">The channel name to check.</param>
        /// <returns>True if subscription is allowed, false otherwise.</returns>
        bool CanSubscribeToMarket(string marketTicker, string channel);

        /// <summary>
        /// Sets the subscription state for a market and channel combination.
        /// </summary>
        /// <param name="marketTicker">The market ticker.</param>
        /// <param name="channel">The channel name.</param>
        /// <param name="state">The new subscription state.</param>
        void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state);

        /// <summary>
        /// Clears the order book message queue for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to clear the queue for.</param>
        void ClearOrderBookQueue(string marketTicker);

        /// <summary>
        /// Waits asynchronously for the order book queue to be empty for a specific market.
        /// </summary>
        /// <param name="marketTicker">The market ticker to wait for.</param>
        /// <param name="timeout">The maximum time to wait before timing out.</param>
        /// <returns>A task representing the asynchronous wait operation.</returns>
        Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout);

        /// <summary>
        /// Gets or sets the collection of market tickers that are being watched.
        /// Markets in this collection will receive WebSocket subscriptions for enabled channels.
        /// </summary>
        HashSet<string> WatchedMarkets { get; set; }

        /// <summary>
        /// Gets the current event counts for different message types processed.
        /// </summary>
        ConcurrentDictionary<string, long> EventCounts { get; }

        /// <summary>
        /// Gets the current count of the subscription update semaphore.
        /// </summary>
        int SubscriptionUpdateSemaphoreCount { get; }

        /// <summary>
        /// Gets the current count of the channel subscription semaphore.
        /// </summary>
        int ChannelSubscriptionSemaphoreCount { get; }

        /// <summary>
        /// Gets the count of queued subscription updates waiting to be processed.
        /// </summary>
        int QueuedSubscriptionUpdatesCount { get; }

        /// <summary>
        /// Resets all event count counters to zero.
        /// </summary>
        void ResetEventCounts();

        /// <summary>
        /// Gets the count of different event types processed for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get event counts for.</param>
        /// <returns>A tuple containing counts for orderbook, trade, and ticker events.</returns>
        (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker);

        /// <summary>
        /// Updates the subscription state based on a received confirmation message.
        /// </summary>
        /// <param name="sid">The subscription ID from the confirmation.</param>
        /// <param name="channel">The channel name.</param>
        /// <returns>A task representing the asynchronous update operation.</returns>
        Task UpdateSubscriptionStateFromConfirmationAsync(int sid, string channel);

        /// <summary>
        /// Removes a pending confirmation by its ID.
        /// </summary>
        /// <param name="id">The confirmation ID to remove.</param>
        /// <returns>True if removed, false if not found.</returns>
        bool RemovePendingConfirmation(int id);

        /// <summary>
        /// Gets the details of a pending confirmation by its ID.
        /// </summary>
        /// <param name="id">The confirmation ID to retrieve.</param>
        /// <returns>A tuple with channel and market tickers, or null if not found.</returns>
        (string Channel, string[] MarketTickers)? GetPendingConfirm(int id);

        /// <summary>
        /// Event raised when WebSocket health becomes unhealthy for specific markets.
        /// </summary>
        event EventHandler<string[]>? MarketWebSocketUnhealthy;

        /// <summary>
        /// Event raised when WebSocket health is restored for specific markets.
        /// </summary>
        event EventHandler<string[]>? MarketWebSocketHealthy;

        /// <summary>
        /// Raises the MarketWebSocketUnhealthy event for the specified markets.
        /// </summary>
        /// <param name="markets">Array of market tickers that have unhealthy WebSocket connections.</param>
        void RaiseMarketWebSocketUnhealthy(string[] markets);

        /// <summary>
        /// Raises the MarketWebSocketHealthy event for the specified markets.
        /// </summary>
        /// <param name="markets">Array of market tickers that have restored WebSocket connections.</param>
        void RaiseMarketWebSocketHealthy(string[] markets);

        /// <summary>
        /// Records that a message was received on the specified channel.
        /// Used for stale subscription detection.
        /// </summary>
        /// <param name="channel">The channel name where the message was received.</param>
        void RecordChannelActivity(string channel);

        /// <summary>
        /// Enqueues an order book message for processing.
        /// </summary>
        /// <param name="sid">The subscription ID.</param>
        /// <param name="data">The message data.</param>
        /// <param name="offerType">The offer type.</param>
        /// <param name="seq">The sequence number.</param>
        void EnqueueOrderBookMessage(int sid, JsonElement data, string offerType, long seq);

        /// <summary>
        /// Enqueues an ok message for processing.
        /// </summary>
        /// <param name="sid">The subscription ID.</param>
        /// <param name="data">The message data.</param>
        /// <param name="seq">The sequence number.</param>
        void EnqueueOkMessage(int sid, JsonElement data, long seq);

        /// <summary>
        /// Handles WebSocket disconnection by clearing local subscription state.
        /// This ensures clean reconnection without stale state assumptions.
        /// </summary>
        void HandleDisconnection();
    }
}
