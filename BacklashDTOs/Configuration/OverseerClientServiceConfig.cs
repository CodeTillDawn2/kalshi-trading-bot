using System.Text.Json.Serialization;

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
        public required int OverseerConnectionTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds for semaphore operations.
        /// </summary>
        /// <value>Default is 10 seconds.</value>
        public required int OverseerSemaphoreTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the interval in minutes for overseer discovery operations.
        /// </summary>
        /// <value>Default is 3 minutes.</value>
        public required int OverseerDiscoveryIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds for check-in operations.
        /// </summary>
        /// <value>Default is 30 seconds.</value>
        public required int OverseerCheckInIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the failure threshold for the circuit breaker pattern.
        /// </summary>
        /// <value>Default is 5 failures.</value>
        public required int OverseerCircuitBreakerFailureThreshold { get; set; }

        /// <summary>
        /// Gets or sets the timeout in minutes for the circuit breaker to reset.
        /// </summary>
        /// <value>Default is 5 minutes.</value>
        public required int OverseerCircuitBreakerTimeoutMinutes { get; set; }

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for OverseerClientService operations.
        /// When disabled, metrics collection is skipped to improve performance.
        /// </summary>
        /// <value>Default is true.</value>
        public required bool EnablePerformanceMetrics { get; set; }
    }
}
