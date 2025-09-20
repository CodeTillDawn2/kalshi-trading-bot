using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashOverseer.Config
{
    /// <summary>
    /// Configuration options for the OverseerHub SignalR hub.
    /// </summary>
    public class OverseerHubConfig
    {
        /// <summary>
        /// The configuration section name for OverseerHubConfig.
        /// </summary>
        public const string SectionName = "Endpoints:OverseerHub";

        /// <summary>
        /// Gets or sets the timeout in seconds for connection health monitoring.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        [Required(ErrorMessage = "The 'ConnectionHealthTimeoutSeconds' is missing in the configuration.")]
        public int ConnectionHealthTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds between health checks.
        /// Default is 60 seconds (1 minute).
        /// </summary>
        [Required(ErrorMessage = "The 'HealthCheckIntervalSeconds' is missing in the configuration.")]
        public int HealthCheckIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the validity duration for authentication tokens in hours.
        /// Default is 24 hours (1 day).
        /// </summary>
        [Required(ErrorMessage = "The 'AuthTokenValidityHours' is missing in the configuration.")]
        public int AuthTokenValidityHours { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of handshake requests allowed per minute per IP.
        /// Default is 10.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxHandshakeRequestsPerMinute' is missing in the configuration.")]
        public int MaxHandshakeRequestsPerMinute { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of check-in requests allowed per minute per client.
        /// Default is 60.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxCheckInRequestsPerMinute' is missing in the configuration.")]
        public int MaxCheckInRequestsPerMinute { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether OverseerHub performance metrics collection is enabled.
        /// When enabled, all performance metrics including latency tracking, message counting,
        /// connection monitoring, and brain metrics are collected and recorded.
        /// When disabled, only essential operations are performed with minimal overhead.
        /// Default is false for performance reasons.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
