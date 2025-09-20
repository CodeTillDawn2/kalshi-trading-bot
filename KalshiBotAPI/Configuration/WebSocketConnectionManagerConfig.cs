using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for WebSocketConnectionManager operations.
/// </summary>
public class WebSocketConnectionManagerConfig
{
    /// <summary>
    /// The configuration section name for WebSocketConnectionManagerConfig.
    /// </summary>
    public const string SectionName = "Websockets:WebSocketConnectionManager";

    /// <summary>
    /// Gets or sets the WebSocket buffer size in bytes. Defaults to 16384 (16KB).
    /// </summary>
    [Required(ErrorMessage = "The 'BufferSize' is missing in the configuration.")]
    public int BufferSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of WebSocket connection retry attempts. Defaults to 5.
    /// </summary>
    [Required(ErrorMessage = "The 'MaxRetryAttempts' is missing in the configuration.")]
    public int MaxRetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the array of retry delay intervals in milliseconds for WebSocket connections.
    /// Defaults to [1000, 2000, 4000, 8000, 16000].
    /// </summary>
    [Required(ErrorMessage = "The 'RetryDelays' is missing in the configuration.")]
    public int[] RetryDelays { get; set; } = null!;

    /// <summary>
    /// Gets or sets the duration in minutes for caching WebSocket authentication signatures.
    /// Defaults to 5 minutes.
    /// </summary>
    [Required(ErrorMessage = "The 'SignatureCacheDurationMinutes' is missing in the configuration.")]
    public int SignatureCacheDurationMinutes { get; set; }

    /// <summary>
    /// Gets or sets the delay in milliseconds before resetting WebSocket connection.
    /// Defaults to 5000 (5 seconds).
    /// </summary>
    [Required(ErrorMessage = "The 'ResetDelayMs' is missing in the configuration.")]
    public int ResetDelayMs { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds for acquiring the connection semaphore.
    /// Defaults to 60000 (60 seconds).
    /// </summary>
    [Required(ErrorMessage = "The 'SemaphoreTimeoutMs' is missing in the configuration.")]
    public int SemaphoreTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets whether WebSocket performance metrics collection is enabled.
    /// When disabled, metric tracking is skipped to reduce overhead. Defaults to true.
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }
}
