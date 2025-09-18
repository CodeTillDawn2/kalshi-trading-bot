namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for KalshiWebSocketClient operations.
/// </summary>
public class KalshiWebSocketClientConfig
{
    /// <summary>
    /// Gets or sets whether KalshiWebSocketClient performance metrics are enabled.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;
}