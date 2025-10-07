using System.ComponentModel.DataAnnotations;

namespace BacklashOverseer.Config
{
    /// <summary>
    /// Configuration options for the SnapshotService behavior, including performance metrics settings.
    /// This configuration is loaded from the "SnapshotService" section of the application configuration.
    /// </summary>
    public class SnapshotServiceConfig
    {
        /// <summary>
        /// The configuration section name for SnapshotServiceConfig.
        /// This constant defines the path in the configuration file where these settings are located.
        /// </summary>
        public const string SectionName = "SnapshotService";

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled for the SnapshotService.
        /// When enabled, aggregation execution times are tracked and reported.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}