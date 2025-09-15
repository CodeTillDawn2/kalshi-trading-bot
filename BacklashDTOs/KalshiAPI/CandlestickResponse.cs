using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a response containing candlestick data for a specific market on Kalshi.
    /// </summary>
    public class CandlestickResponse
    {
        /// <summary>
        /// Gets or sets the ticker symbol for the market.
        /// </summary>
        [JsonPropertyName("ticker")] public string? Ticker { get; set; }
        /// <summary>
        /// Gets or sets the list of candlestick data points.
        /// </summary>
        [JsonPropertyName("candlesticks")] public List<APICandlestick>? Candlesticks { get; set; } = new List<APICandlestick>();
    }
}
