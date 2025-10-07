using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing a list of events from the Kalshi API.
    /// </summary>
    public class EventsResponse
    {
        /// <summary>
        /// Gets or sets the pagination cursor for the next page.
        /// Empty if there are no more results.
        /// </summary>
        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }

        /// <summary>
        /// Gets or sets the array of events matching the query criteria.
        /// </summary>
        [JsonPropertyName("events")]
        public List<KalshiEvent>? Events { get; set; }
    }
}