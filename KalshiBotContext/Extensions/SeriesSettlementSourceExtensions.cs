using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between SeriesSettlementSource model and SeriesSettlementSourceDTO,
    /// supporting settlement source data transfer for series validation and reference.
    /// </summary>
    public static class SeriesSettlementSourceExtensions
    {
        /// <summary>
        /// Converts a SeriesSettlementSource model to its DTO representation,
        /// mapping all settlement source properties for data transfer.
        /// </summary>
        /// <param name="source">The SeriesSettlementSource model to convert.</param>
        /// <returns>A new SeriesSettlementSourceDTO with all settlement source properties mapped.</returns>
        public static SeriesSettlementSourceDTO ToSeriesSettlementSourceDTO(this SeriesSettlementSource source)
        {
            return new SeriesSettlementSourceDTO
            {
                series_ticker = source.series_ticker,
                name = source.name,
                url = source.url
            };
        }

        /// <summary>
        /// Converts a SeriesSettlementSourceDTO to its model representation,
        /// creating a new SeriesSettlementSource with all properties mapped from the DTO.
        /// </summary>
        /// <param name="sourceDTO">The SeriesSettlementSourceDTO to convert.</param>
        /// <returns>A new SeriesSettlementSource model with all properties mapped from the DTO.</returns>
        public static SeriesSettlementSource ToSeriesSettlementSource(this SeriesSettlementSourceDTO sourceDTO)
        {
            return new SeriesSettlementSource
            {
                series_ticker = sourceDTO.series_ticker,
                name = sourceDTO.name,
                url = sourceDTO.url
            };
        }
    }
}
