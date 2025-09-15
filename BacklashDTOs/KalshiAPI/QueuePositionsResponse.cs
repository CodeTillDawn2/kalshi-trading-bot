using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing a list of queue positions from the Kalshi API.
    /// </summary>
    public class QueuePositionsResponse
    {
        /// <summary>
        /// Gets or sets the list of queue position items.
        /// </summary>
        [JsonPropertyName("queue_positions")]
        public List<QueuePositionItem> QueuePositions { get; set; } = new List<QueuePositionItem>();
    }
}