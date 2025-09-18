namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for OverseerClientService settings.
    /// </summary>
    public class OverseerClientServiceConfig
    {
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
        public bool EnablePerformanceMetrics { get; set; } = true;
    }
}