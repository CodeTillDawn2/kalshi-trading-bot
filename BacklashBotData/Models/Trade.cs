namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a completed trade execution in the Kalshi market.
    /// This entity captures the details of individual trades including pricing,
    /// quantity, and market information. Trades are the fundamental records of
    /// market activity and provide the basis for volume analysis, price discovery,
    /// and market trend identification.
    /// </summary>
    public class Trade
    {
        /// <summary>
        /// Gets or sets the unique market identifier from the Kalshi API.
        /// This is the internal market ID used by the exchange.
        /// </summary>
        public Guid market_id { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol for this trade.
        /// This identifies the specific market contract where the trade occurred.
        /// </summary>
        public string market_ticker { get; set; }

        /// <summary>
        /// Gets or sets the price at which the "yes" side of the contract was traded in cents.
        /// This represents the execution price for yes positions in the trade.
        /// </summary>
        public int yes_price { get; set; }

        /// <summary>
        /// Gets or sets the price at which the "no" side of the contract was traded in cents.
        /// This represents the execution price for no positions in the trade.
        /// </summary>
        public int no_price { get; set; }

        /// <summary>
        /// Gets or sets the quantity of contracts traded in this execution.
        /// This represents the number of shares or contracts that changed hands.
        /// </summary>
        public int count { get; set; }

        /// <summary>
        /// Gets or sets the side of the market that was the taker in this trade.
        /// This indicates whether the trade was initiated by a buyer ("yes") or seller ("no").
        /// </summary>
        public string taker_side { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp when this trade was executed.
        /// This provides precise timing information for the trade execution.
        /// </summary>
        public long ts { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this trade data was logged in the system.
        /// This is used for auditing and tracking when the trade was first captured.
        /// </summary>
        public DateTime LoggedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this trade data was processed by the system.
        /// This indicates when the trade was validated and made available for analysis.
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Market entity.
        /// This provides access to the full market details and related trading information.
        /// </summary>
        public Market Market { get; set; }
    }
}
