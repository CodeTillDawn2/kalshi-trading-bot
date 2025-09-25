using BacklashDTOs;

namespace KalshiBotAPI.WebSockets.Interfaces
{
    /// <summary>
    /// Defines the contract for processing WebSocket messages from Kalshi's trading platform.
    /// Handles message routing, event raising, and queue management for real-time market data.
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// Event raised when order book data is received from the WebSocket.
        /// </summary>
        event EventHandler<OrderBookEventArgs> OrderBookReceived;

        /// <summary>
        /// Event raised when ticker data is received from the WebSocket.
        /// </summary>
        event EventHandler<TickerEventArgs> TickerReceived;

        /// <summary>
        /// Event raised when trade data is received from the WebSocket.
        /// </summary>
        event EventHandler<TradeEventArgs> TradeReceived;

        /// <summary>
        /// Event raised when fill data is received from the WebSocket.
        /// </summary>
        event EventHandler<FillEventArgs> FillReceived;

        /// <summary>
        /// Event raised when market lifecycle events are received from the WebSocket.
        /// </summary>
        event EventHandler<MarketLifecycleEventArgs> MarketLifecycleReceived;

        /// <summary>
        /// Event raised when event lifecycle events are received from the WebSocket.
        /// </summary>
        event EventHandler<EventLifecycleEventArgs> EventLifecycleReceived;

        /// <summary>
        /// Event raised when any WebSocket message is received, providing the timestamp.
        /// </summary>
        event EventHandler<DateTime> MessageReceived;

        /// <summary>
        /// Processes an incoming WebSocket message asynchronously.
        /// </summary>
        /// <param name="message">The JSON message string to process.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        Task ProcessMessageAsync(string message);

        /// <summary>
        /// Starts the message processing operations.
        /// </summary>
        /// <returns>A task representing the asynchronous start operation.</returns>
        Task StartProcessingAsync();

        /// <summary>
        /// Stops the message processing operations.
        /// </summary>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        Task StopProcessingAsync();

        /// <summary>
        /// Gets the current count of order book messages in the processing queue.
        /// </summary>
        int OrderBookMessageQueueCount { get; }

        /// <summary>
        /// Gets the count of pending subscription confirmations.
        /// </summary>
        int PendingConfirmsCount { get; }

        /// <summary>
        /// Gets the last sequence number processed from WebSocket messages.
        /// Used for tracking message ordering and detecting missed messages.
        /// </summary>
        long LastSequenceNumber { get; }

        /// <summary>
        /// Resets all event count counters to zero.
        /// </summary>
        void ResetEventCounts();

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
        /// Gets the count of different event types processed for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get event counts for.</param>
        /// <returns>A tuple containing counts for orderbook, trade, and ticker events.</returns>
        (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker);

        /// <summary>
        /// Sets whether market data should be written to SQL database.
        /// </summary>
        /// <param name="writeToSQL">True to enable SQL writing, false to disable.</param>
        void SetWriteToSql(bool writeToSQL);
    }
}
