using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class for OrderbookChangeTracker service parameters.
/// Controls queue sizes, cleanup thresholds, and performance metrics for orderbook change tracking.
/// </summary>
public class OrderbookChangeTrackerConfig
{
    [Range(100, 100000)]
    /// <summary>
    /// Maximum number of orderbook changes to keep in the processing queue.
    /// This prevents memory exhaustion during high-volume market activity.
    /// When the queue exceeds this size, older events are automatically cleaned up.
    /// Typical values: 1000-10000 depending on market volatility and processing capacity.
    /// Used by OrderbookChangeTracker to manage memory usage and prevent queue overflow.
    /// </summary>
    public int MaxOrderbookChangeQueueSize { get; set; } = 10000;

    [Range(100, 100000)]
    /// <summary>
    /// Maximum number of trade events to keep in the processing queue.
    /// This prevents memory exhaustion during high-volume trading activity.
    /// When the queue exceeds this size, older events are automatically cleaned up.
    /// Typical values: 1000-10000 depending on market activity and processing capacity.
    /// Used by OrderbookChangeTracker to manage memory usage and prevent queue overflow.
    /// </summary>
    public int MaxTradeEventQueueSize { get; set; } = 5000;

    [Range(1, 1440)]
    /// <summary>
    /// Age threshold in minutes for cleaning up old orderbook changes.
    /// Events older than this threshold are automatically removed during cleanup operations.
    /// This helps maintain queue sizes and prevents processing of stale data.
    /// Typical values: 30-120 minutes depending on analysis requirements and memory constraints.
    /// Used by OrderbookChangeTracker for periodic cleanup of old events.
    /// </summary>
    public int OrderbookChangeCleanupThresholdMinutes { get; set; } = 60;

    [Range(1, 1440)]
    /// <summary>
    /// Age threshold in minutes for cleaning up old trade events.
    /// Events older than this threshold are automatically removed during cleanup operations.
    /// This helps maintain queue sizes and prevents processing of stale data.
    /// Typical values: 30-120 minutes depending on analysis requirements and memory constraints.
    /// Used by OrderbookChangeTracker for periodic cleanup of old events.
    /// </summary>
    public int TradeEventCleanupThresholdMinutes { get; set; } = 60;

    /// <summary>
    /// Enables or disables performance metrics collection in OrderbookChangeTracker.
    /// When enabled, measures event processing latency (orderbook snapshots, changes, trades)
    /// and timer accuracy (drift and execution times for periodic operations).
    /// Disable for performance optimization in high-throughput scenarios.
    /// Default: true
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;
}