using BacklashInterfaces.Enums;
using System.Collections.Concurrent;

namespace KalshiBotAPI.WebSockets.Interfaces
{
/// <summary>ISubscriptionManager</summary>
/// <summary>ISubscriptionManager</summary>
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
    public interface ISubscriptionManager
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
/// <summary>
/// </summary>
        Task StartAsync();
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        Task SubscribeToChannelAsync(string action, string[] marketTickers);
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        Task SubscribeToWatchedMarketsAsync();
/// <summary>
/// </summary>
        Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction);
        Task UnsubscribeFromChannelAsync(string action);
        Task UnsubscribeFromAllAsync();
        Task ResubscribeAsync(bool force = false);
        string GetChannelName(string action);
        int GenerateNextMessageId();
        bool IsSubscribed(string marketTicker, string action);
        bool CanSubscribeToMarket(string marketTicker, string channel);
        void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state);
/// <summary>
/// </summary>
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
