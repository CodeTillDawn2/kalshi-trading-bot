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
    [JsonRequired]
    public string Environment { get; set; }

    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    [JsonRequired]
    public string BotKeyId { get; set; }

    /// <summary>
    /// Gets or sets the path to the API key file.
    /// </summary>
    [JsonRequired]
    public string BotKeyFile { get; set; }
}


