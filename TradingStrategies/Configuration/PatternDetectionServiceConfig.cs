using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration options for pattern detection parameters.
    /// </summary>
    public class PatternDetectionServiceConfig
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
        /// The types of patterns to detect. If empty, all patterns are detected.
        /// </summary>
        [Required(ErrorMessage = "The 'PatternTypes' is missing in the configuration.")]
        public List<string> PatternTypes { get; set; } = null!;

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
