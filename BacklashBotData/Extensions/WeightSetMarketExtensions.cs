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
    /// Provides extension methods for converting between WeightSetMarket model and WeightSetMarketDTO,
    /// supporting market-specific performance tracking within weight set configurations.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class WeightSetMarketExtensions
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
        /// Converts a WeightSetMarket model to its DTO representation,
        /// mapping all market-specific weight set properties for data transfer.
        /// </summary>
        /// <param name="market">The WeightSetMarket model to convert.</param>
        /// <returns>A new WeightSetMarketDTO with all market weight set properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when market is null.</exception>
        public static WeightSetMarketDTO ToWeightSetMarketDTO(this WeightSetMarket market)
        {
            if (market == null)
                throw new ArgumentNullException(nameof(market));

            var stopwatch = Stopwatch.StartNew();

            var result = new WeightSetMarketDTO
            {
                WeightSetID = market.WeightSetID,
                MarketTicker = market.MarketTicker,
                PnL = market.PnL,
                LastRun = market.LastRun
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToWeightSetMarketDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a WeightSetMarketDTO to its model representation,
        /// creating a new WeightSetMarket with all properties mapped from the DTO.
        /// </summary>
        /// <param name="weightSetMarketDTO">The WeightSetMarketDTO to convert.</param>
        /// <returns>A new WeightSetMarket model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSetMarketDTO is null.</exception>
        public static WeightSetMarket ToWeightSetMarket(this WeightSetMarketDTO weightSetMarketDTO)
        {
            if (weightSetMarketDTO == null)
                throw new ArgumentNullException(nameof(weightSetMarketDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new WeightSetMarket
            {
                WeightSetID = weightSetMarketDTO.WeightSetID,
                MarketTicker = weightSetMarketDTO.MarketTicker,
                PnL = weightSetMarketDTO.PnL,
                LastRun = weightSetMarketDTO.LastRun
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToWeightSetMarket", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing WeightSetMarket model with data from a WeightSetMarketDTO,
        /// applying performance and timing updates for market tracking.
        /// </summary>
        /// <param name="market">The WeightSetMarket model to update.</param>
        /// <param name="weightSetMarketDTO">The WeightSetMarketDTO containing updated data.</param>
        /// <returns>The updated WeightSetMarket model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when market or weightSetMarketDTO is null.</exception>
        public static WeightSetMarket UpdateWeightSetMarket(this WeightSetMarket market, WeightSetMarketDTO weightSetMarketDTO)
        {
            if (market == null)
                throw new ArgumentNullException(nameof(market));
            if (weightSetMarketDTO == null)
                throw new ArgumentNullException(nameof(weightSetMarketDTO));

            var stopwatch = Stopwatch.StartNew();

            market.PnL = weightSetMarketDTO.PnL;
            market.LastRun = weightSetMarketDTO.LastRun;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateWeightSetMarket", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return market;
        }

        /// <summary>
        /// Converts a collection of WeightSetMarket models to their corresponding DTO representations.
        /// </summary>
        /// <param name="markets">The collection of WeightSetMarket models to convert.</param>
        /// <returns>A list of WeightSetMarketDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when markets is null.</exception>
        public static List<WeightSetMarketDTO> ToWeightSetMarketDTOs(this IEnumerable<WeightSetMarket> markets)
        {
            if (markets == null)
                throw new ArgumentNullException(nameof(markets));

            var stopwatch = Stopwatch.StartNew();

            var result = markets.Select(m => m.ToWeightSetMarketDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToWeightSetMarketDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of WeightSetMarketDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="weightSetMarketDTOs">The collection of WeightSetMarketDTOs to convert.</param>
        /// <returns>A list of WeightSetMarket models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSetMarketDTOs is null.</exception>
        public static List<WeightSetMarket> ToWeightSetMarkets(this IEnumerable<WeightSetMarketDTO> weightSetMarketDTOs)
        {
            if (weightSetMarketDTOs == null)
                throw new ArgumentNullException(nameof(weightSetMarketDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = weightSetMarketDTOs.Select(dto => dto.ToWeightSetMarket()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToWeightSetMarkets", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a WeightSetMarket model to prevent unintended mutations.
        /// </summary>
        /// <param name="market">The WeightSetMarket model to clone.</param>
        /// <returns>A new WeightSetMarket instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when market is null.</exception>
        public static WeightSetMarket DeepClone(this WeightSetMarket market)
        {
            if (market == null)
                throw new ArgumentNullException(nameof(market));

            return new WeightSetMarket
            {
                WeightSetID = market.WeightSetID,
                MarketTicker = market.MarketTicker,
                PnL = market.PnL,
                LastRun = market.LastRun
            };
        }

        /// <summary>
        /// Creates a deep clone of a WeightSetMarketDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="weightSetMarketDTO">The WeightSetMarketDTO to clone.</param>
        /// <returns>A new WeightSetMarketDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when weightSetMarketDTO is null.</exception>
        public static WeightSetMarketDTO DeepClone(this WeightSetMarketDTO weightSetMarketDTO)
        {
            if (weightSetMarketDTO == null)
                throw new ArgumentNullException(nameof(weightSetMarketDTO));

            return new WeightSetMarketDTO
            {
                WeightSetID = weightSetMarketDTO.WeightSetID,
                MarketTicker = weightSetMarketDTO.MarketTicker,
                PnL = weightSetMarketDTO.PnL,
                LastRun = weightSetMarketDTO.LastRun
            };
        }
    }
}
