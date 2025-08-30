// SimulatorTests.cs
// Unified discrepancy detection using winsorized window-expected flows (same basis as observed)

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

        public List<PricePoint> DetectDiscrepancies(
    List<MarketSnapshot> s,
    double relativeSlack = 1.5,
    double averagingWindowMin = 5.0,
    int minAbsChangeToFlag = 500,
    int minAbsChangeOnZeroVelocity = 100, // unused; kept for signature compatibility
    double shortIntervalExponent = 0.5,
    double gapThresholdMin = 1.5,
    double leakageFactor = 0.05,
    double winsorPct = 0.2)
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

                var (rollObsYes5m, rollObsNo5m, rollObsYesWin, rollObsNoWin,
                     windowDt, gapNote, windowDy, windowDn,
                     instYesList, instNoList) =
                    ComputeRollingObs(curr, s, averagingWindowMin, gapThresholdMin);

                if (windowDt <= 0) continue;

                var (expYesRateWin, expNoRateWin) = ComputeExpectedFlowsWinsorized(instYesList, instNoList, winsorPct);

                double scale = (windowDt < averagingWindowMin)
                    ? Math.Pow(averagingWindowMin / windowDt, shortIntervalExponent)
                    : 1.0;

                double toleranceYes = 0.0, toleranceNo = 0.0;
                int oldestIncludedIndex = FindOldestIncludedIndex(i, s, averagingWindowMin, gapThresholdMin);
                if (oldestIncludedIndex > 1)
                {
                    var preSnap = s[oldestIncludedIndex - 1];
                    var prePrev = s[oldestIncludedIndex - 2];
                    double preDt = (preSnap.Timestamp - prePrev.Timestamp).TotalMinutes;
                    if (preDt > 0 && preDt <= gapThresholdMin)
                    {
                        double preDy = preSnap.TotalOrderbookDepth_Yes - prePrev.TotalOrderbookDepth_Yes;
                        double preDn = preSnap.TotalOrderbookDepth_No  - prePrev.TotalOrderbookDepth_No;
                        double preVelYes = preDy / (100.0 * preDt);
                        double preVelNo = preDn / (100.0 * preDt);
                        toleranceYes = Math.Abs(preVelYes) * leakageFactor;
                        toleranceNo  = Math.Abs(preVelNo)  * leakageFactor;
                    }
                }

                double magYes = Math.Max(Math.Abs(expYesRateWin), Math.Abs(rollObsYes5m));
                double magNo = Math.Max(Math.Abs(expNoRateWin), Math.Abs(rollObsNo5m));

                double thrYes = Math.Max(magYes * relativeSlack * scale + toleranceYes,
                                         (double)minAbsChangeToFlag / (100.0 * averagingWindowMin));
                double thrNo = Math.Max(magNo  * relativeSlack * scale + toleranceNo,
                                         (double)minAbsChangeToFlag / (100.0 * averagingWindowMin));

                bool discYes = Math.Abs(rollObsYes5m - expYesRateWin) > thrYes;
                bool discNo = Math.Abs(rollObsNo5m  - expNoRateWin)  > thrNo;

                if (!(discYes || discNo)) continue;

                // ----- Spike-driven suppression -----
                // params
                const double DOM_RATIO = 0.75;  // Rule A
                const double GAP_SHARE = 0.70;  // Rule B
                const double EDGE_MULT = 5.0;   // Rule C

                bool Suppress(List<double> flows, double obsRate, double expRate)
                {
                    if (flows == null || flows.Count == 0) return false;

                    double net = flows.Sum();
                    if (Math.Abs(net) < 1e-12) return false;

                    int m = 0;
                    double maxAbs = double.NegativeInfinity;
                    for (int k = 0; k < flows.Count; k++)
                    {
                        double v = Math.Abs(flows[k]);
                        if (v > maxAbs) { maxAbs = v; m = k; }
                    }

                    bool ruleA = (Math.Abs(flows[m]) / Math.Abs(net) >= DOM_RATIO) &&
                                 (Math.Sign(flows[m]) == Math.Sign(net));

                    double avgRaw = flows.Average();
                    // winsorized avg already used as expRate
                    double gapCents = Math.Abs((avgRaw - expRate) * windowDt * 100.0);
                    double residCents = Math.Abs(((obsRate - expRate) * windowDt * 100.0));
                    bool ruleB = residCents > 0 && (gapCents / residCents >= GAP_SHARE);

                    // Edge spike if dominant minute is first or last and far above median|flow|
                    var absSorted = flows.Select(Math.Abs).OrderBy(x => x).ToArray();
                    double medianAbs = absSorted[absSorted.Length / 2];
                    bool isEdge = (m == 0 || m == flows.Count - 1);
                    bool ruleC = isEdge && (Math.Abs(flows[m]) >= EDGE_MULT * (medianAbs <= 1e-12 ? 1.0 : medianAbs));

                    return ruleA || ruleB || ruleC;
                }

                bool suppressYes = discYes && Suppress(instYesList, rollObsYes5m, expYesRateWin);
                bool suppressNo = discNo  && Suppress(instNoList, rollObsNo5m, expNoRateWin);

                if (suppressYes && !discNo) continue;
                if (suppressNo  && !discYes) continue;
                if (suppressYes && suppressNo) continue;
                // -----------------------------------

                double expDepthYesC = expYesRateWin * windowDt * 100.0;
                double expDepthNoC = expNoRateWin  * windowDt * 100.0;

                var lastSnapshots = s.Skip(Math.Max(0, i - 6)).Take(7).ToList();
                string memo = GenerateMemo(
                    curr, lastSnapshots, s,
                    averagingWindowMin, gapThresholdMin,
                    windowDt, windowDy, windowDn,
                    expYesRateWin, expNoRateWin,
                    rollObsYes5m, rollObsNo5m,
                    rollObsYesWin, rollObsNoWin,
                    gapNote, scale, shortIntervalExponent,
                    false, false,
                    toleranceYes, toleranceNo, leakageFactor,
                    expDepthYesC, expDepthNoC);

                outPts.Add(new PricePoint(curr.Timestamp, (curr.BestYesBid + curr.BestYesAsk) / 2.0, memo));
                AppendDiscrepancyLog(curr.MarketTicker ?? "UnknownMarket", memo);
            }

            return outPts;
        }


        // Winsorized expected flows per window (clip extremes at percentile bounds, then average)
        private static (double expYes, double expNo) ComputeExpectedFlowsWinsorized(
            List<double> perMinuteYes, List<double> perMinuteNo, double winsorPct)
        {
            static (double lo, double hi) Bounds(List<double> xs, double p)
            {
                if (xs == null || xs.Count == 0) return (0, 0);
                var a = xs.OrderBy(v => v).ToArray();
                int n = a.Length;
                int li = (int)Math.Floor(p * n);
                int hi = (int)Math.Ceiling((1 - p) * n) - 1;
                li = Math.Clamp(li, 0, Math.Max(0, n - 1));
                hi = Math.Clamp(hi, li, n - 1);
                return (a[li], a[hi]);
            }

            static double WinsorAvg(List<double> xs, double p)
            {
                if (xs == null || xs.Count == 0) return 0.0;
                var (lo, hi) = Bounds(xs, p);
                double sum = 0.0;
                foreach (var v in xs)
                {
                    double w = v < lo ? lo : (v > hi ? hi : v);
                    sum += w;
                }
                return sum / xs.Count;
            }

            return (WinsorAvg(perMinuteYes, winsorPct), WinsorAvg(perMinuteNo, winsorPct));
        }

        private (double RollObsYes5m, double RollObsNo5m, double RollObsYesWin, double RollObsNoWin,
                 double WindowDt, string GapNote, double WindowDy, double WindowDn,
                 List<double> InstYesList, List<double> InstNoList)
            ComputeRollingObs(MarketSnapshot curr, List<MarketSnapshot> fullSnapshots,
                              double averagingWindowMin, double gapThresholdMin)
        {
            int currIndex = fullSnapshots.IndexOf(curr);
            if (currIndex < 1)
                return (0, 0, 0, 0, 0, "Insufficient data", 0, 0, new List<double>(), new List<double>());

            DateTime windowStart = curr.Timestamp.AddMinutes(-averagingWindowMin);
            double windowDt = 0.0, windowDy = 0.0, windowDn = 0.0;
            string gapNote = string.Empty;
            var instYes = new List<double>();
            var instNo = new List<double>();

            int j = currIndex;
            while (j > 0 && fullSnapshots[j - 1].Timestamp >= windowStart)
            {
                var prev = fullSnapshots[j - 1];
                var now = fullSnapshots[j];

                double pairDt = (now.Timestamp - prev.Timestamp).TotalMinutes;
                if (pairDt > gapThresholdMin)
                {
                    gapNote = $"Gap of {pairDt:0.##} min detected; change rolling off from {prev.Timestamp:yyyy-MM-ddTHH:mm:ss.fffffffK}.";
                    break;
                }
                if (prev.Timestamp < windowStart) break;

                double dy = now.TotalOrderbookDepth_Yes - prev.TotalOrderbookDepth_Yes;
                double dn = now.TotalOrderbookDepth_No  - prev.TotalOrderbookDepth_No;

                double flowYes = (pairDt > 0) ? (dy / 100.0) / pairDt : 0.0;
                double flowNo = (pairDt > 0) ? (dn / 100.0) / pairDt : 0.0;

                instYes.Add(flowYes);
                instNo.Add(flowNo);

                windowDy += dy;
                windowDn += dn;
                windowDt += pairDt;

                j--;
            }

            double rollObsYes5m = (windowDt > 0) ? (windowDy / 100.0) / windowDt : 0.0;
            double rollObsNo5m = (windowDt > 0) ? (windowDn  / 100.0) / windowDt : 0.0;

            return (rollObsYes5m, rollObsNo5m, rollObsYes5m, rollObsNo5m,
                    windowDt, gapNote, windowDy, windowDn, instYes, instNo);
        }

        private void AppendDiscrepancyLog(string marketTicker, string memo)
        {
            string logPath = Path.Combine(_cacheDirectory, "discrepancies.log");
            File.AppendAllText(logPath, $"Market: {marketTicker}\n{memo}\n\n");
        }

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
            return j + 1;
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
            double expYesRateWin,
            double expNoRateWin,
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
            double leakageFactor,
            double expDepthYesC,
            double expDepthNoC)
        {
            var sb = new System.Text.StringBuilder();

            string discType = "[DISCREPANCY]";
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
                double yourRollingYes = snap.VelocityPerMinute_Top_Yes_Bid + snap.VelocityPerMinute_Bottom_Yes_Bid;
                double yourRollingNo = snap.VelocityPerMinute_Top_No_Bid  + snap.VelocityPerMinute_Bottom_No_Bid;

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

            sb.AppendLine("Rolling math (end snapshot - orderbook flow):");
            sb.AppendLine($"  Window: ΔYes(¢)={windowDy:0.##} ⇒ {rollObsYes5m:0.##} $/min, ΔNo(¢)={windowDn:0.##} ⇒ {rollObsNo5m:0.##} $/min (Σdt={windowDt:0.##} min)");

            sb.AppendLine($"  Detection Expected: Yes={expYesRateWin:0.##} $/min, No={expNoRateWin:0.##} $/min (ExpScale={scale:0.##})");

            sb.AppendLine("Computation details:");
            sb.AppendLine($"    Short-Window Scale = (Window Minutes<{averagingWindowMin:0.##}? ( {averagingWindowMin:0.##}/Window Minutes )^{shortIntervalExponent:0.###} : 1) = {scale:0.######}");
            sb.AppendLine($"    Expected ΔDepth (¢) = Σ(Expected Flow×min)×100 = {(expYesRateWin * windowDt):0.######}*100 = {expDepthYesC:0.##} c");
            sb.AppendLine($"    Observed ΔDepth (¢) = windowΔ = {windowDy:0.##}");
            sb.AppendLine($"    Edge Tolerance (Leakage Factor={leakageFactor:0.###}): Yes={toleranceYes:0.##} $/min, No={toleranceNo:0.##} $/min");
            sb.AppendLine($"MID={(curr.BestYesBid + curr.BestYesAsk) / 2.0:0.##}");

            if (Math.Abs(rollObsYes5m - expYesRateWin) < 1e-6 && Math.Abs(rollObsNo5m - expNoRateWin) < 1e-6)
                sb.AppendLine("Resolution hint: Observed==Expected; window integration with winsorization muted edge spikes.");

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

            bool TryParseMetrics(string memo, out double obsYes, out double expYes, out double obsNo, out double expNo)
            {
                obsYes = expYes = obsNo = expNo = 0;
                try
                {
                    var lines = memo.Split('\n');
                    var roll = lines.FirstOrDefault(l => l.StartsWith("  Window:", StringComparison.Ordinal));
                    var exp = lines.FirstOrDefault(l => l.StartsWith("  Detection Expected:", StringComparison.Ordinal));
                    if (roll == null || exp == null) return false;

                    int yi = roll.IndexOf("⇒", StringComparison.Ordinal);
                    int yiEnd = roll.IndexOf("$", yi + 1, StringComparison.Ordinal);
                    int wi = roll.LastIndexOf("⇒", StringComparison.Ordinal);
                    int wiEnd = roll.LastIndexOf("$", StringComparison.Ordinal);
                    if (yi < 0 || yiEnd < 0 || wi < 0 || wiEnd < 0) return false;
                    var yStr = roll.Substring(yi + 1, yiEnd - (yi + 1)).Replace("$/min", "").Trim();
                    var wStr = roll.Substring(wi + 1, wiEnd - (wi + 1)).Replace("$/min", "").Trim();
                    if (!double.TryParse(yStr, out obsYes)) return false;
                    if (!double.TryParse(wStr, out obsNo)) return false;

                    var parts = exp.Split(new[] { "Yes=", "No=", "$/min" }, StringSplitOptions.RemoveEmptyEntries);
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
