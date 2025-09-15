using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{

    /// <summary>
    /// Represents the standard hours for trading.
    /// </summary>
    public class StandardHours
    {
        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = "";

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        [JsonPropertyName("end_time")]
        public string EndTime { get; set; } = "";

        /// <summary>
        /// Gets or sets the trading periods for Monday.
        /// </summary>
        public List<TradingPeriod> Monday { get; set; } = new();
        /// <summary>
        /// Gets or sets the trading periods for Tuesday.
        /// </summary>
        public List<TradingPeriod> Tuesday { get; set; } = new();
        /// <summary>
        /// Gets or sets the trading periods for Wednesday.
        /// </summary>
        public List<TradingPeriod> Wednesday { get; set; } = new();
        /// <summary>
        /// Gets or sets the trading periods for Thursday.
        /// </summary>
        public List<TradingPeriod> Thursday { get; set; } = new();
        /// <summary>
        /// Gets or sets the trading periods for Friday.
        /// </summary>
        public List<TradingPeriod> Friday { get; set; } = new();
        /// <summary>
        /// Gets or sets the trading periods for Saturday.
        /// </summary>
        public List<TradingPeriod> Saturday { get; set; } = new();
        /// <summary>
        /// Gets or sets the trading periods for Sunday.
        /// </summary>
        public List<TradingPeriod> Sunday { get; set; } = new();
    }
}
