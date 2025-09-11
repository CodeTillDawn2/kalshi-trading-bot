using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class IncentiveProgram
    {
        [JsonPropertyName("end_date")]
        public string EndDate { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("incentive_type")]
        public string IncentiveType { get; set; } = "";

        [JsonPropertyName("market_ticker")]
        public string MarketTicker { get; set; } = "";

        [JsonPropertyName("paid_out")]
        public bool PaidOut { get; set; }

        [JsonPropertyName("period_reward")]
        public int PeriodReward { get; set; }

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; } = "";
    }
}