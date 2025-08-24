using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class SeriesSettlementSourceExtensions
    {
        public static SeriesSettlementSourceDTO ToSeriesSettlementSourceDTO(this SeriesSettlementSource source)
        {
            return new SeriesSettlementSourceDTO
            {
                series_ticker = source.series_ticker,
                name = source.name,
                url = source.url
            };
        }

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