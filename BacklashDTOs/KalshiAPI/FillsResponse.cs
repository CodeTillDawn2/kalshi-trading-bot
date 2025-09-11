using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class FillsResponse
    {
        [JsonPropertyName("cursor")]
        public string Cursor { get; set; } = "";

        [JsonPropertyName("fills")]
        public List<Fill> Fills { get; set; } = new List<Fill>();
    }
}