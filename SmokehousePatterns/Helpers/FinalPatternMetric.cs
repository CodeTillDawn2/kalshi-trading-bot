namespace SmokehousePatterns.Helpers
{
    // Helper class for final pattern metrics (to save in JSON)
    public class FinalPatternMetric
    {
        public double Strength { get; set; } // -100 to +100
        public double Certainty { get; set; } // 0–100
        public double Uncertainty { get; set; } // 0–100
    }
}
