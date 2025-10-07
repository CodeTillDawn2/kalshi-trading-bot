using System.Net.WebSockets;

namespace KalshiBotAPI.WebSockets.Interfaces
{
    /// <summary>
    /// Defines the contract for managing WebSocket connections to Kalshi's trading platform.
    /// Handles connection establishment, message sending/receiving, and reconnection logic.
    /// </summary>
    public interface IWebSocketConnectionManager
    {
        /// <summary>
        /// Establishes a WebSocket connection to Kalshi's trading platform.
        /// </summary>
        /// <param name="retryCount">The number of retry attempts made so far.</param>
        /// <returns>A task representing the asynchronous connection operation.</returns>
        Task ConnectAsync(int retryCount = 0);

        /// <summary>
        /// Resets the current WebSocket connection, forcing a reconnection.
        /// </summary>
        /// <param name="isExchangeOutage">Whether this reset is due to an exchange-wide outage (default: false).</param>
        /// <returns>A task representing the asynchronous reset operation.</returns>
        Task ResetConnectionAsync(bool isExchangeOutage = false);

        /// <summary>
        /// Sends a message through the WebSocket connection.
        /// </summary>
        /// <param name="message">The message string to send.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        Task SendMessageAsync(string message);

        /// <summary>
        /// Starts receiving messages from the WebSocket connection.
        /// This typically runs in a continuous loop until the connection is closed.
        /// </summary>
        /// <returns>A task representing the asynchronous receive operation.</returns>
        Task ReceiveAsync();

        /// <summary>
        /// Determines whether the WebSocket is currently connected.
        /// </summary>
        /// <returns>True if connected, false otherwise.</returns>
        bool IsConnected();

        /// <summary>
        /// Disables automatic reconnection attempts.
        /// </summary>
        void DisableReconnect();

        /// <summary>
        /// Enables automatic reconnection attempts.
        /// </summary>
        void EnableReconnect();

        /// <summary>
        /// Stops the WebSocket connection and cleans up resources.
        /// </summary>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        Task StopAsync();

        /// <summary>
        /// Gets the underlying ClientWebSocket instance for direct access if needed.
        /// </summary>
        /// <returns>The ClientWebSocket instance, or null if not connected.</returns>
        ClientWebSocket? GetWebSocket();

        /// <summary>
        /// Gets the current count of the connection semaphore, indicating connection operation status.
        /// </summary>
        int ConnectSemaphoreCount { get; }

        /// <summary>
        /// Gets whether the WebSocket connection is currently experiencing an exchange-wide outage.
        /// When true, connection issues are due to exchange problems rather than individual market issues.
        /// </summary>
        bool IsExchangeOutage { get; }
    }
}
