using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{

    /// <summary>
    /// Represents a trading period with open and close times.
    /// </summary>
    public class TradingPeriod
    {
        /// <summary>
        /// Gets or sets the open time.
        /// </summary>
        [JsonPropertyName("open_time")]
        public string OpenTime { get; set; } = "";

        /// <summary>
        /// Gets or sets the close time.
        /// </summary>
        [JsonPropertyName("close_time")]
        public string CloseTime { get; set; } = "";
    }
}
