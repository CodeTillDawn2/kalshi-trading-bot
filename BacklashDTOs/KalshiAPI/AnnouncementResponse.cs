using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class AnnouncementResponse
    {
        [JsonPropertyName("announcements")]
        public List<AnnouncementApi> Announcements { get; set; } = new List<AnnouncementApi>();
    }
}
