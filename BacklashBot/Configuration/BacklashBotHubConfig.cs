using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for BacklashBotHub settings.
    /// </summary>
    public class BacklashBotHubConfig
    {
        /// <summary>
        /// The configuration section name for BacklashBotHub settings.
        /// </summary>
        public const string SectionName = "Communications:BacklashBotHub";

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for BacklashBotHub operations.
        /// </summary>
        /// <value>Default is true.</value>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }

        /// <summary>
        /// Gets or sets the interval in minutes for aggregating and posting performance metrics.
        /// </summary>
        /// <value>Default is 1 minute.</value>
        [Required(ErrorMessage = "The 'AggregationIntervalMinutes' is missing in the configuration.")]
        public int AggregationIntervalMinutes { get; set; } = 1;
    }
}