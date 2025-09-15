namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for receiving performance metrics from SqlDataService.
    /// Classes implementing this interface will automatically receive performance data
    /// from the SqlDataService when metrics collection is enabled.
    /// </summary>
    public interface ISqlDataServicePerformanceMetrics
    {
        /// <summary>
        /// Receives throughput metrics from SqlDataService.
        /// </summary>
        /// <param name="operationsPerSecond">Current operations per second rate.</param>
        /// <param name="totalProcessed">Total operations processed successfully.</param>
        /// <param name="totalFailed">Total operations that failed.</param>
        void ReceiveThroughputMetrics(double operationsPerSecond, long totalProcessed, long totalFailed);

        /// <summary>
        /// Receives latency metrics from SqlDataService.
        /// </summary>
        /// <param name="averageLatencyMs">Average latency in milliseconds for processed operations.</param>
        /// <param name="sampleCount">Number of latency samples collected.</param>
        void ReceiveLatencyMetrics(double averageLatencyMs, long sampleCount);

        /// <summary>
        /// Receives resource utilization metrics from SqlDataService.
        /// </summary>
        /// <param name="cpuUsagePercent">Current CPU usage percentage.</param>
        /// <param name="memoryUsageMB">Current memory usage in MB.</param>
        void ReceiveResourceMetrics(double cpuUsagePercent, double memoryUsageMB);

        /// <summary>
        /// Receives queue depth metrics from SqlDataService.
        /// </summary>
        /// <param name="orderBookQueueDepth">Current depth of order book queue.</param>
        /// <param name="tradeQueueDepth">Current depth of trade queue.</param>
        /// <param name="fillQueueDepth">Current depth of fill queue.</param>
        /// <param name="eventLifecycleQueueDepth">Current depth of event lifecycle queue.</param>
        /// <param name="marketLifecycleQueueDepth">Current depth of market lifecycle queue.</param>
        /// <param name="totalQueuedOperations">Total operations across all queues.</param>
        void ReceiveQueueMetrics(int orderBookQueueDepth, int tradeQueueDepth, int fillQueueDepth,
                                int eventLifecycleQueueDepth, int marketLifecycleQueueDepth, int totalQueuedOperations);

        /// <summary>
        /// Receives success rate metrics from SqlDataService.
        /// </summary>
        /// <param name="successRatePercent">Success rate as a percentage (0-100).</param>
        void ReceiveSuccessRateMetrics(double successRatePercent);
    }
}