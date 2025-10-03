namespace BacklashBotData.Models
{
    /// <summary>
    /// Database entity for storing BrainPersistence data.
    /// Uses JSON serialization to store complex nested objects.
    /// </summary>
    public class BrainPersistenceEntity
    {
        /// <summary>
        /// Gets or sets the unique name identifier for this brain instance.
        /// This serves as the primary key for the brain persistence data.
        /// </summary>
        public string BrainInstanceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON serialized BrainPersistence data.
        /// Contains all the configuration, state, and historical metrics.
        /// </summary>
        public string PersistenceData { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when this data was last saved.
        /// Used for tracking data freshness and cleanup operations.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the version of the persistence format.
        /// Allows for future schema migrations.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
