using BacklashDTOs;
using BacklashPatterns.PatternDefinitions;
using Microsoft.Extensions.Logging;
using static BacklashPatterns.PatternUtils;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BacklashPatterns
{
    /// <summary>
    /// Configuration class for pattern detection thresholds and settings.
    /// </summary>
    public class PatternDetectionConfig
    {
        /// <summary>
        /// Minimum price change threshold for significance check.
        /// </summary>
        public double SignificancePriceThreshold { get; set; } = 1.0;

        /// <summary>
        /// Minimum volume increase multiplier for context check.
        /// </summary>
        public double VolumeIncreaseMultiplier { get; set; } = 1.1;

        /// <summary>
        /// Initial capacity for patterns array per candle.
        /// </summary>
        public int InitialPatternCapacity { get; set; } = 10;

        /// <summary>
        /// Whether to enable parallel processing for pattern detection.
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = false;

        /// <summary>
        /// Maximum degree of parallelism for pattern checks.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    }

    /// <summary>
    /// Service for collecting performance metrics during pattern detection.
    /// </summary>
    public class PatternDetectionMetrics
    {
        private readonly Stopwatch _totalStopwatch = new Stopwatch();
        private readonly Dictionary<string, long> _patternCheckTimes = new Dictionary<string, long>();
        private readonly Dictionary<string, int> _patternCounts = new Dictionary<string, int>();
        private int _totalCandlesProcessed;
        private int _totalPatternsFound;

        /// <summary>
        /// Starts overall detection timing.
        /// </summary>
        public void StartDetection() => _totalStopwatch.Start();

        /// <summary>
        /// Stops overall detection timing.
        /// </summary>
        public void StopDetection() => _totalStopwatch.Stop();

        /// <summary>
        /// Records time taken for a specific pattern check.
        /// </summary>
        public void RecordPatternCheckTime(string patternName, long ticks)
        {
            if (!_patternCheckTimes.ContainsKey(patternName))
                _patternCheckTimes[patternName] = 0;
            _patternCheckTimes[patternName] += ticks;
        }

        /// <summary>
        /// Records a pattern detection.
        /// </summary>
        public void RecordPatternFound(string patternName)
        {
            if (!_patternCounts.ContainsKey(patternName))
                _patternCounts[patternName] = 0;
            _patternCounts[patternName]++;
            _totalPatternsFound++;
        }

        /// <summary>
        /// Increments the candle processing count.
        /// </summary>
        public void IncrementCandlesProcessed() => _totalCandlesProcessed++;

        /// <summary>
        /// Gets the performance metrics summary.
        /// </summary>
        public PatternDetectionMetricsSummary GetSummary()
        {
            return new PatternDetectionMetricsSummary
            {
                TotalDetectionTimeMs = _totalStopwatch.ElapsedMilliseconds,
                TotalCandlesProcessed = _totalCandlesProcessed,
                TotalPatternsFound = _totalPatternsFound,
                PatternCheckTimes = new Dictionary<string, long>(_patternCheckTimes),
                PatternCounts = new Dictionary<string, int>(_patternCounts)
            };
        }
    }

    /// <summary>
    /// Summary of pattern detection performance metrics.
    /// </summary>
    public class PatternDetectionMetricsSummary
    {
        public long TotalDetectionTimeMs { get; set; }
        public int TotalCandlesProcessed { get; set; }
        public int TotalPatternsFound { get; set; }
        public Dictionary<string, long> PatternCheckTimes { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, int> PatternCounts { get; set; } = new Dictionary<string, int>();
    }
    /// <summary>
    /// Provides functionality for detecting and filtering candlestick patterns in financial market data.
    /// This static class analyzes arrays of candle data to identify various technical analysis patterns,
    /// applying filtering rules to eliminate redundant or less significant patterns.
    /// </summary>
    /// <remarks>
    /// The class supports detection of single-candle, two-candle, three-candle, four-candle, and five-candle patterns.
    /// Patterns are filtered based on precedence rules to ensure only the most relevant patterns are retained.
    /// </remarks>
    public static class PatternSearch
    {
        /// <summary>
        /// Logger instance for recording pattern detection and filtering activities.
        /// </summary>
        private static ILogger? _logger;

        /// <summary>
        /// Sets the logger instance for this static class.
        /// </summary>
        /// <param name="logger">The logger to use for logging operations.</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Temporary storage for indices of potential patterns during detection process.
        /// Used to track candidate patterns before final filtering.
        /// </summary>
        public static readonly List<int> CandidatePatternIndices = new List<int>();

        /// <summary>
        /// Detects candlestick patterns in the provided price data array (synchronous version for backward compatibility).
        /// </summary>
        /// <param name="prices">Array of candle data containing price and volume information.</param>
        /// <param name="trendLookback">Number of candles to look back for trend analysis and pattern context.</param>
        /// <returns>Dictionary mapping candle indices to lists of detected pattern definitions.</returns>
        public static Dictionary<int, List<PatternDefinition>> DetectPatterns(CandleMids[] prices, int trendLookback)
        {
            var config = new PatternDetectionConfig();
            var metrics = new PatternDetectionMetrics();
            return DetectPatternsAsync(prices, trendLookback, config, metrics).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Detects candlestick patterns in the provided price data array asynchronously.
        /// Analyzes each candle position to identify various technical analysis patterns,
        /// from single-candle formations to complex multi-candle patterns.
        /// </summary>
        /// <param name="prices">Array of candle data containing price and volume information.</param>
        /// <param name="trendLookback">Number of candles to look back for trend analysis and pattern context.</param>
        /// <param name="config">Configuration for pattern detection thresholds and settings.</param>
        /// <param name="metrics">Metrics collector for performance tracking.</param>
        /// <returns>Task containing dictionary mapping candle indices to lists of detected pattern definitions.</returns>
        /// <remarks>
        /// The method performs significance checks to skip insignificant candles and applies
        /// pattern detection algorithms for different candle counts (1-5 candles).
        /// Detected patterns are then filtered to remove redundancies and less significant formations.
        /// </remarks>
        public static async Task<Dictionary<int, List<PatternDefinition>>> DetectPatternsAsync(CandleMids[] prices,
                int trendLookback, PatternDetectionConfig config, PatternDetectionMetrics metrics)
        {
            if (prices == null || prices.Length < 2)
                return new Dictionary<int, List<PatternDefinition>>();

            return await Task.Run(() =>
            {
                metrics.StartDetection();

                Dictionary<int, CandleMetrics> metricsCache = new Dictionary<int, CandleMetrics>();

                var detailedPatterns = new Dictionary<int, List<PatternDefinition>>();
                int initialCapacity = config.InitialPatternCapacity; // Configurable initial size for patterns per candle

            for (int i = 0; i < prices.Length; i++)
            {
                CandidatePatternIndices.Clear();
                var patternsFound = new PatternDefinition[initialCapacity]; // Now stores PatternDefinition objects
                int patternCount = 0;

                var current = prices[i];
                var previous = i > 0 ? prices[i - 1] : null;

                // Significance check to skip insignificant candles
                bool isSignificant = IsPatternSignificant(current, previous) ?? false;
                if (!isSignificant) continue;

                // 1-Candle Patterns
                if (i >= 1)
                {
                    bool hasContext = previous != null && (Math.Abs(current.Close - previous.Close) >= config.SignificancePriceThreshold ||
                                      current.Volume > previous.Volume * config.VolumeIncreaseMultiplier);

                    if (hasContext)
                    {
                        // RickshawMan check
                        var rickshawMan = CheckPatternWithMetrics(() => RickshawManPattern.IsPattern(metricsCache, i, trendLookback, prices), "RickshawMan", metrics);
                        if (rickshawMan != null)
                            AddPattern(rickshawMan, patternsFound, ref patternCount, initialCapacity);

                        // LongLeggedDoji check
                        var longLeggedDoji = CheckPatternWithMetrics(() => LongLeggedDojiPattern.IsPattern(metricsCache, i, trendLookback, prices), "LongLeggedDoji", metrics);
                        if (longLeggedDoji != null)
                            AddPattern(longLeggedDoji, patternsFound, ref patternCount, initialCapacity);

                        // DragonflyDoji check
                        var dragonflyDoji = CheckPatternWithMetrics(() => DragonflyDojiPattern.IsPattern(metricsCache, i, trendLookback, prices), "DragonflyDoji", metrics);
                        if (dragonflyDoji != null)
                            AddPattern(dragonflyDoji, patternsFound, ref patternCount, initialCapacity);

                        // GravestoneDoji check
                        var gravestoneDoji = CheckPatternWithMetrics(() => GravestoneDojiPattern.IsPattern(metricsCache, i, trendLookback, prices), "GravestoneDoji", metrics);
                        if (gravestoneDoji != null)
                            AddPattern(gravestoneDoji, patternsFound, ref patternCount, initialCapacity);
                    }
                }

                var doji = CheckPatternWithMetrics(() => DojiPattern.IsPattern(metricsCache, i, trendLookback, prices), "Doji", metrics);
                if (doji != null)
                    AddPattern(doji, patternsFound, ref patternCount, initialCapacity);

                var hammer = CheckPatternWithMetrics(() => HammerPattern.IsPattern(metricsCache, i, trendLookback, prices), "Hammer", metrics);
                if (hammer != null)
                    AddPattern(hammer, patternsFound, ref patternCount, initialCapacity);

                var hangingMan = CheckPatternWithMetrics(() => HangingManPattern.IsPattern(metricsCache, i, prices, trendLookback), "HangingMan", metrics);
                if (hangingMan != null)
                    AddPattern(hangingMan, patternsFound, ref patternCount, initialCapacity);

                var invertedHammer = CheckPatternWithMetrics(() => InvertedHammerPattern.IsPattern(metricsCache, i, trendLookback, prices), "InvertedHammer", metrics);
                if (invertedHammer != null)
                    AddPattern(invertedHammer, patternsFound, ref patternCount, initialCapacity);

                var shootingStar = CheckPatternWithMetrics(() => ShootingStarPattern.IsPattern(metricsCache, i, trendLookback, prices), "ShootingStar", metrics);
                if (shootingStar != null)
                    AddPattern(shootingStar, patternsFound, ref patternCount, initialCapacity);

                var takuri = CheckPatternWithMetrics(() => TakuriPattern.IsPattern(i, trendLookback, metricsCache, prices), "Takuri", metrics);
                if (takuri != null)
                    AddPattern(takuri, patternsFound, ref patternCount, initialCapacity);

                var spinningTop = CheckPatternWithMetrics(() => SpinningTopPattern.IsPattern(metricsCache, i, trendLookback, prices), "SpinningTop", metrics);
                if (spinningTop != null)
                    AddPattern(spinningTop, patternsFound, ref patternCount, initialCapacity);

                var highWaveCandle = CheckPatternWithMetrics(() => HighWaveCandlePattern.IsPattern(i, trendLookback, prices, metricsCache), "HighWaveCandle", metrics);
                if (highWaveCandle != null)
                    AddPattern(highWaveCandle, patternsFound, ref patternCount, initialCapacity);

                // 2-Candle Patterns
                if (i >= 1)
                {
                    var beltHoldBullish = BeltHoldPattern.IsPattern(i, trendLookback, prices, metricsCache, true);
                    if (beltHoldBullish != null)
                        AddPattern(beltHoldBullish, patternsFound, ref patternCount, initialCapacity);

                    var beltHoldBearish = BeltHoldPattern.IsPattern(i, trendLookback, prices, metricsCache, false);
                    if (beltHoldBearish != null)
                        AddPattern(beltHoldBearish, patternsFound, ref patternCount, initialCapacity);

                    var engulfingBullish = EngulfingPattern.IsPattern(i, metricsCache, prices, trendLookback, true);
                    if (engulfingBullish != null)
                        AddPattern(engulfingBullish, patternsFound, ref patternCount, initialCapacity);

                    var engulfingBearish = EngulfingPattern.IsPattern(i, metricsCache, prices, trendLookback, false);
                    if (engulfingBearish != null)
                        AddPattern(engulfingBearish, patternsFound, ref patternCount, initialCapacity);

                    var closingMarubozuBullish = ClosingMarubozuPattern.IsPattern(i, metricsCache, prices, trendLookback, true);
                    if (closingMarubozuBullish != null)
                        AddPattern(closingMarubozuBullish, patternsFound, ref patternCount, initialCapacity);

                    var closingMarubozuBearish = ClosingMarubozuPattern.IsPattern(i, metricsCache, prices, trendLookback, false);
                    if (closingMarubozuBearish != null)
                        AddPattern(closingMarubozuBearish, patternsFound, ref patternCount, initialCapacity);

                    var counterattackBullish = CounterattackPattern.IsPattern(i, trendLookback, prices, metricsCache, true);
                    if (counterattackBullish != null)
                        AddPattern(counterattackBullish, patternsFound, ref patternCount, initialCapacity);

                    var counterattackBearish = CounterattackPattern.IsPattern(i, trendLookback, prices, metricsCache, false);
                    if (counterattackBearish != null)
                        AddPattern(counterattackBearish, patternsFound, ref patternCount, initialCapacity);

                    var darkCloudCover = DarkCloudCoverPattern.IsPattern(i, prices, trendLookback, metricsCache);
                    if (darkCloudCover != null)
                        AddPattern(darkCloudCover, patternsFound, ref patternCount, initialCapacity);

                    var dojiStar = DojiStarPattern.IsPattern(i, trendLookback, prices, metricsCache);
                    if (dojiStar != null)
                        AddPattern(dojiStar, patternsFound, ref patternCount, initialCapacity);

                    var haramiBullish = HaramiPattern.IsPattern(i, trendLookback, metricsCache, prices, true);
                    if (haramiBullish != null)
                        AddPattern(haramiBullish, patternsFound, ref patternCount, initialCapacity);

                    var haramiBearish = HaramiPattern.IsPattern(i, trendLookback, metricsCache, prices, false);
                    if (haramiBearish != null)
                        AddPattern(haramiBearish, patternsFound, ref patternCount, initialCapacity);

                    var homingPigeon = HomingPigeonPattern.IsPattern(i, trendLookback, metricsCache, prices);
                    if (homingPigeon != null)
                        AddPattern(homingPigeon, patternsFound, ref patternCount, initialCapacity);

                    var inNeck = InNeckPattern.IsPattern(i, trendLookback, metricsCache, prices);
                    if (inNeck != null)
                        AddPattern(inNeck, patternsFound, ref patternCount, initialCapacity);

                    var kickingBullish = KickingPattern.IsPattern(i, true, trendLookback, metricsCache, prices);
                    if (kickingBullish != null)
                        AddPattern(kickingBullish, patternsFound, ref patternCount, initialCapacity);

                    var kickingBearish = KickingPattern.IsPattern(i, false, trendLookback, metricsCache, prices);
                    if (kickingBearish != null)
                        AddPattern(kickingBearish, patternsFound, ref patternCount, initialCapacity);

                    var kickingByLengthBullish = KickingByLengthPattern.IsPattern(i, trendLookback, true, metricsCache, prices);
                    if (kickingByLengthBullish != null)
                        AddPattern(kickingByLengthBullish, patternsFound, ref patternCount, initialCapacity);

                    var kickingByLengthBearish = KickingByLengthPattern.IsPattern(i, trendLookback, false, metricsCache, prices);
                    if (kickingByLengthBearish != null)
                        AddPattern(kickingByLengthBearish, patternsFound, ref patternCount, initialCapacity);

                    var longLineCandleBullish = LongLineCandlePattern.IsPattern(metricsCache, true, i, trendLookback, prices);
                    if (longLineCandleBullish != null)
                        AddPattern(longLineCandleBullish, patternsFound, ref patternCount, initialCapacity);

                    var longLineCandleBearish = LongLineCandlePattern.IsPattern(metricsCache, false, i, trendLookback, prices);
                    if (longLineCandleBearish != null)
                        AddPattern(longLineCandleBearish, patternsFound, ref patternCount, initialCapacity);

                    var marubozuBullish = MarubozuPattern.IsPattern(metricsCache, i, trendLookback, true, prices);
                    if (marubozuBullish != null)
                        AddPattern(marubozuBullish, patternsFound, ref patternCount, initialCapacity);

                    var marubozuBearish = MarubozuPattern.IsPattern(metricsCache, i, trendLookback, false, prices);
                    if (marubozuBearish != null)
                        AddPattern(marubozuBearish, patternsFound, ref patternCount, initialCapacity);

                    var onNeck = OnNeckPattern.IsPattern(i, trendLookback, metricsCache, prices);
                    if (onNeck != null)
                        AddPattern(onNeck, patternsFound, ref patternCount, initialCapacity);

                    var piercing = PiercingPattern.IsPattern(i, trendLookback, metricsCache, prices);
                    if (piercing != null)
                        AddPattern(piercing, patternsFound, ref patternCount, initialCapacity);

                    var separatingLinesBullish = SeparatingLinesPattern.IsPattern(metricsCache, i, trendLookback, true, prices);
                    if (separatingLinesBullish != null)
                        AddPattern(separatingLinesBullish, patternsFound, ref patternCount, initialCapacity);

                    var separatingLinesBearish = SeparatingLinesPattern.IsPattern(metricsCache, i, trendLookback, false, prices);
                    if (separatingLinesBearish != null)
                        AddPattern(separatingLinesBearish, patternsFound, ref patternCount, initialCapacity);

                    var thrustingBullish = ThrustingPattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                    if (thrustingBullish != null)
                        AddPattern(thrustingBullish, patternsFound, ref patternCount, initialCapacity);

                    var thrustingBearish = ThrustingPattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                    if (thrustingBearish != null)
                        AddPattern(thrustingBearish, patternsFound, ref patternCount, initialCapacity);

                    // 3-Candle Patterns
                    if (i >= 2)
                    {
                        var abandonedBabyBullish = AbandonedBabyPattern.IsPattern(i, trendLookback, metricsCache, prices, true);
                        if (abandonedBabyBullish != null)
                            AddPattern(abandonedBabyBullish, patternsFound, ref patternCount, initialCapacity);

                        var abandonedBabyBearish = AbandonedBabyPattern.IsPattern(i, trendLookback, metricsCache, prices, false);
                        if (abandonedBabyBearish != null)
                            AddPattern(abandonedBabyBearish, patternsFound, ref patternCount, initialCapacity);

                        var morningDojiStar = MorningDojiStarPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (morningDojiStar != null)
                            AddPattern(morningDojiStar, patternsFound, ref patternCount, initialCapacity);

                        var twoCrows = TwoCrowsPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (twoCrows != null)
                            AddPattern(twoCrows, patternsFound, ref patternCount, initialCapacity);

                        var morningStar = MorningStarPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (morningStar != null)
                            AddPattern(morningStar, patternsFound, ref patternCount, initialCapacity);

                        var unique3River = Unique3RiverPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (unique3River != null)
                            AddPattern(unique3River, patternsFound, ref patternCount, initialCapacity);

                        var downsideGapThreeMethods = DownsideGapThreeMethodsPattern.IsPattern(i, prices, trendLookback, metricsCache);
                        if (downsideGapThreeMethods != null)
                            AddPattern(downsideGapThreeMethods, patternsFound, ref patternCount, initialCapacity);

                        var eveningDojiStar = EveningDojiStarPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (eveningDojiStar != null)
                            AddPattern(eveningDojiStar, patternsFound, ref patternCount, initialCapacity);

                        var eveningStar = EveningStarPattern.IsPattern(i, prices, trendLookback, metricsCache);
                        if (eveningStar != null)
                            AddPattern(eveningStar, patternsFound, ref patternCount, initialCapacity);

                        var modifiedHikkakeBullish = ModifiedHikkakePattern.IsPattern(i, true, prices, metricsCache, trendLookback);
                        if (modifiedHikkakeBullish != null)
                            AddPattern(modifiedHikkakeBullish, patternsFound, ref patternCount, initialCapacity);

                        var modifiedHikkakeBearish = ModifiedHikkakePattern.IsPattern(i, false, prices, metricsCache, trendLookback);
                        if (modifiedHikkakeBearish != null)
                            AddPattern(modifiedHikkakeBearish, patternsFound, ref patternCount, initialCapacity);

                        var hikkakeBullish = HikkakePattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                        if (hikkakeBullish != null)
                            AddPattern(hikkakeBullish, patternsFound, ref patternCount, initialCapacity);

                        var hikkakeBearish = HikkakePattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                        if (hikkakeBearish != null)
                            AddPattern(hikkakeBearish, patternsFound, ref patternCount, initialCapacity);

                        var identicalThreeCrows = IdenticalThreeCrowsPattern.IsPattern(i, prices, trendLookback, metricsCache);
                        if (identicalThreeCrows != null)
                            AddPattern(identicalThreeCrows, patternsFound, ref patternCount, initialCapacity);

                        var stickSandwichBullish = StickSandwichPattern.IsPattern(i, true, prices, metricsCache, trendLookback);
                        if (stickSandwichBullish != null)
                            AddPattern(stickSandwichBullish, patternsFound, ref patternCount, initialCapacity);

                        var stickSandwichBearish = StickSandwichPattern.IsPattern(i, false, prices, metricsCache, trendLookback);
                        if (stickSandwichBearish != null)
                            AddPattern(stickSandwichBearish, patternsFound, ref patternCount, initialCapacity);

                        var tasukiGapBullish = TasukiGapPattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                        if (tasukiGapBullish != null)
                            AddPattern(tasukiGapBullish, patternsFound, ref patternCount, initialCapacity);

                        var tasukiGapBearish = TasukiGapPattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                        if (tasukiGapBearish != null)
                            AddPattern(tasukiGapBearish, patternsFound, ref patternCount, initialCapacity);

                        var tristarBullish = TristarPattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                        if (tristarBullish != null)
                            AddPattern(tristarBullish, patternsFound, ref patternCount, initialCapacity);

                        var tristarBearish = TristarPattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                        if (tristarBearish != null)
                            AddPattern(tristarBearish, patternsFound, ref patternCount, initialCapacity);

                        var threeInsideUp = ThreeInsidePattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                        if (threeInsideUp != null)
                            AddPattern(threeInsideUp, patternsFound, ref patternCount, initialCapacity);

                        var threeInsideDown = ThreeInsidePattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                        if (threeInsideDown != null)
                            AddPattern(threeInsideDown, patternsFound, ref patternCount, initialCapacity);

                        var threeOutsideUp = ThreeOutsidePattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                        if (threeOutsideUp != null)
                            AddPattern(threeOutsideUp, patternsFound, ref patternCount, initialCapacity);

                        var threeOutsideDown = ThreeOutsidePattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                        if (threeOutsideDown != null)
                            AddPattern(threeOutsideDown, patternsFound, ref patternCount, initialCapacity);

                        var shortLineCandleBullish = ShortLineCandlePattern.IsPattern(i, trendLookback, true, metricsCache, prices);
                        if (shortLineCandleBullish != null)
                            AddPattern(shortLineCandleBullish, patternsFound, ref patternCount, initialCapacity);

                        var shortLineCandleBearish = ShortLineCandlePattern.IsPattern(i, trendLookback, false, metricsCache, prices);
                        if (shortLineCandleBearish != null)
                            AddPattern(shortLineCandleBearish, patternsFound, ref patternCount, initialCapacity);

                        var stalledPattern = StalledPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (stalledPattern != null)
                            AddPattern(stalledPattern, patternsFound, ref patternCount, initialCapacity);

                        var threeAdvancingWhiteSoldiers = ThreeAdvancingWhiteSoldiersPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (threeAdvancingWhiteSoldiers != null)
                            AddPattern(threeAdvancingWhiteSoldiers, patternsFound, ref patternCount, initialCapacity);

                        var threeBlackCrows = ThreeBlackCrowsPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (threeBlackCrows != null)
                            AddPattern(threeBlackCrows, patternsFound, ref patternCount, initialCapacity);

                        var threeStarsInTheSouth = ThreeStarsInTheSouthPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (threeStarsInTheSouth != null)
                            AddPattern(threeStarsInTheSouth, patternsFound, ref patternCount, initialCapacity);

                        var upsideGapTwoCrows = UpsideGapTwoCrowsPattern.IsPattern(i, trendLookback, prices, metricsCache);
                        if (upsideGapTwoCrows != null)
                            AddPattern(upsideGapTwoCrows, patternsFound, ref patternCount, initialCapacity);

                        var upsideDownsideGapThreeMethodsBullish = UpsideDownsideGapThreeMethodsPattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                        if (upsideDownsideGapThreeMethodsBullish != null)
                            AddPattern(upsideDownsideGapThreeMethodsBullish, patternsFound, ref patternCount, initialCapacity);

                        var upsideDownsideGapThreeMethodsBearish = UpsideDownsideGapThreeMethodsPattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                        if (upsideDownsideGapThreeMethodsBearish != null)
                            AddPattern(upsideDownsideGapThreeMethodsBearish, patternsFound, ref patternCount, initialCapacity);

                        var upDownGapSideBySideWhiteLinesBullish = UpDownGapSideBySideWhiteLinesPattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                        if (upDownGapSideBySideWhiteLinesBullish != null)
                            AddPattern(upDownGapSideBySideWhiteLinesBullish, patternsFound, ref patternCount, initialCapacity);

                        var upDownGapSideBySideWhiteLinesBearish = UpDownGapSideBySideWhiteLinesPattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                        if (upDownGapSideBySideWhiteLinesBearish != null)
                            AddPattern(upDownGapSideBySideWhiteLinesBearish, patternsFound, ref patternCount, initialCapacity);

                        // 4-Candle Patterns
                        if (i >= 3)
                        {
                            var concealingBabySwallow = ConcealingBabySwallowPattern.IsPattern(i, prices, metricsCache, trendLookback);
                            if (concealingBabySwallow != null)
                                AddPattern(concealingBabySwallow, patternsFound, ref patternCount, initialCapacity);

                            var threeLineStrikeBullish = ThreeLineStrikePattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                            if (threeLineStrikeBullish != null)
                                AddPattern(threeLineStrikeBullish, patternsFound, ref patternCount, initialCapacity);

                            var threeLineStrikeBearish = ThreeLineStrikePattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                            if (threeLineStrikeBearish != null)
                                AddPattern(threeLineStrikeBearish, patternsFound, ref patternCount, initialCapacity);

                            // 5-Candle Patterns
                            if (i >= 4)
                            {
                                var breakawayBullish = BreakawayPattern.IsPattern(i, prices, metricsCache, true, trendLookback);
                                if (breakawayBullish != null)
                                    AddPattern(breakawayBullish, patternsFound, ref patternCount, initialCapacity);

                                var breakawayBearish = BreakawayPattern.IsPattern(i, prices, metricsCache, false, trendLookback);
                                if (breakawayBearish != null)
                                    AddPattern(breakawayBearish, patternsFound, ref patternCount, initialCapacity);

                                var ladderBottom = LadderBottomPattern.IsPattern(i, trendLookback, prices, metricsCache);
                                if (ladderBottom != null)
                                    AddPattern(ladderBottom, patternsFound, ref patternCount, initialCapacity);

                                var matHoldBullish = MatHoldPattern.IsPattern(i, trendLookback, true, prices, metricsCache);
                                if (matHoldBullish != null)
                                    AddPattern(matHoldBullish, patternsFound, ref patternCount, initialCapacity);

                                var matHoldBearish = MatHoldPattern.IsPattern(i, trendLookback, false, prices, metricsCache);
                                if (matHoldBearish != null)
                                    AddPattern(matHoldBearish, patternsFound, ref patternCount, initialCapacity);

                                var risingFallingThreeMethods = RisingFallingThreeMethodsPattern.IsPattern(i, trendLookback, prices, metricsCache);
                                if (risingFallingThreeMethods != null)
                                    AddPattern(risingFallingThreeMethods, patternsFound, ref patternCount, initialCapacity);
                            }
                        }
                    }
                }

                if (patternCount > 0)
                {
                    var filteredPatterns = FilterPatterns(patternsFound, patternCount, prices);
                    if (filteredPatterns.Count > 0)
                        detailedPatterns[i] = filteredPatterns;
                }

                metrics.IncrementCandlesProcessed();
            }

            metrics.StopDetection();
            return detailedPatterns;
        });
        }

        private static void AddPattern(PatternDefinition pattern, PatternDefinition[] patternsFound, ref int count, int capacity)
        {
            if (count >= capacity) ResizeArrays(ref patternsFound, ref capacity);
            patternsFound[count] = pattern;
            count++;
        }

        private static void ResizeArrays(ref PatternDefinition[] patternsFound, ref int capacity)
        {
            capacity *= 2;
            Array.Resize(ref patternsFound, capacity);
        }

        /// <summary>
        /// Helper method to check a pattern with timing and metrics recording.
        /// </summary>
        private static PatternDefinition? CheckPatternWithMetrics(Func<PatternDefinition?> patternCheck, string patternName, PatternDetectionMetrics metrics)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = patternCheck();
            stopwatch.Stop();

            metrics.RecordPatternCheckTime(patternName, stopwatch.ElapsedTicks);

            if (result != null)
            {
                metrics.RecordPatternFound(patternName);
            }

            return result;
        }

        /// <summary>
        /// Filters detected patterns to remove redundancies and less significant formations.
        /// Applies a comprehensive set of exclusion rules based on pattern precedence and specificity.
        /// </summary>
        /// <param name="patterns">Array of detected pattern definitions to filter.</param>
        /// <param name="patternCount">Number of valid patterns in the array.</param>
        /// <param name="candles">Array of candle data for context information.</param>
        /// <returns>List of filtered patterns with redundancies removed.</returns>
        /// <remarks>
        /// The filtering process groups patterns by their last candle index and applies
        /// hierarchical exclusion rules where more specific patterns take precedence over
        /// general ones. Replacement statistics are logged for analysis.
        /// </remarks>
        private static List<PatternDefinition> FilterPatterns(PatternDefinition[] patterns, int patternCount, CandleMids[] candles)
        {
            var filteredPatterns = new List<PatternDefinition>(patternCount);
            var patternsByCandle = new Dictionary<int, List<PatternDefinition>>();
            // Dictionary to track replacements: Pattern -> ReplacedBy -> Count
            var replacementCounts = new Dictionary<string, Dictionary<string, int>>();

            // Group patterns by last candle index
            for (int i = 0; i < patternCount; i++)
            {
                var pattern = patterns[i];
                int candleIndex = pattern.Candles.Last();
                if (!patternsByCandle.ContainsKey(candleIndex))
                    patternsByCandle[candleIndex] = new List<PatternDefinition>();
                patternsByCandle[candleIndex].Add(pattern);
            }

            foreach (var kvp in patternsByCandle)
            {
                var candlePatterns = kvp.Value;
                int index = kvp.Key;

                string market = index < candles.Length ? candles[index].MarketTicker ?? "Unknown Market" : "Unknown Market";
                string timestamp = index < candles.Length ? candles[index].Timestamp.ToString("yyyy-MM-dd HH:mm:ss") : "Unknown Timestamp";

                _logger?.LogInformation($"Index {index} ({market}, {timestamp}): Patterns before filter: {string.Join(", ", candlePatterns.Select(p => p.Name))}");

                // Define all pattern presence flags (unchanged)
                bool hasDoji = candlePatterns.Any(p => p.Name == "Doji");
                bool hasDragonflyDoji = candlePatterns.Any(p => p.Name == "DragonflyDoji");
                bool hasGravestoneDoji = candlePatterns.Any(p => p.Name == "GravestoneDoji");
                bool hasLongLeggedDoji = candlePatterns.Any(p => p.Name == "LongLeggedDoji");
                bool hasRickshawMan = candlePatterns.Any(p => p.Name == "RickshawMan");
                bool hasHammer = candlePatterns.Any(p => p.Name == "Hammer");
                bool hasHangingMan = candlePatterns.Any(p => p.Name == "HangingMan");
                bool hasInvertedHammer = candlePatterns.Any(p => p.Name == "InvertedHammer");
                bool hasShootingStar = candlePatterns.Any(p => p.Name == "ShootingStar");
                bool hasTakuri = candlePatterns.Any(p => p.Name == "Takuri");
                bool hasSpinningTop = candlePatterns.Any(p => p.Name == "SpinningTop");
                bool hasHighWaveCandle = candlePatterns.Any(p => p.Name == "HighWaveCandle");
                bool hasClosingMarubozuBullish = candlePatterns.Any(p => p.Name == "ClosingMarubozu_Bullish");
                bool hasClosingMarubozuBearish = candlePatterns.Any(p => p.Name == "ClosingMarubozu_Bearish");
                bool hasLongLineCandleBullish = candlePatterns.Any(p => p.Name == "LongLineCandle_Bullish");
                bool hasLongLineCandleBearish = candlePatterns.Any(p => p.Name == "LongLineCandle_Bearish");
                bool hasMarubozuBullish = candlePatterns.Any(p => p.Name == "Marubozu_Bullish");
                bool hasMarubozuBearish = candlePatterns.Any(p => p.Name == "Marubozu_Bearish");
                bool hasShortLineCandleBullish = candlePatterns.Any(p => p.Name == "ShortLineCandle_Bullish");
                bool hasShortLineCandleBearish = candlePatterns.Any(p => p.Name == "ShortLineCandle_Bearish");
                bool hasStickSandwichBearish = candlePatterns.Any(p => p.Name == "StickSandwich_Bearish");
                bool hasStickSandwichBullish = candlePatterns.Any(p => p.Name == "StickSandwich_Bullish");
                bool hasLadderBottom = candlePatterns.Any(p => p.Name == "LadderBottom");
                bool hasMatHoldBullish = candlePatterns.Any(p => p.Name == "MatHold_Bullish");
                bool hasMatHoldBearish = candlePatterns.Any(p => p.Name == "MatHold_Bearish");
                bool hasRisingFallingThreeMethods = candlePatterns.Any(p => p.Name == "RisingFallingThreeMethods");
                bool hasDarkCloudCover = candlePatterns.Any(p => p.Name == "DarkCloudCover");
                bool hasPiercingPattern = candlePatterns.Any(p => p.Name == "Piercing");
                bool hasBullishEngulfing = candlePatterns.Any(p => p.Name == "BullishEngulfing");
                bool hasKickingBullish = candlePatterns.Any(p => p.Name == "Kicking_Bullish");
                bool hasKickingBearish = candlePatterns.Any(p => p.Name == "Kicking_Bearish");
                bool hasKickingByLengthBullish = candlePatterns.Any(p => p.Name == "KickingByLength_Bullish");
                bool hasKickingByLengthBearish = candlePatterns.Any(p => p.Name == "KickingByLength_Bearish");
                bool hasTasukiGapBullish = candlePatterns.Any(p => p.Name == "TasukiGap_Bullish");
                bool hasTasukiGapBearish = candlePatterns.Any(p => p.Name == "TasukiGap_Bearish");
                bool hasThrustingBullish = candlePatterns.Any(p => p.Name == "Thrusting_Bullish");
                bool hasThrustingBearish = candlePatterns.Any(p => p.Name == "Thrusting_Bearish");
                bool hasThreeLineStrikeBullish = candlePatterns.Any(p => p.Name == "ThreeLineStrike_Bullish");
                bool hasThreeLineStrikeBearish = candlePatterns.Any(p => p.Name == "ThreeLineStrike_Bearish");
                bool hasThreeBlackCrows = candlePatterns.Any(p => p.Name == "ThreeBlackCrows");
                bool hasThreeAdvancingWhiteSoldiers = candlePatterns.Any(p => p.Name == "ThreeAdvancingWhiteSoldiers");
                bool hasDownsideGapThreeMethods = candlePatterns.Any(p => p.Name == "DownsideGapThreeMethods");
                bool hasModifiedHikkakeBullish = candlePatterns.Any(p => p.Name == "ModifiedHikkake_Bullish");
                bool hasModifiedHikkakeBearish = candlePatterns.Any(p => p.Name == "ModifiedHikkake_Bearish");
                bool hasAnyDojiVariant = hasDragonflyDoji || hasGravestoneDoji || hasLongLeggedDoji || hasRickshawMan;
                bool hasAnyDirectionalPattern = hasHammer || hasHangingMan || hasInvertedHammer || hasShootingStar || hasTakuri ||
                                              hasClosingMarubozuBullish || hasClosingMarubozuBearish ||
                                              hasLongLineCandleBullish || hasLongLineCandleBearish ||
                                              hasMarubozuBullish || hasMarubozuBearish ||
                                              hasShortLineCandleBullish || hasShortLineCandleBearish;

                foreach (var pattern in candlePatterns)
                {
                    string key = pattern.Name;

                    // Helper method to record a replacement
                    void RecordReplacement(string replacedPattern, string replacedBy)
                    {
                        if (!replacementCounts.ContainsKey(replacedPattern))
                            replacementCounts[replacedPattern] = new Dictionary<string, int>();
                        if (!replacementCounts[replacedPattern].ContainsKey(replacedBy))
                            replacementCounts[replacedPattern][replacedBy] = 0;
                        replacementCounts[replacedPattern][replacedBy]++;
                    }

                    // Apply all exclusion rules and record replacements
                    if (candlePatterns.Any(p => p.Name == "EveningStar") &&
                        key == "Stalled" &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "EveningStar").Candles.Last())
                    {
                        RecordReplacement("Stalled", "EveningStar");
                        continue;
                    }

                    if (hasThreeLineStrikeBearish && key == "ThreeBlackCrows" &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "ThreeLineStrike_Bearish").Candles.Last() - 1)
                    {
                        RecordReplacement("ThreeBlackCrows", "ThreeLineStrike_Bearish");
                        continue;
                    }

                    if (hasThreeLineStrikeBullish && key == "ThreeAdvancingWhiteSoldiers" &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "ThreeLineStrike_Bullish").Candles.Last() - 1)
                    {
                        RecordReplacement("ThreeAdvancingWhiteSoldiers", "ThreeLineStrike_Bullish");
                        continue;
                    }

                    if (key == "UpsideDownsideGapThreeMethods_Bearish" && hasDownsideGapThreeMethods &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "DownsideGapThreeMethods").Candles.Last())
                    {
                        RecordReplacement("UpsideDownsideGapThreeMethods_Bearish", "DownsideGapThreeMethods");
                        continue;
                    }

                    if (key == "UpsideDownsideGapThreeMethods_Bullish" && hasTasukiGapBullish &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "TasukiGap_Bullish").Candles.Last())
                    {
                        RecordReplacement("UpsideDownsideGapThreeMethods_Bullish", "TasukiGap_Bullish");
                        continue;
                    }

                    if (key == "UpsideDownsideGapThreeMethods_Bearish" && hasTasukiGapBearish &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "TasukiGap_Bearish").Candles.Last())
                    {
                        RecordReplacement("UpsideDownsideGapThreeMethods_Bearish", "TasukiGap_Bearish");
                        continue;
                    }

                    if (key == "Engulfing_Bearish" && candlePatterns.Any(p => p.Name == "ThreeOutsideDown" &&
                        p.Candles.Count == 3 && p.Candles[1] == pattern.Candles[0] && p.Candles[2] == pattern.Candles[1]))
                    {
                        RecordReplacement("Engulfing_Bearish", "ThreeOutsideDown");
                        continue;
                    }

                    if (hasDarkCloudCover && key == "Thrusting_Bearish" &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "DarkCloudCover").Candles.Last())
                    {
                        RecordReplacement("Thrusting_Bearish", "DarkCloudCover");
                        continue;
                    }

                    if (hasPiercingPattern && key == "Thrusting_Bullish" &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "Piercing").Candles.Last())
                    {
                        RecordReplacement("Thrusting_Bullish", "Piercing");
                        continue;
                    }

                    if (key == "Engulfing_Bullish" && candlePatterns.Any(p => p.Name == "ThreeOutsideUp" &&
                        p.Candles.Count == 3 && p.Candles[1] == pattern.Candles[0] && p.Candles[2] == pattern.Candles[1]))
                    {
                        RecordReplacement("Engulfing_Bullish", "ThreeOutsideUp");
                        continue;
                    }

                    if (key == "Kicking_Bullish" && hasKickingByLengthBullish)
                    {
                        RecordReplacement("Kicking_Bullish", "KickingByLength_Bullish");
                        continue;
                    }

                    if (key == "Kicking_Bearish" && hasKickingByLengthBearish)
                    {
                        RecordReplacement("Kicking_Bearish", "KickingByLength_Bearish");
                        continue;
                    }

                    if (key == "Hammer" && hasTakuri)
                    {
                        RecordReplacement("Hammer", "Takuri");
                        continue;
                    }

                    if (key == "LongLineCandle_Bullish" && (hasMarubozuBullish || hasClosingMarubozuBullish))
                    {
                        RecordReplacement("LongLineCandle_Bullish", hasMarubozuBullish ? "Marubozu_Bullish" : "ClosingMarubozu_Bullish");
                        continue;
                    }

                    if (key == "LongLineCandle_Bearish" && (hasMarubozuBearish || hasClosingMarubozuBearish))
                    {
                        RecordReplacement("LongLineCandle_Bearish", hasMarubozuBearish ? "Marubozu_Bearish" : "ClosingMarubozu_Bearish");
                        continue;
                    }

                    if (key == "ClosingMarubozu_Bullish" && hasMarubozuBullish)
                    {
                        RecordReplacement("ClosingMarubozu_Bullish", "Marubozu_Bullish");
                        continue;
                    }

                    if (key == "ClosingMarubozu_Bearish" && hasMarubozuBearish)
                    {
                        RecordReplacement("ClosingMarubozu_Bearish", "Marubozu_Bearish");
                        continue;
                    }

                    if (hasTasukiGapBullish && key == "Thrusting_Bullish" &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "TasukiGap_Bullish").Candles.Last())
                    {
                        RecordReplacement("Thrusting_Bullish", "TasukiGap_Bullish");
                        continue;
                    }

                    if (hasTasukiGapBearish && key == "Thrusting_Bearish" &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "TasukiGap_Bearish").Candles.Last())
                    {
                        RecordReplacement("Thrusting_Bearish", "TasukiGap_Bearish");
                        continue;
                    }

                    if ((key == "ShortLineCandle_Bullish" || key == "ShortLineCandle_Bearish") && hasSpinningTop)
                    {
                        RecordReplacement(key, "SpinningTop");
                        continue;
                    }

                    if (key == "SpinningTop" && hasHighWaveCandle)
                    {
                        RecordReplacement("SpinningTop", "HighWaveCandle");
                        continue;
                    }

                    if (hasBullishEngulfing && key == "Piercing" &&
                        pattern.Candles.Last() == candlePatterns.First(p => p.Name == "Engulfing_Bullish").Candles.Last())
                    {
                        RecordReplacement("Piercing", "Engulfing_Bullish");
                        continue;
                    }

                    if (key == "Doji" && hasAnyDojiVariant)
                    {
                        RecordReplacement("Doji", hasDragonflyDoji ? "DragonflyDoji" : hasGravestoneDoji ? "GravestoneDoji" : hasLongLeggedDoji ? "LongLeggedDoji" : "RickshawMan");
                        continue;
                    }

                    if (key == "Doji" && (hasHammer || hasHangingMan || hasInvertedHammer || hasShootingStar || hasTakuri || hasSpinningTop || hasHighWaveCandle))
                    {
                        RecordReplacement("Doji", hasHammer ? "Hammer" : hasHangingMan ? "HangingMan" : hasInvertedHammer ? "InvertedHammer" : hasShootingStar ? "ShootingStar" : hasTakuri ? "Takuri" : hasSpinningTop ? "SpinningTop" : "HighWaveCandle");
                        continue;
                    }

                    if (key == "LongLeggedDoji" && hasRickshawMan)
                    {
                        RecordReplacement("LongLeggedDoji", "RickshawMan");
                        continue;
                    }

                    if (key == "LongLeggedDoji" && (hasDragonflyDoji || hasGravestoneDoji))
                    {
                        RecordReplacement("LongLeggedDoji", hasDragonflyDoji ? "DragonflyDoji" : "GravestoneDoji");
                        continue;
                    }

                    if (key == "LongLeggedDoji" && hasSpinningTop)
                    {
                        RecordReplacement("LongLeggedDoji", "SpinningTop");
                        continue;
                    }

                    if (key == "LongLeggedDoji" && hasHighWaveCandle)
                    {
                        RecordReplacement("LongLeggedDoji", "HighWaveCandle");
                        continue;
                    }

                    if (key == "DragonflyDoji" && hasRickshawMan)
                    {
                        RecordReplacement("DragonflyDoji", "RickshawMan");
                        continue;
                    }

                    if (key == "DragonflyDoji" && (hasHammer || hasTakuri))
                    {
                        RecordReplacement("DragonflyDoji", hasHammer ? "Hammer" : "Takuri");
                        continue;
                    }

                    if (key == "DragonflyDoji" && hasGravestoneDoji)
                    {
                        RecordReplacement("DragonflyDoji", "GravestoneDoji");
                        continue;
                    }

                    if (key == "GravestoneDoji" && hasRickshawMan)
                    {
                        RecordReplacement("GravestoneDoji", "RickshawMan");
                        continue;
                    }

                    if (key == "GravestoneDoji" && hasShootingStar)
                    {
                        RecordReplacement("GravestoneDoji", "ShootingStar");
                        continue;
                    }

                    if (key == "GravestoneDoji" && hasInvertedHammer)
                    {
                        RecordReplacement("GravestoneDoji", "InvertedHammer");
                        continue;
                    }

                    if (key == "GravestoneDoji" && hasDragonflyDoji)
                    {
                        RecordReplacement("GravestoneDoji", "DragonflyDoji");
                        continue;
                    }

                    if (key == "RickshawMan" && (hasHammer || hasTakuri || hasShootingStar || hasInvertedHammer))
                    {
                        RecordReplacement("RickshawMan", hasHammer ? "Hammer" : hasTakuri ? "Takuri" : hasShootingStar ? "ShootingStar" : "InvertedHammer");
                        continue;
                    }

                    if (key == "RickshawMan" && hasHighWaveCandle)
                    {
                        RecordReplacement("RickshawMan", "HighWaveCandle");
                        continue;
                    }

                    if (key == "SpinningTop" && (hasLongLeggedDoji || hasRickshawMan || hasDragonflyDoji || hasGravestoneDoji))
                    {
                        RecordReplacement("SpinningTop", hasLongLeggedDoji ? "LongLeggedDoji" : hasRickshawMan ? "RickshawMan" : hasDragonflyDoji ? "DragonflyDoji" : "GravestoneDoji");
                        continue;
                    }

                    if (key == "HighWaveCandle" && (hasLongLeggedDoji || hasRickshawMan))
                    {
                        RecordReplacement("HighWaveCandle", hasLongLeggedDoji ? "LongLeggedDoji" : "RickshawMan");
                        continue;
                    }

                    if (key == "InvertedHammer" && hasShootingStar)
                    {
                        RecordReplacement("InvertedHammer", "ShootingStar");
                        continue;
                    }

                    if (key == "ShortLineCandle_Bullish" && (hasHammer || hasTakuri || hasClosingMarubozuBullish || hasLongLineCandleBullish || hasMarubozuBullish))
                    {
                        RecordReplacement("ShortLineCandle_Bullish", hasHammer ? "Hammer" : hasTakuri ? "Takuri" : hasClosingMarubozuBullish ? "ClosingMarubozu_Bullish" : hasLongLineCandleBullish ? "LongLineCandle_Bullish" : "Marubozu_Bullish");
                        continue;
                    }

                    if (key == "ShortLineCandle_Bearish" && (hasHangingMan || hasShootingStar || hasClosingMarubozuBearish || hasLongLineCandleBearish || hasMarubozuBearish))
                    {
                        RecordReplacement("ShortLineCandle_Bearish", hasHangingMan ? "HangingMan" : hasShootingStar ? "ShootingStar" : hasClosingMarubozuBearish ? "ClosingMarubozu_Bearish" : hasLongLineCandleBearish ? "LongLineCandle_Bearish" : "Marubozu_Bearish");
                        continue;
                    }

                    filteredPatterns.Add(pattern);
                }

                _logger?.LogInformation($"Index {index} ({market}, {timestamp}): Patterns after filter: {string.Join(", ", filteredPatterns.Where(p => p.Candles.Last() == index).Select(p => p.Name))}");
            }

            // Log CSV summary
            if (_logger != null)
            {
                _logger.LogInformation("Pattern Replacement Summary (CSV):");
                _logger.LogInformation("Pattern,ReplacedPattern,Count");
                foreach (var pattern in replacementCounts)
                {
                    foreach (var replacement in pattern.Value)
                    {
                        _logger.LogInformation($"\"{pattern.Key}\",\"{replacement.Key}\",\"{replacement.Value}\"");
                    }
                }
            }

            return filteredPatterns;
        }
    }


}








