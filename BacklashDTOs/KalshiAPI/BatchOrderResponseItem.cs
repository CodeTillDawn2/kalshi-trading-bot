using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents an item in the batch order response from the Kalshi API.
    /// </summary>
    public class BatchOrderResponseItem
    {
        /// <summary>
        /// Gets or sets the client order ID.
        /// </summary>
        [JsonPropertyName("client_order_id")]
        public string ClientOrderId { get; set; } = "";

        /// <summary>
        /// Gets or sets the error details if the order failed.
        /// </summary>
        [JsonPropertyName("error")]
        public ApiError? Error { get; set; }

        /// <summary>
        /// Gets or sets the order details if the order succeeded.
        /// </summary>
        [JsonPropertyName("order")]
        public OrderDetails? Order { get; set; }
    }
}