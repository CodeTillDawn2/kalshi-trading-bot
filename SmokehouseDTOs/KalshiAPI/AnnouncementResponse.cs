using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    // The API returns a direct array, not wrapped in an object
    public class AnnouncementResponse
    {
        public List<AnnouncementApi> Announcements { get; set; } = new List<AnnouncementApi>();
    }
}