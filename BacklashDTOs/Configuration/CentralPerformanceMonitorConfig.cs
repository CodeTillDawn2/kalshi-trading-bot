using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for CentralPerformanceMonitor settings.
    /// </summary>
    public class CentralPerformanceMonitorConfig
    {
        /// <summary>
        /// Gets or sets the queue high count alert threshold.
        /// </summary>
        /// <value>Default is 80.0.</value>
        [JsonRequired]
        public double QueueHighCountAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets the refresh usage alert threshold.
        /// </summary>
        /// <value>Default is 90.0.</value>
        [JsonRequired]
        public double RefreshUsageAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets the queue count alert threshold.
        /// </summary>
        /// <value>Default is 100.</value>
        [JsonRequired]
        public int QueueCountAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether to enable database performance metrics collection in CentralPerformanceMonitor.
        /// </summary>
        /// <value>Default is true.</value>
        [JsonRequired]
        public bool CentralPerformanceMonitor_EnableDatabaseMetrics { get; set; }
    }
}