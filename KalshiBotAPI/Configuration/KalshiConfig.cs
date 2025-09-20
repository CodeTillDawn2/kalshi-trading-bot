using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for core Kalshi API integration.
/// </summary>
public class KalshiConfig
{
    /// <summary>
    /// The configuration section name for KalshiConfig.
    /// </summary>
    public const string SectionName = "Kalshi";

    /// <summary>
    /// Gets or sets the environment (e.g., "prod" or "dev"). Defaults to "prod".
    /// </summary>
    [Required(ErrorMessage = "The 'Environment' is missing in the configuration.")]
    public string Environment { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    [Required(ErrorMessage = "The 'BotKeyId' is missing in the configuration.")]
    public string KeyId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path to the API key file.
    /// </summary>
    [Required(ErrorMessage = "The 'BotKeyFile' is missing in the configuration.")]
    public string KeyFile { get; set; } = null!;
}


