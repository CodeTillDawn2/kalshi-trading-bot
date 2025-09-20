using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a settlement from the Kalshi API.
    /// </summary>
    public class Settlement
    {
        /// <summary>
        /// Gets or sets the market result.
        /// </summary>
        [JsonPropertyName("market_result")]
        public string MarketResult { get; set; } = "";

        /// <summary>
        /// Gets or sets the no count.
        /// </summary>
        [JsonPropertyName("no_count")]
        public int NoCount { get; set; }

        /// <summary>
        /// Gets or sets the no total cost.
        /// </summary>
        [JsonPropertyName("no_total_cost")]
        public int NoTotalCost { get; set; }

        /// <summary>
        /// Gets or sets the revenue.
        /// </summary>
        [JsonPropertyName("revenue")]
        public int Revenue { get; set; }

        /// <summary>
        /// Gets or sets the settled time.
        /// </summary>
        [JsonPropertyName("settled_time")]
        public string SettledTime { get; set; } = "";

        /// <summary>
        /// Gets or sets the ticker.
        /// </summary>
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = "";

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [JsonPropertyName("value")]
        public int Value { get; set; }

        /// <summary>
        /// Gets or sets the yes count.
        /// </summary>
        [JsonPropertyName("yes_count")]
        public int YesCount { get; set; }

        /// <summary>
        /// Gets or sets the yes total cost.
        /// </summary>
        [JsonPropertyName("yes_total_cost")]
        public int YesTotalCost { get; set; }
    }
}
