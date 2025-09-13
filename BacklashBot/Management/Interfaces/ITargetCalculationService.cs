using BacklashDTOs.Data;
using BacklashDTOs;

namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Interface for calculating optimal target number of markets to watch based on performance metrics.
    /// Provides methods to determine market count targets considering various queue limits and usage patterns.
    /// </summary>
    public interface ITargetCalculationService
    {
        /// <summary>
        /// Calculates the optimal target number of markets to watch based on current performance metrics.
        /// Uses multiple factors including usage targets, queue sizes, and notification patterns to
        /// determine the ideal market count. Returns the minimum valid target from all calculated options.
        /// </summary>
        /// <param name="metrics">Current performance metrics including usage, counts, and queue sizes</param>
        /// <param name="brain">Brain instance configuration containing usage limits and targets</param>
        /// <returns>The calculated target number of markets to watch</returns>
        int CalculateTarget(PerformanceMetrics metrics, BrainInstanceDTO brain);
    }
}