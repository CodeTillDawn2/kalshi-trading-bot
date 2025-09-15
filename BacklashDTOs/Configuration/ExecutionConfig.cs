namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for execution settings.
    /// </summary>
    public class ExecutionConfig
    {
        /// <summary>
        /// Gets or sets the timeout for market updates.
        /// </summary>
        public int MarketUpdateTimeout { get; set; }

        /// <summary>
        /// Gets or sets whether to launch the data dashboard.
        /// </summary>
        public bool LaunchDataDashboard { get; set; }

        /// <summary>
        /// Gets or sets the brain instance identifier.
        /// </summary>
        public string? BrainInstance { get; set; }

        /// <summary>
        /// Gets or sets whether to run overnight activities.
        /// </summary>
        public bool RunOvernightActivities { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of markets per subscription action.
        /// </summary>
        public int MaxMarketsPerSubscriptionAction { get; set; }

        /// <summary>
        /// Gets or sets the hard data storage location.
        /// </summary>
        public required string HardDataStorageLocation { get; set; }

        /// <summary>
        /// Gets or sets the target count for queues.
        /// </summary>
        public int QueuesTargetCount { get; set; }

        /// <summary>
        /// Gets or sets the target percentage for queues.
        /// </summary>
        public double QueuesTargetPercentage { get; set; }

        /// <summary>
        /// Gets or sets the limit for the notification queue.
        /// </summary>
        public int NotificationQueueLimit { get; set; } = 50;

        /// <summary>
        /// Gets or sets the limit for the orderbook queue.
        /// </summary>
        public int OrderbookQueueLimit { get; set; } = 50;

        /// <summary>
        /// Gets or sets the limit for the event queue.
        /// </summary>
        public int EventQueueLimit { get; set; } = 50;

        /// <summary>
        /// Gets or sets the limit for the ticker queue.
        /// </summary>
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
        /// Gets or sets whether to enable performance metrics collection for OverseerClientService operations.
        /// When disabled, metrics collection is skipped to improve performance.
        /// </summary>
        /// <value>Default is true.</value>
        public bool OverseerClientService_EnablePerformanceMetrics { get; set; } = true;

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

        /// <summary>
        /// Gets or sets the connection health check interval in seconds.
        /// </summary>
        /// <value>Default is 30 seconds.</value>
        public int ConnectionHealthCheckIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the connection timeout in seconds for health monitoring.
        /// </summary>
        /// <value>Default is 60 seconds.</value>
        public int ConnectionHealthTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the authentication token validity duration in hours.
        /// </summary>
        /// <value>Default is 24 hours.</value>
        public int AuthTokenValidityHours { get; set; } = 24;

        /// <summary>
        /// Gets or sets the rate limit for handshake operations per minute per IP.
        /// </summary>
        /// <value>Default is 10 handshakes per minute.</value>
        public int HandshakeRateLimitPerMinute { get; set; } = 10;

        /// <summary>
        /// Gets or sets the rate limit for check-in operations per minute per client.
        /// </summary>
        /// <value>Default is 60 check-ins per minute.</value>
        public int CheckInRateLimitPerMinute { get; set; } = 60;

        /// <summary>
        /// Gets or sets the message batch size for broadcast operations.
        /// </summary>
        /// <value>Default is 50 messages per batch.</value>
        public int MessageBatchSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets the message batch processing interval in milliseconds.
        /// </summary>
        /// <value>Default is 100 milliseconds.</value>
        public int MessageBatchIntervalMs { get; set; } = 100;

        /// <summary>
        /// Gets or sets the stale connection cleanup interval in minutes.
        /// </summary>
        /// <value>Default is 5 minutes.</value>
        public int StaleConnectionCleanupIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum age for connections to be considered stale in minutes.
        /// </summary>
        /// <value>Default is 10 minutes.</value>
        public int MaxConnectionAgeMinutes { get; set; } = 10;

        // Candlestick Service Configuration
        /// <summary>
        /// Gets or sets the data retention period in days for candlestick data.
        /// Data older than this will be subject to cleanup.
        /// </summary>
        /// <value>Default is 365 days (1 year).</value>
        public int CandlestickDataRetentionDays { get; set; } = 365;

        /// <summary>
        /// Gets or sets the cleanup interval in hours for removing old candlestick data.
        /// </summary>
        /// <value>Default is 24 hours.</value>
        public int CandlestickCleanupIntervalHours { get; set; } = 24;

        /// <summary>
        /// Gets or sets the maximum number of candlesticks to keep per market per interval.
        /// </summary>
        /// <value>Default is 10000 candlesticks.</value>
        public int MaxCandlesticksPerMarket { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the maximum number of parallel tasks for candlestick processing.
        /// </summary>
        /// <value>Default is 4 parallel tasks.</value>
        public int MaxParallelCandlestickTasks { get; set; } = 4;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism for data loading operations.
        /// </summary>
        /// <value>Default is 2.</value>
        public int MaxDegreeOfParallelismDataLoading { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for CandlestickService operations.
        /// </summary>
        /// <value>Default is true.</value>
        public bool EnableCandlestickServicePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable database performance metrics collection in CentralPerformanceMonitor.
        /// </summary>
        /// <value>Default is true.</value>
        public bool CentralPerformanceMonitor_EnableDatabaseMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for MarketAnalysisHelper operations.
        /// </summary>
        /// <value>Default is true.</value>
        public bool EnableMarketAnalysisHelperPerformanceMetrics { get; set; } = true;
    }
}