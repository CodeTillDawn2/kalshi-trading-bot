namespace SmokehousePatterns.Helpers
{
    public class MarketPatternOccurrence
    {
        public string PatternName { get; set; }
        public string MarketName { get; set; }
        public DateTime Timestamp { get; set; }
        public List<CandleData> Candles { get; set; }
        public List<CandleData> LookbackCandles { get; set; } = new List<CandleData>();
        public List<CandleData> LookForwardCandles { get; set; } = new List<CandleData>();
        public List<int> Indices { get; set; }
        public int LookbackPeriod { get; set; }
        public int LookForwardPeriod { get; set; }
    }
}
