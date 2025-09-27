using System.Collections.Concurrent;

namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for recording and accessing WebSocket performance metrics.
    /// This interface provides a standardized way to track WebSocket connection,
    /// message processing, and subscription performance data.
    /// </summary>
    public interface IWebSocketPerformanceMetrics
    {
     

        /// <summary>
        /// Gets the average processing times for WebSocket messages.
        /// </summary>
        /// <returns>Dictionary of message types to average processing times in milliseconds.</returns>
        ConcurrentDictionary<string, double> GetAverageProcessingTimesMs();

        /// <summary>
        /// Gets the total buffer usage for WebSocket messages.
        /// </summary>
        /// <returns>Dictionary of message types to total buffer usage in bytes.</returns>
        ConcurrentDictionary<string, long> GetBufferUsageBytes();

        /// <summary>
        /// Gets the average times for WebSocket operations.
        /// </summary>
        /// <returns>Dictionary of operations to average times in milliseconds.</returns>
        ConcurrentDictionary<string, double> GetAsyncOperationTimesMs();

        /// <summary>
        /// Gets the semaphore wait counts for WebSocket operations.
        /// </summary>
        /// <returns>Dictionary of operations to wait counts.</returns>
        ConcurrentDictionary<string, int> GetSemaphoreWaitCounts();

        /// <summary>
        /// Resets all WebSocket performance metrics.
        /// </summary>
        void ResetWebSocketMetrics();

      
    }
}
