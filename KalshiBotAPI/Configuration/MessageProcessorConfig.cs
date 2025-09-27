using System.ComponentModel.DataAnnotations;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for MessageProcessor operations.
/// </summary>
public class MessageProcessorConfig
{
    /// <summary>
    /// The configuration section name for MessageProcessorConfig.
    /// </summary>
    public const string SectionName = "Websockets:MessageProcessor";

    /// <summary>
    /// Gets or sets whether message batching is enabled for high-volume scenarios.
    /// When enabled, messages are batched before processing to reduce event overhead.
    /// </summary>
    [Required(ErrorMessage = "The 'EnableMessageBatching' is missing in the configuration.")]
    public bool EnableMessageBatching { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size for message processing.
    /// Messages are processed in batches of this size when batching is enabled.
    /// </summary>
    [Required(ErrorMessage = "The 'MaxBatchSize' is missing in the configuration.")]
    public int MaxBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the batch processing interval in milliseconds.
    /// How often batched messages are processed when batching is enabled.
    /// </summary>
    [Required(ErrorMessage = "The 'BatchProcessingIntervalMs' is missing in the configuration.")]
    public int BatchProcessingIntervalMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of processed sequence numbers to keep in memory.
    /// Used for message deduplication to prevent memory leaks.
    /// </summary>
    [Required(ErrorMessage = "The 'MaxSequenceNumbersToKeep' is missing in the configuration.")]
    public int MaxSequenceNumbersToKeep { get; set; }

    /// <summary>
    /// Gets or sets whether performance metrics collection is enabled.
    /// When enabled, collects metrics on message processing rates and queue depths.
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets the interval in milliseconds for logging performance metrics.
    /// </summary>
    [Required(ErrorMessage = "The 'PerformanceMetricsLogIntervalMs' is missing in the configuration.")]
    public int PerformanceMetricsLogIntervalMs { get; set; }


    /// <summary>
    /// Gets or sets whether to use sophisticated locking mechanisms for high-concurrency scenarios.
    /// When enabled, uses ReaderWriterLockSlim instead of simple lock for order book queue.
    /// </summary>
    [Required(ErrorMessage = "The 'UseAdvancedLocking' is missing in the configuration.")]
    public bool UseAdvancedLocking { get; set; }
}
