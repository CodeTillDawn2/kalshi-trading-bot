using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents an order placed on the Kalshi platform with details about its status and execution.
    /// </summary>
    public class OrderApi
    {
        /// <summary>
        /// Gets or sets the order ID.
        /// </summary>
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        /// <summary>
        /// Gets or sets the ticker.
        /// </summary>
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = "";

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = "";

        /// <summary>
        /// Gets or sets the action (buy or sell).
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        /// <summary>
        /// Gets or sets the side (yes or no).
        /// </summary>
        [JsonPropertyName("side")]
        public string Side { get; set; } = "";

        /// <summary>
        /// Gets or sets the type (market or limit).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        /// <summary>
        /// Gets or sets the yes price.
        /// </summary>
        [JsonPropertyName("yes_price")]
        public int YesPrice { get; set; }

        /// <summary>
        /// Gets or sets the no price.
        /// </summary>
        [JsonPropertyName("no_price")]
        public int NoPrice { get; set; }

        /// <summary>
        /// Gets or sets the created time.
        /// </summary>
        [JsonPropertyName("created_time")]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Gets or sets the last update time.
        /// </summary>
        [JsonPropertyName("last_update_time")]
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        [JsonPropertyName("expiration_time")]
        public DateTime? ExpirationTime { get; set; }

        /// <summary>
        /// Gets or sets the client order ID.
        /// </summary>
        [JsonPropertyName("client_order_id")]
        public string ClientOrderId { get; set; } = "";

        /// <summary>
        /// Gets or sets the order group ID.
        /// </summary>
        [JsonPropertyName("order_group_id")]
        public string OrderGroupId { get; set; } = ""; // Added missing field

        /// <summary>
        /// Gets or sets the place count.
        /// </summary>
        [JsonPropertyName("place_count")]
        public int PlaceCount { get; set; }

        /// <summary>
        /// Gets or sets the decrease count.
        /// </summary>
        [JsonPropertyName("decrease_count")]
        public int DecreaseCount { get; set; }

        /// <summary>
        /// Gets or sets the amend count.
        /// </summary>
        [JsonPropertyName("amend_count")]
        public int AmendCount { get; set; }

        /// <summary>
        /// Gets or sets the amend taker fill count.
        /// </summary>
        [JsonPropertyName("amend_taker_fill_count")]
        public int AmendTakerFillCount { get; set; }

        /// <summary>
        /// Gets or sets the maker fill count.
        /// </summary>
        [JsonPropertyName("maker_fill_count")]
        public int MakerFillCount { get; set; }

        /// <summary>
        /// Gets or sets the taker fill count.
        /// </summary>
        [JsonPropertyName("taker_fill_count")]
        public int TakerFillCount { get; set; }

        /// <summary>
        /// Gets or sets the remaining count.
        /// </summary>
        [JsonPropertyName("remaining_count")]
        public int RemainingCount { get; set; }

        /// <summary>
        /// Gets or sets the queue position.
        /// </summary>
        [JsonPropertyName("queue_position")]
        public int QueuePosition { get; set; }

        /// <summary>
        /// Gets or sets the maker fill cost.
        /// </summary>
        [JsonPropertyName("maker_fill_cost")]
        public long MakerFillCost { get; set; }

        /// <summary>
        /// Gets or sets the taker fill cost.
        /// </summary>
        [JsonPropertyName("taker_fill_cost")]
        public long TakerFillCost { get; set; }

        /// <summary>
        /// Gets or sets the maker fees.
        /// </summary>
        [JsonPropertyName("maker_fees")]
        public long MakerFees { get; set; }

        /// <summary>
        /// Gets or sets the taker fees.
        /// </summary>
        [JsonPropertyName("taker_fees")]
        public long TakerFees { get; set; }

        /// <summary>
        /// Gets or sets the FCC cancel count.
        /// </summary>
        [JsonPropertyName("fcc_cancel_count")]
        public int FccCancelCount { get; set; }

        /// <summary>
        /// Gets or sets the close cancel count.
        /// </summary>
        [JsonPropertyName("close_cancel_count")]
        public int CloseCancelCount { get; set; }

        /// <summary>
        /// Gets or sets the taker self trade cancel count.
        /// </summary>
        [JsonPropertyName("taker_self_trade_cancel_count")]
        public int TakerSelfTradeCancelCount { get; set; }

        /// <summary>
        /// Gets or sets the maker self trade cancel count.
        /// </summary>
        [JsonPropertyName("maker_self_trade_cancel_count")]
        public int MakerSelfTradeCancelCount { get; set; } // Added missing field

        /// <summary>
        /// Gets or sets the self trade prevention type.
        /// </summary>
        [JsonPropertyName("self_trade_prevention_type")]
        public string SelfTradePreventionType { get; set; } = ""; // Added missing field
    }
}

