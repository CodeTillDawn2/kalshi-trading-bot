using BacklashDTOs.Data;
using KalshiBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between MarketWatch model and MarketWatchDTO,
    /// supporting market monitoring and interest scoring data transfer operations.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class MarketWatchExtensions
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
        /// Converts a MarketWatch model to its DTO representation,
        /// mapping all market watch properties for data transfer.
        /// </summary>
        /// <param name="marketWatch">The MarketWatch model to convert.</param>
        /// <returns>A new MarketWatchDTO with all market watch properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketWatch is null.</exception>
        public static MarketWatchDTO ToMarketWatchDTO(this MarketWatch marketWatch)
        {
            if (marketWatch == null)
                throw new ArgumentNullException(nameof(marketWatch));

            var stopwatch = Stopwatch.StartNew();

            var result = new MarketWatchDTO
            {
                market_ticker = marketWatch.market_ticker,
                BrainLock = marketWatch.BrainLock,
                InterestScore = marketWatch.InterestScore,
                InterestScoreDate = marketWatch.InterestScoreDate,
                LastWatched = marketWatch.LastWatched,
                AverageWebsocketEventsPerMinute = marketWatch.AverageWebsocketEventsPerMinute
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMarketWatchDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a MarketWatchDTO to its model representation,
        /// creating a new MarketWatch with all properties mapped from the DTO.
        /// </summary>
        /// <param name="marketWatchDTO">The MarketWatchDTO to convert.</param>
        /// <returns>A new MarketWatch model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketWatchDTO is null.</exception>
        public static MarketWatch ToMarketWatch(this MarketWatchDTO marketWatchDTO)
        {
            if (marketWatchDTO == null)
                throw new ArgumentNullException(nameof(marketWatchDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new MarketWatch
            {
                market_ticker = marketWatchDTO.market_ticker,
                BrainLock = marketWatchDTO.BrainLock,
                InterestScore = marketWatchDTO.InterestScore,
                InterestScoreDate = marketWatchDTO.InterestScoreDate,
                LastWatched = marketWatchDTO.LastWatched,
                AverageWebsocketEventsPerMinute = marketWatchDTO.AverageWebsocketEventsPerMinute
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMarketWatch", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing MarketWatch model with data from a MarketWatchDTO,
        /// validating market ticker match before applying selective property updates.
        /// </summary>
        /// <param name="marketWatch">The MarketWatch model to update.</param>
        /// <param name="marketWatchDTO">The MarketWatchDTO containing updated data.</param>
        /// <returns>The updated MarketWatch model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketWatch or marketWatchDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when market tickers do not match.</exception>
        public static MarketWatch UpdateMarketWatch(this MarketWatch marketWatch, MarketWatchDTO marketWatchDTO)
        {
            if (marketWatch == null)
                throw new ArgumentNullException(nameof(marketWatch));
            if (marketWatchDTO == null)
                throw new ArgumentNullException(nameof(marketWatchDTO));

            if (marketWatch.market_ticker != marketWatchDTO.market_ticker)
            {
                throw new ArgumentException("Market ticker doesn't match for Update MarketWatch", nameof(marketWatchDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            marketWatch.BrainLock = marketWatchDTO.BrainLock;
            if (marketWatchDTO.InterestScore != null) marketWatch.InterestScore = marketWatchDTO.InterestScore;
            if (marketWatchDTO.InterestScoreDate != null) marketWatch.InterestScoreDate = marketWatchDTO.InterestScoreDate;
            if (marketWatchDTO.LastWatched != null) marketWatch.LastWatched = marketWatchDTO.LastWatched;
            marketWatch.AverageWebsocketEventsPerMinute = marketWatchDTO.AverageWebsocketEventsPerMinute;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateMarketWatch", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return marketWatch;
        }

        /// <summary>
        /// Converts a collection of MarketWatch models to their corresponding DTO representations.
        /// </summary>
        /// <param name="marketWatches">The collection of MarketWatch models to convert.</param>
        /// <returns>A list of MarketWatchDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketWatches is null.</exception>
        public static List<MarketWatchDTO> ToMarketWatchDTOs(this IEnumerable<MarketWatch> marketWatches)
        {
            if (marketWatches == null)
                throw new ArgumentNullException(nameof(marketWatches));

            var stopwatch = Stopwatch.StartNew();

            var result = marketWatches.Select(mw => mw.ToMarketWatchDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMarketWatchDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of MarketWatchDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="marketWatchDTOs">The collection of MarketWatchDTOs to convert.</param>
        /// <returns>A list of MarketWatch models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketWatchDTOs is null.</exception>
        public static List<MarketWatch> ToMarketWatches(this IEnumerable<MarketWatchDTO> marketWatchDTOs)
        {
            if (marketWatchDTOs == null)
                throw new ArgumentNullException(nameof(marketWatchDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = marketWatchDTOs.Select(dto => dto.ToMarketWatch()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMarketWatches", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a MarketWatch model to prevent unintended mutations.
        /// </summary>
        /// <param name="marketWatch">The MarketWatch model to clone.</param>
        /// <returns>A new MarketWatch instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketWatch is null.</exception>
        public static MarketWatch DeepClone(this MarketWatch marketWatch)
        {
            if (marketWatch == null)
                throw new ArgumentNullException(nameof(marketWatch));

            return new MarketWatch
            {
                market_ticker = marketWatch.market_ticker,
                BrainLock = marketWatch.BrainLock,
                InterestScore = marketWatch.InterestScore,
                InterestScoreDate = marketWatch.InterestScoreDate,
                LastWatched = marketWatch.LastWatched,
                AverageWebsocketEventsPerMinute = marketWatch.AverageWebsocketEventsPerMinute
            };
        }

        /// <summary>
        /// Creates a deep clone of a MarketWatchDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="marketWatchDTO">The MarketWatchDTO to clone.</param>
        /// <returns>A new MarketWatchDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketWatchDTO is null.</exception>
        public static MarketWatchDTO DeepClone(this MarketWatchDTO marketWatchDTO)
        {
            if (marketWatchDTO == null)
                throw new ArgumentNullException(nameof(marketWatchDTO));

            return new MarketWatchDTO
            {
                market_ticker = marketWatchDTO.market_ticker,
                BrainLock = marketWatchDTO.BrainLock,
                InterestScore = marketWatchDTO.InterestScore,
                InterestScoreDate = marketWatchDTO.InterestScoreDate,
                LastWatched = marketWatchDTO.LastWatched,
                AverageWebsocketEventsPerMinute = marketWatchDTO.AverageWebsocketEventsPerMinute
            };
        }
    }
}
