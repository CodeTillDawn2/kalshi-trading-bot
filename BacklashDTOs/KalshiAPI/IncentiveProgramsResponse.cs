using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class IncentiveProgramsResponse
    {
        [JsonPropertyName("incentive_programs")]
        public List<IncentiveProgram> IncentivePrograms { get; set; } = new List<IncentiveProgram>();

        [JsonPropertyName("next_cursor")]
        public string NextCursor { get; set; } = "";
    }
}