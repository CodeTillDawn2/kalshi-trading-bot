using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents price-related data for a market or event.
    /// </summary>
    public class PriceData
    {
        [JsonPropertyName("close")]
        public int? Close { get; set; }

        [JsonPropertyName("high")]
        public int? High { get; set; }

        [JsonPropertyName("low")]
        public int? Low { get; set; }

        [JsonPropertyName("mean")]
        public int? Mean { get; set; }

        [JsonPropertyName("open")]
        public int? Open { get; set; }

        [JsonPropertyName("previous")]
        public int? Previous { get; set; }
    }
}
