using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration options for the TradingOverseer behavior, including performance metrics settings.
    /// This configuration is loaded from the "TradingOverseer" section of the application configuration.
    /// </summary>
    public class TradingOverseerConfig
    {
        /// <summary>
        /// The configuration section name for TradingOverseerConfig.
        /// This constant defines the path in the configuration file where these settings are located.
        /// </summary>
        public const string SectionName = "TradingOverseer";

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled for the TradingOverseer.
        /// When enabled, simulation execution times and memory usage are tracked and reported.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}