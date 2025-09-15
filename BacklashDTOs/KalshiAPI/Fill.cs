using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a fill (trade execution) from the Kalshi API.
    /// </summary>
    public class Fill
    {
        /// <summary>
        /// Gets or sets the action (buy or sell).
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        /// <summary>
        /// Gets or sets the count of contracts.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the created time.
        /// </summary>
        [JsonPropertyName("created_time")]
        public string CreatedTime { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether this is a taker order.
        /// </summary>
        [JsonPropertyName("is_taker")]
        public bool IsTaker { get; set; }

        /// <summary>
        /// Gets or sets the no price.
        /// </summary>
        [JsonPropertyName("no_price")]
        public int NoPrice { get; set; }

        /// <summary>
        /// Gets or sets the fixed no price.
        /// </summary>
        [JsonPropertyName("no_price_fixed")]
        public string NoPriceFixed { get; set; } = "";

        /// <summary>
        /// Gets or sets the order ID.
        /// </summary>
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        /// <summary>
        /// Gets or sets the side (yes or no).
        /// </summary>
        [JsonPropertyName("side")]
        public string Side { get; set; } = "";

        /// <summary>
        /// Gets or sets the ticker.
        /// </summary>
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = "";

        /// <summary>
        /// Gets or sets the trade ID.
        /// </summary>
        [JsonPropertyName("trade_id")]
        public string TradeId { get; set; } = "";

        /// <summary>
        /// Gets or sets the yes price.
        /// </summary>
        [JsonPropertyName("yes_price")]
        public int YesPrice { get; set; }

        /// <summary>
        /// Gets or sets the fixed yes price.
        /// </summary>
        [JsonPropertyName("yes_price_fixed")]
        public string YesPriceFixed { get; set; } = "";
    }
}