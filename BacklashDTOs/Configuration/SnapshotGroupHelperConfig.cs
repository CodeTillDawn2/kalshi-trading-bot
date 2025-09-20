using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for MarketAnalysisHelper-specific settings.
    /// </summary>
    public class SnapshotGroupHelperConfig
    {
        /// <summary>
        /// The configuration section name for SnapshotGroupHelperConfig.
        /// </summary>
        public const string SectionName = "SnapshotGroupHelper";

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for MarketAnalysisHelper operations.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
