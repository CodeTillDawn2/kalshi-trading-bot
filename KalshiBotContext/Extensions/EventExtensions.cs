using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Event model and EventDTO,
    /// handling event data transfer including associated series information.
    /// </summary>
    public static class EventExtensions
    {
        /// <summary>
        /// Converts an Event model to its DTO representation,
        /// including optional series data if the event has an associated series.
        /// </summary>
        /// <param name="eventModel">The Event model to convert.</param>
        /// <returns>A new EventDTO with all event properties and associated series data mapped.</returns>
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

        /// <summary>
        /// Converts an EventDTO to its model representation,
        /// creating a new Event with all properties mapped from the DTO.
        /// </summary>
        /// <param name="eventDTO">The EventDTO to convert.</param>
        /// <returns>A new Event model with all properties mapped from the DTO.</returns>
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

        /// <summary>
        /// Updates an existing Event model with data from an EventDTO,
        /// validating event ticker match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="eventModel">The Event model to update.</param>
        /// <param name="eventDTO">The EventDTO containing updated data.</param>
        /// <returns>The updated Event model.</returns>
        /// <exception cref="Exception">Thrown when event tickers do not match.</exception>
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
            eventModel.LastModifiedDate = DateTime.UtcNow;
            return eventModel;
        }
    }
}
