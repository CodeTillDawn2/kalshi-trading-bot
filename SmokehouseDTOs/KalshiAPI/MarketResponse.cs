using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a response containing a list of Kalshi markets and pagination info.
    /// </summary>
    public class MarketResponse
    {
        [JsonPropertyName("markets")] public List<KalshiMarket> Markets { get; set; } = new List<KalshiMarket>();
        [JsonPropertyName("cursor")] public string? Cursor { get; set; }
    }
}