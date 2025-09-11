using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class QueuePositionsResponse
    {
        [JsonPropertyName("queue_positions")]
        public List<QueuePositionItem> QueuePositions { get; set; } = new List<QueuePositionItem>();
    }
}