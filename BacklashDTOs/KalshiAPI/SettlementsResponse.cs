using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class SettlementsResponse
    {
        [JsonPropertyName("cursor")]
        public string Cursor { get; set; } = "";

        [JsonPropertyName("settlements")]
        public List<Settlement> Settlements { get; set; } = new List<Settlement>();
    }
}