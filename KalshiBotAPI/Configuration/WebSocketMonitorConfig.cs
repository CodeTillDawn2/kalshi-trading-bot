using System.Text.Json.Serialization;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for WebSocketMonitor operations.
/// </summary>
public class WebSocketMonitorConfig
{
    /// <summary>
    /// Gets or sets the monitoring interval in minutes.
    /// </summary>
    public required int MonitoringIntervalMinutes { get; set; }

    /// <summary>
    /// Gets or sets the retry delay in minutes.
    /// </summary>
    public required int RetryDelayMinutes { get; set; }

    /// <summary>
    /// Gets or sets whether WebSocket monitor metrics are enabled.
    /// </summary>
    public required bool EnableWebSocketMonitorMetrics { get; set; }
}
