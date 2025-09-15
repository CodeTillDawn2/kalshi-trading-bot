using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a user's position in a specific Kalshi market, including trading stats and profit/loss.
    /// </summary>
    public class MarketPositionApi
    {
        /// <summary>
        /// Gets or sets the ticker symbol.
        /// </summary>
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total traded amount.
        /// </summary>
        [JsonPropertyName("total_traded")]
        public long TotalTraded { get; set; }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        [JsonPropertyName("position")]
        public int Position { get; set; }

        /// <summary>
        /// Gets or sets the market exposure.
        /// </summary>
        [JsonPropertyName("market_exposure")]
        public long MarketExposure { get; set; }

        /// <summary>
        /// Gets or sets the realized profit and loss.
        /// </summary>
        [JsonPropertyName("realized_pnl")]
        public long RealizedPnl { get; set; }

        /// <summary>
        /// Gets or sets the resting orders count.
        /// </summary>
        [JsonPropertyName("resting_orders_count")]
        public int RestingOrdersCount { get; set; }

        /// <summary>
        /// Gets or sets the fees paid.
        /// </summary>
        [JsonPropertyName("fees_paid")]
        public long FeesPaid { get; set; }

        /// <summary>
        /// Gets or sets the last updated timestamp.
        /// </summary>
        [JsonPropertyName("last_updated_ts")]
        public DateTime LastUpdatedTs { get; set; }
    }
}
