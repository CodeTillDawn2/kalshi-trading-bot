using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class EventExtensions
    {
        public static EventDTO ToEventDTO(this Event eventModel)
        {

            SeriesDTO? series = null;
            if (eventModel.Series != null)
                series = eventModel.Series.ToSeriesDTO();

            return new EventDTO
            {
                event_ticker = eventModel.event_ticker,
                series_ticker = eventModel.series_ticker,
                title = eventModel.title,
                sub_title = eventModel.sub_title,
                collateral_return_type = eventModel.collateral_return_type,
                mutually_exclusive = eventModel.mutually_exclusive,
                category = eventModel.category,
                CreatedDate = eventModel.CreatedDate,
                LastModifiedDate = eventModel.LastModifiedDate,
                Series = series
            };
        }

        public static Event ToEvent(this EventDTO eventDTO)
        {
            return new Event
            {
                event_ticker = eventDTO.event_ticker,
                series_ticker = eventDTO.series_ticker,
                title = eventDTO.title,
                sub_title = eventDTO.sub_title,
                collateral_return_type = eventDTO.collateral_return_type,
                mutually_exclusive = eventDTO.mutually_exclusive,
                category = eventDTO.category,
                CreatedDate = eventDTO.CreatedDate,
                LastModifiedDate = eventDTO.LastModifiedDate
            };
        }

        public static Event UpdateEvent(this Event eventModel, EventDTO eventDTO)
        {
            if (eventModel.event_ticker != eventDTO.event_ticker)
            {
                throw new Exception("Tickers don't match for Update Event");
            }
            eventModel.series_ticker = eventDTO.series_ticker;
            eventModel.title = eventDTO.title;
            eventModel.sub_title = eventDTO.sub_title;
            eventModel.collateral_return_type = eventDTO.collateral_return_type;
            eventModel.mutually_exclusive = eventDTO.mutually_exclusive;
            eventModel.category = eventDTO.category;
            eventModel.CreatedDate = eventDTO.CreatedDate;
            eventModel.LastModifiedDate = DateTime.Now;
            return eventModel;
        }
    }
}