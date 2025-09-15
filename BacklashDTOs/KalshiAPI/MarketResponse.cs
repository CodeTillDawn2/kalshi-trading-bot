using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a response containing a list of Kalshi markets and pagination info.
    /// </summary>
    public class MarketResponse
    {
        /// <summary>
        /// Gets or sets the list of markets.
        /// </summary>
        [JsonPropertyName("markets")] public List<KalshiMarket> Markets { get; set; } = new List<KalshiMarket>();
        /// <summary>
        /// Gets or sets the cursor for pagination.
        /// </summary>
        [JsonPropertyName("cursor")] public string? Cursor { get; set; }
    }
}
