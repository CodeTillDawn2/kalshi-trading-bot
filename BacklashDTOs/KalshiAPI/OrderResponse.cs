using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class OrderResponse
    {
        [JsonPropertyName("order")]
        public OrderDetails Order { get; set; } = new OrderDetails();
    }
}