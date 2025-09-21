using System.ComponentModel.DataAnnotations;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for KalshiWebSocketClient operations.
/// </summary>
public class KalshiWebSocketClientConfig
{
    /// <summary>
    /// The configuration section name for KalshiWebSocketClientConfig.
    /// </summary>
    public const string SectionName = "Websockets:KalshiWebSocketClient";

    /// <summary>
    /// Gets or sets whether KalshiWebSocketClient performance metrics are enabled.
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }
}
