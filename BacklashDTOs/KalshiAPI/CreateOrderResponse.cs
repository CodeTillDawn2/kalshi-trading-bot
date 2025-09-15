using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response for creating an order from the Kalshi API.
    /// </summary>
    public class CreateOrderResponse
    {
        /// <summary>
        /// Gets or sets the order details.
        /// </summary>
        [JsonPropertyName("order")]
        public OrderDetails Order { get; set; } = new OrderDetails();
    }

}
