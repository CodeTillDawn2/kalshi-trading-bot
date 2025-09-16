// File: TradingStrategies/Strategies/Strats/LowLiquidityExitExec.cs

using BacklashDTOs;
using System.Text.Json;
using TradingStrategies.Trading.Overseer;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    /// <summary>
    /// LowLiquidityExitExec strategy for exiting in low liquidity conditions.
    /// It always returns Exit action.
    /// Why: Ensures positions are closed in thin markets to avoid slippage.
    /// </summary>
    public class LowLiquidityExitExec : Strat
    {
        public string Name { get; private set; }
        public override double Weight { get; }

        public LowLiquidityExitExec(string name = nameof(LowLiquidityExitExec), double weight = 1.0)
        {
            Name = name;
            Weight = weight;
        }

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            // Early exit for illiquid markets
            if (snapshot.BestYesBid == 0 || snapshot.BestNoBid == 0 || snapshot.TotalBidContracts_Yes == 0 || snapshot.TotalBidContracts_No == 0)
            {
                return new ActionDecision { Type = ActionType.Exit, Price = 0, Quantity = 1, Memo = $"Illiquid market: No valid bids or contracts (BestYesBid: {snapshot.BestYesBid}; BestNoBid: {snapshot.BestNoBid}; TotalYesBidContracts: {snapshot.TotalBidContracts_Yes}; TotalNoBidContracts: {snapshot.TotalBidContracts_No})" };
            }

            // Hypothetical order size: equivalent to $10 (1000 cents), adjusted by price
            int hypotheticalContracts = Math.Max(10, 1000 / Math.Max(snapshot.BestYesBid, snapshot.BestNoBid)); // At least 10 contracts

            // Helper: Normalize a value between 0 and 1 using a sigmoid function (higher value = higher score)
            double Normalize(double value, double minCap, double maxCap)
            {
                if (maxCap == minCap) return 0.5; // Avoid division by zero
                double x = (value - minCap) / (maxCap - minCap) * 10 - 5; // Scale to [-5, 5]
                return 1 / (1 + Math.Exp(-x)); // Sigmoid function
            }

            // Metric 1: Spread score (inverted: lower spread = higher score)
            double spreadCapYes = Math.Max(snapshot.YesSpread * 2, 5); // Cap at 5 cents minimum
            double spreadScoreYes = 1 - Normalize(snapshot.YesSpread, 0, spreadCapYes);
            double spreadCapNo = Math.Max(snapshot.NoSpread * 2, 5);
            double spreadScoreNo = 1 - Normalize(snapshot.NoSpread, 0, spreadCapNo);

            // Metric 2: Top-level depth score (non-inverted: higher depth = higher score, in cents)
            double depthYesCents = snapshot.DepthAtBestYesBid * snapshot.BestYesBid; // Yes side: price * contracts
            double depthCapYes = Math.Max(depthYesCents * 2, 10000); // Cap at ~$100
            double depthScoreYes = Normalize(depthYesCents, 0, depthCapYes);
            double depthNoCents = snapshot.DepthAtBestNoBid * snapshot.BestNoBid; // No side: price * contracts (corrected)
            double depthCapNo = Math.Max(depthNoCents * 2, 10000);
            double depthScoreNo = Normalize(depthNoCents, 0, depthCapNo);

            // Metric 3: Cumulative depth score within tolerance (non-inverted, in cents)
            double cumDepthYesCents = 0;
            foreach (var bid in snapshot.GetYesBids().Where(bid => bid.Key >= snapshot.BestYesBid * (1 - snapshot.TolerancePercentage / 100)))
            {
                cumDepthYesCents += bid.Key * bid.Value; // Price * contracts in cents
            }
            double cumDepthCapYes = Math.Max(cumDepthYesCents * 2, 50000); // Cap at ~$500
            double cumDepthScoreYes = Normalize(cumDepthYesCents, 0, cumDepthCapYes);
            double cumDepthNoCents = 0;
            foreach (var bid in snapshot.GetNoBids().Where(bid => bid.Key >= snapshot.BestNoBid * (1 - snapshot.TolerancePercentage / 100)))
            {
                cumDepthNoCents += bid.Key * bid.Value; // Corrected: price * contracts for No
            }
            double cumDepthCapNo = Math.Max(cumDepthNoCents * 2, 50000);
            double cumDepthScoreNo = Normalize(cumDepthNoCents, 0, cumDepthCapNo);

            // Metric 4: Recent volume score (non-inverted, in contracts)
            double maxVolume = Math.Max(snapshot.HighestVolume_Hour, 1); // Avoid division by zero
            double volumeCap = maxVolume * 2;
            double volumeScore = Normalize(snapshot.RecentVolume_LastHour, 0, volumeCap); // Shared for both sides

            // Metric 5: Imbalance score (inverted: lower imbalance = higher score, in cents)
            double imbalanceCents = Math.Abs(snapshot.TotalOrderbookDepth_Yes - snapshot.TotalOrderbookDepth_No);
            double imbalanceCap = Math.Max(imbalanceCents, 50000); // Cap at ~$500
            double imbalanceScore = 1 - Normalize(imbalanceCents, 0, imbalanceCap);

            // Metric 6: Slippage estimate (price impact for hypothetical order, in cents; inverted)
            double SlippageForSize(string side)
            {
                var bids = side == "yes" ? snapshot.GetYesBids() : snapshot.GetNoBids();
                var sortedBids = new SortedDictionary<int, int>(bids, new DescendingComparer<int>()); // Highest prices first
                double totalCostCents = 0;
                int remaining = hypotheticalContracts;
                int startPrice = side == "yes" ? snapshot.BestYesBid : snapshot.BestNoBid;
                foreach (var level in sortedBids)
                {
                    int take = Math.Min(remaining, level.Value);
                    totalCostCents += take * level.Key; // Cost in cents
                    remaining -= take;
                    if (remaining <= 0) break;
                }
                // Penalize insufficient depth by assuming remaining filled at 0 cents
                double avgPriceCents = totalCostCents / hypotheticalContracts;
                return Math.Abs(startPrice - avgPriceCents); // Slippage in cents
            }
            double slippageYes = SlippageForSize("yes");
            double slippageNo = SlippageForSize("no");
            double slippageCap = 3; // Fixed cap at 3 cents for bad slippage
            double slippageScoreYes = 1 - Normalize(slippageYes, 0, slippageCap);
            double slippageScoreNo = 1 - Normalize(slippageNo, 0, slippageCap);

            // Weighted average for each side (weights sum to 1.0)
            double yesLiquidity =
                0.25 * spreadScoreYes + // 25% weight
                0.25 * depthScoreYes +  // 25% weight
                0.15 * cumDepthScoreYes + // 15% weight
                0.05 * volumeScore +    // 5% weight
                0.10 * imbalanceScore + // 10% weight
                0.20 * slippageScoreYes; // 20% weight

            double noLiquidity =
                0.25 * spreadScoreNo +
                0.25 * depthScoreNo +
                0.15 * cumDepthScoreNo +
                0.05 * volumeScore +
                0.10 * imbalanceScore +
                0.20 * slippageScoreNo;

            // Average sides and scale to 0-100
            double totalLiquidity = (yesLiquidity + noLiquidity) / 2.0 * 100.0;

            // Memo to illuminate all fields, raw values, normalized scores, and weights
            string memo = $"Low liquidity score: {totalLiquidity:F2}, " +
                          $"Breakdown (raw | normalized | weight): ," +
                          $"YesSpread: {snapshot.YesSpread:F2} cents | {spreadScoreYes:F2} | 25%, " +
                          $"NoSpread: {snapshot.NoSpread:F2} cents | {spreadScoreNo:F2} | 25%, " +
                          $"DepthAtBestYesBid: {depthYesCents:F2} cents ({snapshot.DepthAtBestYesBid} contracts @ {snapshot.BestYesBid} cents) | {depthScoreYes:F2} | 25%, " +
                          $"DepthAtBestNoBid: {depthNoCents:F2} cents ({snapshot.DepthAtBestNoBid} contracts @ {snapshot.BestNoBid} cents) | {depthScoreNo:F2} | 25%, " +
                          $"CumulativeYesBidDepth: {cumDepthYesCents:F2} cents | {cumDepthScoreYes:F2} | 15%, " +
                          $"CumulativeNoBidDepth: {cumDepthNoCents:F2} cents | {cumDepthScoreNo:F2} | 15%, " +
                          $"RecentVolume_LastHour: {snapshot.RecentVolume_LastHour:F2} contracts | {volumeScore:F2} | 5%, " +
                          $"BidImbalance: {imbalanceCents:F2} cents (Yes: {snapshot.TotalOrderbookDepth_Yes} , No: {snapshot.TotalOrderbookDepth_No}) | {imbalanceScore:F2} | 10%, " +
                          $"SlippageYes: {slippageYes:F2} cents (for {hypotheticalContracts} contracts) | {slippageScoreYes:F2} | 20%, " +
                          $"SlippageNo: {slippageNo:F2} cents (for {hypotheticalContracts} contracts) | {slippageScoreNo:F2} | 20%, " +
                          $"YesLiquidity: {yesLiquidity * 100.0:F2}, NoLiquidity: {noLiquidity * 100.0:F2}";

            // Always exit in low liquidity scenarios
            return new ActionDecision { Type = ActionType.Exit, Price = 0, Quantity = 1, Memo = memo };
        }

        public override string ToJson()
        {
            var data = new
            {
                type = "LowLiquidityExitExec",
                name = Name,
                weight = Weight
            };
            return JsonSerializer.Serialize(data);
        }
        internal class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
        {
            public int Compare(T? x, T? y)
            {
                if (x is null || y is null) return 0;
                return y.CompareTo(x);
            }
        }

    }
}
