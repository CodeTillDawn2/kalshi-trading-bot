using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    public class AnnouncementResponse
    {
        [JsonPropertyName("announcements")]
        public List<AnnouncementApi> Announcements { get; set; } = new List<AnnouncementApi>();
    }
}