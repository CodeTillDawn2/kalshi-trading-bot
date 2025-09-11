using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class DeleteOrdersBatchResponse
    {
        [JsonPropertyName("orders")]
        public List<DeleteBatchOrderResponseItem> Orders { get; set; } = new List<DeleteBatchOrderResponseItem>();
    }
}