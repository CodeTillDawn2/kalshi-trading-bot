using System.Text.Json.Serialization;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for WebSocketConnectionManager operations.
/// </summary>
public class WebSocketConnectionManagerConfig
{
    /// <summary>
    /// Gets or sets the WebSocket buffer size in bytes. Defaults to 16384 (16KB).
    /// </summary>
    [JsonRequired]
    public int BufferSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of WebSocket connection retry attempts. Defaults to 5.
    /// </summary>
    [JsonRequired]
    public int MaxRetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the array of retry delay intervals in milliseconds for WebSocket connections.
    /// Defaults to [1000, 2000, 4000, 8000, 16000].
    /// </summary>
    [JsonRequired]
    public int[] RetryDelays { get; set; }

    /// <summary>
    /// Gets or sets the duration in minutes for caching WebSocket authentication signatures.
    /// Defaults to 5 minutes.
    /// </summary>
    [JsonRequired]
    public int SignatureCacheDurationMinutes { get; set; }

    /// <summary>
    /// Gets or sets the delay in milliseconds before resetting WebSocket connection.
    /// Defaults to 5000 (5 seconds).
    /// </summary>
    [JsonRequired]
    public int ResetDelayMs { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds for acquiring the connection semaphore.
    /// Defaults to 60000 (60 seconds).
    /// </summary>
    [JsonRequired]
    public int SemaphoreTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets whether WebSocket performance metrics collection is enabled.
    /// When disabled, metric tracking is skipped to reduce overhead. Defaults to true.
    /// </summary>
    [JsonRequired]
    public bool EnablePerformanceMetrics { get; set; }
}