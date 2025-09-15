
namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for snapshot schema data.
    /// </summary>
    public class SnapshotSchemaDTO
    {
        /// <summary>
        /// Gets or sets the schema version number.
        /// </summary>
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets the schema definition.
        /// </summary>
        public string? SchemaDefinition { get; set; }
    }
}
