using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration options for the MarketTypeService.
    /// </summary>
    public class MarketTypeServiceConfig
    {
        /// <summary>
        /// The configuration section name for MarketTypeServiceConfig.
        /// </summary>
        public const string SectionName = "Simulator:MarketTypeService";

        /// <summary>
        /// The expiration time for cached market type results in minutes.
        /// </summary>
        [Required(ErrorMessage = "The 'CacheExpirationMinutes' is missing in the configuration.")]
        public int CacheExpirationMinutes { get; set; }

        /// <summary>
        /// Whether to enable performance metrics collection for market type classification.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
