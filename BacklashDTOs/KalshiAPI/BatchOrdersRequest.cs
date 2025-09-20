using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a request to create multiple orders in batch from the Kalshi API.
    /// </summary>
    public class BatchOrdersRequest
    {
        /// <summary>
        /// Gets or sets the list of orders to create.
        /// </summary>
        [JsonPropertyName("orders")]
        public List<CreateOrderRequest> Orders { get; set; } = new List<CreateOrderRequest>();
    }
}
