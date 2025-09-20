using System.Text.Json.Serialization;

namespace BacklashOverseer.Config
{
    /// <summary>
    /// Configuration options for the BrainPersistenceService.
    /// </summary>
    public class BrainPersistenceServiceConfig
    {
        /// <summary>
        /// Gets or sets the maximum number of entries to keep in metric history lists.
        /// Default is 50.
        /// </summary>
        required
        public int MaxHistoryEntries { get; set; }

        /// <summary>
        /// Gets or sets whether to enable BrainPersistenceService performance metrics collection.
        /// When enabled, detailed performance metrics including operation statistics,
        /// lock metrics, memory usage data, and metrics transmission are collected.
        /// </summary>
        required
        public bool EnablePerformanceMetrics { get; set; }

        /// <summary>
        /// Gets or sets whether to enable data persistence to database.
        /// </summary>
        required
        public bool EnablePersistence { get; set; }

        /// <summary>
        /// Gets or sets the interval in minutes for saving data to persistence store.
        /// </summary>
        required
        public int PersistenceSaveIntervalMinutes { get; set; }
    }
}
