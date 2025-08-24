namespace SmokehouseDTOs
{
    public class SupportResistanceLevel
    {
        public double Price { get; set; }
        public int TestCount { get; set; } // Number of times tested
        public long TotalVolume { get; set; } // Cumulative volume at this level
        public int CandlestickCount { get; set; } // Number of candlesticks contributing
        public double Strength { get; set; }
    }
}
