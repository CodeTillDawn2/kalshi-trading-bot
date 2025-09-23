using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Services
{
    /// <summary>
    /// Configuration options for the WebSocketMonitorService behavior, including monitoring intervals
    /// and performance metrics settings. This configuration is loaded from the
    /// "Websocket:WebSocketMonitor" section of the application configuration.
    /// </summary>
    public class WebSocketMonitorServiceConfig
    {
        /// <summary>
        /// The configuration section name for WebSocketMonitorServiceConfig.
        /// This constant defines the path in the configuration file where these settings are located.
        /// </summary>
        public const string SectionName = "Websockets:WebSocketMonitor";

        /// <summary>
        /// Gets or sets the interval in minutes between exchange status checks.
        /// This controls how frequently the service monitors the exchange's operational status.
        /// </summary>
        [Required(ErrorMessage = "The 'MonitoringIntervalMinutes' is missing in the configuration.")]
        public int MonitoringIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the delay in minutes before retrying after a failed exchange status check.
        /// This helps prevent overwhelming the API with rapid retry attempts during outages.
        /// </summary>
        [Required(ErrorMessage = "The 'RetryDelayMinutes' is missing in the configuration.")]
        public int RetryDelayMinutes { get; set; }

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled for the WebSocketMonitorService.
        /// When enabled, detailed timing and reliability metrics are tracked and reported.
        /// </summary>
        [Required(ErrorMessage = "The 'EnableWebSocketMonitorMetrics' is missing in the configuration.")]
        public bool EnableWebSocketMonitorMetrics { get; set; }
    }
}