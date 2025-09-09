
using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class CreateOrderRequest
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = ""; // Required: "buy" or "sell"

        [JsonPropertyName("type")]
        public string Type { get; set; } = ""; // Required: "market" or "limit"
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = ""; // Required

        [JsonPropertyName("side")]
        public string Side { get; set; } = ""; // Required: "yes" or "no"

        [JsonPropertyName("count")]
        public int Count { get; set; } // Required

        [JsonPropertyName("client_order_id")]
        public string? ClientOrderId { get; set; } // Optional, but docs say required—treat as optional

        [JsonPropertyName("expiration_ts")]
        public long? ExpirationTs { get; set; } // Optional

        [JsonPropertyName("yes_price")]
        public long? YesPrice { get; set; } // Optional for limit; exactly one of yes/no_price

        [JsonPropertyName("no_price")]
        public long? NoPrice { get; set; } // Optional for limit; exactly one of yes/no_price

        [JsonPropertyName("buy_max_cost")]
        public long? BuyMaxCost { get; set; } // Optional: for market buy

        [JsonPropertyName("post_only")]
        public bool? PostOnly { get; set; } // Optional

        [JsonPropertyName("sell_position_floor")]
        public int? SellPositionFloor { get; set; } // Optional

        [JsonPropertyName("time_in_force")]
        public string? TimeInForce { get; set; } // Optional: only "fill_or_kill" supported
    }
}
