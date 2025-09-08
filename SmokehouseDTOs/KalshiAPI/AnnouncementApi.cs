using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    public class AnnouncementApi
    {
        [JsonPropertyName("delivery_time")]
        public DateTime DeliveryTime { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}