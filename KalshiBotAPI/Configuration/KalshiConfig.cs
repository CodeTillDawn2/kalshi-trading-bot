using System.Text.Json.Serialization;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for core Kalshi API integration.
/// </summary>
public class KalshiConfig
{
    /// <summary>
    /// Gets or sets the environment (e.g., "prod" or "dev"). Defaults to "prod".
    /// </summary>
    required
    public string Environment { get; set; }

    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    required
    public string BotKeyId { get; set; }

    /// <summary>
    /// Gets or sets the path to the API key file.
    /// </summary>
    required
    public string BotKeyFile { get; set; }
}


