namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class containing timing parameters for trading operations in the Kalshi trading bot system.
/// These settings control decision frequency, change detection windows, trade matching tolerances, and refresh intervals
/// used throughout the trading strategy execution pipeline. Values are injected via dependency injection
/// and consumed by TradingStrategy, TradingSnapshotService, and related components for consistent timing behavior.
/// </summary>
public class TradingConfig
{
    /// <summary>
    /// Frequency in seconds at which trading decisions are evaluated and executed.
    /// This controls how often the trading strategy analyzes market conditions and makes buy/sell/hold decisions.
    /// Lower values provide more responsive trading but increase computational load and potential for overtrading.
    /// Typical values: 30-300 seconds depending on strategy requirements and market volatility.
    /// Used by TradingStrategy to determine snapshot intervals and decision timing.
    /// </summary>
    public int DecisionFrequencySeconds { get; set; }

    /// <summary>
    /// Duration in minutes over which price changes are analyzed for trend detection and momentum calculations.
    /// This window defines the lookback period for identifying significant price movements and volatility patterns.
    /// Longer windows provide more stable trend analysis but may miss short-term opportunities.
    /// Typical values: 5-60 minutes depending on trading timeframe and market characteristics.
    /// Used by MarketData and TradingCalculator for change-over-time metrics and technical indicators.
    /// </summary>
    public double ChangeWindowDurationMinutes { get; set; }

    /// <summary>
    /// Time window in seconds for matching trades to orderbook changes during analysis.
    /// This tolerance allows for slight timing discrepancies between trade execution and orderbook updates.
    /// Used to correlate trade events with corresponding orderbook modifications for accurate position tracking.
    /// Typical values: 1-10 seconds depending on market latency and data feed quality.
    /// Used by OrderbookChangeTracker for trade-orderbook correlation and position reconciliation.
    /// </summary>
    public double TradeMatchingWindowSeconds { get; set; }

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

    /// <summary>
    /// Interval in minutes for refreshing market data and recalculating trading metrics.
    /// This controls how often the system updates cached market information and technical indicators.
    /// Longer intervals reduce API load but may delay response to market changes.
    /// Typical values: 1-15 minutes depending on data freshness requirements and API rate limits.
    /// Used by MarketRefreshService and MarketData for periodic data synchronization.
    /// </summary>
    public int RefreshIntervalMinutes { get; set; }

    /// <summary>
    /// Threshold ratio for triggering additional refresh pass when refresh ratio falls below this value.
    /// This determines when to perform a forced refresh on markets that haven't been updated recently.
    /// Typical values: 0.1-0.5 (10%-50%) depending on desired refresh coverage.
    /// Used by MarketRefreshService for determining when to initiate additional refresh cycles.
    /// </summary>
    public double RefreshThresholdRatio { get; set; } = 0.25;

    /// <summary>
    /// Time budget ratio for the forced refresh pass relative to the total refresh interval.
    /// This limits how much time the additional refresh pass can consume to avoid exceeding the interval.
    /// Typical values: 0.4-0.8 (40%-80%) depending on refresh interval and processing requirements.
    /// Used by MarketRefreshService to prevent forced refresh from delaying the next regular cycle.
    /// </summary>
    public double TimeBudgetRatio { get; set; } = 0.60;

    /// <summary>
    /// Maximum position size limit as a percentage of total portfolio value.
    /// This prevents over-concentration in any single market position.
    /// Typical values: 0.05-0.20 (5%-20%) depending on risk tolerance and diversification strategy.
    /// Used by TradingStrategy to limit position sizes during simulation.
    /// </summary>
    public double MaxPositionSizePercent { get; set; } = 0.10;

    /// <summary>
    /// Maximum total exposure limit as a percentage of total portfolio value.
    /// This controls the overall risk level across all positions.
    /// Typical values: 0.50-2.0 (50%-200%) depending on leverage and risk tolerance.
    /// Used by TradingStrategy to prevent excessive total exposure.
    /// </summary>
    public double MaxTotalExposurePercent { get; set; } = 1.0;

    /// <summary>
    /// Stop-loss threshold as a percentage of entry price.
    /// Positions will be automatically closed if losses exceed this threshold.
    /// Typical values: 0.05-0.20 (5%-20%) depending on volatility and risk tolerance.
    /// Used by TradingStrategy for risk management during simulation.
    /// </summary>
    public double StopLossPercent { get; set; } = 0.10;

    /// <summary>
    /// Take-profit threshold as a percentage of entry price.
    /// Positions will be automatically closed if gains exceed this threshold.
    /// Typical values: 0.05-0.50 (5%-50%) depending on profit targets and market conditions.
    /// Used by TradingStrategy for profit-taking during simulation.
    /// </summary>
    public double TakeProfitPercent { get; set; } = 0.20;

    /// <summary>
    /// Maximum number of concurrent positions allowed.
    /// This limits portfolio diversification and position management complexity.
    /// Typical values: 5-20 depending on available capital and management capacity.
    /// Used by TradingStrategy to prevent over-diversification.
    /// </summary>
    public int MaxConcurrentPositions { get; set; } = 10;

    /// <summary>
    /// Maximum drawdown limit as a percentage of total portfolio value.
    /// Simulation will pause or stop if drawdown exceeds this threshold.
    /// Typical values: 0.10-0.30 (10%-30%) depending on risk tolerance.
    /// Used by TradingStrategy for portfolio-level risk management.
    /// </summary>
    public double MaxDrawdownPercent { get; set; } = 0.20;

}
