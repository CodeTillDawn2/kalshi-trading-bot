
using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    public class OrderDetails
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = "";

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("yes_price")]
        public int YesPrice { get; set; }

        [JsonPropertyName("no_price")]
        public int NoPrice { get; set; }

        [JsonPropertyName("created_time")]
        public string CreatedTime { get; set; } = "";

        [JsonPropertyName("expiration_time")]
        public string? ExpirationTime { get; set; }

        [JsonPropertyName("self_trade_prevention_type")]
        public string SelfTradePreventionType { get; set; } = "";

        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [JsonPropertyName("side")]
        public string Side { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("client_order_id")]
        public string ClientOrderId { get; set; } = "";

        [JsonPropertyName("order_group_id")]
        public string OrderGroupId { get; set; } = "";
    }
}
