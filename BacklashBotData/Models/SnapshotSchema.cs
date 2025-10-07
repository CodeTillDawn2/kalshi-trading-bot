
namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents the schema definition and versioning for market snapshots.
    /// This entity defines the structure and format of snapshot data, ensuring
    /// consistency and compatibility across different versions of the system.
    /// Schema definitions are crucial for data validation, migration, and
    /// maintaining backward compatibility as the snapshot format evolves.
    /// </summary>
    public class SnapshotSchema
    {
        /// <summary>
        /// Gets or sets the version number of this snapshot schema.
        /// This serves as the primary key and version identifier for the schema.
        /// </summary>
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema definition that describes the structure of snapshot data.
        /// This contains the complete schema specification for validating and parsing snapshots.
        /// </summary>
        public string SchemaDefinition { get; set; }
    }
}
