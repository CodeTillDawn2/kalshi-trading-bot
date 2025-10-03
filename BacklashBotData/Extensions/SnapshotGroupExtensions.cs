using BacklashDTOs.Data;
using BacklashBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between SnapshotGroup model and SnapshotGroupDTO,
    /// supporting grouped snapshot data transfer for market analysis over time periods.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class SnapshotGroupExtensions
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
        /// Converts a SnapshotGroup model to its DTO representation,
        /// mapping all snapshot group properties including time ranges and price data.
        /// </summary>
        /// <param name="snapshotGroup">The SnapshotGroup model to convert.</param>
        /// <returns>A new SnapshotGroupDTO with all snapshot group properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotGroup is null.</exception>
        public static SnapshotGroupDTO ToSnapshotGroupDTO(this SnapshotGroup snapshotGroup)
        {
            if (snapshotGroup == null)
                throw new ArgumentNullException(nameof(snapshotGroup));

            var stopwatch = Stopwatch.StartNew();

            var result = new SnapshotGroupDTO
            {
                MarketTicker = snapshotGroup.MarketTicker,
                StartTime = snapshotGroup.StartTime,
                EndTime = snapshotGroup.EndTime,
                YesStart = snapshotGroup.YesStart,
                NoStart = snapshotGroup.NoStart,
                YesEnd = snapshotGroup.YesEnd,
                NoEnd = snapshotGroup.NoEnd,
                AverageLiquidity = snapshotGroup.AverageLiquidity,
                SnapshotSchema = snapshotGroup.SnapshotSchema,
                ProcessedDttm = snapshotGroup.ProcessedDttm,
                JsonPath = snapshotGroup.JsonPath,
                SnapshotGroupID = snapshotGroup.SnapshotGroupID
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotGroupDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a SnapshotGroupDTO to its model representation,
        /// creating a new SnapshotGroup with all properties mapped from the DTO.
        /// </summary>
        /// <param name="snapshotGroupDTO">The SnapshotGroupDTO to convert.</param>
        /// <returns>A new SnapshotGroup model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotGroupDTO is null.</exception>
        public static SnapshotGroup ToSnapshotGroup(this SnapshotGroupDTO snapshotGroupDTO)
        {
            if (snapshotGroupDTO == null)
                throw new ArgumentNullException(nameof(snapshotGroupDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new SnapshotGroup
            {
                MarketTicker = snapshotGroupDTO.MarketTicker,
                StartTime = snapshotGroupDTO.StartTime,
                EndTime = snapshotGroupDTO.EndTime,
                YesStart = snapshotGroupDTO.YesStart,
                NoStart = snapshotGroupDTO.NoStart,
                YesEnd = snapshotGroupDTO.YesEnd,
                NoEnd = snapshotGroupDTO.NoEnd,
                AverageLiquidity = snapshotGroupDTO.AverageLiquidity,
                ProcessedDttm = snapshotGroupDTO.ProcessedDttm,
                JsonPath = snapshotGroupDTO.JsonPath,
                SnapshotSchema = snapshotGroupDTO.SnapshotSchema,
                SnapshotGroupID = snapshotGroupDTO.SnapshotGroupID
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotGroup", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing SnapshotGroup model with data from a SnapshotGroupDTO,
        /// validating market ticker and start time match before applying changes.
        /// </summary>
        /// <param name="snapshotGroup">The SnapshotGroup model to update.</param>
        /// <param name="snapshotGroupDTO">The SnapshotGroupDTO containing updated data.</param>
        /// <returns>The updated SnapshotGroup model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotGroup or snapshotGroupDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when market tickers or start times do not match.</exception>
        public static SnapshotGroup UpdateSnapshotGroup(this SnapshotGroup snapshotGroup, SnapshotGroupDTO snapshotGroupDTO)
        {
            if (snapshotGroup == null)
                throw new ArgumentNullException(nameof(snapshotGroup));
            if (snapshotGroupDTO == null)
                throw new ArgumentNullException(nameof(snapshotGroupDTO));

            if (snapshotGroup.MarketTicker != snapshotGroupDTO.MarketTicker || snapshotGroup.StartTime != snapshotGroupDTO.StartTime)
            {
                throw new ArgumentException("Market ticker or start time don't match for Update SnapshotGroup", nameof(snapshotGroupDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            snapshotGroup.EndTime = snapshotGroupDTO.EndTime;
            snapshotGroup.YesStart = snapshotGroupDTO.YesStart;
            snapshotGroup.NoStart = snapshotGroupDTO.NoStart;
            snapshotGroup.YesEnd = snapshotGroupDTO.YesEnd;
            snapshotGroup.NoEnd = snapshotGroupDTO.NoEnd;
            snapshotGroup.AverageLiquidity = snapshotGroupDTO.AverageLiquidity;
            snapshotGroup.ProcessedDttm = snapshotGroupDTO.ProcessedDttm;
            snapshotGroup.JsonPath = snapshotGroupDTO.JsonPath;
            snapshotGroup.SnapshotSchema = snapshotGroupDTO.SnapshotSchema;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateSnapshotGroup", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return snapshotGroup;
        }

        /// <summary>
        /// Converts a collection of SnapshotGroup models to their corresponding DTO representations.
        /// </summary>
        /// <param name="snapshotGroups">The collection of SnapshotGroup models to convert.</param>
        /// <returns>A list of SnapshotGroupDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotGroups is null.</exception>
        public static List<SnapshotGroupDTO> ToSnapshotGroupDTOs(this IEnumerable<SnapshotGroup> snapshotGroups)
        {
            if (snapshotGroups == null)
                throw new ArgumentNullException(nameof(snapshotGroups));

            var stopwatch = Stopwatch.StartNew();

            var result = snapshotGroups.Select(sg => sg.ToSnapshotGroupDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotGroupDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of SnapshotGroupDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="snapshotGroupDTOs">The collection of SnapshotGroupDTOs to convert.</param>
        /// <returns>A list of SnapshotGroup models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotGroupDTOs is null.</exception>
        public static List<SnapshotGroup> ToSnapshotGroups(this IEnumerable<SnapshotGroupDTO> snapshotGroupDTOs)
        {
            if (snapshotGroupDTOs == null)
                throw new ArgumentNullException(nameof(snapshotGroupDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = snapshotGroupDTOs.Select(dto => dto.ToSnapshotGroup()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSnapshotGroups", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a SnapshotGroup model to prevent unintended mutations.
        /// </summary>
        /// <param name="snapshotGroup">The SnapshotGroup model to clone.</param>
        /// <returns>A new SnapshotGroup instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotGroup is null.</exception>
        public static SnapshotGroup DeepClone(this SnapshotGroup snapshotGroup)
        {
            if (snapshotGroup == null)
                throw new ArgumentNullException(nameof(snapshotGroup));

            return new SnapshotGroup
            {
                MarketTicker = snapshotGroup.MarketTicker,
                StartTime = snapshotGroup.StartTime,
                EndTime = snapshotGroup.EndTime,
                YesStart = snapshotGroup.YesStart,
                NoStart = snapshotGroup.NoStart,
                YesEnd = snapshotGroup.YesEnd,
                NoEnd = snapshotGroup.NoEnd,
                AverageLiquidity = snapshotGroup.AverageLiquidity,
                SnapshotSchema = snapshotGroup.SnapshotSchema,
                ProcessedDttm = snapshotGroup.ProcessedDttm,
                JsonPath = snapshotGroup.JsonPath,
                SnapshotGroupID = snapshotGroup.SnapshotGroupID
            };
        }

        /// <summary>
        /// Creates a deep clone of a SnapshotGroupDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="snapshotGroupDTO">The SnapshotGroupDTO to clone.</param>
        /// <returns>A new SnapshotGroupDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshotGroupDTO is null.</exception>
        public static SnapshotGroupDTO DeepClone(this SnapshotGroupDTO snapshotGroupDTO)
        {
            if (snapshotGroupDTO == null)
                throw new ArgumentNullException(nameof(snapshotGroupDTO));

            return new SnapshotGroupDTO
            {
                MarketTicker = snapshotGroupDTO.MarketTicker,
                StartTime = snapshotGroupDTO.StartTime,
                EndTime = snapshotGroupDTO.EndTime,
                YesStart = snapshotGroupDTO.YesStart,
                NoStart = snapshotGroupDTO.NoStart,
                YesEnd = snapshotGroupDTO.YesEnd,
                NoEnd = snapshotGroupDTO.NoEnd,
                AverageLiquidity = snapshotGroupDTO.AverageLiquidity,
                SnapshotSchema = snapshotGroupDTO.SnapshotSchema,
                ProcessedDttm = snapshotGroupDTO.ProcessedDttm,
                JsonPath = snapshotGroupDTO.JsonPath,
                SnapshotGroupID = snapshotGroupDTO.SnapshotGroupID
            };
        }
    }
}
