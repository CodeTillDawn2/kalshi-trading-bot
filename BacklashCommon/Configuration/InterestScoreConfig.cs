using System.ComponentModel.DataAnnotations;

namespace BacklashCommon.Configuration
{
    /// <summary>
    /// Configuration class for InterestScoreService settings.
    /// Controls cache duration, performance monitoring, and operational parameters
    /// for the interest scoring system. These settings can be configured via appsettings.json
    /// and are injected through dependency injection.
    /// </summary>
    public class InterestScoreConfig
    {
        /// <summary>
        /// The configuration section name for InterestScoreConfig.
        /// </summary>
        public const string SectionName = "WatchedMarkets:InterestScore";

        /// <summary>
        /// Gets or sets the cache duration in hours for percentile thresholds and market values.
        /// Default is 6 hours. Increasing this reduces database queries but may use stale data.
        /// Lower values provide more current data but increase computational overhead.
        /// </summary>
        [Required(ErrorMessage = "The 'CacheDurationHours' is missing in the configuration.")]
        public int CacheDurationHours { get; set; }

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection.
        /// When enabled, tracks scoring operation times, cache hit rates, and other performance indicators.
        /// Disabling this improves performance but removes monitoring capabilities.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of performance metrics to retain in memory.
        /// Older metrics are discarded when this limit is reached to prevent memory leaks.
        /// Higher values provide better historical analysis but consume more memory.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxPerformanceMetricsHistory' is missing in the configuration.")]
        public int MaxPerformanceMetricsHistory { get; set; }
    }
}
