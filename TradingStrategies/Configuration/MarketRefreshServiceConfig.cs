using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class for MarketRefreshService parameters.
/// Controls refresh intervals, thresholds, and performance metrics for market data synchronization.
/// </summary>
public class MarketRefreshServiceConfig
{
    /// <summary>
    /// The configuration section name for MarketRefreshServiceConfig.
    /// </summary>
    public const string SectionName = "WatchedMarkets:MarketRefreshService";

    /// <summary>
    /// Interval in minutes for refreshing market data and recalculating trading metrics.
    /// This controls how often the system updates cached market information and technical indicators.
    /// Longer intervals reduce API load but may delay response to market changes.
    /// Typical values: 1-15 minutes depending on data freshness requirements and API rate limits.
    /// Used by MarketRefreshService and MarketData for periodic data synchronization.
    /// </summary>
    [Required(ErrorMessage = "The 'RefreshIntervalMinutes' is missing in the configuration.")]
    public int RefreshIntervalMinutes { get; set; }

    /// <summary>
    /// Threshold ratio for triggering additional refresh pass when refresh ratio falls below this value.
    /// This determines when to perform a forced refresh on markets that haven't been updated recently.
    /// Typical values: 0.1-0.5 (10%-50%) depending on desired refresh coverage.
    /// Used by MarketRefreshService for determining when to initiate additional refresh cycles.
    /// </summary>
    [Required(ErrorMessage = "The 'RefreshThresholdRatio' is missing in the configuration.")]
    public double RefreshThresholdRatio { get; set; }

    /// <summary>
    /// Time budget ratio for the forced refresh pass relative to the total refresh interval.
    /// This limits how much time the additional refresh pass can consume to avoid exceeding the interval.
    /// Typical values: 0.4-0.8 (40%-80%) depending on refresh interval and processing requirements.
    /// Used by MarketRefreshService to prevent forced refresh from delaying the next regular cycle.
    /// </summary>
    [Required(ErrorMessage = "The 'TimeBudgetRatio' is missing in the configuration.")]
    public double TimeBudgetRatio { get; set; }

    /// <summary>
    /// Enables or disables performance metrics collection in MarketRefreshService.
    /// When enabled, measures duration, market counts, throughput, CPU time, and memory usage during refresh operations.
    /// Disable for performance optimization in high-throughput scenarios.
    /// Default: true
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }
}
