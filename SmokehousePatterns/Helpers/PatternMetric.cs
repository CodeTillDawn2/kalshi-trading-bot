namespace SmokehousePatterns.Helpers
{
    public class PatternMetric
    {
        public string PatternName { get; set; } // Added for clarity
        public List<double> Strengths { get; set; } = new List<double>();
        public List<double> Certainties { get; set; } = new List<double>();
        public List<double> Uncertainties { get; set; } = new List<double>();

        public PatternMetric()
        {
            PatternName = "";
        }

        public PatternMetric(string patternName)
        {
            PatternName = patternName;
        }
    }
}
