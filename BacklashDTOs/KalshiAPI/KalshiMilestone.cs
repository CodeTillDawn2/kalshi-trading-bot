using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a milestone from the Kalshi API.
    /// </summary>
    public class KalshiMilestone
    {
        /// <summary>
        /// Gets or sets the category of the milestone.
        /// </summary>
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets additional details about the milestone.
        /// </summary>
        [JsonPropertyName("details")]
        public object? Details { get; set; }

        /// <summary>
        /// Gets or sets the end date of the milestone, if any.
        /// </summary>
        [JsonPropertyName("end_date")]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the milestone.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the last time this milestone was updated.
        /// </summary>
        [JsonPropertyName("last_updated_ts")]
        public DateTime LastUpdatedTs { get; set; }

        /// <summary>
        /// Gets or sets the notification message for the milestone.
        /// </summary>
        [JsonPropertyName("notification_message")]
        public string? NotificationMessage { get; set; }

        /// <summary>
        /// Gets or sets the list of event tickers directly related to the outcome of this milestone.
        /// </summary>
        [JsonPropertyName("primary_event_tickers")]
        public List<string>? PrimaryEventTickers { get; set; }

        /// <summary>
        /// Gets or sets the list of event tickers related to this milestone.
        /// </summary>
        [JsonPropertyName("related_event_tickers")]
        public List<string>? RelatedEventTickers { get; set; }

        /// <summary>
        /// Gets or sets the source id of milestone if available.
        /// </summary>
        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }

        /// <summary>
        /// Gets or sets the start date of the milestone.
        /// </summary>
        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the title of the milestone.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the type of the milestone.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}