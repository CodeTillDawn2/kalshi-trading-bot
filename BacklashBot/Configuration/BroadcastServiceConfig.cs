using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for BroadcastService settings.
    /// </summary>
    public class BroadcastServiceConfig
    {
        /// <summary>
        /// Gets or sets the interval in seconds between broadcast operations.
        /// </summary>
        /// <value>Default is 30 seconds.</value>
        [Range(1, 3600, ErrorMessage = "BroadcastIntervalSeconds must be between 1 and 3600 seconds.")]
        public int BroadcastIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for broadcast operations.
        /// </summary>
        /// <value>Default is 3 attempts.</value>
        [Range(1, 10, ErrorMessage = "BroadcastMaxRetryAttempts must be between 1 and 10.")]
        public int BroadcastMaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay in seconds between broadcast retry attempts.
        /// </summary>
        /// <value>Default is 1 second.</value>
        [Range(1, 60, ErrorMessage = "BroadcastRetryDelaySeconds must be between 1 and 60 seconds.")]
        public int BroadcastRetryDelaySeconds { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for BroadcastService operations.
        /// </summary>
        /// <value>Default is false.</value>
        public bool EnablePerformanceMetrics { get; set; } = false;
    }
}