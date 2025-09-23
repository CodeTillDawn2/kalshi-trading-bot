using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for queue monitoring and alert settings.
    /// </summary>
    public class QueueMonitoringConfig
    {
        /// <summary>
        /// The configuration section name for QueueMonitoringConfig.
        /// </summary>
        public const string SectionName = "Central:QueueMonitoring";

        /// <summary>
        /// Gets or sets the percentage threshold for queue high count alerts.
        /// When the EventQueue utilization exceeds this percentage, a performance alert is logged.
        /// </summary>
        [Required(ErrorMessage = "The 'QueueHighCountAlertThreshold' is missing in the configuration.")]
        public double QueueHighCountAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets the percentage threshold for refresh usage alerts.
        /// When the market refresh cycle usage exceeds this percentage, a performance alert is logged.
        /// </summary>
        [Required(ErrorMessage = "The 'RefreshUsageAlertThreshold' is missing in the configuration.")]
        public double RefreshUsageAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets the absolute count threshold for queue alerts.
        /// When any queue's average count exceeds this value, a performance alert is logged.
        /// </summary>
        [Required(ErrorMessage = "The 'QueueCountAlertThreshold' is missing in the configuration.")]
        public int QueueCountAlertThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether to enable database metrics collection in CentralPerformanceMonitor.
        /// </summary>
        [Required(ErrorMessage = "The 'CentralPerformanceMonitor_EnableDatabaseMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
