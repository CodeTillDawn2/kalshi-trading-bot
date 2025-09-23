using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for KalshiBotScopeManagerService settings.
    /// </summary>
    public class KalshiBotScopeManagerServiceConfig
    {
        /// <summary>
        /// The configuration section name for KalshiBotScopeManagerServiceConfig.
        /// </summary>
        public const string SectionName = "Central:KalshiBotScopeManagerService";

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for KalshiBotScopeManagerService operations.
        /// </summary>
        /// <value>Default is true.</value>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
