using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class containing timing parameters for trading operations.
/// Controls decision frequency, change detection windows, and trade matching tolerances.
/// </summary>
public class TradingTimingConfig
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
}