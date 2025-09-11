using BacklashInterfaces.Enums;
using System.Collections.Concurrent;

namespace KalshiBotAPI.WebSockets.Interfaces
{
    public interface ISubscriptionManager
    {
        Task StartAsync();
        Task SubscribeToChannelAsync(string action, string[] marketTickers);
        Task SubscribeToWatchedMarketsAsync();
        Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction);
        Task UnsubscribeFromChannelAsync(string action);
        Task UnsubscribeFromAllAsync();
        Task ResubscribeAsync(bool force = false);
        string GetChannelName(string action);
        int GenerateNextMessageId();
        bool IsSubscribed(string marketTicker, string action);
        bool CanSubscribeToMarket(string marketTicker, string channel);
        void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state);
        void ClearOrderBookQueue(string marketTicker);
        Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout);
        HashSet<string> WatchedMarkets { get; set; }
        ConcurrentDictionary<string, long> EventCounts { get; }
        int SubscriptionUpdateSemaphoreCount { get; }
        int ChannelSubscriptionSemaphoreCount { get; }
        int QueuedSubscriptionUpdatesCount { get; }
        void ResetEventCounts();
        (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker);
        Task UpdateSubscriptionStateFromConfirmationAsync(int sid, string channel);
        bool RemovePendingConfirmation(int id);
        (string Channel, string[] MarketTickers)? GetPendingConfirm(int id);
    }
}