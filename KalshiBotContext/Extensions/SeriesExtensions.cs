using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Series model and SeriesDTO,
    /// including nested tags and settlement sources for comprehensive series data transfer.
    /// </summary>
    public static class SeriesExtensions
    {
        /// <summary>
        /// Converts a Series model to its DTO representation,
        /// including optional nested tags and settlement sources if they exist.
        /// </summary>
        /// <param name="series">The Series model to convert.</param>
        /// <returns>A new SeriesDTO with all series properties and nested collections mapped.</returns>
        public static SeriesDTO ToSeriesDTO(this Series series)
        {
            List<SeriesTagDTO>? seriesTags = null;
            if (series.Tags != null)
                seriesTags = series.Tags.Select(x => x.ToSeriesTagDTO()).ToList();
            List<SeriesSettlementSourceDTO>? settlementSources = null;
            if (series.SettlementSources != null)
                settlementSources = series.SettlementSources.Select(x => x.ToSeriesSettlementSourceDTO()).ToList();
            return new SeriesDTO
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
        }

        /// <summary>
        /// Converts a SeriesDTO to its model representation,
        /// creating a new Series with all properties and nested collections mapped from the DTO.
        /// </summary>
        /// <param name="seriesDTO">The SeriesDTO to convert.</param>
        /// <returns>A new Series model with all properties and nested collections mapped.</returns>
        public static Series ToSeries(this SeriesDTO seriesDTO)
        {
            List<SeriesTag> seriesTags = seriesDTO.Tags?.Select(x => x.ToSeriesTag()).ToList() ?? new List<SeriesTag>();
            List<SeriesSettlementSource> settlementSources = seriesDTO.SettlementSources?.Select(x => x.ToSeriesSettlementSource()).ToList() ?? new List<SeriesSettlementSource>();

            return new Series
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
        }

        /// <summary>
        /// Updates an existing Series model with data from a SeriesDTO,
        /// validating series ticker match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="series">The Series model to update.</param>
        /// <param name="seriesDTO">The SeriesDTO containing updated data.</param>
        /// <returns>The updated Series model.</returns>
        /// <exception cref="Exception">Thrown when series tickers do not match.</exception>
        public static Series UpdateSeries(this Series series, SeriesDTO seriesDTO)
        {
            if (series.series_ticker != seriesDTO.series_ticker)
            {
                throw new Exception("Series ticker doesn't match for Update Series");
            }

            series.frequency = seriesDTO.frequency;
            series.title = seriesDTO.title;
            series.category = seriesDTO.category;
            series.contract_url = seriesDTO.contract_url;
            series.CreatedDate = seriesDTO.CreatedDate;
            series.LastModifiedDate = DateTime.UtcNow;

            return series;
        }
    }
}
