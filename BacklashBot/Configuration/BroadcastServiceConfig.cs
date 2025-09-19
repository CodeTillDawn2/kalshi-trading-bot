using System.Text.Json.Serialization;

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
        [JsonRequired]
        public int BroadcastIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for broadcast operations.
        /// </summary>
        /// <value>Default is 3 attempts.</value>
        [JsonRequired]
        public int BroadcastMaxRetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the delay in seconds between broadcast retry attempts.
        /// </summary>
        /// <value>Default is 1 second.</value>
        [JsonRequired]
        public int BroadcastRetryDelaySeconds { get; set; }

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for BroadcastService operations.
        /// </summary>
        /// <value>Default is false.</value>
        [JsonRequired]
        public bool EnablePerformanceMetrics { get; set; }
    }
}