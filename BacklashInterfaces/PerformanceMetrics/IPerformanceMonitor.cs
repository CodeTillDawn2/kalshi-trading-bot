namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for recording performance metrics.
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// Records the execution time for a specific method or operation.
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        void RecordExecutionTime(string methodName, long milliseconds);

        /// <summary>
        /// Records simulation performance metrics from StrategySimulation.
        /// </summary>
        /// <param name="simulationName">The name of the simulation.</param>
        /// <param name="metrics">The detailed metrics dictionary from StrategySimulation.GetDetailedPerformanceMetrics().</param>
        void RecordSimulationMetrics(string simulationName, Dictionary<string, object> metrics);

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