namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that monitors WebSocket connections,
    /// handles connection management, and provides connection status information.
    /// </summary>
    public interface IWebSocketMonitorService
    {
        /// <summary>
        /// Starts the WebSocket monitoring services with the provided cancellation token.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop the service.</param>
        void StartServices(CancellationToken cancellationToken);

        /// <summary>
        /// Shuts down the WebSocket monitoring services asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the shutdown operation.</param>
        /// <returns>A task representing the asynchronous shutdown operation.</returns>
        Task ShutdownAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Triggers an immediate connection check asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous connection check operation.</returns>
        Task TriggerConnectionCheckAsync();

        /// <summary>
        /// Gets a value indicating whether the WebSocket is currently connected.
        /// </summary>
        /// <returns><c>true</c> if connected; otherwise, <c>false</c>.</returns>
        bool IsConnected();
    }
}
