using BacklashDTOs.Data;
using KalshiBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between SnapshotSchema model and SnapshotSchemaDTO,
    /// supporting schema versioning and definition data transfer for snapshot validation.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class SnapshotSchemaExtensions
    {
        private static readonly ConcurrentDictionary<string, List<TimeSpan>> _performanceMetrics = new();

        /// <summary>
        /// Gets the performance metrics for transformation operations
        /// </summary>
        public static IReadOnlyDictionary<string, List<TimeSpan>> GetPerformanceMetrics()
        {
            return _performanceMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        /// <summary>
        /// Converts a SnapshotSchema model to its DTO representation,
        /// mapping schema version and definition for data transfer.
        /// </summary>
        /// <param name="snapshotSchema">The SnapshotSchema model to convert.</param>
        /// <returns>A new SnapshotSchemaDTO with schema properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotSchema is null.</exception>
        public static SnapshotSchemaDTO ToSnapshotSchemaDTO(this SnapshotSchema snapshotSchema)
        {
            if (snapshotSchema == null)
                throw new ArgumentNullException(nameof(snapshotSchema));

            var stopwatch = Stopwatch.StartNew();

            var result = new SnapshotSchemaDTO
            {
                SchemaVersion = snapshotSchema.SchemaVersion,
                SchemaDefinition = snapshotSchema.SchemaDefinition
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotSchemaDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a SnapshotSchemaDTO to its model representation,
        /// creating a new SnapshotSchema with schema definition mapped from the DTO.
        /// </summary>
        /// <param name="snapshotSchemaDTO">The SnapshotSchemaDTO to convert.</param>
        /// <returns>A new SnapshotSchema model with schema definition mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotSchemaDTO is null.</exception>
        public static SnapshotSchema ToSnapshotSchema(this SnapshotSchemaDTO snapshotSchemaDTO)
        {
            if (snapshotSchemaDTO == null)
                throw new ArgumentNullException(nameof(snapshotSchemaDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new SnapshotSchema
            {
                SchemaDefinition = snapshotSchemaDTO.SchemaDefinition
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotSchema", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of SnapshotSchema models to their corresponding DTO representations.
        /// </summary>
        /// <param name="snapshotSchemas">The collection of SnapshotSchema models to convert.</param>
        /// <returns>A list of SnapshotSchemaDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotSchemas is null.</exception>
        public static List<SnapshotSchemaDTO> ToSnapshotSchemaDTOs(this IEnumerable<SnapshotSchema> snapshotSchemas)
        {
            if (snapshotSchemas == null)
                throw new ArgumentNullException(nameof(snapshotSchemas));

            var stopwatch = Stopwatch.StartNew();

            var result = snapshotSchemas.Select(ss => ss.ToSnapshotSchemaDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotSchemaDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of SnapshotSchemaDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="snapshotSchemaDTOs">The collection of SnapshotSchemaDTOs to convert.</param>
        /// <returns>A list of SnapshotSchema models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotSchemaDTOs is null.</exception>
        public static List<SnapshotSchema> ToSnapshotSchemas(this IEnumerable<SnapshotSchemaDTO> snapshotSchemaDTOs)
        {
            if (snapshotSchemaDTOs == null)
                throw new ArgumentNullException(nameof(snapshotSchemaDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = snapshotSchemaDTOs.Select(dto => dto.ToSnapshotSchema()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotSchemas", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a SnapshotSchema model to prevent unintended mutations.
        /// </summary>
        /// <param name="snapshotSchema">The SnapshotSchema model to clone.</param>
        /// <returns>A new SnapshotSchema instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotSchema is null.</exception>
        public static SnapshotSchema DeepClone(this SnapshotSchema snapshotSchema)
        {
            if (snapshotSchema == null)
                throw new ArgumentNullException(nameof(snapshotSchema));

            return new SnapshotSchema
            {
                SchemaVersion = snapshotSchema.SchemaVersion,
                SchemaDefinition = snapshotSchema.SchemaDefinition
            };
        }

        /// <summary>
        /// Creates a deep clone of a SnapshotSchemaDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="snapshotSchemaDTO">The SnapshotSchemaDTO to clone.</param>
        /// <returns>A new SnapshotSchemaDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotSchemaDTO is null.</exception>
        public static SnapshotSchemaDTO DeepClone(this SnapshotSchemaDTO snapshotSchemaDTO)
        {
            if (snapshotSchemaDTO == null)
                throw new ArgumentNullException(nameof(snapshotSchemaDTO));

            return new SnapshotSchemaDTO
            {
                SchemaVersion = snapshotSchemaDTO.SchemaVersion,
                SchemaDefinition = snapshotSchemaDTO.SchemaDefinition
            };
        }
    }
}
