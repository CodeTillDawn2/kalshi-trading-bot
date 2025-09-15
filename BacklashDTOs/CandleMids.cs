namespace BacklashDTOs
{
    /// <summary>
    /// Helper class to cache candlestick mid-price values.
    /// </summary>
    public class CandleMids
    {
        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string? MarketTicker { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the candlestick.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the opening price.
        /// </summary>
        public double Open { get; set; }

        /// <summary>
        /// Gets or sets the closing price.
        /// </summary>
        public double Close { get; set; }

        /// <summary>
        /// Gets or sets the highest price.
        /// </summary>
        public double High { get; set; }

        /// <summary>
        /// Gets or sets the lowest price.
        /// </summary>
        public double Low { get; set; }

        /// <summary>
        /// Gets or sets the trading volume.
        /// </summary>
        public double Volume { get; set; }
    }

}
