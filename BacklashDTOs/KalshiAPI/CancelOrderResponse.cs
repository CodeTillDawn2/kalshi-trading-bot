
using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response for canceling an order from the Kalshi API.
    /// </summary>
    public class CancelOrderResponse
    {
        /// <summary>
        /// Gets or sets the order details.
        /// </summary>
        [JsonPropertyName("order")]
        public OrderDetails Order { get; set; } = new();

        /// <summary>
        /// Gets or sets the amount by which the order was reduced.
        /// </summary>
        [JsonPropertyName("reduced_by")]
        public int ReducedBy { get; set; }
    }
}
