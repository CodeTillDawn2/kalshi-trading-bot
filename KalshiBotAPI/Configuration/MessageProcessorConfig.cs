namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for MessageProcessor operations.
/// </summary>
public class MessageProcessorConfig
{
    /// <summary>
    /// Gets or sets whether message batching is enabled for high-volume scenarios.
    /// When enabled, messages are batched before processing to reduce event overhead.
    /// </summary>
    public bool EnableMessageBatching { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum batch size for message processing.
    /// Messages are processed in batches of this size when batching is enabled.
    /// </summary>
    public int MaxBatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the batch processing interval in milliseconds.
    /// How often batched messages are processed when batching is enabled.
    /// </summary>
    public int BatchProcessingIntervalMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of processed sequence numbers to keep in memory.
    /// Used for message deduplication to prevent memory leaks.
    /// </summary>
    public int MaxSequenceNumbersToKeep { get; set; } = 10000;

    /// <summary>
    /// Gets or sets whether performance metrics collection is enabled.
    /// When enabled, collects metrics on message processing rates and queue depths.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in milliseconds for logging performance metrics.
    /// </summary>
    public int PerformanceMetricsLogIntervalMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the threshold for duplicate message warnings.
    /// If duplicate messages exceed this count within a time window, warnings are logged.
    /// </summary>
    public int DuplicateMessageWarningThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the time window in milliseconds for duplicate message counting.
    /// </summary>
    public int DuplicateMessageTimeWindowMs { get; set; } = 60000;

    /// <summary>
    /// Gets or sets whether to use sophisticated locking mechanisms for high-concurrency scenarios.
    /// When enabled, uses ReaderWriterLockSlim instead of simple lock for order book queue.
    /// </summary>
    public bool UseAdvancedLocking { get; set; } = true;
}