using System.Text.Json.Serialization;

namespace BacklashBotData.Configuration;

/// <summary>
/// Consolidated configuration settings for the BacklashBotData layer.
/// This class combines settings previously scattered across SqlDataService and Database sections.
/// </summary>
public class BacklashBotDataConfig
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts for database operations.
    /// Used by both SqlDataService and BacklashBotContext for transient error handling.
    /// </summary>
    [JsonRequired]
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// Gets or sets the delay in seconds between retry attempts for database operations.
    /// Used by both SqlDataService and BacklashBotContext for transient error handling.
    /// </summary>
    [JsonRequired]
    public double RetryDelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets the batch size for database operations.
    /// Used by both SqlDataService and BacklashBotContext for processing multiple operations.
    /// </summary>
    [JsonRequired]
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of the database operation queues.
    /// Specific to SqlDataService for preventing resource exhaustion.
    /// </summary>
    [JsonRequired]
    public int MaxQueueSize { get; set; }

    /// <summary>
    /// Gets or sets the number of worker tasks per queue type.
    /// Specific to SqlDataService for concurrent processing.
    /// </summary>
    [JsonRequired]
    public int WorkersPerQueue { get; set; }

    /// <summary>
    /// Gets or sets whether performance metrics collection is enabled.
    /// Specific to SqlDataService for monitoring and optimization.
    /// </summary>
    [JsonRequired]
    public bool EnablePerformanceMetrics { get; set; }
}