namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for recording performance metrics across the KalshiBot system.
    /// Provides standardized methods for tracking execution times and simulation metrics
    /// with mandatory enablement status to control when metrics are actually recorded.
    /// </summary>
    /// <remarks>
    /// All methods require an explicit metricsEnabled parameter to ensure granular control
    /// over metric collection. This prevents unnecessary metric collection when monitoring
    /// is disabled and provides clear visibility into which components are recording metrics.
    /// </remarks>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// Records the execution time for a specific method or operation with enablement status.
        /// </summary>
        /// <param name="methodName">The name of the method or operation being timed.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class/component.</param>
        /// <remarks>
        /// This method allows the calling class to pass its enablement status, enabling
        /// more granular control over metric collection. Metrics are only recorded when
        /// both the caller has metrics enabled AND the monitor implementation allows it.
        /// </remarks>
        void RecordExecutionTime(string methodName, long milliseconds, bool metricsEnabled);

        /// <summary>
        /// Records simulation performance metrics from StrategySimulation with enablement status.
        /// </summary>
        /// <param name="simulationName">The name of the simulation (e.g., "BollingerBreakout", "StrategySimulation").</param>
        /// <param name="metrics">The detailed metrics dictionary containing performance data such as execution times, memory usage, trade counts.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class/component.</param>
        /// <remarks>
        /// This method allows the calling class to pass its enablement status, enabling
        /// more granular control over metric collection. The metrics dictionary typically
        /// includes keys like "TotalExecutionTimeMs", "TotalItemsProcessed", "Timestamp", etc.
        /// </remarks>
        void RecordSimulationMetrics(string simulationName, Dictionary<string, object> metrics, bool metricsEnabled);

        /// <summary>
        /// Records comprehensive performance metrics.
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="totalExecutionTimeMs">Total time spent on execution.</param>
        /// <param name="totalItemsProcessed">Number of items processed.</param>
        /// <param name="totalItemsFound">Number of items found.</param>
        /// <param name="itemCheckTimes">Dictionary of item names to their processing times.</param>
        void RecordPerformanceMetrics(
            string methodName,
            long totalExecutionTimeMs,
            int totalItemsProcessed,
            int totalItemsFound,
            Dictionary<string, long>? itemCheckTimes = null);

        /// <summary>
        /// Records metrics for delayed API calls due to rate limiting.
        /// </summary>
        /// <param name="componentName">The name of the component (e.g., "MessageProcessor").</param>
        /// <param name="totalDelayedCalls">Total number of API calls that were delayed.</param>
        /// <param name="averageWaitTimeMs">Average wait time in milliseconds for delayed calls.</param>
        /// <param name="maxWaitTimeMs">Maximum wait time in milliseconds for any delayed call.</param>
        /// <param name="currentQueueDepth">Current number of items in the delay queue.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class/component.</param>
        void RecordDelayedApiCallMetrics(
            string componentName,
            long totalDelayedCalls,
            double averageWaitTimeMs,
            long maxWaitTimeMs,
            int currentQueueDepth,
            bool metricsEnabled);

    }
}
