using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    public class SeriesResponse
    {
        [JsonPropertyName("series")]
        public KalshiSeries Series { get; set; }
    }
}
