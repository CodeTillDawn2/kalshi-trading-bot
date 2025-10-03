using BacklashDTOs.Data;
using BacklashBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Series model and SeriesDTO,
    /// including nested tags and settlement sources for comprehensive series data transfer.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class SeriesExtensions
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
        /// Converts a Series model to its DTO representation,
        /// including optional nested tags and settlement sources if they exist.
        /// </summary>
        /// <param name="series">The Series model to convert.</param>
        /// <returns>A new SeriesDTO with all series properties and nested collections mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when series is null.</exception>
        public static SeriesDTO ToSeriesDTO(this Series series)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));

            var stopwatch = Stopwatch.StartNew();

            List<SeriesTagDTO>? seriesTags = null;
            if (series.Tags != null)
                seriesTags = series.Tags.Select(x => x.ToSeriesTagDTO()).ToList();
            List<SeriesSettlementSourceDTO>? settlementSources = null;
            if (series.SettlementSources != null)
                settlementSources = series.SettlementSources.Select(x => x.ToSeriesSettlementSourceDTO()).ToList();

            var result = new SeriesDTO
            {
                series_ticker = series.series_ticker,
                frequency = series.frequency,
                title = series.title,
                category = series.category,
                contract_url = series.contract_url,
                CreatedDate = series.CreatedDate,
                LastModifiedDate = series.LastModifiedDate,
                Tags = seriesTags,
                SettlementSources = settlementSources
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a SeriesDTO to its model representation,
        /// creating a new Series with all properties and nested collections mapped from the DTO.
        /// </summary>
        /// <param name="seriesDTO">The SeriesDTO to convert.</param>
        /// <returns>A new Series model with all properties and nested collections mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesDTO is null.</exception>
        public static Series ToSeries(this SeriesDTO seriesDTO)
        {
            if (seriesDTO == null)
                throw new ArgumentNullException(nameof(seriesDTO));

            var stopwatch = Stopwatch.StartNew();

            List<SeriesTag> seriesTags = seriesDTO.Tags?.Select(x => x.ToSeriesTag()).ToList() ?? new List<SeriesTag>();
            List<SeriesSettlementSource> settlementSources = seriesDTO.SettlementSources?.Select(x => x.ToSeriesSettlementSource()).ToList() ?? new List<SeriesSettlementSource>();

            var result = new Series
            {
                series_ticker = seriesDTO.series_ticker,
                frequency = seriesDTO.frequency,
                title = seriesDTO.title,
                category = seriesDTO.category,
                contract_url = seriesDTO.contract_url,
                CreatedDate = seriesDTO.CreatedDate,
                LastModifiedDate = seriesDTO.LastModifiedDate,
                Tags = seriesTags,
                SettlementSources = settlementSources
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeries", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing Series model with data from a SeriesDTO,
        /// validating series ticker match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="series">The Series model to update.</param>
        /// <param name="seriesDTO">The SeriesDTO containing updated data.</param>
        /// <returns>The updated Series model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when series or seriesDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when series tickers do not match.</exception>
        public static Series UpdateSeries(this Series series, SeriesDTO seriesDTO)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));
            if (seriesDTO == null)
                throw new ArgumentNullException(nameof(seriesDTO));

            if (series.series_ticker != seriesDTO.series_ticker)
            {
                throw new ArgumentException("Series ticker doesn't match for Update Series", nameof(seriesDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            series.frequency = seriesDTO.frequency;
            series.title = seriesDTO.title;
            series.category = seriesDTO.category;
            series.contract_url = seriesDTO.contract_url;
            series.CreatedDate = seriesDTO.CreatedDate;
            series.LastModifiedDate = DateTime.UtcNow;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateSeries", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return series;
        }

        /// <summary>
        /// Converts a collection of Series models to their corresponding DTO representations.
        /// </summary>
        /// <param name="series">The collection of Series models to convert.</param>
        /// <returns>A list of SeriesDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when series is null.</exception>
        public static List<SeriesDTO> ToSeriesDTOs(this IEnumerable<Series> series)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));

            var stopwatch = Stopwatch.StartNew();

            var result = series.Select(s => s.ToSeriesDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of SeriesDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="seriesDTOs">The collection of SeriesDTOs to convert.</param>
        /// <returns>A list of Series models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesDTOs is null.</exception>
        public static List<Series> ToSeriesList(this IEnumerable<SeriesDTO> seriesDTOs)
        {
            if (seriesDTOs == null)
                throw new ArgumentNullException(nameof(seriesDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = seriesDTOs.Select(dto => dto.ToSeries()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToSeriesList", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a Series model to prevent unintended mutations.
        /// </summary>
        /// <param name="series">The Series model to clone.</param>
        /// <returns>A new Series instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when series is null.</exception>
        public static Series DeepClone(this Series series)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));

            return new Series
            {
                series_ticker = series.series_ticker,
                frequency = series.frequency,
                title = series.title,
                category = series.category,
                contract_url = series.contract_url,
                CreatedDate = series.CreatedDate,
                LastModifiedDate = series.LastModifiedDate,
                Tags = series.Tags?.Select(t => new SeriesTag
                {
                    series_ticker = t.series_ticker,
                    tag = t.tag
                }).ToList() ?? new List<SeriesTag>(),
                SettlementSources = series.SettlementSources?.Select(ss => new SeriesSettlementSource
                {
                    series_ticker = ss.series_ticker,
                    name = ss.name,
                    url = ss.url
                }).ToList() ?? new List<SeriesSettlementSource>()
            };
        }

        /// <summary>
        /// Creates a deep clone of a SeriesDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="seriesDTO">The SeriesDTO to clone.</param>
        /// <returns>A new SeriesDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesDTO is null.</exception>
        public static SeriesDTO DeepClone(this SeriesDTO seriesDTO)
        {
            if (seriesDTO == null)
                throw new ArgumentNullException(nameof(seriesDTO));

            return new SeriesDTO
            {
                series_ticker = seriesDTO.series_ticker,
                frequency = seriesDTO.frequency,
                title = seriesDTO.title,
                category = seriesDTO.category,
                contract_url = seriesDTO.contract_url,
                CreatedDate = seriesDTO.CreatedDate,
                LastModifiedDate = seriesDTO.LastModifiedDate,
                Tags = seriesDTO.Tags?.Select(t => new SeriesTagDTO
                {
                    series_ticker = t.series_ticker,
                    tag = t.tag
                }).ToList() ?? new List<SeriesTagDTO>(),
                SettlementSources = seriesDTO.SettlementSources?.Select(ss => new SeriesSettlementSourceDTO
                {
                    series_ticker = ss.series_ticker,
                    name = ss.name,
                    url = ss.url
                }).ToList() ?? new List<SeriesSettlementSourceDTO>()
            };
        }
    }
}
