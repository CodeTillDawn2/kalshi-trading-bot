using System.ComponentModel.DataAnnotations;

namespace BacklashCommon.Configuration
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
        /// Gets or sets the retry delay in milliseconds for operations that require retries.
        /// </summary>
        [Required(ErrorMessage = "The 'SnapshotGroupRetryDelayMs' is missing in the configuration.")]
        public int SnapshotGroupRetryDelayMs { get; set; }
        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for MarketAnalysisHelper operations.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }

    }
}
