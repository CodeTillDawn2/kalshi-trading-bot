namespace SmokehousePatterns
{
    // Helper class to cache ask price values
    public class CandleMids
    {
        public DateTime Timestamp { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Volume { get; set; }
    }

}
