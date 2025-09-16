using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class containing timing parameters for trading operations in the Kalshi trading bot system.
/// These settings control decision frequency, change detection windows, trade matching tolerances, and refresh intervals
/// used throughout the trading strategy execution pipeline. Values are injected via dependency injection
/// and consumed by TradingStrategy, TradingSnapshotService, and related components for consistent timing behavior.
/// The class includes data validation attributes and a Validate() method to ensure parameters are within acceptable ranges.
/// </summary>
public class TradingConfig
{
    [Range(1, 3600)]
    /// <summary>
    /// Frequency in seconds at which trading decisions are evaluated and executed.
    /// This controls how often the trading strategy analyzes market conditions and makes buy/sell/hold decisions.
    /// Lower values provide more responsive trading but increase computational load and potential for overtrading.
    /// Typical values: 30-300 seconds depending on strategy requirements and market volatility.
    /// Used by TradingStrategy to determine snapshot intervals and decision timing.
    /// </summary>
    public int DecisionFrequencySeconds { get; set; }

    [Range(0.01, 1440.0)]
    /// <summary>
    /// Duration in minutes over which price changes are analyzed for trend detection and momentum calculations.
    /// This window defines the lookback period for identifying significant price movements and volatility patterns.
    /// Longer windows provide more stable trend analysis but may miss short-term opportunities.
    /// Typical values: 5-60 minutes depending on trading timeframe and market characteristics.
    /// Used by MarketData and TradingCalculator for change-over-time metrics and technical indicators.
    /// </summary>
    public double ChangeWindowDurationMinutes { get; set; }

    [Range(0.01, 60.0)]
    /// <summary>
    /// Time window in seconds for matching trades to orderbook changes during analysis.
    /// This tolerance allows for slight timing discrepancies between trade execution and orderbook updates.
    /// Used to correlate trade events with corresponding orderbook modifications for accurate position tracking.
    /// Typical values: 1-10 seconds depending on market latency and data feed quality.
    /// Used by OrderbookChangeTracker for trade-orderbook correlation and position reconciliation.
    /// </summary>
    public double TradeMatchingWindowSeconds { get; set; }

    [Range(0.01, 60.0)]
    /// <summary>
    /// Time window in seconds for detecting order cancellations in the orderbook.
    /// This defines how long to wait before considering an orderbook level change as a cancellation rather than a fill.
    /// Helps distinguish between actual cancellations and rapid fill-and-replace order patterns.
    /// Typical values: 5-30 seconds depending on market speed and order book dynamics.
    /// Used by OrderbookChangeTracker for cancellation rate calculations and order flow analysis.
    /// </summary>
    public double OrderbookCancelWindowSeconds { get; set; }

    /// <summary>
    /// Computed TimeSpan representation of the change window duration for use in time-based calculations.
    /// Provides convenient TimeSpan format for operations requiring duration arithmetic.
    /// Automatically converts ChangeWindowDurationMinutes to TimeSpan for consistent time handling.
    /// </summary>
    public TimeSpan ChangeWindowDuration => TimeSpan.FromMinutes(ChangeWindowDurationMinutes);

    /// <summary>
    /// Computed TimeSpan representation of the trade matching window for use in timing comparisons.
    /// Provides convenient TimeSpan format for trade correlation and timing validation operations.
    /// Automatically converts TradeMatchingWindowSeconds to TimeSpan for consistent time handling.
    /// </summary>
    public TimeSpan TradeMatchingWindow => TimeSpan.FromSeconds(TradeMatchingWindowSeconds);

    /// <summary>
    /// Computed TimeSpan representation of the orderbook cancel window for use in cancellation detection.
    /// Provides convenient TimeSpan format for orderbook analysis and cancellation rate calculations.
    /// Automatically converts OrderbookCancelWindowSeconds to TimeSpan for consistent time handling.
    /// </summary>
    public TimeSpan OrderbookCancelWindow => TimeSpan.FromSeconds(OrderbookCancelWindowSeconds);

    [Range(1, 1440)]
    /// <summary>
    /// Interval in minutes for refreshing market data and recalculating trading metrics.
    /// This controls how often the system updates cached market information and technical indicators.
    /// Longer intervals reduce API load but may delay response to market changes.
    /// Typical values: 1-15 minutes depending on data freshness requirements and API rate limits.
    /// Used by MarketRefreshService and MarketData for periodic data synchronization.
    /// </summary>
    public int RefreshIntervalMinutes { get; set; }

    [Range(0.01, 0.99)]
    /// <summary>
    /// Threshold ratio for triggering additional refresh pass when refresh ratio falls below this value.
    /// This determines when to perform a forced refresh on markets that haven't been updated recently.
    /// Typical values: 0.1-0.5 (10%-50%) depending on desired refresh coverage.
    /// Used by MarketRefreshService for determining when to initiate additional refresh cycles.
    /// </summary>
    public double RefreshThresholdRatio { get; set; } = 0.25;

    [Range(0.01, 0.99)]
    /// <summary>
    /// Time budget ratio for the forced refresh pass relative to the total refresh interval.
    /// This limits how much time the additional refresh pass can consume to avoid exceeding the interval.
    /// Typical values: 0.4-0.8 (40%-80%) depending on refresh interval and processing requirements.
    /// Used by MarketRefreshService to prevent forced refresh from delaying the next regular cycle.
    /// </summary>
    public double TimeBudgetRatio { get; set; } = 0.60;

    [Range(0.01, 1.0)]
    /// <summary>
    /// Maximum position size limit as a percentage of total portfolio value.
    /// This prevents over-concentration in any single market position.
    /// Typical values: 0.05-0.20 (5%-20%) depending on risk tolerance and diversification strategy.
    /// Used by TradingStrategy to limit position sizes during simulation.
    /// </summary>
    public double MaxPositionSizePercent { get; set; } = 0.10;

    [Range(0.01, 2.0)]
    /// <summary>
    /// Maximum total exposure limit as a percentage of total portfolio value.
    /// This controls the overall risk level across all positions.
    /// Typical values: 0.50-2.0 (50%-200%) depending on leverage and risk tolerance.
    /// Used by TradingStrategy to prevent excessive total exposure.
    /// </summary>
    public double MaxTotalExposurePercent { get; set; } = 1.0;

    [Range(0.01, 1.0)]
    /// <summary>
    /// Stop-loss threshold as a percentage of entry price.
    /// Positions will be automatically closed if losses exceed this threshold.
    /// Typical values: 0.05-0.20 (5%-20%) depending on volatility and risk tolerance.
    /// Used by TradingStrategy for risk management during simulation.
    /// </summary>
    public double StopLossPercent { get; set; } = 0.10;

    [Range(0.01, 1.0)]
    /// <summary>
    /// Take-profit threshold as a percentage of entry price.
    /// Positions will be automatically closed if gains exceed this threshold.
    /// Typical values: 0.05-0.50 (5%-50%) depending on profit targets and market conditions.
    /// Used by TradingStrategy for profit-taking during simulation.
    /// </summary>
    public double TakeProfitPercent { get; set; } = 0.20;

    [Range(1, 100)]
    /// <summary>
    /// Maximum number of concurrent positions allowed.
    /// This limits portfolio diversification and position management complexity.
    /// Typical values: 5-20 depending on available capital and management capacity.
    /// Used by TradingStrategy to prevent over-diversification.
    /// </summary>
    public int MaxConcurrentPositions { get; set; } = 10;

    [Range(0.01, 1.0)]
    /// <summary>
    /// Maximum drawdown limit as a percentage of total portfolio value.
    /// Simulation will pause or stop if drawdown exceeds this threshold.
    /// Typical values: 0.10-0.30 (10%-30%) depending on risk tolerance.
    /// Used by TradingStrategy for portfolio-level risk management.
    /// </summary>
    public double MaxDrawdownPercent { get; set; } = 0.20;

    [Range(0, 10)]
    /// <summary>
    /// Number of decimal places to round volume values when converting to double.
    /// This controls precision handling for volume data in candlestick conversions.
    /// Typical values: 0-4 depending on required precision and data source.
    /// Used by PseudoCandlestickExtensions for volume precision in ToCandleMids method.
    /// </summary>
    public int VolumePrecisionDigits { get; set; } = 2;

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
    public bool OrderbookChangeTracker_EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Enables or disables performance metrics collection in MarketRefreshService.
    /// When enabled, measures duration, market counts, throughput, CPU time, and memory usage during refresh operations.
    /// Disable for performance optimization in high-throughput scenarios.
    /// Default: true
    /// </summary>
    public bool MarketRefreshService_EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes for market type results in MarketTypeService.
    /// This controls how long cached market type classifications are retained before requiring re-computation.
    /// Longer expiration reduces computational overhead but may use stale classifications.
    /// Typical values: 15-60 minutes depending on market volatility and freshness requirements.
    /// Default: 30 minutes
    /// </summary>
    public int? MarketTypeCacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Enables or disables performance metrics collection in MarketTypeService.
    /// When enabled, collects cache hit/miss statistics, classification timing, and count metrics.
    /// Disable for performance optimization in high-throughput scenarios where metrics are not needed.
    /// Default: true
    /// </summary>
    public bool MarketTypeService_EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Enables or disables performance metrics collection in StrategySelectionHelper.
    /// When enabled, collects strategy instantiation timing and memory allocation metrics for each strategy instance.
    /// Disable for performance optimization in high-throughput scenarios where metrics are not needed.
    /// Default: false (due to performance impact of individual instance tracking)
    /// </summary>
    public bool StrategySelectionHelper_EnablePerformanceMetrics { get; set; } = false;

    /// <summary>
    /// Enables or disables performance metrics collection in EquityCalculator.
    /// When enabled, measures execution time for each equity calculation and collects timing statistics.
    /// Disable for performance optimization in high-throughput scenarios where metrics are not needed.
    /// Default: true
    /// </summary>
    public bool EquityCalculator_EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Validates the configuration parameters to ensure they are within acceptable ranges and prevent invalid combinations.
    /// This method should be called during application startup to catch misconfigurations early.
    /// Throws ArgumentException if any parameter is invalid.
    /// </summary>
    public void Validate()
    {
        // Basic range validations are handled by data annotations, but this method provides additional checks
        // and can be extended for complex parameter relationships if needed.
        // For performance, these checks are simple and only executed on startup.
        if (DecisionFrequencySeconds <= 0 || DecisionFrequencySeconds > 3600)
            throw new ArgumentException("DecisionFrequencySeconds must be between 1 and 3600 seconds.");
        if (ChangeWindowDurationMinutes <= 0 || ChangeWindowDurationMinutes > 1440)
            throw new ArgumentException("ChangeWindowDurationMinutes must be between 0.01 and 1440 minutes.");
        if (TradeMatchingWindowSeconds <= 0 || TradeMatchingWindowSeconds > 60)
            throw new ArgumentException("TradeMatchingWindowSeconds must be between 0.01 and 60 seconds.");
        if (OrderbookCancelWindowSeconds <= 0 || OrderbookCancelWindowSeconds > 60)
            throw new ArgumentException("OrderbookCancelWindowSeconds must be between 0.01 and 60 seconds.");
        if (RefreshIntervalMinutes <= 0 || RefreshIntervalMinutes > 1440)
            throw new ArgumentException("RefreshIntervalMinutes must be between 1 and 1440 minutes.");
        if (RefreshThresholdRatio <= 0 || RefreshThresholdRatio >= 1)
            throw new ArgumentException("RefreshThresholdRatio must be between 0.01 and 0.99.");
        if (TimeBudgetRatio <= 0 || TimeBudgetRatio >= 1)
            throw new ArgumentException("TimeBudgetRatio must be between 0.01 and 0.99.");
        if (MaxPositionSizePercent <= 0 || MaxPositionSizePercent > 1)
            throw new ArgumentException("MaxPositionSizePercent must be between 0.01 and 1.0.");
        if (MaxTotalExposurePercent <= 0 || MaxTotalExposurePercent > 2)
            throw new ArgumentException("MaxTotalExposurePercent must be between 0.01 and 2.0.");
        if (StopLossPercent <= 0 || StopLossPercent > 1)
            throw new ArgumentException("StopLossPercent must be between 0.01 and 1.0.");
        if (TakeProfitPercent <= 0 || TakeProfitPercent > 1)
            throw new ArgumentException("TakeProfitPercent must be between 0.01 and 1.0.");
        if (MaxConcurrentPositions <= 0 || MaxConcurrentPositions > 100)
            throw new ArgumentException("MaxConcurrentPositions must be between 1 and 100.");
        if (MaxDrawdownPercent <= 0 || MaxDrawdownPercent > 1)
            throw new ArgumentException("MaxDrawdownPercent must be between 0.01 and 1.0.");
        if (VolumePrecisionDigits < 0 || VolumePrecisionDigits > 10)
            throw new ArgumentException("VolumePrecisionDigits must be between 0 and 10.");
        // Add any invalid combination checks here, e.g., if (DecisionFrequencySeconds > RefreshIntervalMinutes * 60) ...
    }
}
