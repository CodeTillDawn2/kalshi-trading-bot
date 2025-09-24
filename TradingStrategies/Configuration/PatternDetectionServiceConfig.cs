using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration options for pattern detection parameters.
    /// </summary>
    public class PatternDetectionServiceConfig : IValidatableObject
    {
        /// <summary>
        /// The configuration section name for PatternDetectionServiceConfig.
        /// </summary>
        public const string SectionName = "Simulator:PatternDetectionService";

        /// <summary>
        /// The lookback window in periods for trend context and pattern validation.
        /// </summary>
        [Required(ErrorMessage = "The 'LookbackWindow' is missing in the configuration.")]
        public int LookbackWindow { get; set; }

        /// <summary>
        /// The types of patterns to detect. Use ["All"] to detect all patterns, or specify a list of pattern names.
        /// </summary>
        [Required(ErrorMessage = "The 'PatternTypes' is missing in the configuration.")]
        public string[] PatternTypes { get; set; } = null!;

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PatternTypes.Contains("All") && PatternTypes.Length > 1)
            {
                yield return new ValidationResult("If 'All' is specified in PatternTypes, it must be the only entry.", new[] { nameof(PatternTypes) });
            }
        }

        /// <summary>
        /// Minimum price change threshold for significance check.
        /// </summary>
        [Required(ErrorMessage = "The 'SignificancePriceThreshold' is missing in the configuration.")]
        public double SignificancePriceThreshold { get; set; }

        /// <summary>
        /// Minimum volume increase multiplier for context check.
        /// </summary>
        [Required(ErrorMessage = "The 'VolumeIncreaseMultiplier' is missing in the configuration.")]
        public double VolumeIncreaseMultiplier { get; set; }

        /// <summary>
        /// Initial capacity for patterns array per candle.
        /// </summary>
        [Required(ErrorMessage = "The 'InitialPatternCapacity' is missing in the configuration.")]
        public int InitialPatternCapacity { get; set; }

        /// <summary>
        /// Whether to enable parallel processing for pattern detection.
        /// </summary>
        [Required(ErrorMessage = "The 'EnableParallelProcessing' is missing in the configuration.")]
        public bool EnableParallelProcessing { get; set; }

        /// <summary>
        /// Maximum degree of parallelism for pattern checks.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxDegreeOfParallelism' is missing in the configuration.")]
        public int MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Whether to enable detailed pattern detection performance metrics collection and logging.
        /// When disabled, no performance metrics or logging occurs. When enabled, comprehensive
        /// metrics including execution time, candles processed, patterns found, and per-pattern
        /// timing are collected and logged.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePatternDetectionMetrics' is missing in the configuration.")]
        public bool EnablePatternDetectionMetrics { get; set; }
    }
}
