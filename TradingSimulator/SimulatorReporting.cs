// SimulatorTests.cs
// Updated to avoid redundant data loads for markets, remove parallel operations, and maintain separate strategy set methods

using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework.Constraints;
using SmokehouseBot.Configuration;
using SmokehouseBot.Management;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using System.Text.Json;
using TradingSimulator.Strategies;
using TradingSimulator.TestObjects;
using TradingStrategies;
using TradingStrategies.Classification;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Helpers;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingSimulator.Simulator
{


    [TestFixture]
    public class SimulatorReporting
    {

        private readonly string _cacheDirectory = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");


        // Detect discrepancies: compares rolling observed vs expected only (zero-velocity override removed).
        public List<PricePoint> DetectDiscrepancies(
            List<MarketSnapshot> s,
            double relativeSlack = 1.5,
            double averagingWindowMin = 5.0,
            int minAbsChangeToFlag = 500,
            int minAbsChangeOnZeroVelocity = 100, // retained for signature compatibility
            double shortIntervalExponent = 0.5,
            double gapThresholdMin = 1.5,
            double leakageFactor = 0.05)
        {
            var outPts = new List<PricePoint>();
            if (s == null || s.Count <= 1) return outPts;

            for (int i = 1; i < s.Count; i++)
            {
                var curr = s[i];
                var prev = s[i - 1];
                if (!curr.ChangeMetricsMature) continue;

                double dtMin = (curr.Timestamp - prev.Timestamp).TotalMinutes;
                if (dtMin <= 0) continue;

                double dy = curr.TotalOrderbookDepth_Yes - prev.TotalOrderbookDepth_Yes;
                double dn = curr.TotalOrderbookDepth_No  - prev.TotalOrderbookDepth_No;

                double vYes = curr.VelocityPerMinute_Top_Yes_Bid + curr.VelocityPerMinute_Bottom_Yes_Bid;
                double vNo = curr.VelocityPerMinute_Top_No_Bid  + curr.VelocityPerMinute_Bottom_No_Bid;

                var (rollObsYes5m, rollObsNo5m, rollObsYesWin, rollObsNoWin,
                     windowDt, gapNote, windowDy, windowDn) =
                    ComputeRollingObs(curr, s, averagingWindowMin, gapThresholdMin);

                double rollExpYes = vYes;
                double rollExpNo = vNo;

                double scale = 1.0;
                if (windowDt > 0 && windowDt < averagingWindowMin)
                    scale = Math.Pow(averagingWindowMin / windowDt, shortIntervalExponent);

                double toleranceYes = 0.0, toleranceNo = 0.0;
                int oldestIncludedIndex = FindOldestIncludedIndex(i, s, averagingWindowMin, gapThresholdMin);
                if (oldestIncludedIndex > 1)
                {
                    var preSnap = s[oldestIncludedIndex - 1];
                    var prePrevSnap = s[oldestIncludedIndex - 2];
                    double preDt = (preSnap.Timestamp - prePrevSnap.Timestamp).TotalMinutes;
                    if (preDt > 0 && preDt <= gapThresholdMin)
                    {
                        double preDy = preSnap.TotalOrderbookDepth_Yes - prePrevSnap.TotalOrderbookDepth_Yes;
                        double preDn = preSnap.TotalOrderbookDepth_No  - prePrevSnap.TotalOrderbookDepth_No;
                        double preVelYes = preDy / (100.0 * preDt);
                        double preVelNo = preDn / (100.0 * preDt);
                        toleranceYes = Math.Abs(preVelYes) * leakageFactor;
                        toleranceNo  = Math.Abs(preVelNo)  * leakageFactor;
                    }
                }

                double baseThrYes = Math.Abs(rollExpYes) * relativeSlack * scale;
                double thrYes = Math.Max(baseThrYes + toleranceYes, (double)minAbsChangeToFlag / (100.0 * averagingWindowMin));
                double baseThrNo = Math.Abs(rollExpNo) * relativeSlack * scale;
                double thrNo = Math.Max(baseThrNo + toleranceNo, (double)minAbsChangeToFlag / (100.0 * averagingWindowMin));

                bool discYes = Math.Abs(rollObsYes5m - rollExpYes) > thrYes;
                bool discNo = Math.Abs(rollObsNo5m  - rollExpNo)  > thrNo;

                if (discYes || discNo)
                {
                    var lastSnapshots = s.Skip(Math.Max(0, i - 6)).Take(7).ToList();
                    string memo = GenerateMemo(
                        curr, lastSnapshots, s,
                        averagingWindowMin, gapThresholdMin,
                        windowDt, windowDy, windowDn,
                        rollExpYes, rollExpNo,
                        rollObsYes5m, rollObsNo5m,
                        rollObsYesWin, rollObsNoWin,
                        gapNote, scale, shortIntervalExponent,
                        false, false, // zero-velocity flags removed
                        toleranceYes, toleranceNo, leakageFactor);

                    outPts.Add(new PricePoint(curr.Timestamp, (curr.BestYesBid + curr.BestYesAsk) / 2.0, memo));
                    AppendDiscrepancyLog(curr.MarketTicker ?? "UnknownMarket", memo);
                }
            }

            return outPts;
        }

        // Returns rolling averages and aggregated depth deltas across the window.
        // Signature now returns windowDy, windowDn for correct 5‑minute window calculations.
        private (double RollObsYes5m, double RollObsNo5m, double RollObsYesWin, double RollObsNoWin,
                double WindowDt, string GapNote, double WindowDy, double WindowDn)
            ComputeRollingObs(MarketSnapshot curr, List<MarketSnapshot> fullSnapshots,
                              double averagingWindowMin, double gapThresholdMin)
        {
            int currIndex = fullSnapshots.IndexOf(curr);
            if (currIndex < 1) return (0, 0, 0, 0, 0, "Insufficient data", 0, 0);

            DateTime windowStart = curr.Timestamp.AddMinutes(-averagingWindowMin);
            double windowDt = 0.0;
            double windowDy = 0.0;
            double windowDn = 0.0;
            string gapNote = string.Empty;

            int j = currIndex;
            while (j > 0 && fullSnapshots[j - 1].Timestamp >= windowStart)
            {
                var prev = fullSnapshots[j - 1];
                double pairDt = (fullSnapshots[j].Timestamp - prev.Timestamp).TotalMinutes;
                if (pairDt > gapThresholdMin)
                {
                    gapNote = $"Gap of {pairDt:0.##} min detected; change rolling off from {prev.Timestamp:yyyy-MM-ddTHH:mm:ss.fffffffK}.";
                    break;
                }
                if (prev.Timestamp < windowStart) break;

                double dy = fullSnapshots[j].TotalOrderbookDepth_Yes - prev.TotalOrderbookDepth_Yes;
                double dn = fullSnapshots[j].TotalOrderbookDepth_No  - prev.TotalOrderbookDepth_No;

                windowDy += dy;
                windowDn += dn;
                windowDt += pairDt;
                j--;
            }

            double rollObsYes5m = (windowDt > 0) ? (windowDy / 100.0) / windowDt : 0.0;
            double rollObsNo5m = (windowDt > 0) ? (windowDn  / 100.0) / windowDt : 0.0;
            double rollObsYesWin = rollObsYes5m;
            double rollObsNoWin = rollObsNo5m;

            return (rollObsYes5m, rollObsNo5m, rollObsYesWin, rollObsNoWin, windowDt, gapNote, windowDy, windowDn);
        }

        private void AppendDiscrepancyLog(string marketTicker, string memo)
        {
            string logPath = Path.Combine(_cacheDirectory, "discrepancies.log");  // Or a per-market file if preferred
            File.AppendAllText(logPath, $"Market: {marketTicker}\n{memo}\n\n");
        }

        // Updated helper signature to include gapThresholdMin
        private int FindOldestIncludedIndex(int currIndex, List<MarketSnapshot> s, double averagingWindowMin, double gapThresholdMin)
        {
            DateTime windowStart = s[currIndex].Timestamp.AddMinutes(-averagingWindowMin);
            int j = currIndex - 1;
            while (j > 0 && s[j].Timestamp >= windowStart)
            {
                var priorPrev = s[j - 1];
                double priorDt = (s[j].Timestamp - priorPrev.Timestamp).TotalMinutes;
                if (priorDt > gapThresholdMin) break;
                if (priorPrev.Timestamp < windowStart) break;
                j--;
            }
            return j + 1;  // Oldest included is the current j after loop (adjusted for decrement)
        }

        private string GenerateMemo(
            MarketSnapshot curr,
            List<MarketSnapshot> lastSnapshots,
            List<MarketSnapshot> fullSnapshots,
            double averagingWindowMin,
            double gapThresholdMin,
            double windowDt,
            double windowDy,
            double windowDn,
            double rollExpYes,
            double rollExpNo,
            double rollObsYes5m,
            double rollObsNo5m,
            double rollObsYesWin,
            double rollObsNoWin,
            string gapNote,
            double scale,
            double shortIntervalExponent,
            bool zeroVelYesDisc,
            bool zeroVelNoDisc,
            double toleranceYes,
            double toleranceNo,
            double leakageFactor)
        {
            var sb = new System.Text.StringBuilder();

            string discType = zeroVelYesDisc ? "[YES ZERO-VELOCITY]" : zeroVelNoDisc ? "[NO ZERO-VELOCITY]" : "[DISCREPANCY]";
            sb.AppendLine($"{discType} {curr.Timestamp:yyyy-MM-ddTHH:mm:ss.fffffffK}");

            int steps = lastSnapshots.Count - 1;
            sb.AppendLine($"Window: size={steps} steps, duration={windowDt:0.##} min");
            if (!string.IsNullOrEmpty(gapNote))
                sb.AppendLine(gapNote);

            sb.AppendLine("Last 7 snapshots (oldest first):");
            string headerFormat = "{0,-35} {1,8} {2,28} {3,32} {4,12} {5,28} {6,32} {7,12}";
            string header = string.Format(headerFormat,
                "Timestamp (UTC)",
                "WinMin",
                "Your Rolling Yes ($/min)",
                "Orderbook Flow Yes (inst $/min)",
                "YesDepth(¢)",
                "Your Rolling No ($/min)",
                "Orderbook Flow No (inst $/min)",
                "NoDepth(¢)");
            sb.AppendLine("    " + header);

            string rowFormat = "{0,-35} {1,8:0.##} {2,28:0.##} {3,32:0.##} {4,12:0.##} {5,28:0.##} {6,32:0.##} {7,12:0.##}";
            foreach (var snap in lastSnapshots)
            {
                // Rolling velocities from the snapshot (user’s data)
                double yourRollingYes = snap.VelocityPerMinute_Top_Yes_Bid + snap.VelocityPerMinute_Bottom_Yes_Bid;
                double yourRollingNo = snap.VelocityPerMinute_Top_No_Bid  + snap.VelocityPerMinute_Bottom_No_Bid;

                // Instantaneous orderbook-derived flow between consecutive snapshots
                double instFlowYes = 0.0, instFlowNo = 0.0;
                int idx = fullSnapshots.IndexOf(snap);
                if (idx > 0)
                {
                    var prevSnap = fullSnapshots[idx - 1];
                    double dtRow = (snap.Timestamp - prevSnap.Timestamp).TotalMinutes;
                    if (dtRow > 0)
                    {
                        double dyRow = snap.TotalOrderbookDepth_Yes - prevSnap.TotalOrderbookDepth_Yes;
                        double dnRow = snap.TotalOrderbookDepth_No  - prevSnap.TotalOrderbookDepth_No;
                        instFlowYes = (dyRow / 100.0) / dtRow;
                        instFlowNo  = (dnRow  / 100.0) / dtRow;
                    }
                }

                double winMin = (curr.Timestamp - snap.Timestamp).TotalMinutes;
                sb.AppendLine("    " + string.Format(rowFormat,
                    snap.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK"),
                    winMin,
                    yourRollingYes,
                    instFlowYes,
                    snap.TotalOrderbookDepth_Yes,
                    yourRollingNo,
                    instFlowNo,
                    snap.TotalOrderbookDepth_No));
            }

            // Rolling orderbook flow summary for the window
            sb.AppendLine("Rolling math (end snapshot - orderbook flow):");
            sb.AppendLine($"  Window: ΔYes(¢)={windowDy:0.##} ⇒ {rollObsYes5m:0.##} $/min, ΔNo(¢)={windowDn:0.##} ⇒ {rollObsNo5m:0.##} $/min (Σdt={windowDt:0.##} min)");

            // If the HUD velocities are zero but the depth-change indicates a flow, infer an implied expected velocity
            double displayExpYes = (Math.Abs(rollExpYes) < 1e-9 && Math.Abs(windowDy) > 0 && windowDt > 0)
                ? (windowDy / 100.0) / windowDt
                : rollExpYes;
            double displayExpNo = (Math.Abs(rollExpNo)  < 1e-9 && Math.Abs(windowDn)  > 0 && windowDt > 0)
                ? (windowDn / 100.0) / windowDt
                : rollExpNo;
            sb.AppendLine($"  Detection Expected (may use implied): Yes={displayExpYes:0.##} $/min, No={displayExpNo:0.##} $/min (ExpScale={scale:0.##})");

            // Details for debugging
            sb.AppendLine("Computation details:");
            sb.AppendLine($"    Short-Window Scale = (Window Minutes<{averagingWindowMin:0.##}? ( {averagingWindowMin:0.##}/Window Minutes )^{shortIntervalExponent:0.###} : 1) = {scale:0.######}");
            sb.AppendLine($"    Expected ΔDepth (¢) = Sum(Expected Flow×min)*100 = {displayExpYes * windowDt:0.######}*100 = {(displayExpYes * windowDt * 100):0.##} c");
            sb.AppendLine($"    Observed ΔDepth (¢) = windowΔ = {windowDy:0.##}");
            sb.AppendLine($"    Edge Tolerance (Leakage Factor={leakageFactor:0.###}): Yes={toleranceYes:0.##} $/min, No={toleranceNo:0.##} $/min");
            sb.AppendLine($"MID={(curr.BestYesBid + curr.BestYesAsk) / 2.0:0.##}");

            if (Math.Abs(rollObsYes5m - displayExpYes) < 1e-6 && Math.Abs(rollObsNo5m - displayExpNo) < 1e-6)
                sb.AppendLine("Resolution hint: Implied==Observed; likely timing skew cancel with adjacent snapshot.");

            return sb.ToString();
        }

        private List<PricePoint> CoalesceDiscrepancies(
    List<PricePoint> pts,
    int maxPairGapMinutes,
    double maxVelocityDiff)
        {
            if (pts == null || pts.Count < 2) return pts;

            var keep = new bool[pts.Count];
            Array.Fill(keep, true);

            // Extract compact metrics from memo lines
            bool TryParseMetrics(string memo, out double obsYes, out double expYes, out double obsNo, out double expNo)
            {
                obsYes = expYes = obsNo = expNo = 0;
                // Parse "Window: ΔYes(¢)=X ⇒ Y $/min, ΔNo(¢)=Z ⇒ W $/min" and "Expected (HUD-scale): Yes=A $/min, No=B $/min"
                // Safe parsing; failures just return false -> no coalescing.
                try
                {
                    var lines = memo.Split('\n');
                    var roll = lines.FirstOrDefault(l => l.StartsWith("  Window:", StringComparison.Ordinal));
                    var exp = lines.FirstOrDefault(l => l.StartsWith("  Expected (HUD-scale):", StringComparison.Ordinal));
                    if (roll == null || exp == null) return false;

                    // Y and W
                    int yi = roll.IndexOf("⇒", StringComparison.Ordinal);
                    int yiEnd = roll.IndexOf("$", yi + 1, StringComparison.Ordinal);
                    int wi = roll.LastIndexOf("⇒", StringComparison.Ordinal);
                    int wiEnd = roll.LastIndexOf("$", StringComparison.Ordinal);
                    if (yi < 0 || yiEnd < 0 || wi < 0 || wiEnd < 0) return false;
                    var yStr = roll.Substring(yi + 1, yiEnd - (yi + 1)).Replace("$/min", "").Trim();
                    var wStr = roll.Substring(wi + 1, wiEnd - (wi + 1)).Replace("$/min", "").Trim();
                    if (!double.TryParse(yStr, out obsYes)) return false;
                    if (!double.TryParse(wStr, out obsNo)) return false;

                    // A and B
                    var parts = exp.Split(new[] { "Yes=", "No=", "$/min" }, StringSplitOptions.RemoveEmptyEntries);
                    // parts like: ["  Expected (HUD-scale): ", "A ", " , ", "B ", " (ExpScale=..."]
                    var nums = parts.Where(p => double.TryParse(p.Trim(), out _))
                                    .Select(p => double.Parse(p.Trim()))
                                    .ToList();
                    if (nums.Count < 2) return false;
                    expYes = nums[0];
                    expNo  = nums[1];
                    return true;
                }
                catch { return false; }
            }

            for (int i = 1; i < pts.Count; i++)
            {
                if (!keep[i]) continue;
                for (int j = i - 1; j >= 0; j--)
                {
                    if (!keep[j]) continue;
                    var dt = (pts[i].Date - pts[j].Date).TotalMinutes;
                    if (dt > maxPairGapMinutes) break;

                    if (TryParseMetrics(pts[i].Memo, out var oYi, out var eYi, out var oNi, out var eNi) &&
                        TryParseMetrics(pts[j].Memo, out var oYj, out var eYj, out var oNj, out var eNj))
                    {
                        // Compare residuals (Observed-Expected) for Yes and No
                        double riY = oYi - eYi, rjY = oYj - eYj;
                        double riN = oNi - eNi, rjN = oNj - eNj;

                        bool cancelsY = Math.Abs(riY + rjY) <= maxVelocityDiff;
                        bool cancelsN = Math.Abs(riN + rjN) <= maxVelocityDiff;

                        if (cancelsY || cancelsN)
                        {
                            keep[i] = false;
                            keep[j] = false;
                            break;
                        }
                    }
                }
            }

            var result = new List<PricePoint>(pts.Count);
            for (int k = 0; k < pts.Count; k++)
                if (keep[k]) result.Add(pts[k]);
            return result;
        }
    }
}