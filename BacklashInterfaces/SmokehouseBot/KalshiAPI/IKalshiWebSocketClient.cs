using BacklashDTOs;
using BacklashInterfaces.Enums;
using System.Collections.Concurrent;

namespace KalshiBotAPI.WebSockets.Interfaces
{
    public interface IKalshiWebSocketClient
    {
        event EventHandler<OrderBookEventArgs> OrderBookReceived;
        event EventHandler<TickerEventArgs> TickerReceived;
        event EventHandler<TradeEventArgs> TradeReceived;
        event EventHandler<FillEventArgs> FillReceived;
        event EventHandler<MarketLifecycleEventArgs> MarketLifecycleReceived;
        event EventHandler<EventLifecycleEventArgs> EventLifecycleReceived;
        event EventHandler<DateTime> MessageReceived;

        bool IsTradingActive { get; set; }
        ConcurrentDictionary<string, long> EventCounts { get; }
        int ConnectSemaphoreCount { get; }
        int SubscriptionUpdateSemaphoreCount { get; }
        int ChannelSubscriptionSemaphoreCount { get; }
        int QueuedSubscriptionUpdatesCount { get; }
        int OrderBookMessageQueueCount { get; }
        int PendingConfirmsCount { get; }
        HashSet<string> WatchedMarkets { get; set; }

        Task ConnectAsync(int retryCount = 0);
        Task SubscribeToWatchedMarketsAsync();
        Task SubscribeToChannelAsync(string action, string[] marketTickers);
        Task UnsubscribeFromChannelAsync(string action);
        Task UnsubscribeFromAllAsync();
        Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction);
        Task ResetConnectionAsync();
        Task StopServicesAsync();
        Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout);
        bool IsConnected();
        bool IsSubscribed(string marketTicker, string action);
        bool CanSubscribe(string marketTicker, string channel);
        void UpdateSubscriptionState(string marketTicker, string channel, SubscriptionState state);
        void ClearOrderBookQueueForMarket(string marketTicker);
        void DisableReconnect();
        void EnableReconnect();
        int GetNextMessageId();
        void ResetEventCounts();
        string GetChannelName(string action);
        Task SendMessageAsync(string message);
        Task ResubscribeAsync(bool force = false);

        (int orderbookEvents, int tradeEvents, int tickerEvents) ReturnWebSocketCountsByMarket(string marketTicker);
    }
}
