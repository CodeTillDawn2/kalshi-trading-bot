namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for KalshiAPIService operations.
/// </summary>
public class KalshiAPIServiceConfig
{
    /// <summary>
    /// Gets or sets the KalshiAPIService configuration settings.
    /// </summary>
    public KalshiAPIServiceSettings KalshiAPIService { get; set; } = new();
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
    public bool EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the mandatory overlap period in days for minute-interval candlesticks.
    /// This ensures sufficient historical data is available for reliable minute-level analysis.
    /// </summary>
    public int CandlestickMandatoryOverlapDaysMinute { get; set; } = 3;

    /// <summary>
    /// Gets or sets the mandatory overlap period in days for hour-interval candlesticks.
    /// This ensures sufficient historical data is available for reliable hour-level analysis.
    /// </summary>
    public int CandlestickMandatoryOverlapDaysHour { get; set; } = 7;

    /// <summary>
    /// Gets or sets the mandatory overlap period in days for day-interval candlesticks.
    /// This ensures sufficient historical data is available for reliable day-level analysis.
    /// </summary>
    public int CandlestickMandatoryOverlapDaysDay { get; set; } = 15;

}