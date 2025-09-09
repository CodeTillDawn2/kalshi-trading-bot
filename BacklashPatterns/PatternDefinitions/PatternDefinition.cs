namespace BacklashPatterns.PatternDefinitions
{
    public abstract class PatternDefinition
    {

        // Abstract property for the pattern name
        public abstract string Name { get; }
        public abstract double Strength { get; protected set; }
        public abstract double Certainty { get; protected set; }
        public abstract double Uncertainty { get; protected set; }

        // Instance property to store the candle indices forming the pattern
        public List<int> Candles { get; protected set; }

        // Protected constructor to ensure derived classes initialize Candles
        protected PatternDefinition(List<int> candles)
        {
            Candles = new List<int>(candles); // Defensive copy
        }

    }
}
