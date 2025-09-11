using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class EventMetadataResponse
    {
        [JsonPropertyName("competition")]
        public string Competition { get; set; } = "";

        [JsonPropertyName("competition_scope")]
        public string CompetitionScope { get; set; } = "";

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = "";

        [JsonPropertyName("settlement_sources")]
        public List<SettlementSource> SettlementSources { get; set; } = new List<SettlementSource>();
    }
}