namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for core Kalshi API integration.
/// </summary>
public class KalshiConfig
{
    /// <summary>
    /// Gets or sets the environment (e.g., "prod" or "dev"). Defaults to "prod".
    /// </summary>
    public string Environment { get; set; } = "QA";

    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the API key file.
    /// </summary>
    public string KeyFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether KalshiAPIService performance metrics collection is enabled.
    /// When disabled, method execution times, API response times, and error counts are not tracked to reduce overhead. Defaults to true.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;
}


