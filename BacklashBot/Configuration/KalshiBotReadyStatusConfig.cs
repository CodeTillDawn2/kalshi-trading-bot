using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration options for the KalshiBotReadyStatus behavior.
    /// This configuration is loaded from the "Central:KalshiBotReadyStatus" section of the application configuration.
    /// </summary>
    public class KalshiBotReadyStatusConfig
    {
        /// <summary>
        /// The configuration section name for KalshiBotReadyStatusConfig.
        /// This constant defines the path in the configuration file where these settings are located.
        /// </summary>
        public const string SectionName = "Central:KalshiBotReadyStatus";

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled for the KalshiBotReadyStatus.
        /// When enabled, detailed timing and state change metrics are tracked and reported.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}