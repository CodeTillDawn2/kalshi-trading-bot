using System.ComponentModel.DataAnnotations;

namespace BacklashCommon.Configuration
{
    /// <summary>
    /// Configuration class for data storage settings.
    /// </summary>
    public class DataStorageConfig
    {
        /// <summary>
        /// The configuration section name for GeneralExecutionConfig.
        /// </summary>
        public const string SectionName = "DataStorage";

        /// <summary>
        /// Gets or sets the hard data storage location.
        /// </summary>
        [Required(ErrorMessage = "The 'HardDataStorageLocation' is missing in the configuration.")]
        public string HardDataStorageLocation { get; set; } = null!;

        /// <summary>
        /// Gets or sets the cache directory for trading simulation.
        /// </summary>
        [Required(ErrorMessage = "The 'SimulationCacheDirectory' is missing in the configuration.")]
        public string SimulationCacheDirectory { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for data storage operations.
        /// When enabled, tracks operation times, resource usage, and other performance indicators.
        /// Disabling this improves performance but removes monitoring capabilities.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
