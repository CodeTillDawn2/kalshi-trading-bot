namespace BacklashInterfaces.SmokehouseBot.Services
{
    /// <summary>
    /// Defines the contract for a client service that manages communication and oversight with an Overseer server.
    /// This interface provides methods to start and stop the service, as well as check connection status.
    /// The service is responsible for maintaining a persistent connection to monitor and report on trading bot operations.
    /// </summary>
    public interface IOverseerClientService
    {
        /// <summary>
        /// Gets a value indicating whether the client service is currently connected to an Overseer server.
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        bool IsConnected { get; }

        /// <summary>
        /// Asynchronously starts the overseer client service, establishing connection to the Overseer server
        /// and beginning periodic status reporting and monitoring operations.
        /// </summary>
        /// <returns>A task representing the asynchronous start operation.</returns>
        Task StartAsync();

        /// <summary>
        /// Asynchronously stops the overseer client service, gracefully disconnecting from the Overseer server
        /// and halting all monitoring and reporting activities.
        /// </summary>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        Task StopAsync();
    }
}
