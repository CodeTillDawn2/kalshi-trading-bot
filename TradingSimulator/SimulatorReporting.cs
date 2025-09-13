using BacklashDTOs;
using TradingSimulator.TestObjects;

namespace TradingSimulator.Simulator
{
    /// <summary>
    /// Provides comprehensive reporting and analysis functionality for trading simulator operations.
    /// This class serves as the core engine for detecting velocity discrepancies in market snapshots,
    /// computing rolling observations, generating detailed memos, and coalescing discrepancy data.
    /// It is designed to analyze orderbook flow patterns and identify potential anomalies in market data
    /// that could indicate trading opportunities or data quality issues.
    /// </summary>
    [TestFixture]
    public class SimulatorReporting
    {
        private readonly string _cacheDirectory = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");

        /// <summary>
        /// Detects velocity discrepancies in market snapshots by analyzing orderbook flow patterns.
        /// This method compares observed orderbook velocity changes against expected flows derived from
        /// rolling window analysis, identifying potential anomalies that may indicate trading opportunities
        /// or data quality issues. The analysis includes spike suppression, leakage tolerance, and
        /// various statistical thresholds to reduce false positives.
        /// </summary>
        /// <param name="s">The list of market snapshots to analyze for velocity discrepancies.</param>
        /// <param name="CreateLog">Whether to create log files for detected discrepancies.</param>
        /// <param name="relativeSlack">Relative slack factor for threshold calculation (default: 1.5).</param>
        /// <param name="averagingWindowMin">Time window in minutes for rolling average calculations (default: 5.0).</param>
        /// <param name="minAbsChangeToFlag">Minimum absolute change in cents to consider for flagging (default: 500).</param>
        /// <param name="shortIntervalExponent">Exponent for scaling thresholds on short intervals (default: 0.5).</param>
        /// <param name="gapThresholdMin">Maximum gap in minutes to allow in rolling window (default: 1.5).</param>
        /// <param name="leakageFactor">Factor for calculating leakage tolerance from previous intervals (default: 0.05).</param>
        /// <param name="winsorPct">Percentage for Winsorization of extreme values in expected flows (default: 0.2).</param>
        /// <param name="useMaxMagnitudeForThreshold">Whether to use maximum magnitude for threshold calculation (default: false).</param>
        /// <param name="ratioSlack">Slack factor for ratio-based discrepancy detection (default: 0.5).</param>
        /// <param name="hardFlagOnSignFlip">Whether to hard-flag sign flips in velocity (default: true).</param>
        /// <param name="suppressSpikes">Whether to suppress spike-driven discrepancies (default: true).</param>
        /// <param name="domRatio">Dominance ratio threshold for spike suppression (default: 0.90).</param>
        /// <param name="gapShare">Gap share threshold for spike suppression (default: 0.85).</param>
        /// <param name="edgeMult">Edge multiplier for spike suppression at window boundaries (default: 8.0).</param>
        /// <param name="ratioFloor">Minimum ratio floor in $/min to avoid tiny-exp blowups (default: 0.5).</param>
        /// <returns>A list of PricePoint objects representing detected velocity discrepancies.</returns>
        public List<PricePoint> DetectVelocityDiscrepancies(
    List<MarketSnapshot> s,
    bool CreateLog,
    double relativeSlack = 1.5,
    double averagingWindowMin = 5.0,
    int minAbsChangeToFlag = 500,
    double shortIntervalExponent = 0.5,
    double gapThresholdMin = 1.5,
    double leakageFactor = 0.05,
    double winsorPct = 0.2,
    bool useMaxMagnitudeForThreshold = false,
    double ratioSlack = 0.5,
    bool hardFlagOnSignFlip = true,
    bool suppressSpikes = true,
    double domRatio = 0.90,
    double gapShare = 0.85,
    double edgeMult = 8.0,
    double ratioFloor = 0.5 // $/min
)
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
                    ComputeRollingObservations(curr, s, averagingWindowMin, gapThresholdMin);

                if (windowDt <= 0) continue;

                var (expYesRateWin, expNoRateWin) =
                    ComputeExpectedFlowsWinsorized(instYesList, instNoList, winsorPct);

                double scale = (windowDt < averagingWindowMin)
                    ? Math.Pow(averagingWindowMin / windowDt, shortIntervalExponent)
                    : 1.0;

                // leakage tolerance
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

                // base magnitudes
                double magYes = useMaxMagnitudeForThreshold
                                ? Math.Max(Math.Abs(expYesRateWin), Math.Abs(rollObsYes5m))
                                : Math.Abs(expYesRateWin);
                double magNo = useMaxMagnitudeForThreshold
                                ? Math.Max(Math.Abs(expNoRateWin), Math.Abs(rollObsNo5m))
                                : Math.Abs(expNoRateWin);

                double floorRate = (double)minAbsChangeToFlag / (100.0 * averagingWindowMin);

                double thrYes = Math.Max(magYes * relativeSlack * scale + toleranceYes, floorRate);
                double thrNo = Math.Max(magNo  * relativeSlack * scale + toleranceNo, floorRate);

                // residual tests
                double residYes = Math.Abs(rollObsYes5m - expYesRateWin);
                double residNo = Math.Abs(rollObsNo5m  - expNoRateWin);

                // Gate ratio rule by ratioFloor to avoid tiny-exp blowups
                bool ratioHitYes = (Math.Abs(expYesRateWin) >= Math.Max(floorRate, ratioFloor)) &&
                                   (Math.Abs(rollObsYes5m / (expYesRateWin == 0 ? 1e-9 : expYesRateWin) - 1.0) > ratioSlack);
                bool ratioHitNo = (Math.Abs(expNoRateWin)  >= Math.Max(floorRate, ratioFloor)) &&
                                   (Math.Abs(rollObsNo5m  / (expNoRateWin  == 0 ? 1e-9 : expNoRateWin)  - 1.0) > ratioSlack);

                bool signFlipYes = hardFlagOnSignFlip &&
                                   (Math.Sign(rollObsYes5m) != Math.Sign(expYesRateWin)) &&
                                   (Math.Max(Math.Abs(rollObsYes5m), Math.Abs(expYesRateWin)) >= floorRate);
                bool signFlipNo = hardFlagOnSignFlip &&
                                   (Math.Sign(rollObsNo5m) != Math.Sign(expNoRateWin)) &&
                                   (Math.Max(Math.Abs(rollObsNo5m), Math.Abs(expNoRateWin))  >= floorRate);

                bool discYes = (residYes > thrYes) || ratioHitYes || signFlipYes;
                bool discNo = (residNo  > thrNo)  || ratioHitNo  || signFlipNo;

                if (!(discYes || discNo)) continue;

                // spike-driven suppression (parametric)
                bool IsSpikeSuppressed(List<double> flows, double obsRate, double expRate)
                {
                    if (!suppressSpikes || flows == null || flows.Count == 0) return false;

                    double net = flows.Sum();
                    if (Math.Abs(net) < 1e-12) return false;

                    int m = 0; double maxAbs = double.NegativeInfinity;
                    for (int k = 0; k < flows.Count; k++)
                    {
                        double v = Math.Abs(flows[k]);
                        if (v > maxAbs) { maxAbs = v; m = k; }
                    }
                    bool ruleA = (Math.Abs(flows[m]) / Math.Abs(net) >= domRatio) &&
                                 (Math.Sign(flows[m]) == Math.Sign(net));

                    double avgRaw = flows.Average();
                    double gapCents = Math.Abs((avgRaw - expRate) * windowDt * 100.0);
                    double residCents = Math.Abs((obsRate - expRate) * windowDt * 100.0);
                    bool ruleB = residCents > 0 && (gapCents / residCents >= gapShare);

                    var absSorted = flows.Select(Math.Abs).OrderBy(x => x).ToArray();
                    double medianAbs = absSorted[absSorted.Length / 2];
                    bool isEdge = (m == 0 || m == flows.Count - 1);
                    bool ruleC = isEdge && (Math.Abs(flows[m]) >= edgeMult * (medianAbs <= 1e-12 ? 1.0 : medianAbs));

                    return ruleA || ruleB || ruleC;
                }

                bool suppressYes = discYes && IsSpikeSuppressed(instYesList, rollObsYes5m, expYesRateWin);
                bool suppressNo = discNo  && IsSpikeSuppressed(instNoList, rollObsNo5m, expNoRateWin);

                if (suppressYes && !discNo) continue;
                if (suppressNo  && !discYes) continue;
                if (suppressYes && suppressNo) continue;

                double expDepthYesC = expYesRateWin * windowDt * 100.0;
                double expDepthNoC = expNoRateWin  * windowDt * 100.0;

                var lastSnapshots = s.Skip(Math.Max(0, i - 6)).Take(7).ToList();
                string memo = GenerateDiscrepancyMemo(
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
                if (CreateLog)
                    AppendDiscrepancyLog(curr.MarketTicker ?? "UnknownMarket", memo);
            }

            return outPts;
        }

        /// <summary>
        /// Computes expected flows for Yes and No sides using Winsorized averaging to handle outliers.
        /// This method applies Winsorization (clipping extreme values at percentile bounds) to the
        /// per-minute flow data before averaging, providing a robust estimate of expected orderbook
        /// velocity that is less sensitive to extreme outliers.
        /// </summary>
        /// <param name="perMinuteYes">List of per-minute flow rates for the Yes side.</param>
        /// <param name="perMinuteNo">List of per-minute flow rates for the No side.</param>
        /// <param name="winsorPct">Percentage of data to Winsorize at each tail (e.g., 0.2 for 20% at each end).</param>
        /// <returns>A tuple containing the Winsorized average flows for Yes and No sides.</returns>
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

        /// <summary>
        /// Computes rolling observations of orderbook velocity over a specified time window.
        /// This method analyzes the sequence of market snapshots to calculate observed flow rates
        /// for both Yes and No sides, handling gaps in data and providing detailed window statistics.
        /// The method builds a rolling window of snapshots within the averaging period, excluding
        /// snapshots separated by gaps larger than the threshold.
        /// </summary>
        /// <param name="curr">The current market snapshot being analyzed.</param>
        /// <param name="fullSnapshots">The complete list of market snapshots for the analysis period.</param>
        /// <param name="averagingWindowMin">The time window in minutes for rolling calculations.</param>
        /// <param name="gapThresholdMin">Maximum allowed gap in minutes between snapshots in the window.</param>
        /// <returns>A tuple containing:
        /// - RollObsYes5m: Rolling observed flow rate for Yes side ($/min)
        /// - RollObsNo5m: Rolling observed flow rate for No side ($/min)
        /// - RollObsYesWin: Winsorized rolling observation for Yes (same as RollObsYes5m in this implementation)
        /// - RollObsNoWin: Winsorized rolling observation for No (same as RollObsNo5m in this implementation)
        /// - WindowDt: Total duration of the rolling window in minutes
        /// - GapNote: Description of any gaps detected in the window
        /// - WindowDy: Total depth change for Yes side in cents
        /// - WindowDn: Total depth change for No side in cents
        /// - InstYesList: List of instantaneous flow rates for Yes side
        /// - InstNoList: List of instantaneous flow rates for No side</returns>
        private (double RollObsYes5m, double RollObsNo5m, double RollObsYesWin, double RollObsNoWin,
                  double WindowDt, string GapNote, double WindowDy, double WindowDn,
                  List<double> InstYesList, List<double> InstNoList)
            ComputeRollingObservations(MarketSnapshot curr, List<MarketSnapshot> fullSnapshots,
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

        /// <summary>
        /// Appends a discrepancy log entry to a timestamped log file for the specified market.
        /// This method creates or appends to a log file containing detailed information about
        /// detected velocity discrepancies, including the market ticker and comprehensive memo data.
        /// Log files are stored in the configured cache directory with timestamp-based filenames.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market where the discrepancy was detected.</param>
        /// <param name="memo">The detailed memo containing discrepancy analysis and context information.</param>
        private void AppendDiscrepancyLog(string marketTicker, string memo)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string logFilename = $"discrepancies_{timestamp}.log";
            string logPath = Path.Combine(_cacheDirectory, logFilename);
            File.AppendAllText(logPath, $"Market: {marketTicker}\n{memo}\n\n");
        }

        /// <summary>
        /// Finds the oldest snapshot index that should be included in the rolling window analysis.
        /// This method searches backwards from the current index to find the earliest snapshot
        /// that falls within the averaging window and doesn't exceed gap thresholds between
        /// consecutive snapshots. The method ensures the rolling window contains continuous
        /// data suitable for velocity calculations.
        /// </summary>
        /// <param name="currIndex">The index of the current snapshot being analyzed.</param>
        /// <param name="s">The complete list of market snapshots.</param>
        /// <param name="averagingWindowMin">The time window in minutes for the analysis.</param>
        /// <param name="gapThresholdMin">Maximum allowed gap in minutes between snapshots.</param>
        /// <returns>The index of the oldest snapshot to include in the rolling window.</returns>
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

        /// <summary>
        /// Generates a comprehensive memo string containing detailed analysis of the velocity discrepancy.
        /// This method creates a formatted report that includes discrepancy type, window statistics,
        /// rolling observations, expected flows, computational details, and recent snapshot data.
        /// The memo provides complete context for understanding why a particular snapshot was flagged
        /// as having a velocity discrepancy.
        /// </summary>
        /// <param name="curr">The current market snapshot where the discrepancy was detected.</param>
        /// <param name="lastSnapshots">The last 7 snapshots for context and detailed analysis.</param>
        /// <param name="fullSnapshots">The complete list of snapshots for the analysis period.</param>
        /// <param name="averagingWindowMin">The averaging window duration in minutes.</param>
        /// <param name="gapThresholdMin">The gap threshold used in the analysis.</param>
        /// <param name="windowDt">The actual duration of the rolling window in minutes.</param>
        /// <param name="windowDy">The total depth change for Yes side in cents.</param>
        /// <param name="windowDn">The total depth change for No side in cents.</param>
        /// <param name="expYesRateWin">Expected flow rate for Yes side ($/min).</param>
        /// <param name="expNoRateWin">Expected flow rate for No side ($/min).</param>
        /// <param name="rollObsYes5m">Rolling observed flow rate for Yes side ($/min).</param>
        /// <param name="rollObsNo5m">Rolling observed flow rate for No side ($/min).</param>
        /// <param name="rollObsYesWin">Winsorized rolling observation for Yes side.</param>
        /// <param name="rollObsNoWin">Winsorized rolling observation for No side.</param>
        /// <param name="gapNote">Note about any gaps detected in the rolling window.</param>
        /// <param name="scale">Scaling factor applied for short intervals.</param>
        /// <param name="shortIntervalExponent">Exponent used for short interval scaling.</param>
        /// <param name="zeroVelYesDisc">Flag for zero velocity discrepancy on Yes side.</param>
        /// <param name="zeroVelNoDisc">Flag for zero velocity discrepancy on No side.</param>
        /// <param name="toleranceYes">Leakage tolerance for Yes side ($/min).</param>
        /// <param name="toleranceNo">Leakage tolerance for No side ($/min).</param>
        /// <param name="leakageFactor">Factor used to calculate leakage tolerance.</param>
        /// <param name="expDepthYesC">Expected depth change for Yes side in cents.</param>
        /// <param name="expDepthNoC">Expected depth change for No side in cents.</param>
        /// <returns>A formatted string containing the complete discrepancy analysis memo.</returns>
        private string GenerateDiscrepancyMemo(
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
                "YesDepth(�)",
                "Your Rolling No ($/min)",
                "Orderbook Flow No (inst $/min)",
                "NoDepth(�)");
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
            sb.AppendLine($"  Window: ?Yes(�)={windowDy:0.##} ? {rollObsYes5m:0.##} $/min, ?No(�)={windowDn:0.##} ? {rollObsNo5m:0.##} $/min (Sdt={windowDt:0.##} min)");

            sb.AppendLine($"  Detection Expected: Yes={expYesRateWin:0.##} $/min, No={expNoRateWin:0.##} $/min (ExpScale={scale:0.##})");

            sb.AppendLine("Computation details:");
            sb.AppendLine($"    Short-Window Scale = (Window Minutes<{averagingWindowMin:0.##}? ( {averagingWindowMin:0.##}/Window Minutes )^{shortIntervalExponent:0.###} : 1) = {scale:0.######}");
            sb.AppendLine($"    Expected ?Depth (�) = S(Expected Flow�min)�100 = {(expYesRateWin * windowDt):0.######}*100 = {expDepthYesC:0.##} c");
            sb.AppendLine($"    Observed ?Depth (�) = window? = {windowDy:0.##}");
            sb.AppendLine($"    Edge Tolerance (Leakage Factor={leakageFactor:0.###}): Yes={toleranceYes:0.##} $/min, No={toleranceNo:0.##} $/min");
            sb.AppendLine($"MID={(curr.BestYesBid + curr.BestYesAsk) / 2.0:0.##}");

            if (Math.Abs(rollObsYes5m - expYesRateWin) < 1e-6 && Math.Abs(rollObsNo5m - expNoRateWin) < 1e-6)
                sb.AppendLine("Resolution hint: Observed==Expected; window integration with winsorization muted edge spikes.");

            return sb.ToString();
        }

        /// <summary>
        /// Coalesces overlapping or canceling discrepancy points to reduce noise in the output.
        /// This method analyzes pairs of discrepancy points and removes those that cancel each other out
        /// (i.e., opposite velocity changes that sum to near zero) or are too close together in time.
        /// The process helps eliminate false positives and redundant signals from the discrepancy detection.
        /// </summary>
        /// <param name="pts">The list of discrepancy points to coalesce.</param>
        /// <param name="maxPairGapMinutes">Maximum time gap in minutes between points to consider for coalescing.</param>
        /// <param name="maxVelocityDiff">Maximum velocity difference to consider points as canceling each other.</param>
        /// <returns>A filtered list of discrepancy points with overlapping/canceling points removed.</returns>
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

                    int yi = roll.IndexOf("?", StringComparison.Ordinal);
                    int yiEnd = roll.IndexOf("$", yi + 1, StringComparison.Ordinal);
                    int wi = roll.LastIndexOf("?", StringComparison.Ordinal);
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
