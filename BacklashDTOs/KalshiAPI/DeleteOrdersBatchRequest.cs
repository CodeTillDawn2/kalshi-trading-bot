using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a request to delete multiple orders in batch from the Kalshi API.
    /// </summary>
    public class DeleteOrdersBatchRequest
    {
        /// <summary>
        /// Gets or sets the list of order IDs to delete.
        /// </summary>
        [JsonPropertyName("ids")]
        public List<string> Ids { get; set; } = new List<string>();
    }
}
