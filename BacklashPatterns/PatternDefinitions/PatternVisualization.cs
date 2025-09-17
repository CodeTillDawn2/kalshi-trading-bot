using BacklashPatterns.PatternDefinitions;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Wrapper class for PatternDefinition that includes visualization data.
    /// </summary>
    public class PatternVisualization
    {
        /// <summary>
        /// Gets the underlying pattern definition.
        /// </summary>
        public PatternDefinition Pattern { get; }

        /// <summary>
        /// Gets the path to the generated image file.
        /// </summary>
        public string? ImagePath { get; set; }

        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public string Name => Pattern.Name;

        /// <summary>
        /// Gets the strength of the pattern.
        /// </summary>
        public double Strength => Pattern.Strength;

        /// <summary>
        /// Gets the certainty of the pattern.
        /// </summary>
        public double Certainty => Pattern.Certainty;

        /// <summary>
        /// Gets the uncertainty of the pattern.
        /// </summary>
        public double Uncertainty => Pattern.Uncertainty;

        /// <summary>
        /// Gets the list of candle indices forming the pattern.
        /// </summary>
        public List<int> Candles => Pattern.Candles;

        /// <summary>
        /// Initializes a new instance of the PatternVisualization class.
        /// </summary>
        /// <param name="pattern">The pattern definition to wrap.</param>
        /// <param name="imagePath">The path to the image file.</param>
        public PatternVisualization(PatternDefinition pattern, string? imagePath = null)
        {
            Pattern = pattern;
            ImagePath = imagePath;
        }
    }
}