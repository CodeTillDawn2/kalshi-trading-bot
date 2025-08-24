using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a single candlestick data point for a Kalshi market, including price and volume info.
    /// </summary>
    public class APICandlestick
    {
        [JsonPropertyName("end_period_ts")] public long EndPeriodTs { get; set; }
        [JsonPropertyName("open_interest")] public int OpenInterest { get; set; }
        [JsonPropertyName("volume")] public int Volume { get; set; }
        [JsonPropertyName("price")] public PriceData Price { get; set; }
        [JsonPropertyName("yes_ask")] public PriceData YesAsk { get; set; }
        [JsonPropertyName("yes_bid")] public PriceData YesBid { get; set; }
    }
}