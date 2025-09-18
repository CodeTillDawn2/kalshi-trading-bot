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
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay in seconds between retry attempts for database operations.
    /// Used by both SqlDataService and BacklashBotContext for transient error handling.
    /// </summary>
    public double RetryDelaySeconds { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the batch size for database operations.
    /// Used by both SqlDataService and BacklashBotContext for processing multiple operations.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum size of the database operation queues.
    /// Specific to SqlDataService for preventing resource exhaustion.
    /// </summary>
    public int MaxQueueSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the number of worker tasks per queue type.
    /// Specific to SqlDataService for concurrent processing.
    /// </summary>
    public int WorkersPerQueue { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether performance metrics collection is enabled.
    /// Specific to SqlDataService for monitoring and optimization.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = false;
}