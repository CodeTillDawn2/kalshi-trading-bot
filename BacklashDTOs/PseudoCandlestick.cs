namespace BacklashDTOs
{
    /// <summary>
    /// Represents a pseudo candlestick data point.
    /// </summary>
    public class PseudoCandlestick
    {
        /// <summary>
        /// Gets or sets the timestamp (end of the 1-minute period).
        /// </summary>
        public DateTime Timestamp { get; set; }  // End of the 1-minute period
        /// <summary>
        /// Gets or sets the mid close price (average yes_ask for tickers, or yes_ask_close for candlesticks).
        /// </summary>
        public double MidClose { get; set; }     // Average yes_ask for tickers, or yes_ask_close for candlesticks
        /// <summary>
        /// Gets or sets the mid high price (max yes_ask for tickers, or yes_ask_high for candlesticks).
        /// </summary>
        public double MidHigh { get; set; }      // Max yes_ask for tickers, or yes_ask_high for candlesticks
        /// <summary>
        /// Gets or sets the mid low price (min yes_ask for tickers, or yes_ask_low for candlesticks).
        /// </summary>
        public double MidLow { get; set; }       // Min yes_ask for tickers, or yes_ask_low for candlesticks
        /// <summary>
        /// Gets or sets the volume (summed volume in the minute).
        /// </summary>
        public decimal Volume { get; set; }          // Summed volume in the minute
        /// <summary>
        /// Gets or sets a value indicating whether this is from a candlestick (track source for debugging or logic).
        /// </summary>
        public bool IsFromCandlestick { get; set; }  // Track source for debugging or logic


    }
}
