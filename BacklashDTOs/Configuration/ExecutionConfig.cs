namespace BacklashDTOs.Configuration
{
    public class ExecutionConfig
    {
        public int MarketUpdateTimeout { get; set; }
        public bool LaunchDataDashboard { get; set; }
        public string? BrainInstance { get; set; }
        public bool RunOvernightActivities { get; set; }
        public int MaxMarketsPerSubscriptionAction { get; set; }
        public required string HardDataStorageLocation { get; set; }
        public int QueuesTargetCount { get; set; }
        public double QueuesTargetPercentage { get; set; }
        public int NotificationQueueLimit { get; set; } = 50;
        public int OrderbookQueueLimit { get; set; } = 50;
        public int EventQueueLimit { get; set; } = 50;
        public int TickerQueueLimit { get; set; } = 50;

        /// <summary>
        /// Gets or sets the percentage threshold for queue high count alerts.
        /// When the EventQueue utilization exceeds this percentage, a performance alert is logged.
        /// </summary>
        /// <value>Default is 80.0%.</value>
        public double QueueHighCountAlertThreshold { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the percentage threshold for refresh usage alerts.
        /// When the market refresh cycle usage exceeds this percentage, a performance alert is logged.
        /// </summary>
        /// <value>Default is 90.0%.</value>
        public double RefreshUsageAlertThreshold { get; set; } = 90.0;

        /// <summary>
        /// Gets or sets the absolute count threshold for queue alerts.
        /// When any queue's average count exceeds this value, a performance alert is logged.
        /// </summary>
        /// <value>Default is 100 items.</value>
        public int QueueCountAlertThreshold { get; set; } = 100;

        /// <summary>
        /// Gets or sets the retry delay in milliseconds for operations that require retries.
        /// </summary>
        /// <value>Default is 5000 milliseconds (5 seconds).</value>
        public int RetryDelayMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the length of the session identifier string.
        /// </summary>
        /// <value>Default is 5 characters.</value>
        public int SessionIdLength { get; set; } = 5;

        // OverseerClientService Configuration
        /// <summary>
        /// Gets or sets the timeout in seconds for connection attempts to the overseer.
        /// </summary>
        /// <value>Default is 30 seconds.</value>
        public int OverseerConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the timeout in seconds for semaphore operations.
        /// </summary>
        /// <value>Default is 10 seconds.</value>
        public int OverseerSemaphoreTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Gets or sets the interval in minutes for overseer discovery operations.
        /// </summary>
        /// <value>Default is 3 minutes.</value>
        public int OverseerDiscoveryIntervalMinutes { get; set; } = 3;

        /// <summary>
        /// Gets or sets the interval in seconds for check-in operations.
        /// </summary>
        /// <value>Default is 30 seconds.</value>
        public int OverseerCheckInIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the failure threshold for the circuit breaker pattern.
        /// </summary>
        /// <value>Default is 5 failures.</value>
        public int OverseerCircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the timeout in minutes for the circuit breaker to reset.
        /// </summary>
        /// <value>Default is 5 minutes.</value>
        public int OverseerCircuitBreakerTimeoutMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets the batch size for overnight market refresh operations.
        /// </summary>
        /// <value>Default is 20 markets per batch.</value>
        public int OvernightBatchSize { get; set; } = 20;

        /// <summary>
        /// Gets or sets the retry delay in minutes for overnight market refresh operations.
        /// </summary>
        /// <value>Default is 30 minutes.</value>
        public int OvernightRetryDelayMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets the age threshold in hours for interest score calculations.
        /// Markets with interest scores older than this will be recalculated.
        /// </summary>
        /// <value>Default is 12 hours.</value>
        public int InterestScoreAgeThresholdHours { get; set; } = 12;

        /// <summary>
        /// Gets or sets the failure threshold for the market refresh circuit breaker.
        /// </summary>
        /// <value>Default is 5 failures.</value>
        public int MarketRefreshCircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the timeout in minutes for the market refresh circuit breaker to reset.
        /// </summary>
        /// <value>Default is 5 minutes.</value>
        public int MarketRefreshCircuitBreakerTimeoutMinutes { get; set; } = 5;
    }
}