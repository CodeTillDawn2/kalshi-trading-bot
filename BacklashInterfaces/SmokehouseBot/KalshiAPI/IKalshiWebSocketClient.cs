using BacklashDTOs;
using BacklashInterfaces.Enums;
using System.Collections.Concurrent;

namespace KalshiBotAPI.WebSockets.Interfaces
{
/// <summary>IKalshiWebSocketClient</summary>
/// <summary>IKalshiWebSocketClient</summary>
    public interface IKalshiWebSocketClient
/// <summary>Gets or sets the TickerReceived.</summary>
/// <summary>Gets or sets the OrderBookReceived.</summary>
    {
/// <summary>Gets or sets the MarketLifecycleReceived.</summary>
/// <summary>Gets or sets the TradeReceived.</summary>
        event EventHandler<OrderBookEventArgs> OrderBookReceived;
/// <summary>Gets or sets the MarketLifecycleReceived.</summary>
        event EventHandler<TickerEventArgs> TickerReceived;
/// <summary>Gets or sets the EventCounts.</summary>
/// <summary>Gets or sets the MessageReceived.</summary>
        event EventHandler<TradeEventArgs> TradeReceived;
/// <summary>Gets or sets the ChannelSubscriptionSemaphoreCount.</summary>
/// <summary>Gets or sets the IsTradingActive.</summary>
        event EventHandler<FillEventArgs> FillReceived;
/// <summary>Gets or sets the PendingConfirmsCount.</summary>
/// <summary>Gets or sets the ConnectSemaphoreCount.</summary>
        event EventHandler<MarketLifecycleEventArgs> MarketLifecycleReceived;
/// <summary>ConnectAsync</summary>
/// <summary>Gets or sets the ChannelSubscriptionSemaphoreCount.</summary>
        event EventHandler<EventLifecycleEventArgs> EventLifecycleReceived;
/// <summary>UnsubscribeFromChannelAsync</summary>
/// <summary>Gets or sets the OrderBookMessageQueueCount.</summary>
        event EventHandler<DateTime> MessageReceived;
/// <summary>Gets or sets the WatchedMarkets.</summary>

/// <summary>WaitForEmptyOrderBookQueueAsync</summary>
/// <summary>ConnectAsync</summary>
        bool IsTradingActive { get; set; }
/// <summary>CanSubscribeToMarket</summary>
/// <summary>SubscribeToChannelAsync</summary>
        ConcurrentDictionary<string, long> EventCounts { get; }
/// <summary>DisableReconnect</summary>
/// <summary>UnsubscribeFromAllAsync</summary>
        int ConnectSemaphoreCount { get; }
/// <summary>ResetEventCounts</summary>
/// <summary>ResetConnectionAsync</summary>
        int SubscriptionUpdateSemaphoreCount { get; }
/// <summary>ResubscribeAsync</summary>
/// <summary>WaitForEmptyOrderBookQueueAsync</summary>
        int ChannelSubscriptionSemaphoreCount { get; }
/// <summary>IsSubscribed</summary>
        int QueuedSubscriptionUpdatesCount { get; }
/// <summary>SetSubscriptionState</summary>
        int OrderBookMessageQueueCount { get; }
/// <summary>DisableReconnect</summary>
        int PendingConfirmsCount { get; }
/// <summary>GenerateNextMessageId</summary>
        HashSet<string> WatchedMarkets { get; set; }
/// <summary>GetChannelName</summary>

/// <summary>ResubscribeAsync</summary>
        Task ConnectAsync(int retryCount = 0);
/// <summary>GetEventCountsByMarket</summary>
        Task SubscribeToWatchedMarketsAsync();
        Task SubscribeToChannelAsync(string action, string[] marketTickers);
        Task UnsubscribeFromChannelAsync(string action);
        Task UnsubscribeFromAllAsync();
        Task UpdateSubscriptionAsync(string action, string[] marketTickers, string channelAction);
        Task ResetConnectionAsync();
        Task ShutdownAsync();
        Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout);
        bool IsConnected();
        bool IsSubscribed(string marketTicker, string action);
        bool CanSubscribeToMarket(string marketTicker, string channel);
        void SetSubscriptionState(string marketTicker, string channel, SubscriptionState state);
        void ClearOrderBookQueue(string marketTicker);
        void DisableReconnect();
        void EnableReconnect();
        int GenerateNextMessageId();
        void ResetEventCounts();
        string GetChannelName(string action);
        Task SendMessageAsync(string message);
        Task ResubscribeAsync(bool force = false);

        (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker);
    }
}
