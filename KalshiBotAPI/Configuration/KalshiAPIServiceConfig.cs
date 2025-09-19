using System.Text.Json.Serialization;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for KalshiAPIService operations.
/// </summary>
public class KalshiAPIServiceConfig
{
    /// <summary>
    /// Gets or sets the KalshiAPIService configuration settings.
    /// </summary>
    [JsonRequired]
    public KalshiAPIServiceSettings KalshiAPIService { get; set; }
}

/// <summary>
/// Configuration settings for KalshiAPIService operations.
/// </summary>
public class KalshiAPIServiceSettings
{
    /// <summary>
    /// Gets or sets whether KalshiAPIService performance metrics collection is enabled.
    /// When disabled, method execution times, API response times, and error counts are not tracked to reduce overhead. Defaults to true.
    /// </summary>
    [JsonRequired]
    public bool EnablePerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets the mandatory overlap period in days for minute-interval candlesticks.
    /// This ensures sufficient historical data is available for reliable minute-level analysis.
    /// </summary>
    [JsonRequired]
    public int CandlestickMandatoryOverlapDaysMinute { get; set; }

    /// <summary>
    /// Gets or sets the mandatory overlap period in days for hour-interval candlesticks.
    /// This ensures sufficient historical data is available for reliable hour-level analysis.
    /// </summary>
    [JsonRequired]
    public int CandlestickMandatoryOverlapDaysHour { get; set; }

    /// <summary>
    /// Gets or sets the mandatory overlap period in days for day-interval candlesticks.
    /// This ensures sufficient historical data is available for reliable day-level analysis.
    /// </summary>
    [JsonRequired]
    public int CandlestickMandatoryOverlapDaysDay { get; set; }

}