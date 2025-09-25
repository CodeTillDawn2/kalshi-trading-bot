using Microsoft.Extensions.Configuration;

namespace BacklashPatterns
{
    /// <summary>
    /// Configuration class for pattern detection thresholds and settings.
    /// </summary>
    public class PatternDetectionConfig
    {
        /// <summary>
        /// Gets the configuration section name for pattern detection settings.
        /// </summary>
        public const string SectionName = "Simulator:PatternDetectionService";

        /// <summary>
        /// Minimum price change threshold for significance check.
        /// </summary>
        public double SignificancePriceThreshold { get; set; }

        /// <summary>
        /// Minimum volume increase multiplier for context check.
        /// </summary>
        public double VolumeIncreaseMultiplier { get; set; }

        /// <summary>
        /// Initial capacity for patterns array per candle.
        /// </summary>
        public int InitialPatternCapacity { get; set; }

        /// <summary>
        /// Whether to enable parallel processing for pattern detection.
        /// </summary>
        public bool EnableParallelProcessing { get; set; }

        /// <summary>
        /// Maximum degree of parallelism for pattern checks.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }
    }
}