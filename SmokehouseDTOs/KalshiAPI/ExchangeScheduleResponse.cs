using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{
    public class ExchangeScheduleResponse
    {
        [JsonPropertyName("schedule")]
        public ScheduleData Schedule { get; set; } = new ScheduleData();
    }

    public class ScheduleData
    {
        [JsonPropertyName("maintenance_windows")]
        public List<MaintenanceWindowApi> MaintenanceWindows { get; set; } = new List<MaintenanceWindowApi>();

        [JsonPropertyName("standard_hours")]
        public List<StandardHoursApi> StandardHours { get; set; } = new List<StandardHoursApi>();
    }

    public class MaintenanceWindowApi
    {
        [JsonPropertyName("end_datetime")]
        public DateTime EndDateTime { get; set; }

        [JsonPropertyName("start_datetime")]
        public DateTime StartDateTime { get; set; }
    }

    public class StandardHoursApi
    {
        [JsonPropertyName("end_time")]
        public DateTime EndTime { get; set; }

        [JsonPropertyName("friday")]
        public List<TradingSession> Friday { get; set; } = new List<TradingSession>();

        [JsonPropertyName("monday")]
        public List<TradingSession> Monday { get; set; } = new List<TradingSession>();

        [JsonPropertyName("saturday")]
        public List<TradingSession> Saturday { get; set; } = new List<TradingSession>();

        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("sunday")]
        public List<TradingSession> Sunday { get; set; } = new List<TradingSession>();

        [JsonPropertyName("thursday")]
        public List<TradingSession> Thursday { get; set; } = new List<TradingSession>();

        [JsonPropertyName("tuesday")]
        public List<TradingSession> Tuesday { get; set; } = new List<TradingSession>();

        [JsonPropertyName("wednesday")]
        public List<TradingSession> Wednesday { get; set; } = new List<TradingSession>();
    }

    public class TradingSession
    {
        [JsonPropertyName("close_time")]
        public string CloseTime { get; set; }

        [JsonPropertyName("open_time")]
        public string OpenTime { get; set; }
    }
}
