namespace SmokehousePatterns.Helpers
{
    public class CandleData
    {
        public DateTime Timestamp { get; set; }
        public long AskOpen { get; set; }
        public long AskHigh { get; set; }
        public long AskLow { get; set; }
        public long AskClose { get; set; }
        public long BidOpen { get; set; }
        public long BidHigh { get; set; }
        public long BidLow { get; set; }
        public long BidClose { get; set; }
        public long Volume { get; set; }

        /// <summary>
        /// Calculates the mid price using Open prices
        /// </summary>
        public double MidOpen()
        {
            return (AskOpen + BidOpen) / 2.0;
        }

        /// <summary>
        /// Calculates the mid price using High prices
        /// </summary>
        public double MidHigh()
        {
            return (AskHigh + BidHigh) / 2.0;
        }

        /// <summary>
        /// Calculates the mid price using Low prices
        /// </summary>
        public double MidLow()
        {
            return (AskLow + BidLow) / 2.0;
        }

        /// <summary>
        /// Calculates the mid price using Close prices
        /// </summary>
        public double MidClose()
        {
            return (AskClose + BidClose) / 2.0;
        }
    }

}
