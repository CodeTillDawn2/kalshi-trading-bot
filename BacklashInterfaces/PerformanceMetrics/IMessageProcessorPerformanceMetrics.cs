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
        /// Posts message processing performance metrics from MessageProcessor.
        /// </summary>
        /// <param name="totalMessagesProcessed">Total number of messages processed since last reset.</param>
        /// <param name="totalProcessingTimeMs">Total processing time in milliseconds since last reset.</param>
        /// <param name="averageProcessingTimeMs">Average processing time per message in milliseconds.</param>
        /// <param name="messagesPerSecond">Current messages per second rate.</param>
        /// <param name="orderBookQueueDepth">Current depth of the order book update queue.</param>
        /// <remarks>
        /// This method is called by MessageProcessor to post comprehensive performance metrics
        /// for monitoring message throughput, latency, and queue utilization.
        /// </remarks>
        void PostMessageProcessingMetrics(long totalMessagesProcessed, long totalProcessingTimeMs,
            double averageProcessingTimeMs, double messagesPerSecond, int orderBookQueueDepth);

        /// <summary>
        /// Posts duplicate message detection metrics from MessageProcessor.
        /// </summary>
        /// <param name="duplicateMessageCount">Total number of duplicate messages detected.</param>
        /// <param name="duplicatesInWindow">Number of duplicates detected in the current time window.</param>
        /// <param name="lastDuplicateWarningTime">Timestamp of the last duplicate message warning.</param>
        /// <remarks>
        /// This method is called by MessageProcessor to post metrics related to duplicate message
        /// detection and warnings for monitoring message stream quality.
        /// </remarks>
        void PostDuplicateMessageMetrics(int duplicateMessageCount, int duplicatesInWindow, DateTime lastDuplicateWarningTime);

        /// <summary>
        /// Posts message type distribution metrics from MessageProcessor.
        /// </summary>
        /// <param name="messageTypeCounts">Dictionary containing counts for each message type processed.</param>
        /// <remarks>
        /// This method is called by MessageProcessor to post the distribution of different
        /// message types (OrderBook, Ticker, Trade, etc.) for monitoring message patterns.
        /// </remarks>
        void PostMessageTypeMetrics(IReadOnlyDictionary<string, long> messageTypeCounts);

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
