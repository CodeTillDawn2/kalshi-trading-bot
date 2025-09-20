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

    /// <summary>
    /// Gets or sets the mandatory overlap period in days for minute-interval candlesticks.
    /// This ensures sufficient historical data is available for reliable minute-level analysis.
    /// </summary>
    [Required(ErrorMessage = "The 'CandlestickMandatoryOverlapDaysMinute' is missing in the configuration.")]
    public int CandlestickMandatoryOverlapDaysMinute { get; set; }

    /// <summary>
    /// Gets or sets the mandatory overlap period in days for hour-interval candlesticks.
    /// This ensures sufficient historical data is available for reliable hour-level analysis.
    /// </summary>
    [Required(ErrorMessage = "The 'CandlestickMandatoryOverlapDaysHour' is missing in the configuration.")]
    public int CandlestickMandatoryOverlapDaysHour { get; set; }

    /// <summary>
    /// Gets or sets the mandatory overlap period in days for day-interval candlesticks.
    /// This ensures sufficient historical data is available for reliable day-level analysis.
    /// </summary>
    [Required(ErrorMessage = "The 'CandlestickMandatoryOverlapDaysDay' is missing in the configuration.")]
    public int CandlestickMandatoryOverlapDaysDay { get; set; }
}
