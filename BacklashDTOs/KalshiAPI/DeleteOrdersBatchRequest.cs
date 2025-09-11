using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class DeleteOrdersBatchRequest
    {
        [JsonPropertyName("ids")]
        public List<string> Ids { get; set; } = new List<string>();
    }
}