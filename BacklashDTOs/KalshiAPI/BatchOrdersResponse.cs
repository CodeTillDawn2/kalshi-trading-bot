using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class BatchOrdersResponse
    {
        [JsonPropertyName("orders")]
        public List<BatchOrderResponseItem> Orders { get; set; } = new List<BatchOrderResponseItem>();
    }
}