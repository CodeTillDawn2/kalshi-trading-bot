using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    /// <summary>
    /// Represents an order placed on the Kalshi platform with details about its status and execution.
    /// </summary>
    public class OrderApi
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = "";

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = "";

        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [JsonPropertyName("side")]
        public string Side { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("yes_price")]
        public int YesPrice { get; set; }

        [JsonPropertyName("no_price")]
        public int NoPrice { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("last_update_time")]
        public DateTime LastUpdateTime { get; set; }

        [JsonPropertyName("expiration_time")]
        public DateTime? ExpirationTime { get; set; }

        [JsonPropertyName("client_order_id")]
        public string ClientOrderId { get; set; } = "";

        [JsonPropertyName("order_group_id")]
        public string OrderGroupId { get; set; } = ""; // Added missing field

        [JsonPropertyName("place_count")]
        public int PlaceCount { get; set; }

        [JsonPropertyName("decrease_count")]
        public int DecreaseCount { get; set; }

        [JsonPropertyName("amend_count")]
        public int AmendCount { get; set; }

        [JsonPropertyName("amend_taker_fill_count")]
        public int AmendTakerFillCount { get; set; }

        [JsonPropertyName("maker_fill_count")]
        public int MakerFillCount { get; set; }

        [JsonPropertyName("taker_fill_count")]
        public int TakerFillCount { get; set; }

        [JsonPropertyName("remaining_count")]
        public int RemainingCount { get; set; }

        [JsonPropertyName("queue_position")]
        public int QueuePosition { get; set; }

        [JsonPropertyName("maker_fill_cost")]
        public long MakerFillCost { get; set; }

        [JsonPropertyName("taker_fill_cost")]
        public long TakerFillCost { get; set; }

        [JsonPropertyName("maker_fees")]
        public long MakerFees { get; set; }

        [JsonPropertyName("taker_fees")]
        public long TakerFees { get; set; }

        [JsonPropertyName("fcc_cancel_count")]
        public int FccCancelCount { get; set; }

        [JsonPropertyName("close_cancel_count")]
        public int CloseCancelCount { get; set; }

        [JsonPropertyName("taker_self_trade_cancel_count")]
        public int TakerSelfTradeCancelCount { get; set; }

        [JsonPropertyName("maker_self_trade_cancel_count")]
        public int MakerSelfTradeCancelCount { get; set; } // Added missing field

        [JsonPropertyName("self_trade_prevention_type")]
        public string SelfTradePreventionType { get; set; } = ""; // Added missing field
    }
}

