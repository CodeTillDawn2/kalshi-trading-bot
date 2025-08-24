namespace TradingSimulator.Strategies
{
    public class TradingDecision
    {
        public Dictionary<string, double> Signals { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public bool IsFinal { get; set; } // Indicates if decision is actionable

        public void AddSignal(string key, double value) => Signals[key] = value;
        public void AddMetadata(string key, object value) => Metadata[key] = value;
    }
}