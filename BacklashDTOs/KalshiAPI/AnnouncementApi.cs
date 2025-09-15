using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents an announcement from the Kalshi API.
    /// </summary>
    public class AnnouncementApi
    {
        /// <summary>
        /// Gets or sets the delivery time of the announcement.
        /// </summary>
        [JsonPropertyName("delivery_time")]
        public DateTime DeliveryTime { get; set; }

        /// <summary>
        /// Gets or sets the message of the announcement.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the status of the announcement.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the type of the announcement.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
