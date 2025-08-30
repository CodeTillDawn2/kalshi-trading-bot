using SmokehouseDTOs;
using TradingStrategies.Trading.Overseer;

namespace TradingStrategies.Extensions
{
    public static class MarketSnapshotExtensions
    {
        /// <summary>
        /// Updates the order book metrics of the MarketSnapshot instance based on the provided SimulatedOrderbook.
        /// </summary>
        /// <param name="snapshot">The MarketSnapshot instance to update.</param>
        /// <param name="book">The SimulatedOrderbook containing the updated data.</param>
        public static void UpdateOrderbookMetricsFromSimulated(this MarketSnapshot snapshot, SimulatedOrderbook book)
        {
            snapshot.BestYesBid = book.GetBestYesBid();
            snapshot.BestNoBid = book.GetBestNoBid();

            snapshot.YesSpread = snapshot.BestYesAsk - snapshot.BestYesBid;
            snapshot.NoSpread = snapshot.BestNoAsk - snapshot.BestNoBid;

            snapshot.DepthAtBestYesBid = book.GetDepthAtBestYesBid();
            snapshot.DepthAtBestNoBid = book.GetDepthAtBestNoBid();

            // Cumulative depths (recompute using tolerance)
            snapshot.TopTenPercentLevelDepth_Yes = CalculateCumulativeDepth(book.YesBids, snapshot.BestYesBid, snapshot.TolerancePercentage);
            snapshot.TopTenPercentLevelDepth_No = CalculateCumulativeDepth(book.NoBids, snapshot.BestNoBid, snapshot.TolerancePercentage);

            // Ranges
            snapshot.BidRange_Yes = CalculateBidRange(book.YesBids);
            snapshot.BidRange_No = CalculateBidRange(book.NoBids);

            // Totals
            snapshot.TotalBidContracts_Yes = CalculateTotalContracts(book.YesBids);
            snapshot.TotalBidContracts_No = CalculateTotalContracts(book.NoBids);

            // Imbalances
            snapshot.BidCountImbalance = snapshot.TotalBidContracts_Yes - snapshot.TotalBidContracts_No;

            // Top 4 depths (recompute)
            snapshot.DepthAtTop4YesBids = CalculateTopNDepth(book.YesBids, 4);
            snapshot.DepthAtTop4NoBids = CalculateTopNDepth(book.NoBids, 4);

            snapshot.TotalBidVolume_Yes = snapshot.TotalBidVolume_Yes;
            snapshot.TotalBidVolume_No = snapshot.TotalBidVolume_No;

            // Centers of mass (recompute)
            snapshot.YesBidCenterOfMass = CalculateCenterOfMass(book.YesBids);
            snapshot.NoBidCenterOfMass = CalculateCenterOfMass(book.NoBids);

        }

        private static int CalculateCumulativeDepth(List<(int count, DateTime timestamp)>[] bids, int bestBid, double tolerancePct)
        {
            int cumulative = 0;
            double minPrice = bestBid * (1 - tolerancePct / 100.0);
            for (int p = bestBid; p >= minPrice && p >= 1; p--)
            {
                if (bids[p] != null)
                {
                    cumulative += bids[p].Sum(o => o.count);
                }
            }
            return cumulative;
        }

        private static int CalculateBidRange(List<(int count, DateTime timestamp)>[] bids)
        {
            int maxPrice = 0;
            int minPrice = 100;
            for (int p = 1; p <= 99; p++)
            {
                if (bids[p] != null && bids[p].Count > 0)
                {
                    if (p > maxPrice) maxPrice = p;
                    if (p < minPrice) minPrice = p;
                }
            }
            return maxPrice > 0 ? maxPrice - minPrice : 0;
        }

        private static int CalculateTotalContracts(List<(int count, DateTime timestamp)>[] bids)
        {
            int total = 0;
            for (int p = 1; p <= 99; p++)
            {
                if (bids[p] != null)
                {
                    total += bids[p].Sum(o => o.count);
                }
            }
            return total;
        }

        private static int CalculateTopNDepth(List<(int count, DateTime timestamp)>[] bids, int n)
        {
            int depth = 0;
            int count = 0;
            for (int p = 99; p >= 1 && count < n; p--)
            {
                if (bids[p] != null && bids[p].Count > 0)
                {
                    depth += bids[p].Sum(o => o.count);
                    count++;
                }
            }
            return depth;
        }

        private static double CalculateCenterOfMass(List<(int count, DateTime timestamp)>[] bids)
        {
            double weightedSum = 0;
            int totalMass = 0;
            for (int p = 1; p <= 99; p++)
            {
                if (bids[p] != null)
                {
                    int levelCount = bids[p].Sum(o => o.count);
                    weightedSum += p * levelCount;
                    totalMass += levelCount;
                }
            }
            return totalMass > 0 ? weightedSum / totalMass : 0;
        }
    }
}