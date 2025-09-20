using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashBotData.Configuration;

/// <summary>
/// Consolidated configuration settings for the BacklashBotData layer.
/// This class combines settings previously scattered across SqlDataService and Database sections.
/// </summary>
public class BacklashBotDataConfig
{
    public const string SectionName = "DBConnection:BacklashBotData";

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for database operations.
    /// Used by both SqlDataService and BacklashBotContext for transient error handling.
    /// </summary>
    [Required(ErrorMessage = "The 'MaxRetryCount' is missing in the configuration.")]
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// Gets or sets the delay in seconds between retry attempts for database operations.
    /// Used by both SqlDataService and BacklashBotContext for transient error handling.
    /// </summary>
    [Required(ErrorMessage = "The 'RetryDelaySeconds' is missing in the configuration.")]
    public double RetryDelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets the batch size for database operations.
    /// Used by both SqlDataService and BacklashBotContext for processing multiple operations.
    /// </summary>
    [Required(ErrorMessage = "The 'BatchSize' is missing in the configuration.")]
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of the database operation queues.
    /// Specific to SqlDataService for preventing resource exhaustion.
    /// </summary>
    [Required(ErrorMessage = "The 'MaxQueueSize' is missing in the configuration.")]
    public int MaxQueueSize { get; set; }

    /// <summary>
    /// Gets or sets the number of worker tasks per queue type.
    /// Specific to SqlDataService for concurrent processing.
    /// </summary>
    [Required(ErrorMessage = "The 'WorkersPerQueue' is missing in the configuration.")]
    public int WorkersPerQueue { get; set; }

    /// <summary>
    /// Gets or sets whether performance metrics collection is enabled.
    /// Specific to SqlDataService for monitoring and optimization.
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }
}
