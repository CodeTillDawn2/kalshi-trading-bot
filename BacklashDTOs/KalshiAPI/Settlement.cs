using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class Settlement
    {
        [JsonPropertyName("market_result")]
        public string MarketResult { get; set; } = "";

        [JsonPropertyName("no_count")]
        public int NoCount { get; set; }

        [JsonPropertyName("no_total_cost")]
        public int NoTotalCost { get; set; }

        [JsonPropertyName("revenue")]
        public int Revenue { get; set; }

        [JsonPropertyName("settled_time")]
        public string SettledTime { get; set; } = "";

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = "";

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("yes_count")]
        public int YesCount { get; set; }

        [JsonPropertyName("yes_total_cost")]
        public int YesTotalCost { get; set; }
    }
}