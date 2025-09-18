namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for WebSocketMonitor operations.
/// </summary>
public class WebSocketMonitorConfig
{
    /// <summary>
    /// Gets or sets the monitoring interval in minutes.
    /// </summary>
    public int MonitoringIntervalMinutes { get; set; } = 1;

    /// <summary>
    /// Gets or sets the retry delay in minutes.
    /// </summary>
    public int RetryDelayMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether WebSocket monitor metrics are enabled.
    /// </summary>
    public bool EnableWebSocketMonitorMetrics { get; set; } = true;
}