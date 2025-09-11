using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class OrderQueuePositionResponse
    {
        [JsonPropertyName("queue_position")]
        public int QueuePosition { get; set; }
    }
}