using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for BroadcastService settings.
    /// </summary>
    public class BroadcastServiceConfig
    {
        public const string SectionName = "Communications:BroadcastService";

        /// <summary>
        /// Gets or sets the interval in seconds between broadcast operations.
        /// </summary>
        /// <value>Default is 30 seconds.</value>
        [Required(ErrorMessage = "The 'BroadcastIntervalSeconds' is missing in the configuration.")]
        public int BroadcastIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for broadcast operations.
        /// </summary>
        /// <value>Default is 3 attempts.</value>
        [Required(ErrorMessage = "The 'BroadcastMaxRetryAttempts' is missing in the configuration.")]
        public int BroadcastMaxRetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the delay in seconds between broadcast retry attempts.
        /// </summary>
        /// <value>Default is 1 second.</value>
        [Required(ErrorMessage = "The 'BroadcastRetryDelaySeconds' is missing in the configuration.")]
        public int BroadcastRetryDelaySeconds { get; set; }

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for BroadcastService operations.
        /// </summary>
        /// <value>Default is false.</value>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
