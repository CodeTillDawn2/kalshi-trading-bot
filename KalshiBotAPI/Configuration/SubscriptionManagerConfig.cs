using System.Text.Json.Serialization;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for SubscriptionManager operations.
/// </summary>
public class SubscriptionManagerConfig
{
    /// <summary>
    /// Gets or sets whether SubscriptionManager performance metrics collection is enabled.
    /// </summary>
    required
    public bool EnableSubscriptionManagerMetrics { get; set; }

    /// <summary>
    /// Gets or sets the subscription timeout in milliseconds.
    /// </summary>
    required
    public int SubscriptionTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the confirmation timeout in seconds.
    /// </summary>
    required
    public int ConfirmationTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the retry delay in milliseconds.
    /// </summary>
    required
    public int RetryDelayMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum queue size.
    /// </summary>
    required
    public int MaxQueueSize { get; set; }

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    required
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the health check interval in milliseconds.
    /// </summary>
    required
    public int HealthCheckIntervalMs { get; set; }
}
