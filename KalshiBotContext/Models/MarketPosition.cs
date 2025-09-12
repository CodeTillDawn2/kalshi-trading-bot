namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents the current trading position and performance metrics for a specific market.
    /// This entity tracks real-time position information including exposure, P&L, and order status
    /// for individual markets. It serves as the primary source of position data for risk management,
    /// performance monitoring, and trading decision making throughout the bot system.
    /// </summary>
    public class MarketPosition
    {
        /// <summary>
        /// Gets or sets the market ticker identifier for this position.
        /// This uniquely identifies the market contract this position relates to.
        /// </summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of contracts traded in this market.
        /// This represents the cumulative trading volume for position tracking.
        /// </summary>
        public long TotalTraded { get; set; }

        /// <summary>
        /// Gets or sets the current net position in this market.
        /// Positive values indicate a long position, negative values indicate a short position.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets or sets the current market exposure in cents.
        /// This represents the dollar value of the current position at current market prices.
        /// </summary>
        public long MarketExposure { get; set; }

        /// <summary>
        /// Gets or sets the realized profit and loss for this position in cents.
        /// This tracks the actual gains or losses that have been locked in through trading.
        /// </summary>
        public long RealizedPnl { get; set; }

        /// <summary>
        /// Gets or sets the number of resting orders currently active in this market.
        /// This includes limit orders that have not yet been filled.
        /// </summary>
        public int RestingOrdersCount { get; set; }

        /// <summary>
        /// Gets or sets the total fees paid for trading in this market in cents.
        /// This tracks the cumulative trading costs associated with this position.
        /// </summary>
        public long FeesPaid { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this position data was last updated, in UTC.
        /// This indicates the freshness of the position information.
        /// </summary>
        public DateTime LastUpdatedUTC { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this position record was last modified.
        /// This is used for tracking changes to position data over time.
        /// </summary>
        public DateTime? LastModified { get; set; }
    }
}
