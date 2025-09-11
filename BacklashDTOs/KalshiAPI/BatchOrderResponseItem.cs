using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class BatchOrderResponseItem
    {
        [JsonPropertyName("client_order_id")]
        public string ClientOrderId { get; set; } = "";

        [JsonPropertyName("error")]
        public ApiError? Error { get; set; }

        [JsonPropertyName("order")]
        public OrderDetails? Order { get; set; }
    }
}