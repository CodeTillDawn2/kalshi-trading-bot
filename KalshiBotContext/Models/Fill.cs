namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a trade execution (fill) in the Kalshi trading system.
    /// This entity captures the details of individual trade executions, including pricing,
    /// quantity, and market information. Fills are generated when orders are matched
    /// and executed in the market, providing a complete audit trail of all trading activity.
    /// </summary>
    public class Fill
    {
        /// <summary>
        /// Gets or sets the unique identifier for the trade that this fill is part of.
        /// This links the fill to the broader trade execution.
        /// </summary>
        public Guid trade_id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the order that generated this fill.
        /// This links the fill back to the original order that was placed.
        /// </summary>
        public Guid order_id { get; set; }

        /// <summary>
        /// Gets or sets the market ticker identifier where this fill occurred.
        /// This identifies the specific market contract that was traded.
        /// </summary>
        public string market_ticker { get; set; }

        /// <summary>
        /// Gets or sets whether this fill was executed as a taker order.
        /// Taker orders remove liquidity from the market, while maker orders add liquidity.
        /// </summary>
        public bool is_taker { get; set; }

        /// <summary>
        /// Gets or sets the side of the market this fill was executed on.
        /// This indicates whether the fill was for the "yes" or "no" side of the contract.
        /// </summary>
        public string side { get; set; }

        /// <summary>
        /// Gets or sets the price at which the "yes" side of the contract was traded.
        /// This is expressed in the standard Kalshi pricing format (cents).
        /// </summary>
        public int yes_price { get; set; }

        /// <summary>
        /// Gets or sets the price at which the "no" side of the contract was traded.
        /// This is expressed in the standard Kalshi pricing format (cents).
        /// </summary>
        public int no_price { get; set; }

        /// <summary>
        /// Gets or sets the quantity of contracts that were filled in this execution.
        /// This represents the number of shares or contracts traded.
        /// </summary>
        public int count { get; set; }

        /// <summary>
        /// Gets or sets the action type for this fill (e.g., "buy", "sell").
        /// This indicates the direction of the trade from the perspective of the trader.
        /// </summary>
        public string action { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp when this fill was executed.
        /// This provides precise timing information for the trade execution.
        /// </summary>
        public long ts { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this fill data was logged in the system.
        /// This is used for auditing and tracking when the fill was first captured.
        /// </summary>
        public DateTime LoggedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this fill data was processed by the system.
        /// This indicates when the fill was validated and made available for analysis.
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Market entity.
        /// This provides access to the full market details and related trading data.
        /// </summary>
        public Market Market { get; set; }
    }
}
