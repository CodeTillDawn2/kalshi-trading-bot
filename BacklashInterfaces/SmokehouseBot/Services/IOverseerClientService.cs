namespace BacklashInterfaces.SmokehouseBot.Services
{
    /// <summary>
    /// Interface for overseeing client service operations in the trading bot system.
    /// </summary>
    public interface IOverseerClientService
    {
        /// <summary>
        /// Starts the overseer client service asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartAsync();

        /// <summary>
        /// Stops the overseer client service asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StopAsync();
    }
}
