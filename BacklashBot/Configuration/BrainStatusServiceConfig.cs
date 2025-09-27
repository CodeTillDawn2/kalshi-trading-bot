using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for BrainStatusService-specific settings.
    /// </summary>
    public class BrainStatusServiceConfig
    {
        /// <summary>
        /// The configuration section name for BrainStatusServiceConfig.
        /// </summary>
        public const string SectionName = "Central:BrainStatusService";

        /// <summary>
        /// Gets or sets whether performance metrics are enabled for the BrainStatusService.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; }
    }
}
