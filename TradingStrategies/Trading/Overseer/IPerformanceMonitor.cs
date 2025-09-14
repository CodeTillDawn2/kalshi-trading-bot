namespace TradingStrategies.Trading.Overseer
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
    }
}