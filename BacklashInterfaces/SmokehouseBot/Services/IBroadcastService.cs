namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles broadcasting operations
    /// within the trading bot system, including starting and stopping broadcast services.
    /// </summary>
    public interface IBroadcastService : IDisposable
    {
        /// <summary>
        /// Starts the broadcast services asynchronously, initializing all necessary
        /// broadcasting components and connections.
        /// </summary>
        /// <returns>A task representing the asynchronous start operation.</returns>
        Task StartServicesAsync();

        /// <summary>
        /// Stops the broadcast services asynchronously, gracefully shutting down
        /// all broadcasting operations and cleaning up resources.
        /// </summary>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        Task StopServicesAsync();
    }
}
