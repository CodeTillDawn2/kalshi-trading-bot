using System.ComponentModel.DataAnnotations;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for SubscriptionManager operations.
/// </summary>
public class SubscriptionManagerConfig
{
    /// <summary>
    /// The configuration section name for SubscriptionManagerConfig.
    /// </summary>
    public const string SectionName = "Websockets:SubscriptionManager";

    /// <summary>
    /// Gets or sets whether SubscriptionManager performance metrics collection is enabled.
    /// </summary>
    [Required(ErrorMessage = "The 'EnableSubscriptionManagerMetrics' is missing in the configuration.")]
    public bool EnableSubscriptionManagerMetrics { get; set; }

    /// <summary>
    /// Gets or sets the subscription timeout in milliseconds.
    /// </summary>
    [Required(ErrorMessage = "The 'SubscriptionTimeoutMs' is missing in the configuration.")]
    public int SubscriptionTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the confirmation timeout in seconds.
    /// </summary>
    [Required(ErrorMessage = "The 'ConfirmationTimeoutSeconds' is missing in the configuration.")]
    public int ConfirmationTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the retry delay in milliseconds.
    /// </summary>
    [Required(ErrorMessage = "The 'RetryDelayMs' is missing in the configuration.")]
    public int SubscriptionManagerRetryDelayMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum queue size.
    /// </summary>
    [Required(ErrorMessage = "The 'MaxQueueSize' is missing in the configuration.")]
    public int MaxQueueSize { get; set; }

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    [Required(ErrorMessage = "The 'BatchSize' is missing in the configuration.")]
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the health check interval in milliseconds.
    /// </summary>
    [Required(ErrorMessage = "The 'HealthCheckIntervalMs' is missing in the configuration.")]
    public int HealthCheckIntervalMs { get; set; }

    /// <summary>
    /// Gets or sets the timeout in seconds for waiting for sequence gaps to be filled before resetting subscriptions.
    /// </summary>
    [Required(ErrorMessage = "The 'SequenceGapTimeoutSeconds' is missing in the configuration.")]
    public int SequenceGapTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the interval in milliseconds between processing batches of subscription requests.
    /// </summary>
    [Required(ErrorMessage = "The 'SubscriptionBatchIntervalMs' is missing in the configuration.")]
    public int SubscriptionBatchIntervalMs { get; set; }
}
