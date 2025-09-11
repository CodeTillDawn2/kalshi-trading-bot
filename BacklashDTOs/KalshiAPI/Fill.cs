using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class Fill
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("created_time")]
        public string CreatedTime { get; set; } = "";

        [JsonPropertyName("is_taker")]
        public bool IsTaker { get; set; }

        [JsonPropertyName("no_price")]
        public int NoPrice { get; set; }

        [JsonPropertyName("no_price_fixed")]
        public string NoPriceFixed { get; set; } = "";

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        [JsonPropertyName("side")]
        public string Side { get; set; } = "";

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = "";

        [JsonPropertyName("trade_id")]
        public string TradeId { get; set; } = "";

        [JsonPropertyName("yes_price")]
        public int YesPrice { get; set; }

        [JsonPropertyName("yes_price_fixed")]
        public string YesPriceFixed { get; set; } = "";
    }
}