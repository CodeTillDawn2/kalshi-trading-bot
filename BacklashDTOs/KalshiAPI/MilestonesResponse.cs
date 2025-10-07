using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing a list of milestones from the Kalshi API.
    /// </summary>
    public class MilestonesResponse
    {
        /// <summary>
        /// Gets or sets the pagination cursor for the next page.
        /// Empty if there are no more results.
        /// </summary>
        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }

        /// <summary>
        /// Gets or sets the array of milestones matching the query criteria.
        /// </summary>
        [JsonPropertyName("milestones")]
        public List<KalshiMilestone>? Milestones { get; set; }
    }
}