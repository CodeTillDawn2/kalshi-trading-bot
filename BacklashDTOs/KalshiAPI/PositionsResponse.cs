using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a response containing market and event position data.
    /// </summary>
    public class PositionsResponse
    {
        /// <summary>
        /// Gets or sets the list of market positions.
        /// </summary>
        [JsonPropertyName("market_positions")]
        public List<MarketPositionApi> MarketPositions { get; set; } = new List<MarketPositionApi>();

        /// <summary>
        /// Gets or sets the list of event positions.
        /// </summary>
        [JsonPropertyName("event_positions")]
        public List<EventPosition> EventPositions { get; set; } = new List<EventPosition>();

        /// <summary>
        /// Gets or sets the cursor for pagination.
        /// </summary>
        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }
    }
}
