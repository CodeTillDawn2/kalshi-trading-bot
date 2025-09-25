using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for MarketDataInitializer settings.
    /// </summary>
    public class MarketDataInitializerConfig
    {
        /// <summary>
        /// The configuration section name for MarketDataInitializer settings.
        /// </summary>
        public const string SectionName = "WatchedMarkets:MarketDataInitializer";

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for MarketDataInitializer operations.
        /// </summary>
        /// <value>Default is false.</value>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
