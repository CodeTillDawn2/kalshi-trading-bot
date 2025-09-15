using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing the exchange schedule from the Kalshi API.
    /// </summary>
    public class ExchangeScheduleResponse
    {
        /// <summary>
        /// Gets or sets the schedule data.
        /// </summary>
        [JsonPropertyName("schedule")]
        public ScheduleData Schedule { get; set; } = new ScheduleData();
    }

    /// <summary>
    /// Represents the schedule data.
    /// </summary>
    public class ScheduleData
    {
        /// <summary>
        /// Gets or sets the list of maintenance windows.
        /// </summary>
        [JsonPropertyName("maintenance_windows")]
        public List<MaintenanceWindowApi> MaintenanceWindows { get; set; } = new List<MaintenanceWindowApi>();

        /// <summary>
        /// Gets or sets the list of standard hours.
        /// </summary>
        [JsonPropertyName("standard_hours")]
        public List<StandardHoursApi> StandardHours { get; set; } = new List<StandardHoursApi>();
    }

    /// <summary>
    /// Represents a maintenance window.
    /// </summary>
    public class MaintenanceWindowApi
    {
        /// <summary>
        /// Gets or sets the end date and time.
        /// </summary>
        [JsonPropertyName("end_datetime")]
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the start date and time.
        /// </summary>
        [JsonPropertyName("start_datetime")]
        public DateTime StartDateTime { get; set; }
    }

    /// <summary>
    /// Represents the standard hours for trading.
    /// </summary>
    public class StandardHoursApi
    {
        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        [JsonPropertyName("end_time")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the trading sessions for Friday.
        /// </summary>
        [JsonPropertyName("friday")]
        public List<TradingSession> Friday { get; set; } = new List<TradingSession>();

        /// <summary>
        /// Gets or sets the trading sessions for Monday.
        /// </summary>
        [JsonPropertyName("monday")]
        public List<TradingSession> Monday { get; set; } = new List<TradingSession>();

        /// <summary>
        /// Gets or sets the trading sessions for Saturday.
        /// </summary>
        [JsonPropertyName("saturday")]
        public List<TradingSession> Saturday { get; set; } = new List<TradingSession>();

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the trading sessions for Sunday.
        /// </summary>
        [JsonPropertyName("sunday")]
        public List<TradingSession> Sunday { get; set; } = new List<TradingSession>();

        /// <summary>
        /// Gets or sets the trading sessions for Thursday.
        /// </summary>
        [JsonPropertyName("thursday")]
        public List<TradingSession> Thursday { get; set; } = new List<TradingSession>();

        /// <summary>
        /// Gets or sets the trading sessions for Tuesday.
        /// </summary>
        [JsonPropertyName("tuesday")]
        public List<TradingSession> Tuesday { get; set; } = new List<TradingSession>();

        /// <summary>
        /// Gets or sets the trading sessions for Wednesday.
        /// </summary>
        [JsonPropertyName("wednesday")]
        public List<TradingSession> Wednesday { get; set; } = new List<TradingSession>();
    }

    /// <summary>
    /// Represents a trading session.
    /// </summary>
    public class TradingSession
    {
        /// <summary>
        /// Gets or sets the close time.
        /// </summary>
        [JsonPropertyName("close_time")]
        public string? CloseTime { get; set; }

        /// <summary>
        /// Gets or sets the open time.
        /// </summary>
        [JsonPropertyName("open_time")]
        public string? OpenTime { get; set; }
    }
}
