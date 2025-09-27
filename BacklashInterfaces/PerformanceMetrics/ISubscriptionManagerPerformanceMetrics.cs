namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for posting SubscriptionManager performance metrics to central monitoring services.
    /// This interface provides a standardized way to collect and aggregate performance data
    /// from WebSocket subscription operations and lock contention monitoring.
    /// </summary>
    public interface ISubscriptionManagerPerformanceMetrics
    {
       
        /// <summary>
        /// Gets the current operation performance metrics.
        /// </summary>
        /// <returns>Dictionary containing operation names and their performance statistics.</returns>
        IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)> GetOperationMetrics();

        /// <summary>
        /// Gets the current lock contention metrics.
        /// </summary>
        /// <returns>Dictionary containing lock names and their contention statistics.</returns>
        IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)> GetLockContentionMetrics();

        /// <summary>
        /// Resets all SubscriptionManager performance metrics.
        /// </summary>
        void ResetSubscriptionManagerMetrics();
    }
}
