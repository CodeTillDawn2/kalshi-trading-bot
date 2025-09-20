using System.Collections.Generic;

namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for accessing performance metrics from KalshiBotContext.
    /// This interface provides a standardized way to access database operation performance data.
    /// </summary>
    public interface IKalshiBotContextPerformanceMetrics
    {
        /// <summary>
        /// Gets the current database performance metrics.
        /// </summary>
        /// <returns>Dictionary containing operation names and their performance statistics.</returns>
        IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> GetPerformanceMetrics();

        /// <summary>
        /// Resets all performance metrics.
        /// </summary>
        void ResetPerformanceMetrics();
    }
}
