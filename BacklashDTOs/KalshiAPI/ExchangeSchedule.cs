using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{

    /// <summary>
    /// Represents the exchange schedule from the Kalshi API.
    /// </summary>
    public class ExchangeSchedule
    {
        /// <summary>
        /// Gets or sets the list of standard hours.
        /// </summary>
        [JsonPropertyName("standard_hours")]
        public List<StandardHours> StandardHours { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of maintenance windows.
        /// </summary>
        [JsonPropertyName("maintenance_windows")]
        public List<object> MaintenanceWindows { get; set; } = new();
    }
}
