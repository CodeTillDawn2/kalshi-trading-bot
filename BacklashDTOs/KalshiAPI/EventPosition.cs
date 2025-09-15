using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a user's aggregated position for an event across multiple markets on Kalshi.
    /// </summary>
    public class EventPosition
    {
        /// <summary>
        /// Gets or sets the event ticker.
        /// </summary>
        [JsonPropertyName("event_ticker")] public string EventTicker { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the total cost.
        /// </summary>
        [JsonPropertyName("total_cost")] public long TotalCost { get; set; }
        /// <summary>
        /// Gets or sets the event exposure.
        /// </summary>
        [JsonPropertyName("event_exposure")] public long EventExposure { get; set; }
        /// <summary>
        /// Gets or sets the realized profit and loss.
        /// </summary>
        [JsonPropertyName("realized_pnl")] public long RealizedPnl { get; set; }
        /// <summary>
        /// Gets or sets the resting order count.
        /// </summary>
        [JsonPropertyName("resting_order_count")] public int RestingOrderCount { get; set; }
        /// <summary>
        /// Gets or sets the fees paid.
        /// </summary>
        [JsonPropertyName("fees_paid")] public long FeesPaid { get; set; }
    }
}
