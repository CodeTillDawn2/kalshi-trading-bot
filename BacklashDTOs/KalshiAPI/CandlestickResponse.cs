using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a response containing candlestick data for a specific market on Kalshi.
    /// </summary>
    public class CandlestickResponse
    {
        [JsonPropertyName("ticker")] public string Ticker { get; set; }
        [JsonPropertyName("candlesticks")] public List<APICandlestick> Candlesticks { get; set; } = new List<APICandlestick>();
    }
}
