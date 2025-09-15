using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing a list of settlements from the Kalshi API.
    /// </summary>
    public class SettlementsResponse
    {
        /// <summary>
        /// Gets or sets the cursor for pagination.
        /// </summary>
        [JsonPropertyName("cursor")]
        public string Cursor { get; set; } = "";

        /// <summary>
        /// Gets or sets the list of settlements.
        /// </summary>
        [JsonPropertyName("settlements")]
        public List<Settlement> Settlements { get; set; } = new List<Settlement>();
    }
}