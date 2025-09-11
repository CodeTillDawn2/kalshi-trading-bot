using System.Net.WebSockets;

namespace KalshiBotAPI.WebSockets.Interfaces
{
    public interface IWebSocketConnectionManager
    {
        Task ConnectAsync(int retryCount = 0);
        Task ResetConnectionAsync();
        Task SendMessageAsync(string message);
        Task ReceiveAsync();
        bool IsConnected();
        void DisableReconnect();
        void EnableReconnect();
        Task StopAsync();
        ClientWebSocket? GetWebSocket();
        int ConnectSemaphoreCount { get; }
    }
}