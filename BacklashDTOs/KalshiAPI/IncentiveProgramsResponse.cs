using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing a list of incentive programs from the Kalshi API.
    /// </summary>
    public class IncentiveProgramsResponse
    {
        /// <summary>
        /// Gets or sets the list of incentive programs.
        /// </summary>
        [JsonPropertyName("incentive_programs")]
        public List<IncentiveProgram> IncentivePrograms { get; set; } = new List<IncentiveProgram>();

        /// <summary>
        /// Gets or sets the next cursor for pagination.
        /// </summary>
        [JsonPropertyName("next_cursor")]
        public string NextCursor { get; set; } = "";
    }
}
