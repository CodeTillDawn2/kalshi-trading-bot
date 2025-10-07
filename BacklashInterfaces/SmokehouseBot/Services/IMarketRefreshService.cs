namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles market data refresh operations,
    /// including periodic updates, immediate refreshes, and performance monitoring.
    /// </summary>
    public interface IMarketRefreshService
    {
        /// <summary>
        /// Executes the market refresh services asynchronously with the provided stopping token.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to stop the service execution.</param>
        void ExecuteServicesAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Triggers an immediate refresh for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to refresh immediately.</param>
        /// <returns>A task representing the asynchronous refresh operation.</returns>
        Task TriggerImmediateRefreshAsync(string marketTicker);

        /// <summary>
        /// Gets a value indicating whether the refresh service is currently running.
        /// </summary>
        /// <returns><c>true</c> if the service is running; otherwise, <c>false</c>.</returns>
        bool IsRunning();

        /// <summary>
        /// Stops the market refresh service asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the stop operation.</param>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        Task StopAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the duration of the last work cycle.
        /// </summary>
        TimeSpan LastWorkDuration { get; }

        /// <summary>
        /// Gets the number of markets processed in the last work cycle.
        /// </summary>
        int LastWorkMarketCount { get; }

        /// <summary>
        /// Gets the total number of refresh operations performed.
        /// </summary>
        long TotalRefreshOperations { get; }

        /// <summary>
        /// Gets the average time spent refreshing each market.
        /// </summary>
        TimeSpan AverageRefreshTimePerMarket { get; }

        /// <summary>
        /// Gets the number of markets refreshed in the last cycle.
        /// </summary>
        int LastRefreshCount { get; }

        /// <summary>
        /// Gets the CPU time used in the last refresh operation.
        /// </summary>
        TimeSpan LastCpuTime { get; }

        /// <summary>
        /// Gets the memory usage during the last refresh operation.
        /// </summary>
        long LastMemoryUsage { get; }

        /// <summary>
        /// Gets the refresh throughput (operations per unit time).
        /// </summary>
        double RefreshThroughput { get; }
    }
}
