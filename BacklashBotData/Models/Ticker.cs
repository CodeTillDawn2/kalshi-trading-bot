namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents real-time ticker data for a Kalshi market, capturing price and volume information at a specific point in time.
    /// This entity stores the most recent market data including bid/ask prices, trading volume, and open interest.
    /// Ticker data is crucial for real-time trading decisions and market analysis, providing the current state
    /// of market pricing and liquidity for individual contracts.
    /// </summary>
    public class Ticker
    {
        /// <summary>
        /// Gets or sets the unique market identifier from the Kalshi API.
        /// This is the internal market ID used by the exchange.
        /// </summary>
        public Guid market_id { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol for this ticker data.
        /// This identifies the specific market contract (e.g., "KXUSD-25DEC31").
        /// </summary>
        public string market_ticker { get; set; }

        /// <summary>
        /// Gets or sets the current market price in cents.
        /// This represents the last traded price for the contract.
        /// </summary>
        public int price { get; set; }

        /// <summary>
        /// Gets or sets the best bid price for the "yes" side of the contract in cents.
        /// This is the highest price buyers are willing to pay for yes positions.
        /// </summary>
        public int yes_bid { get; set; }

        /// <summary>
        /// Gets or sets the best ask price for the "yes" side of the contract in cents.
        /// This is the lowest price sellers are willing to accept for yes positions.
        /// </summary>
        public int yes_ask { get; set; }

        /// <summary>
        /// Gets or sets the trading volume for this market.
        /// This represents the number of contracts traded in the recent period.
        /// </summary>
        public int volume { get; set; }

        /// <summary>
        /// Gets or sets the open interest for this market.
        /// This represents the total number of outstanding contracts.
        /// </summary>
        public int open_interest { get; set; }

        /// <summary>
        /// Gets or sets the dollar volume traded in this market.
        /// This represents the monetary value of contracts traded.
        /// </summary>
        public int dollar_volume { get; set; }

        /// <summary>
        /// Gets or sets the dollar value of open interest in this market.
        /// This represents the monetary value of all outstanding contracts.
        /// </summary>
        public int dollar_open_interest { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp when this ticker data was captured.
        /// This provides precise timing information for the market data.
        /// </summary>
        public long ts { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this ticker data was logged in the system.
        /// This is used for auditing and tracking when the data was first captured.
        /// </summary>
        public DateTime LoggedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this ticker data was processed by the system.
        /// This indicates when the data was validated and made available for analysis.
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Market entity.
        /// This provides access to the full market details and related trading information.
        /// </summary>
        public Market Market { get; set; }
    }
}
