using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing event metadata from the Kalshi API.
    /// </summary>
    public class EventMetadataResponse
    {
        /// <summary>
        /// Gets or sets the competition name.
        /// </summary>
        [JsonPropertyName("competition")]
        public string Competition { get; set; } = "";

        /// <summary>
        /// Gets or sets the competition scope.
        /// </summary>
        [JsonPropertyName("competition_scope")]
        public string CompetitionScope { get; set; } = "";

        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = "";

        /// <summary>
        /// Gets or sets the list of settlement sources.
        /// </summary>
        [JsonPropertyName("settlement_sources")]
        public List<SettlementSource> SettlementSources { get; set; } = new List<SettlementSource>();
    }
}
