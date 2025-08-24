using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class SnapshotSchemaExtensions
    {
        public static SnapshotSchemaDTO ToSnapshotSchemaDTO(this SnapshotSchema snapshotSchema)
        {
            return new SnapshotSchemaDTO
            {
                SchemaVersion = snapshotSchema.SchemaVersion,
                SchemaDefinition = snapshotSchema.SchemaDefinition
            };
        }

        public static SnapshotSchema ToSnapshotSchema(this SnapshotSchemaDTO snapshotSchemaDTO)
        {
            return new SnapshotSchema
            {
                SchemaDefinition = snapshotSchemaDTO.SchemaDefinition
            };
        }

    }
}