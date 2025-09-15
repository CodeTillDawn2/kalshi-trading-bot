using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a single candlestick data point for a Kalshi market, including price and volume info.
    /// </summary>
    public class APICandlestick
    {
        /// <summary>
        /// Gets or sets the end period timestamp.
        /// </summary>
        [JsonPropertyName("end_period_ts")] public long EndPeriodTs { get; set; }
        /// <summary>
        /// Gets or sets the open interest.
        /// </summary>
        [JsonPropertyName("open_interest")] public int OpenInterest { get; set; }
        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        [JsonPropertyName("volume")] public int Volume { get; set; }
        /// <summary>
        /// Gets or sets the price data.
        /// </summary>
        [JsonPropertyName("price")] public PriceData? Price { get; set; }
        /// <summary>
        /// Gets or sets the yes ask price data.
        /// </summary>
        [JsonPropertyName("yes_ask")] public PriceData? YesAsk { get; set; }
        /// <summary>
        /// Gets or sets the yes bid price data.
        /// </summary>
        [JsonPropertyName("yes_bid")] public PriceData? YesBid { get; set; }
    }
}
