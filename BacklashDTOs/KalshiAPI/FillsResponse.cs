using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing a list of fills from the Kalshi API.
    /// </summary>
    public class FillsResponse
    {
        /// <summary>
        /// Gets or sets the cursor for pagination.
        /// </summary>
        [JsonPropertyName("cursor")]
        public string Cursor { get; set; } = "";

        /// <summary>
        /// Gets or sets the list of fills.
        /// </summary>
        [JsonPropertyName("fills")]
        public List<Fill> Fills { get; set; } = new List<Fill>();
    }
}
