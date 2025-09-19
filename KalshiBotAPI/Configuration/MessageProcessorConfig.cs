using System.Text.Json.Serialization;

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
    [JsonRequired]
    public bool EnableMessageBatching { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size for message processing.
    /// Messages are processed in batches of this size when batching is enabled.
    /// </summary>
    [JsonRequired]
    public int MaxBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the batch processing interval in milliseconds.
    /// How often batched messages are processed when batching is enabled.
    /// </summary>
    [JsonRequired]
    public int BatchProcessingIntervalMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of processed sequence numbers to keep in memory.
    /// Used for message deduplication to prevent memory leaks.
    /// </summary>
    [JsonRequired]
    public int MaxSequenceNumbersToKeep { get; set; }

    /// <summary>
    /// Gets or sets whether performance metrics collection is enabled.
    /// When enabled, collects metrics on message processing rates and queue depths.
    /// </summary>
    [JsonRequired]
    public bool EnablePerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets the interval in milliseconds for logging performance metrics.
    /// </summary>
    [JsonRequired]
    public int PerformanceMetricsLogIntervalMs { get; set; }

    /// <summary>
    /// Gets or sets the threshold for duplicate message warnings.
    /// If duplicate messages exceed this count within a time window, warnings are logged.
    /// </summary>
    [JsonRequired]
    public int DuplicateMessageWarningThreshold { get; set; }

    /// <summary>
    /// Gets or sets the time window in milliseconds for duplicate message counting.
    /// </summary>
    [JsonRequired]
    public int DuplicateMessageTimeWindowMs { get; set; }

    /// <summary>
    /// Gets or sets whether to use sophisticated locking mechanisms for high-concurrency scenarios.
    /// When enabled, uses ReaderWriterLockSlim instead of simple lock for order book queue.
    /// </summary>
    [JsonRequired]
    public bool UseAdvancedLocking { get; set; }
}