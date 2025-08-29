using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using System;
using System.Threading.Tasks;

namespace KalshiBotOverseer.Interfaces
{
    public interface ISlimWebSocketClient
    {
        event EventHandler<FillEventArgs>? FillReceived;
        event EventHandler<MarketLifecycleEventArgs>? MarketLifecycleReceived;
        event EventHandler<EventLifecycleEventArgs>? EventLifecycleReceived;
        event EventHandler<DateTime>? MessageReceived;

        bool IsTradingActive { get; set; }

        int ConnectSemaphoreCount { get; }
        int ChannelSubscriptionSemaphoreCount { get; }
        int PendingConfirmsCount { get; }

        DateTime LastFillReceived { get; }
        DateTime LastLifecycleReceived { get; }

        Task StopServicesAsync();
        Task UnsubscribeFromChannelAsync(string action);
        int GetNextMessageId();
        bool IsSubscribed(string action);
        Task ConnectAsync(int retryCount = 0);
        void DisableReconnect();
        void EnableReconnect();
        Task ResetConnectionAsync();
        bool IsConnected();
        Task SubscribeToChannelAsync(string action);
        string GetChannelName(string action);
        Task SendMessageAsync(string message);
        Task UnsubscribeFromAllAsync();
    }
}