using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class SeriesTagExtensions
    {
        public static SeriesTagDTO ToSeriesTagDTO(this SeriesTag seriesTag)
        {
            return new SeriesTagDTO
            {
                series_ticker = seriesTag.series_ticker,
                tag = seriesTag.tag
            };
        }

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