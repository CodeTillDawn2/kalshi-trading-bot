using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for CentralPerformanceMonitor settings.
    /// </summary>
    public class CentralPerformanceMonitorConfig
    {
        /// <summary>
        /// The configuration section name for CentralPerformanceMonitorConfig.
        /// </summary>
        public const string SectionName = "Central:CentralPerformanceMonitor";

        /// <summary>
        /// Gets or sets the queue high count alert threshold.
        /// </summary>
        /// <value>Default is 80.0.</value>
        [Required(ErrorMessage = "The 'QueueHighCountAlertThreshold' is missing in the configuration.")]
        public double QueueHighCountAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets the refresh usage alert threshold.
        /// </summary>
        /// <value>Default is 90.0.</value>
        [Required(ErrorMessage = "The 'RefreshUsageAlertThreshold' is missing in the configuration.")]
        public double RefreshUsageAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets the queue count alert threshold.
        /// </summary>
        /// <value>Default is 100.</value>
        [Required(ErrorMessage = "The 'QueueCountAlertThreshold' is missing in the configuration.")]
        public int QueueCountAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether to enable database performance metrics collection in CentralPerformanceMonitor.
        /// </summary>
        /// <value>Default is true.</value>
        [Required(ErrorMessage = "The 'CentralPerformanceMonitor_EnableDatabaseMetrics' is missing in the configuration.")]
        public bool CentralPerformanceMonitor_EnableDatabaseMetrics { get; set; }
    }
}
