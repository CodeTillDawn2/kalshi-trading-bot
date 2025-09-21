using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for OverseerClientService settings.
    /// </summary>
    public class OverseerClientServiceConfig
    {
        /// <summary>
        /// The configuration section name for OverseerClientServiceConfig.
        /// </summary>
        public const string SectionName = "Communications:OverseerClientService";

        /// <summary>
        /// Gets or sets the timeout in seconds for connection attempts to the overseer.
        /// </summary>
        /// <value>Default is 30 seconds.</value>
        [Required(ErrorMessage = "The 'OverseerConnectionTimeoutSeconds' is missing in the configuration.")]
        public int OverseerConnectionTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds for semaphore operations.
        /// </summary>
        /// <value>Default is 10 seconds.</value>
        [Required(ErrorMessage = "The 'OverseerSemaphoreTimeoutSeconds' is missing in the configuration.")]
        public int OverseerSemaphoreTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the interval in minutes for overseer discovery operations.
        /// </summary>
        /// <value>Default is 3 minutes.</value>
        [Required(ErrorMessage = "The 'OverseerDiscoveryIntervalMinutes' is missing in the configuration.")]
        public int OverseerDiscoveryIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds for check-in operations.
        /// </summary>
        /// <value>Default is 30 seconds.</value>
        [Required(ErrorMessage = "The 'OverseerCheckInIntervalSeconds' is missing in the configuration.")]
        public int OverseerCheckInIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the failure threshold for the circuit breaker pattern.
        /// </summary>
        /// <value>Default is 5 failures.</value>
        [Required(ErrorMessage = "The 'OverseerCircuitBreakerFailureThreshold' is missing in the configuration.")]
        public int OverseerCircuitBreakerFailureThreshold { get; set; }

        /// <summary>
        /// Gets or sets the timeout in minutes for the circuit breaker to reset.
        /// </summary>
        /// <value>Default is 5 minutes.</value>
        [Required(ErrorMessage = "The 'OverseerCircuitBreakerTimeoutMinutes' is missing in the configuration.")]
        public int OverseerCircuitBreakerTimeoutMinutes { get; set; }

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for OverseerClientService operations.
        /// When disabled, metrics collection is skipped to improve performance.
        /// </summary>
        /// <value>Default is true.</value>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
