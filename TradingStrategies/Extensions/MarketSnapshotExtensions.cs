using BacklashDTOs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace TradingStrategies.Extensions
{
    /// <summary>
    /// Provides extension methods for MarketSnapshot to compute and update order book metrics from simulated order book data.
    /// This class serves as a bridge between the raw SimulatedOrderbook data structure and the calculated metrics
    /// stored in MarketSnapshot, enabling efficient computation of spreads, depths, volumes, and other trading indicators.
    /// All calculations are performed in a stateless manner, ensuring thread safety and reusability across different
    /// market simulation scenarios.
    ///
    /// Performance monitoring can be configured via MarketSnapshotExtensions:EnablePerformanceMetrics setting.
    /// </summary>
    public static class MarketSnapshotExtensions
    {
        private static readonly bool _enablePerformanceMetrics;
        private static ILogger _logger;

        /// <summary>
        /// Static constructor to initialize configuration and logging for performance metrics.
        /// </summary>
        static MarketSnapshotExtensions()
        {
            // Note: In a real application, these would be injected via DI
            // For now, we'll use a simple approach that can be configured
            _enablePerformanceMetrics = GetPerformanceMetricsEnabled();
        }

        /// <summary>
        /// Gets whether performance metrics are enabled for this class.
        /// Configuration key: MarketSnapshotExtensions:EnablePerformanceMetrics
        /// Can be configured via environment variable or appsettings.json
        /// </summary>
        private static bool GetPerformanceMetricsEnabled()
        {
            try
            {
                // First try environment variable
                var envValue = Environment.GetEnvironmentVariable("MarketSnapshotExtensions_EnablePerformanceMetrics");
                if (!string.IsNullOrEmpty(envValue) && bool.TryParse(envValue, out var envResult))
                {
                    return envResult;
                }

                // Try to read from appsettings.json if it exists
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(configPath))
                {
                    var configJson = File.ReadAllText(configPath);
                    var config = JsonDocument.Parse(configJson);

                    if (config.RootElement.TryGetProperty("MarketSnapshotExtensions", out var marketSnapshotExtensions) &&
                        marketSnapshotExtensions.TryGetProperty("EnablePerformanceMetrics", out var enableMetrics))
                    {
                        return enableMetrics.GetBoolean();
                    }
                }

                // Default to disabled for performance
                return false;
            }
            catch
            {
                // If configuration fails, default to disabled
                return false;
            }
        }
        /// <summary>
        /// Updates all order book-related metrics in the MarketSnapshot based on the current state of the SimulatedOrderbook.
        /// This method recalculates spreads, depths, volumes, ranges, imbalances, and center of mass values to reflect
        /// the latest order book state. It is typically called after order book modifications during trading simulations
        /// to ensure all derived metrics remain consistent with the underlying order book data.
        /// Includes input validation and performance timing for robustness and monitoring.
        /// </summary>
        /// <param name="snapshot">The MarketSnapshot instance whose metrics will be updated.</param>
        /// <param name="book">The SimulatedOrderbook containing the current order book state to analyze.</param>
        /// <remarks>
        /// This method performs comprehensive order book analysis including:
        /// - Best bid/ask price identification
        /// - Spread calculations between bid and ask prices
        /// - Depth analysis at best prices and within tolerance ranges
        /// - Volume calculations in both contract count and dollar value
        /// - Price range analysis for bid orders
        /// - Imbalance calculations between yes/no sides
        /// - Center of mass calculations for order book distribution
        ///
        /// All calculations use the snapshot's TolerancePercentage for range-based metrics and assume
        /// prices are in cents (1-99 range) as per Kalshi market conventions.
        ///
        /// Includes input validation to prevent null reference exceptions and performance timing
        /// measurement for monitoring execution time in high-frequency scenarios.
        /// </remarks>
        public static void UpdateOrderbookMetricsFromSimulated(this MarketSnapshot snapshot, SimulatedOrderbook book)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (book == null) throw new ArgumentNullException(nameof(book));

            var stopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;

            // Update best prices from order book
            snapshot.BestYesBid = book.GetBestYesBid();
            snapshot.BestNoBid = book.GetBestNoBid();

            // Calculate spreads between best bid and ask prices
            snapshot.YesSpread = snapshot.BestYesAsk - snapshot.BestYesBid;
            snapshot.NoSpread = snapshot.BestNoAsk - snapshot.BestNoBid;

            // Update depth at best prices
            snapshot.DepthAtBestYesBid = book.GetDepthAtBestYesBid();
            snapshot.DepthAtBestNoBid = book.GetDepthAtBestNoBid();

            // Calculate cumulative depths within tolerance percentage of best prices
            snapshot.TopTenPercentLevelDepth_Yes = CalculateCumulativeDepth(book.YesBids, snapshot.BestYesBid, snapshot.TolerancePercentage);
            snapshot.TopTenPercentLevelDepth_No = CalculateCumulativeDepth(book.NoBids, snapshot.BestNoBid, snapshot.TolerancePercentage);

            // Calculate price ranges for bid orders
            snapshot.BidRange_Yes = CalculateBidRange(book.YesBids);
            snapshot.BidRange_No = CalculateBidRange(book.NoBids);

            // Calculate total contract counts for each side
            snapshot.TotalBidContracts_Yes = CalculateTotalContracts(book.YesBids);
            snapshot.TotalBidContracts_No = CalculateTotalContracts(book.NoBids);

            // Calculate total dollar volumes for each side (price * contracts)
            snapshot.TotalBidVolume_Yes = CalculateTotalVolume(book.YesBids);
            snapshot.TotalBidVolume_No = CalculateTotalVolume(book.NoBids);

            // Calculate imbalance between yes and no sides
            snapshot.BidCountImbalance = snapshot.TotalBidContracts_Yes - snapshot.TotalBidContracts_No;

            // Calculate depth at top 4 price levels for each side
            snapshot.DepthAtTop4YesBids = CalculateTopNDepth(book.YesBids, 4);
            snapshot.DepthAtTop4NoBids = CalculateTopNDepth(book.NoBids, 4);

            // Calculate center of mass (weighted average price) for order book distribution
            snapshot.YesBidCenterOfMass = CalculateCenterOfMass(book.YesBids);
            snapshot.NoBidCenterOfMass = CalculateCenterOfMass(book.NoBids);

            if (_enablePerformanceMetrics && stopwatch != null)
            {
                stopwatch.Stop();
                LogPerformanceMetrics(stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Logs performance metrics for the order book calculations.
        /// Only logs when performance metrics are enabled.
        /// </summary>
        /// <param name="elapsedMilliseconds">Time taken for the calculations in milliseconds.</param>
        private static void LogPerformanceMetrics(long elapsedMilliseconds)
        {
            // Use a simple console output for now, but this could be enhanced to use proper logging
            // In a real application, this would use ILogger or integrate with performance monitoring
            Console.WriteLine($"MarketSnapshotExtensions.UpdateOrderbookMetricsFromSimulated took {elapsedMilliseconds} ms");
        }

        /// <summary>
        /// Calculates the cumulative depth of bid orders within a tolerance percentage of the best bid price.
        /// </summary>
        /// <param name="bids">Dictionary of bid order queues indexed by price (1-99).</param>
        /// <param name="bestBid">The best (highest) bid price to use as reference.</param>
        /// <param name="tolerancePct">Tolerance percentage (e.g., 5.0 for 5%) defining the price range.</param>
        /// <returns>Total contract count within the tolerance range below the best bid.</returns>
        /// <remarks>
        /// This method sums all contract counts from the best bid price down to the minimum price
        /// calculated as bestBid * (1 - tolerancePct/100). It provides insight into the depth of
        /// the order book near the best bid, useful for assessing market liquidity and slippage potential.
        /// Only considers prices within the valid 1-99 range.
        /// Handles null input dictionaries by returning 0.
        /// </remarks>
        private static int CalculateCumulativeDepth(SortedDictionary<int, List<SimulatedOrderbook.OrderLevel>> bids, int bestBid, double tolerancePct)
        {
            if (bids == null) return 0;
            int cumulative = 0;
            double minPrice = bestBid * (1 - tolerancePct / 100.0);
            for (int p = bestBid; p >= minPrice && p >= 1; p--)
            {
                if (bids.TryGetValue(p, out var queue))
                {
                    cumulative += queue.Sum(o => o.Count);
                }
            }
            return cumulative;
        }

        /// <summary>
        /// Calculates the price range (max - min) of all active bid orders in the order book.
        /// </summary>
        /// <param name="bids">Dictionary of bid order queues indexed by price (1-99).</param>
        /// <returns>The difference between the highest and lowest bid prices with active orders.</returns>
        /// <remarks>
        /// This method scans all price levels to find the minimum and maximum prices that have
        /// at least one resting order. The range indicates the spread of bid prices in the order book,
        /// which can be useful for assessing market concentration and liquidity distribution.
        /// Returns 0 if no active bids exist.
        /// Handles null input dictionaries by returning 0.
        /// </remarks>
        private static int CalculateBidRange(SortedDictionary<int, List<SimulatedOrderbook.OrderLevel>> bids)
        {
            if (bids == null || bids.Count == 0) return 0;
            int maxPrice = bids.Keys.Max();
            int minPrice = bids.Keys.Min();
            return maxPrice - minPrice;
        }

        /// <summary>
        /// Calculates the total number of contracts across all bid orders in the order book.
        /// </summary>
        /// <param name="bids">Dictionary of bid order queues indexed by price (1-99).</param>
        /// <returns>Total contract count across all price levels.</returns>
        /// <remarks>
        /// This method sums the contract counts from all price levels that have active orders.
        /// It provides a measure of the total market participation at the bid side, useful for
        /// assessing overall market liquidity and order book size.
        /// Handles null input dictionaries by returning 0.
        /// </remarks>
        private static int CalculateTotalContracts(SortedDictionary<int, List<SimulatedOrderbook.OrderLevel>> bids)
        {
            if (bids == null) return 0;
            return bids.Values.Sum(queue => queue.Sum(o => o.Count));
        }


        /// <summary>
        /// Calculates the total dollar volume of all bid orders in the order book.
        /// </summary>
        /// <param name="bids">Dictionary of bid order queues indexed by price (1-99).</param>
        /// <returns>Total dollar volume as double (price in cents * contract count).</returns>
        /// <remarks>
        /// Volume is calculated as the sum of (price * contract_count) for all orders at each price level.
        /// This provides the total dollar value of all resting bid orders, useful for liquidity analysis.
        /// Prices are assumed to be in cents (1-99 range) as per Kalshi market conventions.
        /// Handles null input dictionaries by returning 0.0.
        /// </remarks>
        private static double CalculateTotalVolume(SortedDictionary<int, List<SimulatedOrderbook.OrderLevel>> bids)
        {
            if (bids == null) return 0.0;
            return bids.Sum(kvp => kvp.Key * kvp.Value.Sum(o => o.Count));
        }

        /// <summary>
        /// Calculates the total depth of the top N highest-priced bid levels in the order book.
        /// </summary>
        /// <param name="bids">Dictionary of bid order queues indexed by price (1-99).</param>
        /// <param name="n">Number of top price levels to include in the calculation.</param>
        /// <returns>Total contract count across the top N price levels.</returns>
        /// <remarks>
        /// This method starts from the highest price and works downward, summing contract counts
        /// at each price level until it has processed N levels with active orders. It provides insight
        /// into the concentration of liquidity at the best bid prices, useful for assessing market depth
        /// and potential execution quality. If fewer than N levels have orders, it sums all available levels.
        /// Handles null input dictionaries by returning 0.
        /// </remarks>
        private static int CalculateTopNDepth(SortedDictionary<int, List<SimulatedOrderbook.OrderLevel>> bids, int n)
        {
            if (bids == null) return 0;
            int depth = 0;
            int count = 0;
            foreach (var kvp in bids.Reverse())
            {
                if (count >= n) break;
                depth += kvp.Value.Sum(o => o.Count);
                count++;
            }
            return depth;
        }

        /// <summary>
        /// Calculates the center of mass (weighted average price) of all bid orders in the order book.
        /// </summary>
        /// <param name="bids">Dictionary of bid order queues indexed by price (1-99).</param>
        /// <returns>Weighted average price of all resting bid orders, or 0 if no orders exist.</returns>
        /// <remarks>
        /// The center of mass is calculated as the sum of (price * contract_count) divided by total contract count.
        /// This provides a measure of where the majority of the order book liquidity is concentrated,
        /// useful for understanding the distribution of bids and potential price levels of interest.
        /// A higher center of mass indicates bids are concentrated at higher prices, suggesting bullish sentiment.
        /// Handles null input dictionaries by returning 0.0.
        /// </remarks>
        private static double CalculateCenterOfMass(SortedDictionary<int, List<SimulatedOrderbook.OrderLevel>> bids)
        {
            if (bids == null) return 0.0;
            double weightedSum = 0;
            int totalMass = 0;
            foreach (var kvp in bids)
            {
                int levelCount = kvp.Value.Sum(o => o.Count);
                weightedSum += kvp.Key * levelCount;
                totalMass += levelCount;
            }
            return totalMass > 0 ? weightedSum / totalMass : 0;
        }
    }
}
