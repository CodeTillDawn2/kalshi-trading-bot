namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents the base class for all pattern definitions.
    /// </summary>
    public abstract class PatternDefinition
    {

        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public abstract string Description { get; }
        /// <summary>
        /// Gets the strength of the pattern.
        /// </summary>
        public abstract double Strength { get; protected set; }
        /// <summary>
        /// Gets the certainty of the pattern.
        /// </summary>
        public abstract double Certainty { get; protected set; }
        /// <summary>
        /// Gets the uncertainty of the pattern.
        /// </summary>
        public abstract double Uncertainty { get; protected set; }

        /// <summary>
        /// Gets the list of candle indices forming the pattern.
        /// </summary>
        public List<int> Candles { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the PatternDefinition class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        protected PatternDefinition(List<int> candles)
        {
            Candles = new List<int>(candles); // Defensive copy
        }

        /// <summary>
        /// Calculates comparative strength using historical cache data.
        /// </summary>
        /// <param name="cache">The historical pattern cache.</param>
        /// <param name="patternName">The name of the pattern.</param>
        /// <param name="powerScore">The power score (0-1).</param>
        /// <param name="matchScore">The match score (0-1).</param>
        /// <returns>The comparative strength (0-1).</returns>
        public static double CalculateComparativeStrength(HistoricalPatternCache cache, string patternName, double powerScore, double matchScore)
        {
            return cache.CalculateComparativeStrength(patternName, powerScore, matchScore);
        }
    }
}








