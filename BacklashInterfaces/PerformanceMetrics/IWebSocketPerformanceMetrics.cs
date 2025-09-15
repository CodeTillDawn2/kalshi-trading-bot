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
        /// Records WebSocket message processing performance.
        /// </summary>
        /// <param name="messageType">The type of WebSocket message (e.g., "WebSocketMessage").</param>
        /// <param name="processingTimeTicks">The processing time in stopwatch ticks.</param>
        /// <param name="messageCount">The number of messages processed.</param>
        /// <param name="bufferSizeBytes">The size of the message buffer in bytes.</param>
        void RecordWebSocketMessageProcessing(string messageType, long processingTimeTicks, int messageCount, long bufferSizeBytes);

        /// <summary>
        /// Records WebSocket connection performance.
        /// </summary>
        /// <param name="operation">The operation name (e.g., "Connect").</param>
        /// <param name="duration">The duration of the operation.</param>
        void RecordWebSocketOperation(string operation, TimeSpan duration);

        /// <summary>
        /// Records semaphore wait counts for WebSocket operations.
        /// </summary>
        /// <param name="operation">The operation name.</param>
        /// <param name="waitCount">The number of semaphore waits.</param>
        void RecordSemaphoreWait(string operation, int waitCount);

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