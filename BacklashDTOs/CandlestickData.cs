namespace BacklashDTOs
{
    /// <summary>
    /// Represents candlestick data for a market.
    /// </summary>
    public class CandlestickData
    {
        /// <summary>
        /// Gets or sets the date of the candlestick.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string? MarketTicker { get; set; }

        /// <summary>
        /// Gets or sets the interval type.
        /// </summary>
        public int IntervalType { get; set; }

        /// <summary>
        /// Gets or sets the open interest.
        /// </summary>
        public int OpenInterest { get; set; }

        /// <summary>
        /// Gets or sets the trading volume.
        /// </summary>
        public int Volume { get; set; }

        /// <summary>
        /// Gets or sets the ask opening price.
        /// </summary>
        public int AskOpen { get; set; }

        /// <summary>
        /// Gets or sets the ask highest price.
        /// </summary>
        public int AskHigh { get; set; }

        /// <summary>
        /// Gets or sets the ask lowest price.
        /// </summary>
        public int AskLow { get; set; }

        /// <summary>
        /// Gets or sets the ask closing price.
        /// </summary>
        public int AskClose { get; set; }

        /// <summary>
        /// Gets or sets the bid opening price.
        /// </summary>
        public int BidOpen { get; set; }

        /// <summary>
        /// Gets or sets the bid highest price.
        /// </summary>
        public int BidHigh { get; set; }

        /// <summary>
        /// Gets or sets the bid lowest price.
        /// </summary>
        public int BidLow { get; set; }

        /// <summary>
        /// Gets or sets the bid closing price.
        /// </summary>
        public int BidClose { get; set; }
    }

}

