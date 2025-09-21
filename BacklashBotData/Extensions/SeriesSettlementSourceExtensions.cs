using BacklashDTOs.Data;
using KalshiBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between SeriesSettlementSource model and SeriesSettlementSourceDTO,
    /// supporting settlement source data transfer for series validation and reference.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class SeriesSettlementSourceExtensions
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
        /// Converts a SeriesSettlementSource model to its DTO representation,
        /// mapping all settlement source properties for data transfer.
        /// </summary>
        /// <param name="source">The SeriesSettlementSource model to convert.</param>
        /// <returns>A new SeriesSettlementSourceDTO with all settlement source properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
        public static SeriesSettlementSourceDTO ToSeriesSettlementSourceDTO(this SeriesSettlementSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var stopwatch = Stopwatch.StartNew();

            var result = new SeriesSettlementSourceDTO
            {
                series_ticker = source.series_ticker,
                name = source.name,
                url = source.url
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesSettlementSourceDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a SeriesSettlementSourceDTO to its model representation,
        /// creating a new SeriesSettlementSource with all properties mapped from the DTO.
        /// </summary>
        /// <param name="sourceDTO">The SeriesSettlementSourceDTO to convert.</param>
        /// <returns>A new SeriesSettlementSource model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when sourceDTO is null.</exception>
        public static SeriesSettlementSource ToSeriesSettlementSource(this SeriesSettlementSourceDTO sourceDTO)
        {
            if (sourceDTO == null)
                throw new ArgumentNullException(nameof(sourceDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new SeriesSettlementSource
            {
                series_ticker = sourceDTO.series_ticker,
                name = sourceDTO.name,
                url = sourceDTO.url
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesSettlementSource", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of SeriesSettlementSource models to their corresponding DTO representations.
        /// </summary>
        /// <param name="sources">The collection of SeriesSettlementSource models to convert.</param>
        /// <returns>A list of SeriesSettlementSourceDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when sources is null.</exception>
        public static List<SeriesSettlementSourceDTO> ToSeriesSettlementSourceDTOs(this IEnumerable<SeriesSettlementSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var stopwatch = Stopwatch.StartNew();

            var result = sources.Select(s => s.ToSeriesSettlementSourceDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesSettlementSourceDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of SeriesSettlementSourceDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="sourceDTOs">The collection of SeriesSettlementSourceDTOs to convert.</param>
        /// <returns>A list of SeriesSettlementSource models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when sourceDTOs is null.</exception>
        public static List<SeriesSettlementSource> ToSeriesSettlementSources(this IEnumerable<SeriesSettlementSourceDTO> sourceDTOs)
        {
            if (sourceDTOs == null)
                throw new ArgumentNullException(nameof(sourceDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = sourceDTOs.Select(dto => dto.ToSeriesSettlementSource()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesSettlementSources", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a SeriesSettlementSource model to prevent unintended mutations.
        /// </summary>
        /// <param name="source">The SeriesSettlementSource model to clone.</param>
        /// <returns>A new SeriesSettlementSource instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
        public static SeriesSettlementSource DeepClone(this SeriesSettlementSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new SeriesSettlementSource
            {
                series_ticker = source.series_ticker,
                name = source.name,
                url = source.url
            };
        }

        /// <summary>
        /// Creates a deep clone of a SeriesSettlementSourceDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="sourceDTO">The SeriesSettlementSourceDTO to clone.</param>
        /// <returns>A new SeriesSettlementSourceDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when sourceDTO is null.</exception>
        public static SeriesSettlementSourceDTO DeepClone(this SeriesSettlementSourceDTO sourceDTO)
        {
            if (sourceDTO == null)
                throw new ArgumentNullException(nameof(sourceDTO));

            return new SeriesSettlementSourceDTO
            {
                series_ticker = sourceDTO.series_ticker,
                name = sourceDTO.name,
                url = sourceDTO.url
            };
        }
    }
}
