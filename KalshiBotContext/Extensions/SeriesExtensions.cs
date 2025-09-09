using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class SeriesExtensions
    {
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

        public static Series ToSeries(this SeriesDTO seriesDTO)
        {
            List<SeriesTag> seriesTags = seriesDTO.Tags.Select(x => x.ToSeriesTag()).ToList();
            List<SeriesSettlementSource> settlementSources = seriesDTO.SettlementSources.Select(x => x.ToSeriesSettlementSource()).ToList();

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
            series.LastModifiedDate = DateTime.Now;

            return series;
        }
    }
}
