using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response for deleting multiple orders in batch from the Kalshi API.
    /// </summary>
    public class DeleteOrdersBatchResponse
    {
        /// <summary>
        /// Gets or sets the list of delete batch order response items.
        /// </summary>
        [JsonPropertyName("orders")]
        public List<DeleteBatchOrderResponseItem> Orders { get; set; } = new List<DeleteBatchOrderResponseItem>();
    }
}