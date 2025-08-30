using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{

    public class ExchangeSchedule
    {
        [JsonPropertyName("standard_hours")]
        public List<StandardHours> StandardHours { get; set; } = new();

        [JsonPropertyName("maintenance_windows")]
        public List<object> MaintenanceWindows { get; set; } = new();
    }
}
