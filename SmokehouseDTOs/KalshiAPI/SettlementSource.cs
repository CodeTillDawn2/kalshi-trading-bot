using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    public class SettlementSource
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
