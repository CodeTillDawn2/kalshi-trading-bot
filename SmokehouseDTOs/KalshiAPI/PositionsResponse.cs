using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a response containing market and event position data.
    /// </summary>
    public class PositionsResponse
    {
        [JsonPropertyName("market_positions")]
        public List<MarketPositionApi> MarketPositions { get; set; } = new List<MarketPositionApi>();

        [JsonPropertyName("event_positions")]
        public List<EventPosition> EventPositions { get; set; } = new List<EventPosition>();

        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }
    }
}