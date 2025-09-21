using BacklashDTOs;

namespace KalshiBotAPI.WebSockets.Interfaces
{
    /// <summary>IMessageProcessor</summary>
    /// <summary>IMessageProcessor</summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    public interface IMessageProcessor
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    /// <summary>
    /// </summary>
    {
        /// <summary>
        /// </summary>
        /// <summary>
        /// </summary>
        /// <summary>
        /// </summary>
        /// <summary>
        /// </summary>
        /// <summary>
        /// </summary>
        event EventHandler<OrderBookEventArgs> OrderBookReceived;
        /// <summary>
        /// </summary>
        event EventHandler<TickerEventArgs> TickerReceived;
        /// <summary>
        /// </summary>
        /// <summary>
        /// </summary>
        event EventHandler<TradeEventArgs> TradeReceived;
        /// <summary>
        /// </summary>
        event EventHandler<FillEventArgs> FillReceived;
        /// <summary>
        /// </summary>
        event EventHandler<MarketLifecycleEventArgs> MarketLifecycleReceived;
        /// <summary>
        /// </summary>
        event EventHandler<EventLifecycleEventArgs> EventLifecycleReceived;
        event EventHandler<DateTime> MessageReceived;

        Task ProcessMessageAsync(string message);
        Task StartProcessingAsync();
        Task StopProcessingAsync();
        int OrderBookMessageQueueCount { get; }
        int PendingConfirmsCount { get; }
        long LastSequenceNumber { get; }
        void ResetEventCounts();
        void ClearOrderBookQueue(string marketTicker);
        Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout);
        (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker);
        void SetWriteToSql(bool writeToSQL);
    }
}
