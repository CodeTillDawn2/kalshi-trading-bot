using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{

    public class TradingPeriod
    {
        [JsonPropertyName("open_time")]
        public string OpenTime { get; set; } = "";

        [JsonPropertyName("close_time")]
        public string CloseTime { get; set; } = "";
    }
}
