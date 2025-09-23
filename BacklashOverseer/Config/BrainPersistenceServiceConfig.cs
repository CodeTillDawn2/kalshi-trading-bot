using System.ComponentModel.DataAnnotations;

namespace BacklashOverseer.Config
{
    /// <summary>
    /// Configuration options for the BrainPersistenceService.
    /// </summary>
    public class BrainPersistenceServiceConfig
    {
        /// <summary>
        /// The configuration section name for BrainPersistenceServiceConfig.
        /// </summary>
        public const string SectionName = "BrainPersistenceService";

        /// <summary>
        /// Gets or sets the maximum number of entries to keep in metric history lists.
        /// Default is 50.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxHistoryEntries' is missing in the configuration.")]
        public int MaxHistoryEntries { get; set; }

        /// <summary>
        /// Gets or sets whether to enable BrainPersistenceService performance metrics collection.
        /// When enabled, detailed performance metrics including operation statistics,
        /// lock metrics, memory usage data, and metrics transmission are collected.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }


        /// <summary>
        /// Gets or sets the interval in minutes for saving data to persistence store.
        /// </summary>
        [Required(ErrorMessage = "The 'PersistenceSaveIntervalMinutes' is missing in the configuration.")]
        public int PersistenceSaveIntervalMinutes { get; set; }
    }
}
