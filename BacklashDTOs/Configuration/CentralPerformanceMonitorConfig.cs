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
        public required double QueueHighCountAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets the refresh usage alert threshold.
        /// </summary>
        /// <value>Default is 90.0.</value>
        public required double RefreshUsageAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets the queue count alert threshold.
        /// </summary>
        /// <value>Default is 100.</value>
        public required int QueueCountAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether to enable database performance metrics collection in CentralPerformanceMonitor.
        /// </summary>
        /// <value>Default is true.</value>
        public required bool CentralPerformanceMonitor_EnableDatabaseMetrics { get; set; }
    }
}
