namespace BacklashDTOs
{
    public class CandlestickData
    {
        public DateTime Date { get; set; }
        public string? MarketTicker { get; set; }
        public int IntervalType { get; set; }
        public int OpenInterest { get; set; }
        public int Volume { get; set; }
        public int AskOpen { get; set; }
        public int AskHigh { get; set; }
        public int AskLow { get; set; }
        public int AskClose { get; set; }
        public int BidOpen { get; set; }
        public int BidHigh { get; set; }
        public int BidLow { get; set; }
        public int BidClose { get; set; }
    }

}

