namespace BacklashDTOs.Configuration
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
        public int BroadcastIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for broadcast operations.
        /// </summary>
        /// <value>Default is 3 attempts.</value>
        public int BroadcastMaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay in seconds between broadcast retry attempts.
        /// </summary>
        /// <value>Default is 1 second.</value>
        public int BroadcastRetryDelaySeconds { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for BroadcastService operations.
        /// </summary>
        /// <value>Default is false.</value>
        public bool BroadcastService_EnablePerformanceMetrics { get; set; } = false;
    }
}