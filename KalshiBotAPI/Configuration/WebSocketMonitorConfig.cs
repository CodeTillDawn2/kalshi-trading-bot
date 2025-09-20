using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for WebSocketMonitor operations.
/// </summary>
public class WebSocketMonitorConfig
{
    /// <summary>
    /// The configuration section name for WebSocketMonitorConfig.
    /// </summary>
    public const string SectionName = "Websockets:WebSocketMonitor";

    /// <summary>
    /// Gets or sets the monitoring interval in minutes.
    /// </summary>
    [Required(ErrorMessage = "The 'MonitoringIntervalMinutes' is missing in the configuration.")]
    public int MonitoringIntervalMinutes { get; set; }

    /// <summary>
    /// Gets or sets the retry delay in minutes.
    /// </summary>
    [Required(ErrorMessage = "The 'RetryDelayMinutes' is missing in the configuration.")]
    public int RetryDelayMinutes { get; set; }

    /// <summary>
    /// Gets or sets whether WebSocket monitor metrics are enabled.
    /// </summary>
    [Required(ErrorMessage = "The 'EnableWebSocketMonitorMetrics' is missing in the configuration.")]
    public bool EnableWebSocketMonitorMetrics { get; set; }
}
