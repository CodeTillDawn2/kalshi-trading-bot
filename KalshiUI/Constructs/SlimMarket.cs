namespace KalshiUI.Constructs
{
    public class SlimMarket
    {

        public string MarketTicker { get; set; }
        public string EventTicker { get; set; }
        public DateTime? LastCandlestick { get; set; }
        public string Status { get; set; }
        public DateTime? CloseTime { get; set; }
        public DateTime OpenTime { get; set; }
        public SlimMarket(string market_ticker, string event_ticker, DateTime? lastCandlestick, string status, DateTime? closeTime, DateTime openTime)
        {
            MarketTicker = market_ticker;
            LastCandlestick = lastCandlestick;
            Status = status;
            CloseTime = closeTime;
            OpenTime = openTime;
            EventTicker = event_ticker;
        }


    }
}
