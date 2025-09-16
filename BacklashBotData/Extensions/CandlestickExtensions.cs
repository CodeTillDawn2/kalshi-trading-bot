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
    /// Provides extension methods for converting between Candlestick model and CandlestickDTO,
    /// supporting market data analysis and historical price information transfer.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class CandlestickExtensions
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
        /// Converts a Candlestick model to its DTO representation,
        /// mapping all candlestick data including market information, time periods, and price data.
        /// </summary>
        /// <param name="candlestick">The Candlestick model to convert.</param>
        /// <returns>A new CandlestickDTO with all candlestick properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when candlestick is null.</exception>
        public static CandlestickDTO ToCandlestickDTO(this Candlestick candlestick)
        {
            if (candlestick == null)
                throw new ArgumentNullException(nameof(candlestick));

            var stopwatch = Stopwatch.StartNew();

            var result = new CandlestickDTO
            {
                market_ticker = candlestick.market_ticker,
                interval_type = candlestick.interval_type,
                end_period_ts = candlestick.end_period_ts,
                end_period_datetime_utc = candlestick.end_period_datetime_utc,
                year = candlestick.year,
                month = candlestick.month,
                day = candlestick.day,
                hour = candlestick.hour,
                minute = candlestick.minute,
                open_interest = candlestick.open_interest,
                price_close = candlestick.price_close,
                price_high = candlestick.price_high,
                price_low = candlestick.price_low,
                price_mean = candlestick.price_mean,
                price_open = candlestick.price_open,
                price_previous = candlestick.price_previous,
                volume = candlestick.volume,
                yes_ask_close = candlestick.yes_ask_close,
                yes_ask_high = candlestick.yes_ask_high,
                yes_ask_low = candlestick.yes_ask_low,
                yes_ask_open = candlestick.yes_ask_open,
                yes_bid_close = candlestick.yes_bid_close,
                yes_bid_high = candlestick.yes_bid_high,
                yes_bid_low = candlestick.yes_bid_low,
                yes_bid_open = candlestick.yes_bid_open
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToCandlestickDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a CandlestickDTO to its model representation,
        /// creating a new Candlestick with all properties mapped from the DTO.
        /// </summary>
        /// <param name="candlestickDTO">The CandlestickDTO to convert.</param>
        /// <returns>A new Candlestick model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when candlestickDTO is null.</exception>
        public static Candlestick ToCandlestick(this CandlestickDTO candlestickDTO)
        {
            if (candlestickDTO == null)
                throw new ArgumentNullException(nameof(candlestickDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new Candlestick
            {
                market_ticker = candlestickDTO.market_ticker,
                interval_type = candlestickDTO.interval_type,
                end_period_ts = candlestickDTO.end_period_ts,
                end_period_datetime_utc = candlestickDTO.end_period_datetime_utc,
                year = candlestickDTO.year,
                month = candlestickDTO.month,
                day = candlestickDTO.day,
                hour = candlestickDTO.hour,
                minute = candlestickDTO.minute,
                open_interest = candlestickDTO.open_interest,
                price_close = candlestickDTO.price_close,
                price_high = candlestickDTO.price_high,
                price_low = candlestickDTO.price_low,
                price_mean = candlestickDTO.price_mean,
                price_open = candlestickDTO.price_open,
                price_previous = candlestickDTO.price_previous,
                volume = candlestickDTO.volume,
                yes_ask_close = candlestickDTO.yes_ask_close,
                yes_ask_high = candlestickDTO.yes_ask_high,
                yes_ask_low = candlestickDTO.yes_ask_low,
                yes_ask_open = candlestickDTO.yes_ask_open,
                yes_bid_close = candlestickDTO.yes_bid_close,
                yes_bid_high = candlestickDTO.yes_bid_high,
                yes_bid_low = candlestickDTO.yes_bid_low,
                yes_bid_open = candlestickDTO.yes_bid_open
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToCandlestick", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing Candlestick model with data from a CandlestickDTO,
        /// validating market ticker, interval type, and timestamp match before updating price and volume data.
        /// </summary>
        /// <param name="candlestick">The Candlestick model to update.</param>
        /// <param name="candlestickDTO">The CandlestickDTO containing updated data.</param>
        /// <returns>The updated Candlestick model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when candlestick or candlestickDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when market identifiers or timestamps do not match.</exception>
        public static Candlestick UpdateCandlestick(this Candlestick candlestick, CandlestickDTO candlestickDTO)
        {
            if (candlestick == null)
                throw new ArgumentNullException(nameof(candlestick));
            if (candlestickDTO == null)
                throw new ArgumentNullException(nameof(candlestickDTO));

            if (candlestick.market_ticker != candlestickDTO.market_ticker || candlestick.end_period_ts != candlestickDTO.end_period_ts
                || candlestick.interval_type != candlestickDTO.interval_type)
            {
                throw new ArgumentException("Market ticker, interval type or end period timestamp don't match for Update Candlestick", nameof(candlestickDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            candlestick.open_interest = candlestickDTO.open_interest;
            candlestick.price_close = candlestickDTO.price_close;
            candlestick.price_high = candlestickDTO.price_high;
            candlestick.price_low = candlestickDTO.price_low;
            candlestick.price_mean = candlestickDTO.price_mean;
            candlestick.price_open = candlestickDTO.price_open;
            candlestick.price_previous = candlestickDTO.price_previous;
            candlestick.volume = candlestickDTO.volume;
            candlestick.yes_ask_close = candlestickDTO.yes_ask_close;
            candlestick.yes_ask_high = candlestickDTO.yes_ask_high;
            candlestick.yes_ask_low = candlestickDTO.yes_ask_low;
            candlestick.yes_ask_open = candlestickDTO.yes_ask_open;
            candlestick.yes_bid_close = candlestickDTO.yes_bid_close;
            candlestick.yes_bid_high = candlestickDTO.yes_bid_high;
            candlestick.yes_bid_low = candlestickDTO.yes_bid_low;
            candlestick.yes_bid_open = candlestickDTO.yes_bid_open;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateCandlestick", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return candlestick;
        }

        /// <summary>
        /// Converts a collection of Candlestick models to their corresponding DTO representations.
        /// </summary>
        /// <param name="candlesticks">The collection of Candlestick models to convert.</param>
        /// <returns>A list of CandlestickDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when candlesticks is null.</exception>
        public static List<CandlestickDTO> ToCandlestickDTOs(this IEnumerable<Candlestick> candlesticks)
        {
            if (candlesticks == null)
                throw new ArgumentNullException(nameof(candlesticks));

            var stopwatch = Stopwatch.StartNew();

            var result = candlesticks.Select(c => c.ToCandlestickDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToCandlestickDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of CandlestickDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="candlestickDTOs">The collection of CandlestickDTOs to convert.</param>
        /// <returns>A list of Candlestick models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when candlestickDTOs is null.</exception>
        public static List<Candlestick> ToCandlesticks(this IEnumerable<CandlestickDTO> candlestickDTOs)
        {
            if (candlestickDTOs == null)
                throw new ArgumentNullException(nameof(candlestickDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = candlestickDTOs.Select(dto => dto.ToCandlestick()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToCandlesticks", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a Candlestick model to prevent unintended mutations.
        /// </summary>
        /// <param name="candlestick">The Candlestick model to clone.</param>
        /// <returns>A new Candlestick instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when candlestick is null.</exception>
        public static Candlestick DeepClone(this Candlestick candlestick)
        {
            if (candlestick == null)
                throw new ArgumentNullException(nameof(candlestick));

            return new Candlestick
            {
                market_ticker = candlestick.market_ticker,
                interval_type = candlestick.interval_type,
                end_period_ts = candlestick.end_period_ts,
                end_period_datetime_utc = candlestick.end_period_datetime_utc,
                year = candlestick.year,
                month = candlestick.month,
                day = candlestick.day,
                hour = candlestick.hour,
                minute = candlestick.minute,
                open_interest = candlestick.open_interest,
                price_close = candlestick.price_close,
                price_high = candlestick.price_high,
                price_low = candlestick.price_low,
                price_mean = candlestick.price_mean,
                price_open = candlestick.price_open,
                price_previous = candlestick.price_previous,
                volume = candlestick.volume,
                yes_ask_close = candlestick.yes_ask_close,
                yes_ask_high = candlestick.yes_ask_high,
                yes_ask_low = candlestick.yes_ask_low,
                yes_ask_open = candlestick.yes_ask_open,
                yes_bid_close = candlestick.yes_bid_close,
                yes_bid_high = candlestick.yes_bid_high,
                yes_bid_low = candlestick.yes_bid_low,
                yes_bid_open = candlestick.yes_bid_open
            };
        }

        /// <summary>
        /// Creates a deep clone of a CandlestickDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="candlestickDTO">The CandlestickDTO to clone.</param>
        /// <returns>A new CandlestickDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when candlestickDTO is null.</exception>
        public static CandlestickDTO DeepClone(this CandlestickDTO candlestickDTO)
        {
            if (candlestickDTO == null)
                throw new ArgumentNullException(nameof(candlestickDTO));

            return new CandlestickDTO
            {
                market_ticker = candlestickDTO.market_ticker,
                interval_type = candlestickDTO.interval_type,
                end_period_ts = candlestickDTO.end_period_ts,
                end_period_datetime_utc = candlestickDTO.end_period_datetime_utc,
                year = candlestickDTO.year,
                month = candlestickDTO.month,
                day = candlestickDTO.day,
                hour = candlestickDTO.hour,
                minute = candlestickDTO.minute,
                open_interest = candlestickDTO.open_interest,
                price_close = candlestickDTO.price_close,
                price_high = candlestickDTO.price_high,
                price_low = candlestickDTO.price_low,
                price_mean = candlestickDTO.price_mean,
                price_open = candlestickDTO.price_open,
                price_previous = candlestickDTO.price_previous,
                volume = candlestickDTO.volume,
                yes_ask_close = candlestickDTO.yes_ask_close,
                yes_ask_high = candlestickDTO.yes_ask_high,
                yes_ask_low = candlestickDTO.yes_ask_low,
                yes_ask_open = candlestickDTO.yes_ask_open,
                yes_bid_close = candlestickDTO.yes_bid_close,
                yes_bid_high = candlestickDTO.yes_bid_high,
                yes_bid_low = candlestickDTO.yes_bid_low,
                yes_bid_open = candlestickDTO.yes_bid_open
            };
        }
    }
}
