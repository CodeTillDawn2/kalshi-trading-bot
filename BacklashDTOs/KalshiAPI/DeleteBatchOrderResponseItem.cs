using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class DeleteBatchOrderResponseItem
    {
        [JsonPropertyName("error")]
        public ApiError? Error { get; set; }

        [JsonPropertyName("order")]
        public OrderDetails? Order { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        [JsonPropertyName("reduced_by")]
        public int ReducedBy { get; set; }
    }
}