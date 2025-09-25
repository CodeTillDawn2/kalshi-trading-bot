using BacklashDTOs;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages order book data and operations,
    /// including WebSocket event handling, queue management, and synchronization.
    /// </summary>
    public interface IOrderBookService
    {
        /// <summary>
        /// Occurs when the order book is updated for a specific market ticker.
        /// </summary>
        event EventHandler<string> OrderBookUpdated;

        /// <summary>
        /// Occurs when a market is determined to be invalid.
        /// </summary>
        event EventHandler<string> MarketInvalid;

        /// <summary>
        /// Configures WebSocket event handlers for real-time order book updates.
        /// </summary>
        void ConfigureWebSocketEventHandlers();

        /// <summary>
        /// Gets the current order book data for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        /// <returns>A list of order book data entries.</returns>
        List<OrderbookData> GetCurrentOrderBook(string marketTicker);

        /// <summary>
        /// Synchronizes the order book data for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to synchronize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SyncOrderBookAsync(string marketTicker);

        /// <summary>
        /// Clears the queue for the specified market asynchronously.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        void ClearQueueForMarketAsync(string marketTicker);

        /// <summary>
        /// Starts the order book services asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous start operation.</returns>
        Task StartServicesAsync();

        /// <summary>
        /// Determines whether the event queue is under the specified limit.
        /// </summary>
        /// <param name="limit">The maximum allowed queue size.</param>
        /// <returns><c>true</c> if the queue is under the limit; otherwise, <c>false</c>.</returns>
        bool IsEventQueueUnderLimit(int limit);

        /// <summary>
        /// Stops the order book services asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        Task StopServicesAsync();

        /// <summary>
        /// Gets the current counts for event, ticker, and notification queues.
        /// </summary>
        /// <returns>A tuple containing the counts for each queue type.</returns>
        (int EventQueueCount, int TickerQueueCount, int NotificationQueueCount) GetQueueCounts();

        /// <summary>
        /// Gets performance metrics for event queue processing operations.
        /// Returns the average processing time and total operations count.
        /// </summary>
        /// <returns>A tuple containing average processing time in milliseconds and total operations count.</returns>
        (double AverageProcessingTimeMs, int TotalOperations) GetEventQueueProcessingMetrics();

        /// <summary>
        /// Gets performance metrics for ticker queue processing operations.
        /// Returns the average processing time and total operations count.
        /// </summary>
        /// <returns>A tuple containing average processing time in milliseconds and total operations count.</returns>
        (double AverageProcessingTimeMs, int TotalOperations) GetTickerQueueProcessingMetrics();

        /// <summary>
        /// Gets performance metrics for notification queue processing operations.
        /// Returns the average processing time and total operations count.
        /// </summary>
        /// <returns>A tuple containing average processing time in milliseconds and total operations count.</returns>
        (double AverageProcessingTimeMs, int TotalOperations) GetNotificationQueueProcessingMetrics();

        /// <summary>
        /// Gets performance metrics for market lock wait times.
        /// Returns the average wait time and total operations count for the specified market.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get metrics for.</param>
        /// <returns>A tuple containing average wait time in milliseconds and total operations count.</returns>
        (double AverageWaitTimeMs, int TotalOperations) GetMarketLockWaitMetrics(string marketTicker);
    }
}
