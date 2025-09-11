using BacklashDTOs;
using System.Collections.Concurrent;

namespace KalshiBotAPI.WebSockets.Interfaces
{
    public interface IMessageProcessor
    {
        event EventHandler<OrderBookEventArgs> OrderBookReceived;
        event EventHandler<TickerEventArgs> TickerReceived;
        event EventHandler<TradeEventArgs> TradeReceived;
        event EventHandler<FillEventArgs> FillReceived;
        event EventHandler<MarketLifecycleEventArgs> MarketLifecycleReceived;
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