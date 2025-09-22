using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration;

/// <summary>
/// Configuration class for OrderbookChangeTracker service parameters.
/// Controls queue sizes, cleanup thresholds, and performance metrics for orderbook change tracking.
/// </summary>
public class OrderbookChangeTrackerConfig
{
    /// <summary>
    /// The configuration section name for OrderbookChangeTrackerConfig.
    /// </summary>
    public const string SectionName = "WatchedMarkets:OrderbookChangeTracker";

    /// <summary>
    /// Age threshold in minutes for cleaning up old orderbook and trade changes.
    /// Events older than this threshold are automatically removed during cleanup operations.
    /// This helps maintain queue sizes and prevents processing of stale data.
    /// Typical values: 5-20 minutes depending on analysis requirements and memory constraints.
    /// Used by OrderbookChangeTracker for periodic cleanup of old events.
    /// </summary>
    [Required(ErrorMessage = "The 'EventCalculationPeriod' is missing in the configuration.")]
    public int EventCalculationPeriod { get; set; }

    /// <summary>
    /// Enables or disables performance metrics collection in OrderbookChangeTracker.
    /// When enabled, measures event processing latency (orderbook snapshots, changes, trades)
    /// and timer accuracy (drift and execution times for periodic operations).
    /// Disable for performance optimization in high-throughput scenarios.
    /// Default: true
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }
}
