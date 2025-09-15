using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a settlement source from the Kalshi API.
    /// </summary>
    public class SettlementSource
    {
        /// <summary>
        /// Gets or sets the name of the settlement source.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL of the settlement source.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
