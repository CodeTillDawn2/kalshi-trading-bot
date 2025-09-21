using System.ComponentModel.DataAnnotations;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for KalshiAPIService operations.
/// </summary>
public class KalshiAPIServiceConfig
{
    /// <summary>
    /// The configuration section name for KalshiAPIServiceConfig.
    /// </summary>
    public const string SectionName = "API:KalshiAPIService";

    /// <summary>
    /// Gets or sets whether KalshiAPIService performance metrics collection is enabled.
    /// When disabled, method execution times, API response times, and error counts are not tracked to reduce overhead. Defaults to true.
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of lifecycle events to process per second.
    /// Used to throttle API calls triggered by market lifecycle events to prevent rate limiting. Defaults to 10.
    /// </summary>
    [Required(ErrorMessage = "The 'MaxLifecycleEventsPerSecond' is missing in the configuration.")]
    public int MaxLifecycleEventsPerSecond { get; set; } = 10;


}
