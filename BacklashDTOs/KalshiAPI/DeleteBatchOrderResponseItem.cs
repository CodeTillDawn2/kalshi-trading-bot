using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents an item in the delete batch order response from the Kalshi API.
    /// </summary>
    public class DeleteBatchOrderResponseItem
    {
        /// <summary>
        /// Gets or sets the error details if the deletion failed.
        /// </summary>
        [JsonPropertyName("error")]
        public ApiError? Error { get; set; }

        /// <summary>
        /// Gets or sets the order details if the deletion succeeded.
        /// </summary>
        [JsonPropertyName("order")]
        public OrderDetails? Order { get; set; }

        /// <summary>
        /// Gets or sets the order ID.
        /// </summary>
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        /// <summary>
        /// Gets or sets the amount by which the order was reduced.
        /// </summary>
        [JsonPropertyName("reduced_by")]
        public int ReducedBy { get; set; }
    }
}