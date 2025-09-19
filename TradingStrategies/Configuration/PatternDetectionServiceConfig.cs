using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration options for pattern detection parameters.
    /// </summary>
    public class PatternDetectionServiceConfig
    {
        /// <summary>
        /// The lookback window in periods for trend context and pattern validation.
        /// </summary>
        [JsonRequired]
        public int LookbackWindow { get; set; }

        /// <summary>
        /// The types of patterns to detect. If empty, all patterns are detected.
        /// </summary>
        [JsonRequired]
        public List<string> PatternTypes { get; set; } = new List<string>();

        /// <summary>
        /// Minimum price change threshold for significance check.
        /// </summary>
        [JsonRequired]
        public double SignificancePriceThreshold { get; set; }

        /// <summary>
        /// Minimum volume increase multiplier for context check.
        /// </summary>
        [JsonRequired]
        public double VolumeIncreaseMultiplier { get; set; }

        /// <summary>
        /// Initial capacity for patterns array per candle.
        /// </summary>
        [JsonRequired]
        public int InitialPatternCapacity { get; set; }

        /// <summary>
        /// Whether to enable parallel processing for pattern detection.
        /// </summary>
        [JsonRequired]
        public bool EnableParallelProcessing { get; set; }

        /// <summary>
        /// Maximum degree of parallelism for pattern checks.
        /// </summary>
        [JsonRequired]
        public int MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Whether to enable detailed pattern detection performance metrics collection and logging.
        /// When disabled, no performance metrics or logging occurs. When enabled, comprehensive
        /// metrics including execution time, candles processed, patterns found, and per-pattern
        /// timing are collected and logged.
        /// </summary>
        [JsonRequired]
        public bool EnablePatternDetectionMetrics { get; set; }
    }
}