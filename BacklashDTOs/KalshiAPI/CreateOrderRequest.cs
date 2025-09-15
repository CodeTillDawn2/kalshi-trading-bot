
using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a request to create an order on the Kalshi platform.
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>
        /// Gets or sets the action for the order ("buy" or "sell").
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = ""; // Required: "buy" or "sell"

        /// <summary>
        /// Gets or sets the type of order ("market" or "limit").
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = ""; // Required: "market" or "limit"
        /// <summary>
        /// Gets or sets the ticker symbol for the market.
        /// </summary>
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = ""; // Required

        /// <summary>
        /// Gets or sets the side of the order ("yes" or "no").
        /// </summary>
        [JsonPropertyName("side")]
        public string Side { get; set; } = ""; // Required: "yes" or "no"

        /// <summary>
        /// Gets or sets the count of contracts for the order.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; } // Required
        /// <summary>
        /// Gets or sets the client order ID.
        /// </summary>

        [JsonPropertyName("client_order_id")]
        public string? ClientOrderId { get; set; } // Optional, but docs say required�treat as optional
        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>

        [JsonPropertyName("expiration_ts")]
        public long? ExpirationTs { get; set; } // Optional
        /// <summary>
        /// Gets or sets the yes price for limit orders.
        /// </summary>

        [JsonPropertyName("yes_price")]
        public long? YesPrice { get; set; } // Optional for limit; exactly one of yes/no_price
        /// <summary>
        /// Gets or sets the no price for limit orders.
        /// </summary>

        [JsonPropertyName("no_price")]
        public long? NoPrice { get; set; } // Optional for limit; exactly one of yes/no_price
        /// <summary>
        /// Gets or sets the maximum cost for market buy orders.
        /// </summary>

        [JsonPropertyName("buy_max_cost")]
        public long? BuyMaxCost { get; set; } // Optional: for market buy
        /// <summary>
        /// Gets or sets a value indicating whether the order should be post-only.
        /// </summary>

        [JsonPropertyName("post_only")]
        public bool? PostOnly { get; set; } // Optional
        /// <summary>
        /// Gets or sets the sell position floor.
        /// </summary>

        [JsonPropertyName("sell_position_floor")]
        public int? SellPositionFloor { get; set; } // Optional
        /// <summary>
        /// Gets or sets the time in force ("fill_or_kill" only supported).
        /// </summary>

        [JsonPropertyName("time_in_force")]
        public string? TimeInForce { get; set; } // Optional: only "fill_or_kill" supported
    }
}
