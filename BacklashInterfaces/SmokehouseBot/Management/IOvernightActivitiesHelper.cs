using Microsoft.Extensions.DependencyInjection;

namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Defines the contract for a helper service that performs maintenance and cleanup operations
    /// during overnight periods when trading activity is typically lower.
    /// </summary>
    public interface IOvernightActivitiesHelper
    {
        /// <summary>
        /// Runs all scheduled overnight maintenance tasks including data cleanup and system optimization.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency injection.</param>
        /// <param name="token">Cancellation token to allow graceful cancellation of operations.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RunOvernightTasks(IServiceScopeFactory scopeFactory, CancellationToken token);

        /// <summary>
        /// Deletes markets that have not been recorded or processed from the system.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency injection.</param>
        /// <param name="token">Cancellation token to allow graceful cancellation of operations.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteUnrecordedMarkets(IServiceScopeFactory scopeFactory, CancellationToken token);

        /// <summary>
        /// Deletes snapshots that have already been processed and are no longer needed.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency injection.</param>
        /// <param name="token">Cancellation token to allow graceful cancellation of operations.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteProcessedSnapshots(IServiceScopeFactory scopeFactory, CancellationToken token);
    }
}
