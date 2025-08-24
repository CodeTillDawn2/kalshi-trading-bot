
using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    public class CancelOrderResponse
    {
        [JsonPropertyName("order")]
        public OrderDetails Order { get; set; } = new();

        [JsonPropertyName("reduced_by")]
        public int ReducedBy { get; set; }
    }
}
