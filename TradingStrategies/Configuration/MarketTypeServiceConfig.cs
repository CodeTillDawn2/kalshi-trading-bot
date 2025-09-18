using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class for MarketTypeService parameters.
/// Controls cache expiration and performance metrics for market type classification.
/// </summary>
public class MarketTypeServiceConfig
{
    /// <summary>
    /// Cache expiration time in minutes for market type results in MarketTypeService.
    /// This controls how long cached market type classifications are retained before requiring re-computation.
    /// Longer expiration reduces computational overhead but may use stale classifications.
    /// Typical values: 15-60 minutes depending on market volatility and freshness requirements.
    /// Default: 30 minutes
    /// </summary>
    [Required]
    public int? CacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Enables or disables performance metrics collection in MarketTypeService.
    /// When enabled, collects cache hit/miss statistics, classification timing, and count metrics.
    /// Disable for performance optimization in high-throughput scenarios where metrics are not needed.
    /// Default: true
    /// </summary>
    [Required]
    public bool EnablePerformanceMetrics { get; set; } = true;
}