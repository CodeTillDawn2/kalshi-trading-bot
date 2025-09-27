namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for posting MessageProcessor performance metrics to central monitoring services.
    /// This interface provides a standardized way to collect and aggregate performance data
    /// from WebSocket message processing operations including throughput, latency, and queue metrics.
    /// </summary>
    public interface IMessageProcessorPerformanceMetrics
    {
    
        /// <summary>
        /// Gets the current message processing performance metrics.
        /// </summary>
        /// <returns>Tuple containing current performance metrics.</returns>
        (long TotalMessagesProcessed, long TotalProcessingTimeMs, double AverageProcessingTimeMs,
         double MessagesPerSecond, int OrderBookQueueDepth) GetMessageProcessingMetrics();

        /// <summary>
        /// Gets the current duplicate message metrics.
        /// </summary>
        /// <returns>Tuple containing duplicate message statistics.</returns>
        (int DuplicateMessageCount, int DuplicatesInWindow, DateTime LastDuplicateWarningTime) GetDuplicateMessageMetrics();

        /// <summary>
        /// Gets the current message type distribution metrics.
        /// </summary>
        /// <returns>Dictionary containing message type counts.</returns>
        IReadOnlyDictionary<string, long> GetMessageTypeMetrics();

        /// <summary>
        /// Resets all MessageProcessor performance metrics.
        /// </summary>
        void ResetMessageProcessorMetrics();
    }
}
