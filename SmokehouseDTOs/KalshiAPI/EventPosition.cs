using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a user's aggregated position for an event across multiple markets on Kalshi.
    /// </summary>
    public class EventPosition
    {
        [JsonPropertyName("event_ticker")] public string EventTicker { get; set; } = string.Empty;
        [JsonPropertyName("total_cost")] public long TotalCost { get; set; }
        [JsonPropertyName("event_exposure")] public long EventExposure { get; set; }
        [JsonPropertyName("realized_pnl")] public long RealizedPnl { get; set; }
        [JsonPropertyName("resting_order_count")] public int RestingOrderCount { get; set; }
        [JsonPropertyName("fees_paid")] public long FeesPaid { get; set; }
    }
}