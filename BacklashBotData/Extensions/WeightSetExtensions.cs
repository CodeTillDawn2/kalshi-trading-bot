using BacklashDTOs.Data;
using KalshiBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between WeightSet model and WeightSetDTO,
    /// supporting trading strategy weight configuration data transfer and management.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class WeightSetExtensions
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
        /// Converts a WeightSet model to its DTO representation,
        /// mapping all weight set properties for data transfer.
        /// </summary>
        /// <param name="weightSet">The WeightSet model to convert.</param>
        /// <returns>A new WeightSetDTO with all weight set properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSet is null.</exception>
        public static WeightSetDTO ToWeightSetDTO(this WeightSet weightSet)
        {
            if (weightSet == null)
                throw new ArgumentNullException(nameof(weightSet));

            var stopwatch = Stopwatch.StartNew();

            var result = new WeightSetDTO
            {
                WeightSetID = weightSet.WeightSetID,
                StrategyName = weightSet.StrategyName,
                Weights = weightSet.Weights,
                LastRun = weightSet.LastRun
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToWeightSetDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a WeightSetDTO to its model representation,
        /// creating a new WeightSet with all properties mapped from the DTO.
        /// </summary>
        /// <param name="weightSetDTO">The WeightSetDTO to convert.</param>
        /// <returns>A new WeightSet model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSetDTO is null.</exception>
        public static WeightSet ToWeightSet(this WeightSetDTO weightSetDTO)
        {
            if (weightSetDTO == null)
                throw new ArgumentNullException(nameof(weightSetDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new WeightSet
            {
                WeightSetID = weightSetDTO.WeightSetID,
                StrategyName = weightSetDTO.StrategyName,
                Weights = weightSetDTO.Weights,
                LastRun = weightSetDTO.LastRun
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToWeightSet", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing WeightSet model with data from a WeightSetDTO,
        /// applying all property changes for strategy configuration updates.
        /// </summary>
        /// <param name="weightSet">The WeightSet model to update.</param>
        /// <param name="weightSetDTO">The WeightSetDTO containing updated data.</param>
        /// <returns>The updated WeightSet model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSet or weightSetDTO is null.</exception>
        public static WeightSet UpdateWeightSet(this WeightSet weightSet, WeightSetDTO weightSetDTO)
        {
            if (weightSet == null)
                throw new ArgumentNullException(nameof(weightSet));
            if (weightSetDTO == null)
                throw new ArgumentNullException(nameof(weightSetDTO));

            var stopwatch = Stopwatch.StartNew();

            weightSet.StrategyName = weightSetDTO.StrategyName;
            weightSet.Weights = weightSetDTO.Weights;
            weightSet.LastRun = weightSetDTO.LastRun;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateWeightSet", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return weightSet;
        }

        /// <summary>
        /// Converts a collection of WeightSet models to their corresponding DTO representations.
        /// </summary>
        /// <param name="weightSets">The collection of WeightSet models to convert.</param>
        /// <returns>A list of WeightSetDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSets is null.</exception>
        public static List<WeightSetDTO> ToWeightSetDTOs(this IEnumerable<WeightSet> weightSets)
        {
            if (weightSets == null)
                throw new ArgumentNullException(nameof(weightSets));

            var stopwatch = Stopwatch.StartNew();

            var result = weightSets.Select(ws => ws.ToWeightSetDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToWeightSetDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of WeightSetDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="weightSetDTOs">The collection of WeightSetDTOs to convert.</param>
        /// <returns>A list of WeightSet models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSetDTOs is null.</exception>
        public static List<WeightSet> ToWeightSets(this IEnumerable<WeightSetDTO> weightSetDTOs)
        {
            if (weightSetDTOs == null)
                throw new ArgumentNullException(nameof(weightSetDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = weightSetDTOs.Select(dto => dto.ToWeightSet()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToWeightSets", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a WeightSet model to prevent unintended mutations.
        /// </summary>
        /// <param name="weightSet">The WeightSet model to clone.</param>
        /// <returns>A new WeightSet instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSet is null.</exception>
        public static WeightSet DeepClone(this WeightSet weightSet)
        {
            if (weightSet == null)
                throw new ArgumentNullException(nameof(weightSet));

            return new WeightSet
            {
                WeightSetID = weightSet.WeightSetID,
                StrategyName = weightSet.StrategyName,
                Weights = weightSet.Weights,
                LastRun = weightSet.LastRun
            };
        }

        /// <summary>
        /// Creates a deep clone of a WeightSetDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="weightSetDTO">The WeightSetDTO to clone.</param>
        /// <returns>A new WeightSetDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSetDTO is null.</exception>
        public static WeightSetDTO DeepClone(this WeightSetDTO weightSetDTO)
        {
            if (weightSetDTO == null)
                throw new ArgumentNullException(nameof(weightSetDTO));

            return new WeightSetDTO
            {
                WeightSetID = weightSetDTO.WeightSetID,
                StrategyName = weightSetDTO.StrategyName,
                Weights = weightSetDTO.Weights,
                LastRun = weightSetDTO.LastRun
            };
        }
    }
}
