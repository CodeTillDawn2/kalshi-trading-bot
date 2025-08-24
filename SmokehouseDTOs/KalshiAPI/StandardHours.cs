using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{

    public class StandardHours
    {
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = "";

        [JsonPropertyName("end_time")]
        public string EndTime { get; set; } = "";

        public List<TradingPeriod> Monday { get; set; } = new();
        public List<TradingPeriod> Tuesday { get; set; } = new();
        public List<TradingPeriod> Wednesday { get; set; } = new();
        public List<TradingPeriod> Thursday { get; set; } = new();
        public List<TradingPeriod> Friday { get; set; } = new();
        public List<TradingPeriod> Saturday { get; set; } = new();
        public List<TradingPeriod> Sunday { get; set; } = new();
    }
}
