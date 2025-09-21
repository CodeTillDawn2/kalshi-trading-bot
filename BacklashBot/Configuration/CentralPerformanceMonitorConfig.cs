using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
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
        /// Gets or sets whether to enable performance metrics collection in CentralPerformanceMonitor.
        /// </summary>
        /// <value>Default is true.</value>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
