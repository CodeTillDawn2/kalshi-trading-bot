using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response for batch orders from the Kalshi API.
    /// </summary>
    public class BatchOrdersResponse
    {
        /// <summary>
        /// Gets or sets the list of batch order response items.
        /// </summary>
        [JsonPropertyName("orders")]
        public List<BatchOrderResponseItem> Orders { get; set; } = new List<BatchOrderResponseItem>();
    }
}
