using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class SeriesResponse
    {
        [JsonPropertyName("series")]
        public KalshiSeries? Series { get; set; }
    }
}
