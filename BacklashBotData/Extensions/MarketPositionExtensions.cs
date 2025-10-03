using BacklashDTOs.Data;
using BacklashBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between MarketPosition model and MarketPositionDTO,
    /// supporting trading position data transfer and portfolio management operations.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class MarketPositionExtensions
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
        /// Converts a MarketPosition model to its DTO representation,
        /// mapping all position-related properties for data transfer.
        /// </summary>
        /// <param name="marketPosition">The MarketPosition model to convert.</param>
        /// <returns>A new MarketPositionDTO with all position properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketPosition is null.</exception>
        public static MarketPositionDTO ToMarketPositionDTO(this MarketPosition marketPosition)
        {
            if (marketPosition == null)
                throw new ArgumentNullException(nameof(marketPosition));

            var stopwatch = Stopwatch.StartNew();

            var result = new MarketPositionDTO
            {
                Ticker = marketPosition.Ticker,
                TotalTraded = marketPosition.TotalTraded,
                Position = marketPosition.Position,
                MarketExposure = marketPosition.MarketExposure,
                RealizedPnl = marketPosition.RealizedPnl,
                RestingOrdersCount = marketPosition.RestingOrdersCount,
                FeesPaid = marketPosition.FeesPaid,
                LastUpdatedUTC = marketPosition.LastUpdatedUTC,
                LastModified = marketPosition.LastModified
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMarketPositionDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a MarketPositionDTO to its model representation,
        /// creating a new MarketPosition with all properties mapped from the DTO.
        /// </summary>
        /// <param name="marketPositionDTO">The MarketPositionDTO to convert.</param>
        /// <returns>A new MarketPosition model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketPositionDTO is null.</exception>
        public static MarketPosition ToMarketPosition(this MarketPositionDTO marketPositionDTO)
        {
            if (marketPositionDTO == null)
                throw new ArgumentNullException(nameof(marketPositionDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new MarketPosition
            {
                Ticker = marketPositionDTO.Ticker,
                TotalTraded = marketPositionDTO.TotalTraded,
                Position = marketPositionDTO.Position,
                MarketExposure = marketPositionDTO.MarketExposure,
                RealizedPnl = marketPositionDTO.RealizedPnl,
                RestingOrdersCount = marketPositionDTO.RestingOrdersCount,
                FeesPaid = marketPositionDTO.FeesPaid,
                LastUpdatedUTC = marketPositionDTO.LastUpdatedUTC,
                LastModified = marketPositionDTO.LastModified
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMarketPosition", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing MarketPosition model with data from a MarketPositionDTO,
        /// validating ticker match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="marketPosition">The MarketPosition model to update.</param>
        /// <param name="marketPositionDTO">The MarketPositionDTO containing updated data.</param>
        /// <returns>The updated MarketPosition model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketPosition or marketPositionDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when tickers do not match.</exception>
        public static MarketPosition UpdateMarketPosition(this MarketPosition marketPosition, MarketPositionDTO marketPositionDTO)
        {
            if (marketPosition == null)
                throw new ArgumentNullException(nameof(marketPosition));
            if (marketPositionDTO == null)
                throw new ArgumentNullException(nameof(marketPositionDTO));

            if (marketPosition.Ticker != marketPositionDTO.Ticker)
            {
                throw new ArgumentException("Tickers don't match for Update MarketPosition", nameof(marketPositionDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            marketPosition.TotalTraded = marketPositionDTO.TotalTraded;
            marketPosition.Position = marketPositionDTO.Position;
            marketPosition.MarketExposure = marketPositionDTO.MarketExposure;
            marketPosition.RealizedPnl = marketPositionDTO.RealizedPnl;
            marketPosition.RestingOrdersCount = marketPositionDTO.RestingOrdersCount;
            marketPosition.FeesPaid = marketPositionDTO.FeesPaid;
            marketPosition.LastUpdatedUTC = marketPositionDTO.LastUpdatedUTC;
            marketPosition.LastModified = DateTime.UtcNow;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateMarketPosition", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return marketPosition;
        }

        /// <summary>
        /// Converts a collection of MarketPosition models to their corresponding DTO representations.
        /// </summary>
        /// <param name="marketPositions">The collection of MarketPosition models to convert.</param>
        /// <returns>A list of MarketPositionDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketPositions is null.</exception>
        public static List<MarketPositionDTO> ToMarketPositionDTOs(this IEnumerable<MarketPosition> marketPositions)
        {
            if (marketPositions == null)
                throw new ArgumentNullException(nameof(marketPositions));

            var stopwatch = Stopwatch.StartNew();

            var result = marketPositions.Select(mp => mp.ToMarketPositionDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMarketPositionDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of MarketPositionDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="marketPositionDTOs">The collection of MarketPositionDTOs to convert.</param>
        /// <returns>A list of MarketPosition models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketPositionDTOs is null.</exception>
        public static List<MarketPosition> ToMarketPositions(this IEnumerable<MarketPositionDTO> marketPositionDTOs)
        {
            if (marketPositionDTOs == null)
                throw new ArgumentNullException(nameof(marketPositionDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = marketPositionDTOs.Select(dto => dto.ToMarketPosition()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMarketPositions", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a MarketPosition model to prevent unintended mutations.
        /// </summary>
        /// <param name="marketPosition">The MarketPosition model to clone.</param>
        /// <returns>A new MarketPosition instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketPosition is null.</exception>
        public static MarketPosition DeepClone(this MarketPosition marketPosition)
        {
            if (marketPosition == null)
                throw new ArgumentNullException(nameof(marketPosition));

            return new MarketPosition
            {
                Ticker = marketPosition.Ticker,
                TotalTraded = marketPosition.TotalTraded,
                Position = marketPosition.Position,
                MarketExposure = marketPosition.MarketExposure,
                RealizedPnl = marketPosition.RealizedPnl,
                RestingOrdersCount = marketPosition.RestingOrdersCount,
                FeesPaid = marketPosition.FeesPaid,
                LastUpdatedUTC = marketPosition.LastUpdatedUTC,
                LastModified = marketPosition.LastModified
            };
        }

        /// <summary>
        /// Creates a deep clone of a MarketPositionDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="marketPositionDTO">The MarketPositionDTO to clone.</param>
        /// <returns>A new MarketPositionDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketPositionDTO is null.</exception>
        public static MarketPositionDTO DeepClone(this MarketPositionDTO marketPositionDTO)
        {
            if (marketPositionDTO == null)
                throw new ArgumentNullException(nameof(marketPositionDTO));

            return new MarketPositionDTO
            {
                Ticker = marketPositionDTO.Ticker,
                TotalTraded = marketPositionDTO.TotalTraded,
                Position = marketPositionDTO.Position,
                MarketExposure = marketPositionDTO.MarketExposure,
                RealizedPnl = marketPositionDTO.RealizedPnl,
                RestingOrdersCount = marketPositionDTO.RestingOrdersCount,
                FeesPaid = marketPositionDTO.FeesPaid,
                LastUpdatedUTC = marketPositionDTO.LastUpdatedUTC,
                LastModified = marketPositionDTO.LastModified
            };
        }
    }
}
