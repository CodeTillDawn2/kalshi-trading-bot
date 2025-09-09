using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a user's position in a specific Kalshi market, including trading stats and profit/loss.
    /// </summary>
    public class MarketPositionApi
    {
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("total_traded")]
        public long TotalTraded { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("market_exposure")]
        public long MarketExposure { get; set; }

        [JsonPropertyName("realized_pnl")]
        public long RealizedPnl { get; set; }

        [JsonPropertyName("resting_orders_count")]
        public int RestingOrdersCount { get; set; }

        [JsonPropertyName("fees_paid")]
        public long FeesPaid { get; set; }

        [JsonPropertyName("last_updated_ts")]
        public DateTime LastUpdatedTs { get; set; }
    }
}
