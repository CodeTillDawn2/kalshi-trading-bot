using KalshiBotData.Models;
using BacklashDTOs.Data;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Event model and EventDTO,
    /// handling event data transfer including associated series information.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class EventExtensions
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
        /// Converts an Event model to its DTO representation,
        /// including optional series data if the event has an associated series.
        /// </summary>
        /// <param name="eventModel">The Event model to convert.</param>
        /// <returns>A new EventDTO with all event properties and associated series data mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when eventModel is null.</exception>
        public static EventDTO ToEventDTO(this Event eventModel)
        {
            if (eventModel == null)
                throw new ArgumentNullException(nameof(eventModel));

            var stopwatch = Stopwatch.StartNew();

            SeriesDTO? series = null;
            if (eventModel.Series != null)
                series = eventModel.Series.ToSeriesDTO();

            var result = new EventDTO
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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToEventDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts an EventDTO to its model representation,
        /// creating a new Event with all properties mapped from the DTO.
        /// </summary>
        /// <param name="eventDTO">The EventDTO to convert.</param>
        /// <returns>A new Event model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when eventDTO is null.</exception>
        public static Event ToEvent(this EventDTO eventDTO)
        {
            if (eventDTO == null)
                throw new ArgumentNullException(nameof(eventDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new Event
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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToEvent", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing Event model with data from an EventDTO,
        /// validating event ticker match before applying changes and updating the modification timestamp.
        /// Only updates mutable fields; CreatedDate is preserved from the original model.
        /// </summary>
        /// <param name="eventModel">The Event model to update.</param>
        /// <param name="eventDTO">The EventDTO containing updated data.</param>
        /// <returns>The updated Event model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when eventModel or eventDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when event tickers do not match.</exception>
        public static Event UpdateEvent(this Event eventModel, EventDTO eventDTO)
        {
            if (eventModel == null)
                throw new ArgumentNullException(nameof(eventModel));
            if (eventDTO == null)
                throw new ArgumentNullException(nameof(eventDTO));

            if (eventModel.event_ticker != eventDTO.event_ticker)
            {
                throw new ArgumentException("Tickers don't match for Update Event", nameof(eventDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            eventModel.series_ticker = eventDTO.series_ticker;
            eventModel.title = eventDTO.title;
            eventModel.sub_title = eventDTO.sub_title;
            eventModel.collateral_return_type = eventDTO.collateral_return_type;
            eventModel.mutually_exclusive = eventDTO.mutually_exclusive;
            eventModel.category = eventDTO.category;
            // CreatedDate is not updated to preserve original creation time
            eventModel.LastModifiedDate = ExtensionConfiguration.TimestampProvider();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateEvent", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return eventModel;
        }

        /// <summary>
        /// Converts a collection of Event models to their corresponding DTO representations.
        /// </summary>
        /// <param name="events">The collection of Event models to convert.</param>
        /// <returns>A list of EventDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when events is null.</exception>
        public static List<EventDTO> ToEventDTOs(this IEnumerable<Event> events)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            var stopwatch = Stopwatch.StartNew();

            var result = events.Select(e => e.ToEventDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToEventDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of EventDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="eventDTOs">The collection of EventDTOs to convert.</param>
        /// <returns>A list of Event models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when eventDTOs is null.</exception>
        public static List<Event> ToEvents(this IEnumerable<EventDTO> eventDTOs)
        {
            if (eventDTOs == null)
                throw new ArgumentNullException(nameof(eventDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = eventDTOs.Select(dto => dto.ToEvent()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToEvents", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of an Event model to prevent unintended mutations.
        /// </summary>
        /// <param name="eventModel">The Event model to clone.</param>
        /// <returns>A new Event instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when eventModel is null.</exception>
        public static Event DeepClone(this Event eventModel)
        {
            if (eventModel == null)
                throw new ArgumentNullException(nameof(eventModel));

            return new Event
            {
                event_ticker = eventModel.event_ticker,
                series_ticker = eventModel.series_ticker,
                title = eventModel.title,
                sub_title = eventModel.sub_title,
                collateral_return_type = eventModel.collateral_return_type,
                mutually_exclusive = eventModel.mutually_exclusive,
                category = eventModel.category,
                CreatedDate = eventModel.CreatedDate,
                LastModifiedDate = eventModel.LastModifiedDate
            };
        }

        /// <summary>
        /// Creates a deep clone of an EventDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="eventDTO">The EventDTO to clone.</param>
        /// <returns>A new EventDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when eventDTO is null.</exception>
        public static EventDTO DeepClone(this EventDTO eventDTO)
        {
            if (eventDTO == null)
                throw new ArgumentNullException(nameof(eventDTO));

            return new EventDTO
            {
                event_ticker = eventDTO.event_ticker,
                series_ticker = eventDTO.series_ticker,
                title = eventDTO.title,
                sub_title = eventDTO.sub_title,
                collateral_return_type = eventDTO.collateral_return_type,
                mutually_exclusive = eventDTO.mutually_exclusive,
                category = eventDTO.category,
                CreatedDate = eventDTO.CreatedDate,
                LastModifiedDate = eventDTO.LastModifiedDate,
                Series = eventDTO.Series?.DeepClone()
            };
        }
    }
}
