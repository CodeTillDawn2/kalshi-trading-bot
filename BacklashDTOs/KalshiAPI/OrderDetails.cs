
using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
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

        [JsonPropertyName("last_update_time")]
        public string? LastUpdateTime { get; set; }

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

        [JsonPropertyName("fill_count")]
        public int FillCount { get; set; }

        [JsonPropertyName("initial_count")]
        public int InitialCount { get; set; }

        [JsonPropertyName("remaining_count")]
        public int RemainingCount { get; set; }

        [JsonPropertyName("queue_position")]
        public int QueuePosition { get; set; }

        [JsonPropertyName("maker_fees")]
        public int MakerFees { get; set; }

        [JsonPropertyName("taker_fees")]
        public int TakerFees { get; set; }

        [JsonPropertyName("maker_fill_cost")]
        public int MakerFillCost { get; set; }

        [JsonPropertyName("taker_fill_cost")]
        public int TakerFillCost { get; set; }

        [JsonPropertyName("yes_price_dollars")]
        public string YesPriceDollars { get; set; } = "";

        [JsonPropertyName("no_price_dollars")]
        public string NoPriceDollars { get; set; } = "";
    }
}
