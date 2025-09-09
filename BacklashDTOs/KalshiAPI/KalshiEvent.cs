using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class KalshiEvent
    {
        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("collateral_return_type")]
        public string CollateralReturnType { get; set; }

        [JsonPropertyName("event_ticker")]
        public string EventTicker { get; set; }

        [JsonPropertyName("mutually_exclusive")]
        public bool MutuallyExclusive { get; set; }

        [JsonPropertyName("series_ticker")]
        public string SeriesTicker { get; set; }

        [JsonPropertyName("strike_date")]
        public DateTime? StrikeDate { get; set; }

        [JsonPropertyName("strike_period")]
        public string StrikePeriod { get; set; }

        [JsonPropertyName("sub_title")]
        public string SubTitle { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("markets")]
        public List<KalshiMarket> Markets { get; set; } // Only populated if withNestedMarkets=true
    }
}

