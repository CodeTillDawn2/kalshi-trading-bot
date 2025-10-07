using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration options for the KalshiBotStatusTracker behavior.
    /// This configuration is loaded from the "Central:KalshiBotStatusTracker" section of the application configuration.
    /// </summary>
    public class KalshiBotStatusTrackerConfig
    {
        /// <summary>
        /// The configuration section name for KalshiBotStatusTrackerConfig.
        /// This constant defines the path in the configuration file where these settings are located.
        /// </summary>
        public const string SectionName = "Central:KalshiBotStatusTracker";

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled for the KalshiBotStatusTracker.
        /// When enabled, detailed timing and operation metrics are tracked and reported.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}