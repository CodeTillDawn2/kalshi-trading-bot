using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class QueuePositionItem
    {
        [JsonPropertyName("market_ticker")]
        public string MarketTicker { get; set; } = "";

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        [JsonPropertyName("queue_position")]
        public int QueuePosition { get; set; }
    }
}