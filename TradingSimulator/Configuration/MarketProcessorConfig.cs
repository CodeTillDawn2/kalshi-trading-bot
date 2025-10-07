using System.ComponentModel.DataAnnotations;

namespace TradingSimulator.Configuration
{
    /// <summary>
    /// Configuration class for MarketProcessor settings.
    /// </summary>
    public class MarketProcessorConfig
    {
        /// <summary>
        /// Configuration section name for MarketProcessor settings.
        /// </summary>
        public const string SectionName = "Simulator:MarketProcessor";

        /// <summary>
        /// Directory path where processed market data is cached.
        /// </summary>
        [Required]
        public string CacheDirectory { get; set; }

        /// <summary>
        /// File naming convention for cache files.
        /// </summary>
        [Required]
        public string FileNameTemplate { get; set; }

        /// <summary>
        /// Maximum number of markets to process concurrently in batch operations.
        /// </summary>
        [Required]
        public int MaxConcurrentMarkets { get; set; }

        /// <summary>
        /// Size of batches for processing multiple markets to reduce memory pressure.
        /// </summary>
        [Required]
        public int BatchSize { get; set; }

        /// <summary>
        /// Configuration for discrepancy detection.
        /// </summary>
        [Required]
        public DiscrepancyDetectionConfig DiscrepancyDetection { get; set; }

        /// <summary>
        /// Whether to enable performance metrics tracking (includes memory tracking).
        /// </summary>
        [Required]
        public bool EnablePerformanceMetrics { get; set; }
    }
}