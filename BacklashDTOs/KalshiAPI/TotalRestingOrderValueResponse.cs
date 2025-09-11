using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class TotalRestingOrderValueResponse
    {
        [JsonPropertyName("total_value")]
        public int TotalValue { get; set; }
    }
}