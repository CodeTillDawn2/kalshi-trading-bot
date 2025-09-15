using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents price-related data for a market or event.
    /// </summary>
    public class PriceData
    {
        /// <summary>
        /// Gets or sets the close price.
        /// </summary>
        [JsonPropertyName("close")]
        public int? Close { get; set; }

        /// <summary>
        /// Gets or sets the high price.
        /// </summary>
        [JsonPropertyName("high")]
        public int? High { get; set; }

        /// <summary>
        /// Gets or sets the low price.
        /// </summary>
        [JsonPropertyName("low")]
        public int? Low { get; set; }

        /// <summary>
        /// Gets or sets the mean price.
        /// </summary>
        [JsonPropertyName("mean")]
        public int? Mean { get; set; }

        /// <summary>
        /// Gets or sets the open price.
        /// </summary>
        [JsonPropertyName("open")]
        public int? Open { get; set; }

        /// <summary>
        /// Gets or sets the previous price.
        /// </summary>
        [JsonPropertyName("previous")]
        public int? Previous { get; set; }
    }
}
