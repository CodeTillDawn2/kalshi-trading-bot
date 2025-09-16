using BacklashDTOs;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TradingSimulator.Simulator
{
    /// <summary>
    /// Configuration class for velocity discrepancy analysis parameters.
    /// This class contains all configurable parameters for the SimulatorReporting analysis engine,
    /// allowing fine-tuning of detection thresholds, performance settings, and output configuration.
    /// Parameters can be loaded from appsettings.json under the "SimulatorReporting" section.
    /// </summary>
    public class AnalysisConfiguration
    {
        /// <summary>
        /// Gets or sets the relative slack factor for threshold calculations.
        /// </summary>
        public double RelativeSlack { get; set; } = 1.5;
        /// <summary>
        /// Gets or sets the averaging window in minutes for rolling calculations.
        /// </summary>
        public double AveragingWindowMin { get; set; } = 5.0;
        /// <summary>
        /// Gets or sets the minimum absolute change to flag as a discrepancy.
        /// </summary>
        public int MinAbsChangeToFlag { get; set; } = 500;
        /// <summary>
        /// Gets or sets the exponent for short interval scaling.
        /// </summary>
        public double ShortIntervalExponent { get; set; } = 0.5;
        /// <summary>
        /// Gets or sets the gap threshold in minutes for rolling windows.
        /// </summary>
        public double GapThresholdMin { get; set; } = 1.5;
        /// <summary>
        /// Gets or sets the leakage factor for tolerance calculations.
        /// </summary>
        public double LeakageFactor { get; set; } = 0.05;
        /// <summary>
        /// Gets or sets the Winsorization percentage for outlier handling.
        /// </summary>
        public double WinsorPct { get; set; } = 0.2;
        /// <summary>
        /// Gets or sets whether to use maximum magnitude for threshold calculations.
        /// </summary>
        public bool UseMaxMagnitudeForThreshold { get; set; } = false;
        /// <summary>
        /// Gets or sets the ratio slack for discrepancy detection.
        /// </summary>
        public double RatioSlack { get; set; } = 0.5;
        /// <summary>
        /// Gets or sets whether to hard flag on sign flips.
        /// </summary>
        public bool HardFlagOnSignFlip { get; set; } = true;
        /// <summary>
        /// Gets or sets whether to suppress spike-driven discrepancies.
        /// </summary>
        public bool SuppressSpikes { get; set; } = true;
        /// <summary>
        /// Gets or sets the dominance ratio for spike suppression.
        /// </summary>
        public double DomRatio { get; set; } = 0.90;
        /// <summary>
        /// Gets or sets the gap share for spike suppression.
        /// </summary>
        public double GapShare { get; set; } = 0.85;
        /// <summary>
        /// Gets or sets the edge multiplier for spike detection.
        /// </summary>
        public double EdgeMult { get; set; } = 8.0;
        /// <summary>
        /// Gets or sets the ratio floor for calculations.
        /// </summary>
        public double RatioFloor { get; set; } = 0.5;
        /// <summary>
        /// Gets or sets the directory for log files.
        /// </summary>
        public string LogDirectory { get; set; } = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");
        /// <summary>
        /// Gets or sets the prefix for log file names.
        /// </summary>
        public string LogFilePrefix { get; set; } = "discrepancies";
        /// <summary>
        /// Gets or sets whether to enable parallel processing.
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = false;
        /// <summary>
        /// Gets or sets the maximum degree of parallelism.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
        /// <summary>
        /// Gets or sets whether to enable performance metrics collection.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = false;
    }

    /// <summary>
    /// Performance metrics class for tracking velocity discrepancy analysis execution timing and statistics.
    /// This class provides detailed performance information about the analysis process, including
    /// timing for different computational phases and counts of processed data points.
    /// </summary>
    public class PerformanceMetrics
    {
        /// <summary>
        /// Gets or sets the total time spent on analysis.
        /// </summary>
        public TimeSpan TotalAnalysisTime { get; set; }
        /// <summary>
        /// Gets or sets the time spent on rolling observations.
        /// </summary>
        public TimeSpan RollingObservationsTime { get; set; }
        /// <summary>
        /// Gets or sets the time spent on computing expected flows.
        /// </summary>
        public TimeSpan ExpectedFlowsTime { get; set; }
        /// <summary>
        /// Gets or sets the time spent on spike suppression.
        /// </summary>
        public TimeSpan SpikeSuppressionTime { get; set; }
        /// <summary>
        /// Gets or sets the number of snapshots processed.
        /// </summary>
        public int SnapshotsProcessed { get; set; }
        /// <summary>
        /// Gets or sets the number of discrepancies detected.
        /// </summary>
        public int DiscrepanciesDetected { get; set; }

        /// <summary>
        /// Returns a string representation of the performance metrics.
        /// </summary>
        /// <returns>A formatted string containing performance metrics.</returns>
        public override string ToString()
        {
            return $"Analysis Time: {TotalAnalysisTime.TotalMilliseconds:F2}ms, " +
                   $"Rolling Obs: {RollingObservationsTime.TotalMilliseconds:F2}ms, " +
                   $"Expected Flows: {ExpectedFlowsTime.TotalMilliseconds:F2}ms, " +
                   $"Spike Suppression: {SpikeSuppressionTime.TotalMilliseconds:F2}ms, " +
                   $"Snapshots: {SnapshotsProcessed}, Discrepancies: {DiscrepanciesDetected}";
        }
    }

    /// <summary>
    /// Provides comprehensive reporting and analysis functionality for trading simulator operations.
    /// This class serves as the core engine for detecting velocity discrepancies in market snapshots,
    /// computing rolling observations, generating detailed memos, and coalescing discrepancy data.
    /// It is designed to analyze orderbook flow patterns and identify potential anomalies in market data
    /// that could indicate trading opportunities or data quality issues.
    ///
    /// Key Features:
    /// - Configurable analysis parameters via AnalysisConfiguration class
    /// - Async processing support for better performance with large datasets
    /// - Parallel processing for multiple market analyses
    /// - Comprehensive input validation for data integrity
    /// - Performance metrics collection and timing analysis
    /// - Configurable logging with automatic directory creation
    /// - Spike suppression to reduce false positives
    /// - Robust error handling and recovery
    /// </summary>
    public class SimulatorReporting
    {
        private readonly AnalysisConfiguration _config;
        private readonly PerformanceMetrics _metrics;

        /// <summary>
        /// Initializes a new instance of the SimulatorReporting class.
        /// </summary>
        /// <param name="config">The analysis configuration to use. If null, default configuration is used.</param>
        public SimulatorReporting(AnalysisConfiguration config = null)
        {
            _config = config ?? new AnalysisConfiguration();
            _metrics = new PerformanceMetrics();
        }

        /// <summary>
        /// Gets the current performance metrics for the analysis.
        /// </summary>
        /// <returns>The performance metrics instance.</returns>
        public PerformanceMetrics GetPerformanceMetrics() => _metrics;

        /// <summary>
        /// Validates the integrity of market snapshot data before processing.
        /// </summary>
        /// <param name="snapshots">The list of market snapshots to validate.</param>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        private void ValidateMarketSnapshots(List<MarketSnapshot> snapshots)
        {
            if (snapshots == null)
                throw new ArgumentNullException(nameof(snapshots), "Market snapshots list cannot be null.");

            if (snapshots.Count == 0)
                throw new ArgumentException("Market snapshots list cannot be empty.", nameof(snapshots));

            if (snapshots.Count < 2)
                throw new ArgumentException("At least 2 market snapshots are required for analysis.", nameof(snapshots));

            for (int i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot == null)
                    throw new ArgumentException($"Market snapshot at index {i} is null.", nameof(snapshots));

                if (string.IsNullOrWhiteSpace(snapshot.MarketTicker))
                    throw new ArgumentException($"Market snapshot at index {i} has invalid or missing market ticker.", nameof(snapshots));

                if (snapshot.Timestamp == default)
                    throw new ArgumentException($"Market snapshot at index {i} has invalid timestamp.", nameof(snapshots));

                if (snapshot.BestYesBid <= 0 || snapshot.BestYesAsk <= 0)
                    throw new ArgumentException($"Market snapshot at index {i} has invalid bid/ask prices.", nameof(snapshots));

                if (snapshot.TotalOrderbookDepth_Yes < 0 || snapshot.TotalOrderbookDepth_No < 0)
                    throw new ArgumentException($"Market snapshot at index {i} has negative orderbook depth.", nameof(snapshots));
            }

            // Check for chronological order
            for (int i = 1; i < snapshots.Count; i++)
            {
                if (snapshots[i].Timestamp <= snapshots[i - 1].Timestamp)
                    throw new ArgumentException($"Market snapshots are not in chronological order at index {i}.", nameof(snapshots));
            }
        }

        /// <summary>
        /// Validates analysis configuration parameters.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when configuration validation fails.</exception>
        private void ValidateConfiguration()
        {
            if (_config.RelativeSlack <= 0)
                throw new ArgumentException("RelativeSlack must be greater than 0.", nameof(_config.RelativeSlack));

            if (_config.AveragingWindowMin <= 0)
                throw new ArgumentException("AveragingWindowMin must be greater than 0.", nameof(_config.AveragingWindowMin));

            if (_config.MinAbsChangeToFlag < 0)
                throw new ArgumentException("MinAbsChangeToFlag must be non-negative.", nameof(_config.MinAbsChangeToFlag));

            if (_config.ShortIntervalExponent < 0)
                throw new ArgumentException("ShortIntervalExponent must be non-negative.", nameof(_config.ShortIntervalExponent));

            if (_config.GapThresholdMin < 0)
                throw new ArgumentException("GapThresholdMin must be non-negative.", nameof(_config.GapThresholdMin));

            if (_config.LeakageFactor < 0 || _config.LeakageFactor > 1)
                throw new ArgumentException("LeakageFactor must be between 0 and 1.", nameof(_config.LeakageFactor));

            if (_config.WinsorPct < 0 || _config.WinsorPct > 0.5)
                throw new ArgumentException("WinsorPct must be between 0 and 0.5.", nameof(_config.WinsorPct));

            if (_config.RatioSlack < 0)
                throw new ArgumentException("RatioSlack must be non-negative.", nameof(_config.RatioSlack));

            if (_config.DomRatio < 0 || _config.DomRatio > 1)
                throw new ArgumentException("DomRatio must be between 0 and 1.", nameof(_config.DomRatio));

            if (_config.GapShare < 0 || _config.GapShare > 1)
                throw new ArgumentException("GapShare must be between 0 and 1.", nameof(_config.GapShare));

            if (_config.EdgeMult <= 0)
                throw new ArgumentException("EdgeMult must be greater than 0.", nameof(_config.EdgeMult));

            if (_config.RatioFloor < 0)
                throw new ArgumentException("RatioFloor must be non-negative.", nameof(_config.RatioFloor));

            if (_config.MaxDegreeOfParallelism < 1)
                throw new ArgumentException("MaxDegreeOfParallelism must be at least 1.", nameof(_config.MaxDegreeOfParallelism));
        }

        /// <summary>
        /// Asynchronously detects velocity discrepancies in market snapshots by analyzing orderbook flow patterns.
        /// This method provides the same functionality as the synchronous version but runs on a background thread
        /// to avoid blocking the calling thread. Ideal for large datasets or when responsiveness is critical.
        /// </summary>
        /// <param name="s">The list of market snapshots to analyze for velocity discrepancies.</param>
        /// <param name="CreateLog">Whether to create log files for detected discrepancies.</param>
        /// <returns>A task that represents the asynchronous operation, containing a list of PricePoint objects representing detected velocity discrepancies.</returns>
        public async Task<List<PricePoint>> DetectVelocityDiscrepanciesAsync(
            List<MarketSnapshot> s,
            bool CreateLog)
        {
            return await Task.Run(() => DetectVelocityDiscrepancies(s, CreateLog));
        }

        /// <summary>
        /// Detects velocity discrepancies in market snapshots by analyzing orderbook flow patterns.
        /// This method compares observed orderbook velocity changes against expected flows derived from
        /// rolling window analysis, identifying potential anomalies that may indicate trading opportunities
        /// or data quality issues. The analysis includes spike suppression, leakage tolerance, and
        /// various statistical thresholds to reduce false positives.
        ///
        /// Features:
        /// - Automatic parallel processing for large datasets (>100 snapshots)
        /// - Comprehensive input validation and error handling
        /// - Performance metrics collection when enabled
        /// - Configurable logging with automatic directory creation
        /// - Robust spike suppression to reduce false positives
        /// </summary>
        /// <param name="s">The list of market snapshots to analyze for velocity discrepancies.</param>
        /// <param name="CreateLog">Whether to create log files for detected discrepancies.</param>
        /// <returns>A list of PricePoint objects representing detected velocity discrepancies.</returns>
        public List<PricePoint> DetectVelocityDiscrepancies(
            List<MarketSnapshot> s,
            bool CreateLog)
        {
            // Validate inputs and configuration
            ValidateMarketSnapshots(s);
            ValidateConfiguration();

            var stopwatch = Stopwatch.StartNew();
            var outPts = new List<PricePoint>();
            _metrics.SnapshotsProcessed = s.Count;

            if (_config.EnableParallelProcessing && s.Count > 100)
            {
                // Use parallel processing for large datasets
                var options = new ParallelOptions { MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism };
                var results = new ConcurrentBag<PricePoint>();

                Parallel.For(1, s.Count, options, i =>
                {
                    var point = ProcessSnapshot(i, s, CreateLog);
                    if (point != null)
                        results.Add(point);
                });

                outPts = results.ToList();
            }
            else
            {
                // Sequential processing
                for (int i = 1; i < s.Count; i++)
                {
                    var point = ProcessSnapshot(i, s, CreateLog);
                    if (point != null)
                        outPts.Add(point);
                }
            }

            stopwatch.Stop();
            _metrics.TotalAnalysisTime = stopwatch.Elapsed;
            _metrics.DiscrepanciesDetected = outPts.Count;

            return outPts;
        }

        private PricePoint ProcessSnapshot(int i, List<MarketSnapshot> s, bool CreateLog)
        {
            var curr = s[i];
            var prev = s[i - 1];
            if (!curr.ChangeMetricsMature) return null;

            double dtMin = (curr.Timestamp - prev.Timestamp).TotalMinutes;
            if (dtMin <= 0) return null;

            var rollingStopwatch = Stopwatch.StartNew();
            var (rollObsYes5m, rollObsNo5m, rollObsYesWin, rollObsNoWin,
                  windowDt, gapNote, windowDy, windowDn,
                  instYesList, instNoList) =
                ComputeRollingObservations(curr, s, _config.AveragingWindowMin, _config.GapThresholdMin);
            rollingStopwatch.Stop();
            _metrics.RollingObservationsTime += rollingStopwatch.Elapsed;

            if (windowDt <= 0) return null;

            var expectedStopwatch = Stopwatch.StartNew();
            var (expYesRateWin, expNoRateWin) =
                ComputeExpectedFlowsWinsorized(instYesList, instNoList, _config.WinsorPct);
            expectedStopwatch.Stop();
            _metrics.ExpectedFlowsTime += expectedStopwatch.Elapsed;

            double scale = (windowDt < _config.AveragingWindowMin)
                ? Math.Pow(_config.AveragingWindowMin / windowDt, _config.ShortIntervalExponent)
                : 1.0;

            // leakage tolerance
            double toleranceYes = 0.0, toleranceNo = 0.0;
            int oldestIncludedIndex = FindOldestIncludedIndex(i, s, _config.AveragingWindowMin, _config.GapThresholdMin);
            if (oldestIncludedIndex > 1)
            {
                var preSnap = s[oldestIncludedIndex - 1];
                var prePrev = s[oldestIncludedIndex - 2];
                double preDt = (preSnap.Timestamp - prePrev.Timestamp).TotalMinutes;
                if (preDt > 0 && preDt <= _config.GapThresholdMin)
                {
                    double preDy = preSnap.TotalOrderbookDepth_Yes - prePrev.TotalOrderbookDepth_Yes;
                    double preDn = preSnap.TotalOrderbookDepth_No  - prePrev.TotalOrderbookDepth_No;
                    double preVelYes = preDy / (100.0 * preDt);
                    double preVelNo = preDn / (100.0 * preDt);
                    toleranceYes = Math.Abs(preVelYes) * _config.LeakageFactor;
                    toleranceNo  = Math.Abs(preVelNo)  * _config.LeakageFactor;
                }
            }

            // base magnitudes
            double magYes = _config.UseMaxMagnitudeForThreshold
                            ? Math.Max(Math.Abs(expYesRateWin), Math.Abs(rollObsYes5m))
                            : Math.Abs(expYesRateWin);
            double magNo = _config.UseMaxMagnitudeForThreshold
                            ? Math.Max(Math.Abs(expNoRateWin), Math.Abs(rollObsNo5m))
                            : Math.Abs(expNoRateWin);

            double floorRate = (double)_config.MinAbsChangeToFlag / (100.0 * _config.AveragingWindowMin);

            double thrYes = Math.Max(magYes * _config.RelativeSlack * scale + toleranceYes, floorRate);
            double thrNo = Math.Max(magNo  * _config.RelativeSlack * scale + toleranceNo, floorRate);

            // residual tests
            double residYes = Math.Abs(rollObsYes5m - expYesRateWin);
            double residNo = Math.Abs(rollObsNo5m  - expNoRateWin);

            // Gate ratio rule by ratioFloor to avoid tiny-exp blowups
            bool ratioHitYes = (Math.Abs(expYesRateWin) >= Math.Max(floorRate, _config.RatioFloor)) &&
                               (Math.Abs(rollObsYes5m / (expYesRateWin == 0 ? 1e-9 : expYesRateWin) - 1.0) > _config.RatioSlack);
            bool ratioHitNo = (Math.Abs(expNoRateWin)  >= Math.Max(floorRate, _config.RatioFloor)) &&
                               (Math.Abs(rollObsNo5m  / (expNoRateWin  == 0 ? 1e-9 : expNoRateWin)  - 1.0) > _config.RatioSlack);

            bool signFlipYes = _config.HardFlagOnSignFlip &&
                               (Math.Sign(rollObsYes5m) != Math.Sign(expYesRateWin)) &&
                               (Math.Max(Math.Abs(rollObsYes5m), Math.Abs(expYesRateWin)) >= floorRate);
            bool signFlipNo = _config.HardFlagOnSignFlip &&
                               (Math.Sign(rollObsNo5m) != Math.Sign(expNoRateWin)) &&
                               (Math.Max(Math.Abs(rollObsNo5m), Math.Abs(expNoRateWin))  >= floorRate);

            bool discYes = (residYes > thrYes) || ratioHitYes || signFlipYes;
            bool discNo = (residNo  > thrNo)  || ratioHitNo  || signFlipNo;

            if (!(discYes || discNo)) return null;

            // spike-driven suppression (parametric)
            var spikeStopwatch = Stopwatch.StartNew();
            bool suppressYes = discYes && IsSpikeSuppressed(instYesList, rollObsYes5m, expYesRateWin);
            bool suppressNo = discNo  && IsSpikeSuppressed(instNoList, rollObsNo5m, expNoRateWin);
            spikeStopwatch.Stop();
            _metrics.SpikeSuppressionTime += spikeStopwatch.Elapsed;

            if (suppressYes && !discNo) return null;
            if (suppressNo  && !discYes) return null;
            if (suppressYes && suppressNo) return null;

            double expDepthYesC = expYesRateWin * windowDt * 100.0;
            double expDepthNoC = expNoRateWin  * windowDt * 100.0;

            var lastSnapshots = s.Skip(Math.Max(0, i - 6)).Take(7).ToList();
            string memo = GenerateDiscrepancyMemo(
                curr, lastSnapshots, s,
                _config.AveragingWindowMin, _config.GapThresholdMin,
                windowDt, windowDy, windowDn,
                expYesRateWin, expNoRateWin,
                rollObsYes5m, rollObsNo5m,
                rollObsYesWin, rollObsNoWin,
                gapNote, scale, _config.ShortIntervalExponent,
                false, false,
                toleranceYes, toleranceNo, _config.LeakageFactor,
                expDepthYesC, expDepthNoC);

            var pricePoint = new PricePoint(curr.Timestamp, (curr.BestYesBid + curr.BestYesAsk) / 2.0, memo);
            if (CreateLog)
                AppendDiscrepancyLog(curr.MarketTicker ?? "UnknownMarket", memo);

            return pricePoint;
        }

        /// <summary>
        /// Determines if a discrepancy should be suppressed due to spike-driven behavior.
        /// </summary>
        private bool IsSpikeSuppressed(List<double> flows, double obsRate, double expRate)
        {
            if (!_config.SuppressSpikes || flows == null || flows.Count == 0) return false;

            double net = flows.Sum();
            if (Math.Abs(net) < 1e-12) return false;

            int m = 0; double maxAbs = double.NegativeInfinity;
            for (int k = 0; k < flows.Count; k++)
            {
                double v = Math.Abs(flows[k]);
                if (v > maxAbs) { maxAbs = v; m = k; }
            }
            bool ruleA = (Math.Abs(flows[m]) / Math.Abs(net) >= _config.DomRatio) &&
                         (Math.Sign(flows[m]) == Math.Sign(net));

            double avgRaw = flows.Average();
            double gapCents = Math.Abs((avgRaw - expRate) * _config.AveragingWindowMin * 100.0);
            double residCents = Math.Abs((obsRate - expRate) * _config.AveragingWindowMin * 100.0);
            bool ruleB = residCents > 0 && (gapCents / residCents >= _config.GapShare);

            var absSorted = flows.Select(Math.Abs).OrderBy(x => x).ToArray();
            double medianAbs = absSorted[absSorted.Length / 2];
            bool isEdge = (m == 0 || m == flows.Count - 1);
            bool ruleC = isEdge && (Math.Abs(flows[m]) >= _config.EdgeMult * (medianAbs <= 1e-12 ? 1.0 : medianAbs));

            return ruleA || ruleB || ruleC;
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
        /// Log files are stored in the configured log directory with configurable naming.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market where the discrepancy was detected.</param>
        /// <param name="memo">The detailed memo containing discrepancy analysis and context information.</param>
        private void AppendDiscrepancyLog(string marketTicker, string memo)
        {
            try
            {
                // Ensure log directory exists
                Directory.CreateDirectory(_config.LogDirectory);

                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string logFilename = $"{_config.LogFilePrefix}_{timestamp}.log";
                string logPath = Path.Combine(_config.LogDirectory, logFilename);
                File.AppendAllText(logPath, $"Market: {marketTicker}\n{memo}\n\n");
            }
            catch (Exception ex)
            {
                // Log to console if file logging fails
                Console.WriteLine($"Failed to write discrepancy log: {ex.Message}");
            }
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
                "YesDepth( )",
                "Your Rolling No ($/min)",
                "Orderbook Flow No (inst $/min)",
                "NoDepth( )");
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
            sb.AppendLine($"  Window: ?Yes( )={windowDy:0.##} ? {rollObsYes5m:0.##} $/min, ?No( )={windowDn:0.##} ? {rollObsNo5m:0.##} $/min (Sdt={windowDt:0.##} min)");

            sb.AppendLine($"  Detection Expected: Yes={expYesRateWin:0.##} $/min, No={expNoRateWin:0.##} $/min (ExpScale={scale:0.##})");

            sb.AppendLine("Computation details:");
            sb.AppendLine($"    Short-Window Scale = (Window Minutes<{averagingWindowMin:0.##}? ( {averagingWindowMin:0.##}/Window Minutes )^{shortIntervalExponent:0.###} : 1) = {scale:0.######}");
            sb.AppendLine($"    Expected ?Depth ( ) = S(Expected Flow min) 100 = {(expYesRateWin * windowDt):0.######}*100 = {expDepthYesC:0.##} c");
            sb.AppendLine($"    Observed ?Depth ( ) = window? = {windowDy:0.##}");
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
