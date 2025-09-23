using System.ComponentModel.DataAnnotations;

namespace BacklashOverseer.Config
{
    /// <summary>
    /// Configuration options for the MarketWatchController behavior, including cache durations
    /// and performance metrics settings. This configuration is loaded from the
    /// "Endpoints:MarketWatchController" section of the application configuration.
    /// </summary>
    public class MarketWatchControllerConfig
    {
        /// <summary>
        /// The configuration section name for MarketWatchControllerConfig.
        /// This constant defines the path in the configuration file where these settings are located.
        /// </summary>
        public const string SectionName = "Endpoints:MarketWatchController";

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled for the MarketWatchController.
        /// When enabled, cache hit/miss statistics are tracked and reported to the performance metrics service.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }

        /// <summary>
        /// Gets or sets the duration in minutes for which active markets data is cached in memory.
        /// This helps reduce database load by caching frequently accessed market information.
        /// </summary>
        [Required(ErrorMessage = "The 'MarketsCacheDurationMinutes' is missing in the configuration.")]
        public int MarketsCacheDurationMinutes { get; set; }

        /// <summary>
        /// Gets or sets the duration in minutes for which log data (snapshot and error timestamps) is cached.
        /// Shorter cache duration ensures more up-to-date log information while still providing performance benefits.
        /// </summary>
        [Required(ErrorMessage = "The 'LogDataCacheDurationMinutes' is missing in the configuration.")]
        public int LogDataCacheDurationMinutes { get; set; }
    }
}
