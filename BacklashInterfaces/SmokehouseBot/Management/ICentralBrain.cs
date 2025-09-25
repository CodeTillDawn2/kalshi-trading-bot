using Microsoft.Extensions.Hosting;

namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Defines the contract for the central brain component that manages the overall lifecycle
    /// and coordination of the trading bot system, including startup, shutdown, and service management.
    /// </summary>
    public interface ICentralBrain : IDisposable, IHostedService
    {
        /// <summary>
        /// Gets a value indicating whether the services are currently stopped.
        /// </summary>
        /// <value><c>true</c> if services are stopped; otherwise, <c>false</c>.</value>
        bool IsServicesStopped { get; }

        /// <summary>
        /// Gets a value indicating whether the system is currently starting up.
        /// </summary>
        /// <value><c>true</c> if starting up; otherwise, <c>false</c>.</value>
        bool IsStartingUp { get; }

        /// <summary>
        /// Gets a value indicating whether the system is currently shutting down.
        /// </summary>
        /// <value><c>true</c> if shutting down; otherwise, <c>false</c>.</value>
        bool IsShuttingDown { get; }

        /// <summary>
        /// Starts the dashboard and initializes the central brain operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartDashboard();

        /// <summary>
        /// Shuts down the dashboard and stops all central brain operations asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous shutdown operation.</returns>
        Task ShutdownDashboardAsync();
    }
}
