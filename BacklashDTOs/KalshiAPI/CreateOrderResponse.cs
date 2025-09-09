using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class CreateOrderResponse
    {
        [JsonPropertyName("order")]
        public OrderDetails Order { get; set; } = new OrderDetails();
    }

}
