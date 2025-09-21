using BacklashDTOs.Data;
using KalshiBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between SeriesTag model and SeriesTagDTO,
    /// supporting series categorization and tagging data transfer operations.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class SeriesTagExtensions
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
        /// Converts a SeriesTag model to its DTO representation,
        /// mapping all tag properties for data transfer.
        /// </summary>
        /// <param name="seriesTag">The SeriesTag model to convert.</param>
        /// <returns>A new SeriesTagDTO with all tag properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesTag is null.</exception>
        public static SeriesTagDTO ToSeriesTagDTO(this SeriesTag seriesTag)
        {
            if (seriesTag == null)
                throw new ArgumentNullException(nameof(seriesTag));

            var stopwatch = Stopwatch.StartNew();

            var result = new SeriesTagDTO
            {
                series_ticker = seriesTag.series_ticker,
                tag = seriesTag.tag
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesTagDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a SeriesTagDTO to its model representation,
        /// creating a new SeriesTag with all properties mapped from the DTO.
        /// </summary>
        /// <param name="seriesTagDTO">The SeriesTagDTO to convert.</param>
        /// <returns>A new SeriesTag model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesTagDTO is null.</exception>
        public static SeriesTag ToSeriesTag(this SeriesTagDTO seriesTagDTO)
        {
            if (seriesTagDTO == null)
                throw new ArgumentNullException(nameof(seriesTagDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new SeriesTag
            {
                series_ticker = seriesTagDTO.series_ticker,
                tag = seriesTagDTO.tag
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesTag", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of SeriesTag models to their corresponding DTO representations.
        /// </summary>
        /// <param name="seriesTags">The collection of SeriesTag models to convert.</param>
        /// <returns>A list of SeriesTagDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesTags is null.</exception>
        public static List<SeriesTagDTO> ToSeriesTagDTOs(this IEnumerable<SeriesTag> seriesTags)
        {
            if (seriesTags == null)
                throw new ArgumentNullException(nameof(seriesTags));

            var stopwatch = Stopwatch.StartNew();

            var result = seriesTags.Select(st => st.ToSeriesTagDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesTagDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of SeriesTagDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="seriesTagDTOs">The collection of SeriesTagDTOs to convert.</param>
        /// <returns>A list of SeriesTag models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesTagDTOs is null.</exception>
        public static List<SeriesTag> ToSeriesTags(this IEnumerable<SeriesTagDTO> seriesTagDTOs)
        {
            if (seriesTagDTOs == null)
                throw new ArgumentNullException(nameof(seriesTagDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = seriesTagDTOs.Select(dto => dto.ToSeriesTag()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesTags", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a SeriesTag model to prevent unintended mutations.
        /// </summary>
        /// <param name="seriesTag">The SeriesTag model to clone.</param>
        /// <returns>A new SeriesTag instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesTag is null.</exception>
        public static SeriesTag DeepClone(this SeriesTag seriesTag)
        {
            if (seriesTag == null)
                throw new ArgumentNullException(nameof(seriesTag));

            return new SeriesTag
            {
                series_ticker = seriesTag.series_ticker,
                tag = seriesTag.tag
            };
        }

        /// <summary>
        /// Creates a deep clone of a SeriesTagDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="seriesTagDTO">The SeriesTagDTO to clone.</param>
        /// <returns>A new SeriesTagDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesTagDTO is null.</exception>
        public static SeriesTagDTO DeepClone(this SeriesTagDTO seriesTagDTO)
        {
            if (seriesTagDTO == null)
                throw new ArgumentNullException(nameof(seriesTagDTO));

            return new SeriesTagDTO
            {
                series_ticker = seriesTagDTO.series_ticker,
                tag = seriesTagDTO.tag
            };
        }
    }
}
