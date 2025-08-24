namespace SmokehouseDTOs
{
    public class PseudoCandlestick
    {
        public DateTime Timestamp { get; set; }  // End of the 1-minute period
        public double MidClose { get; set; }     // Average yes_ask for tickers, or yes_ask_close for candlesticks
        public double MidHigh { get; set; }      // Max yes_ask for tickers, or yes_ask_high for candlesticks
        public double MidLow { get; set; }       // Min yes_ask for tickers, or yes_ask_low for candlesticks
        public decimal Volume { get; set; }          // Summed volume in the minute
        public bool IsFromCandlestick { get; set; }  // Track source for debugging or logic
    }
}