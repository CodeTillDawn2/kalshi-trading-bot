using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class BatchOrdersRequest
    {
        [JsonPropertyName("orders")]
        public List<CreateOrderRequest> Orders { get; set; } = new List<CreateOrderRequest>();
    }
}