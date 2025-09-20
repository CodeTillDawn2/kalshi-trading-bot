using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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


}
