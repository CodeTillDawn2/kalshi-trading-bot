using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing the queue position of an order from the Kalshi API.
    /// </summary>
    public class OrderQueuePositionResponse
    {
        /// <summary>
        /// Gets or sets the queue position.
        /// </summary>
        [JsonPropertyName("queue_position")]
        public int QueuePosition { get; set; }
    }
}
