using BacklashDTOs;

namespace BacklashBot.Services.Interfaces
{
    public interface IOrderBookService
    {
        event EventHandler<string> OrderBookUpdated;
        void ConfigureWebSocketEventHandlers();
        List<OrderbookData> GetCurrentOrderBook(string marketTicker);
        Task SyncOrderBookAsync(string marketTicker);
        void ClearQueueForMarketAsync(string marketTicker);
        Task StartServicesAsync();

        bool IsEventQueueUnderLimit(int limit);

        Task StopServicesAsync();

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
