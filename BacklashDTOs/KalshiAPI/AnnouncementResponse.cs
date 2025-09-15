using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing a list of announcements from the Kalshi API.
    /// </summary>
    public class AnnouncementResponse
    {
        /// <summary>
        /// Gets or sets the list of announcements.
        /// </summary>
        [JsonPropertyName("announcements")]
        public List<AnnouncementApi> Announcements { get; set; } = new List<AnnouncementApi>();
    }
}
