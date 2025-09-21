using BacklashBot.State;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashBot.Configuration;

/// <summary>
/// Configuration class for market data settings.
/// </summary>
public class MarketServiceDataConfig
{
    /// <summary>
    /// The configuration section name for MarketDataConfig.
    /// </summary>
    public const string SectionName = "WatchedMarkets:MarketData";

    /// <summary>
    /// Gets or sets the semaphore timeout in milliseconds. Default is 5000.
    /// </summary>
    [Required(ErrorMessage = "The 'SemaphoreTimeoutMs' is missing in the configuration.")]
    public int SemaphoreTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the ticker batch size. Default is 100.
    /// </summary>
    [Required(ErrorMessage = "The 'TickerBatchSize' is missing in the configuration.")]
    public int TickerBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the API retry timeout in milliseconds. Default is 30000.
    /// </summary>
    [Required(ErrorMessage = "The 'ApiRetryTimeoutMs' is missing in the configuration.")]
    public int ApiRetryTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether performance metrics logging is enabled for MarketDataService. Default is false.
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether performance metrics logging is enabled for MarketDataService. Default is false.
    /// </summary>
    [Required(ErrorMessage = "The 'PseudoCandlestickLookbackPeriods' is missing in the configuration.")]
    public int PseudoCandlestickLookbackPeriods { get; set; }

    /// <summary>
    /// Gets or sets the delay in seconds for event lifecycle processing. Default is 120 (2 minutes).
    /// This delay ensures that brand new events are fully ready on the server before attempting to fetch data,
    /// preventing 404 errors that can occur when the API call is made too quickly after the event_lifecycle event.
    /// </summary>
    [Required(ErrorMessage = "The 'EventLifecycleDelaySeconds' is missing in the configuration.")]
    public int EventLifecycleDelaySeconds { get; set; }

    [Required(ErrorMessage = "The 'CalculationConfig' is missing in the configuration.")]
    public CalculationsConfig Calculations { get; set; }
}


