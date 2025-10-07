using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for MarketManagerService-specific settings.
    /// </summary>
    public class MarketManagerServiceConfig
    {
        /// <summary>
        /// The configuration section name for MarketManagerServiceConfig.
        /// </summary>
        public const string SectionName = "Management:MarketManagerService";

        /// <summary>
        /// Gets or sets whether performance metrics are enabled for market management services.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}