using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between SeriesTag model and SeriesTagDTO,
    /// supporting series categorization and tagging data transfer operations.
    /// </summary>
    public static class SeriesTagExtensions
    {
        /// <summary>
        /// Converts a SeriesTag model to its DTO representation,
        /// mapping all tag properties for data transfer.
        /// </summary>
        /// <param name="seriesTag">The SeriesTag model to convert.</param>
        /// <returns>A new SeriesTagDTO with all tag properties mapped.</returns>
        public static SeriesTagDTO ToSeriesTagDTO(this SeriesTag seriesTag)
        {
            return new SeriesTagDTO
            {
                series_ticker = seriesTag.series_ticker,
                tag = seriesTag.tag
            };
        }

        /// <summary>
        /// Converts a SeriesTagDTO to its model representation,
        /// creating a new SeriesTag with all properties mapped from the DTO.
        /// </summary>
        /// <param name="seriesTagDTO">The SeriesTagDTO to convert.</param>
        /// <returns>A new SeriesTag model with all properties mapped from the DTO.</returns>
        public static SeriesTag ToSeriesTag(this SeriesTagDTO seriesTagDTO)
        {
            return new SeriesTag
            {
                series_ticker = seriesTagDTO.series_ticker,
                tag = seriesTagDTO.tag
            };
        }
    }
}
