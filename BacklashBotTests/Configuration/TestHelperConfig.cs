using System.ComponentModel.DataAnnotations;

namespace BacklashBotTests.Configuration
{
    /// <summary>
    /// Configuration class containing test-specific parameters used only by the TestHelper class.
    /// These settings control test scenarios for trading operations and are not used in production code.
    /// </summary>
    public class TestHelperConfig
    {
        [System.ComponentModel.DataAnnotations.Range(0.01, 1440.0)]
        /// <summary>
        /// Duration in minutes over which price changes are analyzed for trend detection and momentum calculations.
        /// This window defines the lookback period for identifying significant price movements and volatility patterns.
        /// Longer windows provide more stable trend analysis but may miss short-term opportunities.
        /// Typical values: 5-60 minutes depending on trading timeframe and market characteristics.
        /// Used by MarketData and TradingCalculator for change-over-time metrics and technical indicators.
        /// </summary>
        public double ChangeWindowDurationMinutes { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0.01, 60.0)]
        /// <summary>
        /// Time window in seconds for matching trades to orderbook changes during analysis.
        /// This tolerance allows for slight timing discrepancies between trade execution and orderbook updates.
        /// Used to correlate trade events with corresponding orderbook modifications for accurate position tracking.
        /// Typical values: 1-10 seconds depending on market latency and data feed quality.
        /// Used by OrderbookChangeTracker for trade-orderbook correlation and position reconciliation.
        /// </summary>
        public double TradeMatchingWindowSeconds { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0.01, 60.0)]
        /// <summary>
        /// Time window in seconds for detecting order cancellations in the orderbook.
        /// This defines how long to wait before considering an orderbook level change as a cancellation rather than a fill.
        /// Helps distinguish between actual cancellations and rapid fill-and-replace order patterns.
        /// Typical values: 5-30 seconds depending on market speed and order book dynamics.
        /// Used by OrderbookChangeTracker for cancellation rate calculations and order flow analysis.
        /// </summary>
        public double OrderbookCancelWindowSeconds { get; set; }
    }
}