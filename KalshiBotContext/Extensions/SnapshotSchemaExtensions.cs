using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between SnapshotSchema model and SnapshotSchemaDTO,
    /// supporting schema versioning and definition data transfer for snapshot validation.
    /// </summary>
    public static class SnapshotSchemaExtensions
    {
        /// <summary>
        /// Converts a SnapshotSchema model to its DTO representation,
        /// mapping schema version and definition for data transfer.
        /// </summary>
        /// <param name="snapshotSchema">The SnapshotSchema model to convert.</param>
        /// <returns>A new SnapshotSchemaDTO with schema properties mapped.</returns>
        public static SnapshotSchemaDTO ToSnapshotSchemaDTO(this SnapshotSchema snapshotSchema)
        {
            return new SnapshotSchemaDTO
            {
                SchemaVersion = snapshotSchema.SchemaVersion,
                SchemaDefinition = snapshotSchema.SchemaDefinition
            };
        }

        /// <summary>
        /// Converts a SnapshotSchemaDTO to its model representation,
        /// creating a new SnapshotSchema with schema definition mapped from the DTO.
        /// </summary>
        /// <param name="snapshotSchemaDTO">The SnapshotSchemaDTO to convert.</param>
        /// <returns>A new SnapshotSchema model with schema definition mapped.</returns>
        public static SnapshotSchema ToSnapshotSchema(this SnapshotSchemaDTO snapshotSchemaDTO)
        {
            return new SnapshotSchema
            {
                SchemaDefinition = snapshotSchemaDTO.SchemaDefinition
            };
        }
    }
}
