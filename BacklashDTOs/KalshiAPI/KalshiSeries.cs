using BacklashDTOs.Data;
using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class KalshiSeries
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("contract_url")]
        public string ContractUrl { get; set; } = string.Empty;

        [JsonPropertyName("frequency")]
        public string Frequency { get; set; } = string.Empty;

        [JsonPropertyName("settlement_sources")]
        public List<SeriesSettlementSourceDTO> SettlementSources { get; set; } = new List<SeriesSettlementSourceDTO>();

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }
}
