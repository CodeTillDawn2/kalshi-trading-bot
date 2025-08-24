using SmokehousePatterns.Helpers;

namespace SmokehousePatterns
{
    // Helper class for final pattern metrics (to save in JSON)
    public class PatternOccurrence
    {
        public DateTime Timestamp { get; set; }
        public List<CandleData> Candles { get; set; }
        public List<int> Indices { get; set; }
    }
}
