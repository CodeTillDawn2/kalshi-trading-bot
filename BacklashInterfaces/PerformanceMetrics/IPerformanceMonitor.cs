namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for recording performance metrics across the KalshiBot system.
    /// Provides standardized methods for tracking execution times and simulation metrics
    /// with configurable enablement status to control when metrics are actually recorded.
    /// </summary>
    /// <remarks>
    /// This interface supports two patterns for each metric type:
    /// 1. Basic methods without enablement status (for backward compatibility)
    /// 2. Overloaded methods with enablement status (recommended for new implementations)
    ///
    /// The enablement status parameter allows calling classes to pass their configuration
    /// state, ensuring metrics are only recorded when both the caller and monitor are enabled.
    /// This prevents unnecessary metric collection when monitoring is disabled.
    /// </remarks>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// Records the execution time for a specific method or operation.
        /// This is the basic method without enablement status for backward compatibility.
        /// </summary>
        /// <param name="methodName">The name of the method or operation being timed.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        /// <remarks>
        /// Use this method when enablement status is not available or for legacy code.
        /// The implementation should check its own enablement configuration.
        /// </remarks>
        void RecordExecutionTime(string methodName, long milliseconds);

        /// <summary>
        /// Records the execution time for a specific method or operation with enablement status.
        /// This is the preferred method for new implementations.
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
        /// Records simulation performance metrics from StrategySimulation.
        /// This is the basic method without enablement status for backward compatibility.
        /// </summary>
        /// <param name="simulationName">The name of the simulation (e.g., "BollingerBreakout", "StrategySimulation").</param>
        /// <param name="metrics">The detailed metrics dictionary containing performance data.</param>
        /// <remarks>
        /// Use this method when enablement status is not available or for legacy code.
        /// The implementation should check its own enablement configuration.
        /// </remarks>
        void RecordSimulationMetrics(string simulationName, Dictionary<string, object> metrics);

        /// <summary>
        /// Records simulation performance metrics from StrategySimulation with enablement status.
        /// This is the preferred method for new implementations.
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
    }
}