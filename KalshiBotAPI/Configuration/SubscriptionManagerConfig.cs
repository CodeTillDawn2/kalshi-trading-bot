namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for SubscriptionManager operations.
/// </summary>
public class SubscriptionManagerConfig
{
    /// <summary>
    /// Gets or sets whether SubscriptionManager performance metrics collection is enabled.
    /// </summary>
    public bool EnableSubscriptionManagerMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the subscription timeout in milliseconds.
    /// </summary>
    public int SubscriptionTimeoutMs { get; set; } = 60000;

    /// <summary>
    /// Gets or sets the confirmation timeout in seconds.
    /// </summary>
    public int ConfirmationTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the retry delay in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum queue size.
    /// </summary>
    public int MaxQueueSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the health check interval in milliseconds.
    /// </summary>
    public int HealthCheckIntervalMs { get; set; } = 30000;
}