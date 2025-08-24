using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    public class CreateOrderResponse
    {
        [JsonPropertyName("order")]
        public OrderDetails Order { get; set; } = new OrderDetails();
    }

}