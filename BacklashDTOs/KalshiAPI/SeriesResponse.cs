using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing a series from the Kalshi API.
    /// </summary>
    public class SeriesResponse
    {
        /// <summary>
        /// Gets or sets the Kalshi series.
        /// </summary>
        [JsonPropertyName("series")]
        public KalshiSeries? Series { get; set; }
    }
}
