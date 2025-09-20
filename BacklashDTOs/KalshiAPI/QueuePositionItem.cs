using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents an item containing queue position information for an order.
    /// </summary>
    public class QueuePositionItem
    {
        /// <summary>
        /// Gets or sets the market ticker.
        /// </summary>
        [JsonPropertyName("market_ticker")]
        public string MarketTicker { get; set; } = "";

        /// <summary>
        /// Gets or sets the order ID.
        /// </summary>
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        /// <summary>
        /// Gets or sets the queue position.
        /// </summary>
        [JsonPropertyName("queue_position")]
        public int QueuePosition { get; set; }
    }
}
