using BacklashDTOs;
using BacklashInterfaces.PerformanceMetrics;
using BacklashPatterns.PatternDefinitions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns
{
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
        /// Configuration key for enabling/disabling performance metrics collection for PatternSearch class.
        /// When disabled, all performance monitoring operations are skipped for better performance.
        /// This affects timing measurements, pattern detection counts, and central performance monitor integration.
        /// Default is true. Can be configured from appsettings.json with key "PatternSearch:EnablePerformanceMetrics".
        /// </summary>
        private static bool _enablePerformanceMetrics = true;

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled for the PatternSearch class.
        /// When disabled, all performance monitoring operations are skipped to improve performance.
        /// This includes timing measurements, pattern detection counts, and central performance monitor integration.
        /// </summary>
        /// <remarks>
        /// Setting this to false will disable:
        /// - Stopwatch timing for pattern detection
        /// - Pattern check time recording
        /// - Pattern found count tracking
        /// - Central performance monitor integration
        /// - Candle processing count increments
        /// </remarks>
        public static bool EnablePerformanceMetrics
        {
            get => _enablePerformanceMetrics;
            set
            {
                _enablePerformanceMetrics = value;
                _logger?.LogInformation("PatternSearch performance metrics collection set to: {Enabled}", value);
            }
        }

        /// <summary>
        /// Sets the logger instance for this static class.
        /// </summary>
        /// <param name="logger">The logger to use for logging operations.</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
            CandleMetrics.SetLogger(logger);
        }

        /// <summary>
        /// Configures performance metrics settings for the PatternSearch class from configuration.
        /// </summary>
        /// <param name="configuration">The configuration instance to read settings from.</param>
        /// <remarks>
        /// Reads the "PatternSearch:EnablePerformanceMetrics" setting from appsettings.json.
        /// If not found, defaults to true.
        /// This method should be called during application startup to configure performance monitoring.
        /// </remarks>
        /// <example>
        /// Usage in Program.cs or Startup.cs:
        /// <code>
        /// PatternSearch.ConfigurePerformanceMetrics(configuration);
        /// </code>
        /// </example>
        public static void ConfigurePerformanceMetrics(IConfiguration configuration)
        {
            if (configuration == null)
            {
                _logger?.LogWarning("Configuration is null, using default performance metrics setting (enabled)");
                return;
            }

            var section = configuration.GetSection(PatternDetectionConfig.SectionName);
            bool originalValue = _enablePerformanceMetrics;
            _enablePerformanceMetrics = section.GetValue("EnablePerformanceMetrics", true);

            if (originalValue != _enablePerformanceMetrics)
            {
                _logger?.LogInformation("PatternSearch performance metrics configured from appsettings.json: {Enabled} (was {Original})",
                    _enablePerformanceMetrics, originalValue);
            }
            else
            {
                _logger?.LogDebug("PatternSearch performance metrics setting: {Enabled}", _enablePerformanceMetrics);
            }
        }

        /// <summary>
        /// Gets the current performance metrics configuration status for the PatternSearch class.
        /// </summary>
        /// <returns>A tuple containing the enabled status and configuration key used.</returns>
        public static (bool IsEnabled, string ConfigurationKey) GetPerformanceMetricsStatus()
        {
            return (_enablePerformanceMetrics, $"{PatternDetectionConfig.SectionName}:EnablePerformanceMetrics");
        }

        /// <summary>
        /// Checks if performance metrics collection is currently enabled for the PatternSearch class.
        /// </summary>
        /// <returns>True if performance metrics are enabled, false otherwise.</returns>
        /// <remarks>
        /// This is a convenience method for checking the current state without accessing the property directly.
        /// </remarks>
        public static bool IsPerformanceMetricsEnabled()
        {
            return _enablePerformanceMetrics;
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
        public static Dictionary<int, List<PatternDefinition>> DetectPatterns(CandleMids[] prices, int trendLookback, BacklashInterfaces.PerformanceMetrics.IPerformanceMonitor? performanceMonitor = null)
        {
            var config = new PatternDetectionConfig();
            var metrics = new PatternDetectionMetrics();
            return DetectPatternsAsync(prices, trendLookback, config, metrics, performanceMonitor).GetAwaiter().GetResult();
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
        /// <param name="performanceMonitor">Optional performance monitor to record metrics centrally.</param>
        /// <returns>Task containing dictionary mapping candle indices to lists of detected pattern definitions.</returns>
        /// <remarks>
        /// The method performs significance checks to skip insignificant candles and applies
        /// pattern detection algorithms for different candle counts (1-5 candles).
        /// Detected patterns are then filtered to remove redundancies and less significant formations.
        /// </remarks>
        public static async Task<Dictionary<int, List<PatternDefinition>>> DetectPatternsAsync(CandleMids[] prices,
                int trendLookback, PatternDetectionConfig config, PatternDetectionMetrics metrics,
                IPerformanceMonitor? performanceMonitor = null)
        {
            if (prices == null || prices.Length < 2)
                return new Dictionary<int, List<PatternDefinition>>();

            return await Task.Run(async () =>
            {
                if (_enablePerformanceMetrics)
                {
                    metrics.StartDetection();
                }

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
                            var rickshawMan = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await RickshawManPattern.IsPatternAsync(metricsCache, i, trendLookback, prices), "RickshawMan", metrics);
                            if (rickshawMan != null)
                                AddPattern(rickshawMan, patternsFound, ref patternCount, initialCapacity);

                            // LongLeggedDoji check
                            var longLeggedDoji = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await LongLeggedDojiPattern.IsPatternAsync(metricsCache, i, trendLookback, prices), "LongLeggedDoji", metrics);
                            if (longLeggedDoji != null)
                                AddPattern(longLeggedDoji, patternsFound, ref patternCount, initialCapacity);

                            // DragonflyDoji check
                            var dragonflyDoji = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await DragonflyDojiPattern.IsPatternAsync(metricsCache, i, trendLookback, prices), "DragonflyDoji", metrics);
                            if (dragonflyDoji != null)
                                AddPattern(dragonflyDoji, patternsFound, ref patternCount, initialCapacity);

                            // GravestoneDoji check
                            var gravestoneDoji = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await GravestoneDojiPattern.IsPatternAsync(metricsCache, i, trendLookback, prices), "gravestoneDoji", metrics);
                            if (gravestoneDoji != null)
                                AddPattern(gravestoneDoji, patternsFound, ref patternCount, initialCapacity);
                        }
                    }

                    var doji = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await DojiPattern.IsPatternAsync(metricsCache, i, trendLookback, prices), "doji", metrics);
                    if (doji != null)
                        AddPattern(doji, patternsFound, ref patternCount, initialCapacity);

                    var hammer = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await HammerPattern.IsPatternAsync(metricsCache, i, trendLookback, prices), "hammer", metrics);
                    if (hammer != null)
                        AddPattern(hammer, patternsFound, ref patternCount, initialCapacity);

                    var hangingMan = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await HangingManPattern.IsPatternAsync(metricsCache, i, prices, trendLookback), "hangingMan", metrics);
                    if (hangingMan != null)
                        AddPattern(hangingMan, patternsFound, ref patternCount, initialCapacity);

                    var invertedHammer = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await InvertedHammerPattern.IsPatternAsync(metricsCache, i, trendLookback, prices), "invertedHammer", metrics);
                    if (invertedHammer != null)
                        AddPattern(invertedHammer, patternsFound, ref patternCount, initialCapacity);

                    var shootingStar = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await ShootingStarPattern.IsPatternAsync(metricsCache, i, trendLookback, prices), "shootingStar", metrics);
                    if (shootingStar != null)
                        AddPattern(shootingStar, patternsFound, ref patternCount, initialCapacity);

                    var takuri = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await TakuriPattern.IsPatternAsync(i, trendLookback, metricsCache, prices), "takuri", metrics);
                    if (takuri != null)
                        AddPattern(takuri, patternsFound, ref patternCount, initialCapacity);

                    var spinningTop = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await SpinningTopPattern.IsPatternAsync(metricsCache, i, trendLookback, prices), "spinningTop", metrics);
                    if (spinningTop != null)
                        AddPattern(spinningTop, patternsFound, ref patternCount, initialCapacity);

                    var highWaveCandle = await CheckPatternWithMetricsAsync(async () => (PatternDefinition?)await HighWaveCandlePattern.IsPatternAsync(i, trendLookback, prices, metricsCache), "highWaveCandle", metrics);
                    if (highWaveCandle != null)
                        AddPattern(highWaveCandle, patternsFound, ref patternCount, initialCapacity);

                    // 2-Candle Patterns
                    if (i >= 1)
                    {
                        var beltHoldBullish = await BeltHoldPattern.IsPatternAsync(i, trendLookback, prices, metricsCache, PatternDirection.Bullish);
                        if (beltHoldBullish != null)
                            AddPattern(beltHoldBullish, patternsFound, ref patternCount, initialCapacity);

                        var beltHoldBearish = await BeltHoldPattern.IsPatternAsync(i, trendLookback, prices, metricsCache, PatternDirection.Bearish);
                        if (beltHoldBearish != null)
                            AddPattern(beltHoldBearish, patternsFound, ref patternCount, initialCapacity);

                        var engulfingBullish = await EngulfingPattern.IsPatternAsync(i, metricsCache, prices, trendLookback, true);
                        if (engulfingBullish != null)
                            AddPattern(engulfingBullish, patternsFound, ref patternCount, initialCapacity);

                        var engulfingBearish = await EngulfingPattern.IsPatternAsync(i, metricsCache, prices, trendLookback, false);
                        if (engulfingBearish != null)
                            AddPattern(engulfingBearish, patternsFound, ref patternCount, initialCapacity);

                        var closingMarubozuBullish = await ClosingMarubozuPattern.IsPatternAsync(i, metricsCache, prices, trendLookback, PatternDirection.Bullish);
                        if (closingMarubozuBullish != null)
                            AddPattern(closingMarubozuBullish, patternsFound, ref patternCount, initialCapacity);

                        var closingMarubozuBearish = await ClosingMarubozuPattern.IsPatternAsync(i, metricsCache, prices, trendLookback, PatternDirection.Bearish);
                        if (closingMarubozuBearish != null)
                            AddPattern(closingMarubozuBearish, patternsFound, ref patternCount, initialCapacity);

                        var counterattackBullish = await CounterattackPattern.IsPatternAsync(i, trendLookback, prices, metricsCache, true);
                        if (counterattackBullish != null)
                            AddPattern(counterattackBullish, patternsFound, ref patternCount, initialCapacity);

                        var counterattackBearish = await CounterattackPattern.IsPatternAsync(i, trendLookback, prices, metricsCache, false);
                        if (counterattackBearish != null)
                            AddPattern(counterattackBearish, patternsFound, ref patternCount, initialCapacity);

                        var darkCloudCover = await DarkCloudCoverPattern.IsPatternAsync(i, prices, trendLookback, metricsCache);
                        if (darkCloudCover != null)
                            AddPattern(darkCloudCover, patternsFound, ref patternCount, initialCapacity);

                        var dojiStar = await DojiStarPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                        if (dojiStar != null)
                            AddPattern(dojiStar, patternsFound, ref patternCount, initialCapacity);

                        var haramiBullish = await HaramiPattern.IsPatternAsync(i, trendLookback, metricsCache, prices, true);
                        if (haramiBullish != null)
                            AddPattern(haramiBullish, patternsFound, ref patternCount, initialCapacity);

                        var haramiBearish = await HaramiPattern.IsPatternAsync(i, trendLookback, metricsCache, prices, false);
                        if (haramiBearish != null)
                            AddPattern(haramiBearish, patternsFound, ref patternCount, initialCapacity);

                        var homingPigeon = await HomingPigeonPattern.IsPatternAsync(i, trendLookback, metricsCache, prices);
                        if (homingPigeon != null)
                            AddPattern(homingPigeon, patternsFound, ref patternCount, initialCapacity);

                        var inNeck = await InNeckPattern.IsPatternAsync(i, trendLookback, metricsCache, prices);
                        if (inNeck != null)
                            AddPattern(inNeck, patternsFound, ref patternCount, initialCapacity);

                        var kickingBullish = await KickingPattern.IsPatternAsync(i, true, trendLookback, metricsCache, prices);
                        if (kickingBullish != null)
                            AddPattern(kickingBullish, patternsFound, ref patternCount, initialCapacity);

                        var kickingBearish = await KickingPattern.IsPatternAsync(i, false, trendLookback, metricsCache, prices);
                        if (kickingBearish != null)
                            AddPattern(kickingBearish, patternsFound, ref patternCount, initialCapacity);

                        var kickingByLengthBullish = await KickingByLengthPattern.IsPatternAsync(i, trendLookback, true, metricsCache, prices);
                        if (kickingByLengthBullish != null)
                            AddPattern(kickingByLengthBullish, patternsFound, ref patternCount, initialCapacity);

                        var kickingByLengthBearish = await KickingByLengthPattern.IsPatternAsync(i, trendLookback, false, metricsCache, prices);
                        if (kickingByLengthBearish != null)
                            AddPattern(kickingByLengthBearish, patternsFound, ref patternCount, initialCapacity);

                        var longLineCandleBullish = await LongLineCandlePattern.IsPatternAsync(metricsCache, true, i, trendLookback, prices);
                        if (longLineCandleBullish != null)
                            AddPattern(longLineCandleBullish, patternsFound, ref patternCount, initialCapacity);

                        var longLineCandleBearish = await LongLineCandlePattern.IsPatternAsync(metricsCache, false, i, trendLookback, prices);
                        if (longLineCandleBearish != null)
                            AddPattern(longLineCandleBearish, patternsFound, ref patternCount, initialCapacity);

                        var marubozuBullish = await MarubozuPattern.IsPatternAsync(metricsCache, i, trendLookback, true, prices);
                        if (marubozuBullish != null)
                            AddPattern(marubozuBullish, patternsFound, ref patternCount, initialCapacity);

                        var marubozuBearish = await MarubozuPattern.IsPatternAsync(metricsCache, i, trendLookback, false, prices);
                        if (marubozuBearish != null)
                            AddPattern(marubozuBearish, patternsFound, ref patternCount, initialCapacity);

                        var onNeck = await OnNeckPattern.IsPatternAsync(i, trendLookback, metricsCache, prices);
                        if (onNeck != null)
                            AddPattern(onNeck, patternsFound, ref patternCount, initialCapacity);

                        var piercing = await PiercingPattern.IsPatternAsync(i, trendLookback, metricsCache, prices);
                        if (piercing != null)
                            AddPattern(piercing, patternsFound, ref patternCount, initialCapacity);

                        var separatingLinesBullish = await SeparatingLinesPattern.IsPatternAsync(metricsCache, i, trendLookback, PatternDirection.Bullish, prices);
                        if (separatingLinesBullish != null)
                            AddPattern(separatingLinesBullish, patternsFound, ref patternCount, initialCapacity);

                        var separatingLinesBearish = await SeparatingLinesPattern.IsPatternAsync(metricsCache, i, trendLookback, PatternDirection.Bearish, prices);
                        if (separatingLinesBearish != null)
                            AddPattern(separatingLinesBearish, patternsFound, ref patternCount, initialCapacity);

                        var thrustingBullish = await ThrustingPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bullish, prices, metricsCache);
                        if (thrustingBullish != null)
                            AddPattern(thrustingBullish, patternsFound, ref patternCount, initialCapacity);

                        var thrustingBearish = await ThrustingPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bearish, prices, metricsCache);
                        if (thrustingBearish != null)
                            AddPattern(thrustingBearish, patternsFound, ref patternCount, initialCapacity);

                        // 3-Candle Patterns
                        if (i >= 2)
                        {
                            var abandonedBabyBullish = await AbandonedBabyPattern.IsPatternAsync(i, trendLookback, metricsCache, prices, PatternDirection.Bullish);
                            if (abandonedBabyBullish != null)
                                AddPattern(abandonedBabyBullish, patternsFound, ref patternCount, initialCapacity);

                            var abandonedBabyBearish = await AbandonedBabyPattern.IsPatternAsync(i, trendLookback, metricsCache, prices, PatternDirection.Bearish);
                            if (abandonedBabyBearish != null)
                                AddPattern(abandonedBabyBearish, patternsFound, ref patternCount, initialCapacity);

                            var morningDojiStar = await MorningDojiStarPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (morningDojiStar != null)
                                AddPattern(morningDojiStar, patternsFound, ref patternCount, initialCapacity);

                            var twoCrows = await TwoCrowsPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (twoCrows != null)
                                AddPattern(twoCrows, patternsFound, ref patternCount, initialCapacity);

                            var morningStar = await MorningStarPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (morningStar != null)
                                AddPattern(morningStar, patternsFound, ref patternCount, initialCapacity);

                            var unique3River = await Unique3RiverPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (unique3River != null)
                                AddPattern(unique3River, patternsFound, ref patternCount, initialCapacity);

                            var downsideGapThreeMethods = await DownsideGapThreeMethodsPattern.IsPatternAsync(i, prices, trendLookback, metricsCache);
                            if (downsideGapThreeMethods != null)
                                AddPattern(downsideGapThreeMethods, patternsFound, ref patternCount, initialCapacity);

                            var eveningDojiStar = await EveningDojiStarPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (eveningDojiStar != null)
                                AddPattern(eveningDojiStar, patternsFound, ref patternCount, initialCapacity);

                            var eveningStar = await EveningStarPattern.IsPatternAsync(i, prices, trendLookback, metricsCache);
                            if (eveningStar != null)
                                AddPattern(eveningStar, patternsFound, ref patternCount, initialCapacity);

                            var modifiedHikkakeBullish = await ModifiedHikkakePattern.IsPatternAsync(i, true, prices, metricsCache, trendLookback);
                            if (modifiedHikkakeBullish != null)
                                AddPattern(modifiedHikkakeBullish, patternsFound, ref patternCount, initialCapacity);

                            var modifiedHikkakeBearish = await ModifiedHikkakePattern.IsPatternAsync(i, false, prices, metricsCache, trendLookback);
                            if (modifiedHikkakeBearish != null)
                                AddPattern(modifiedHikkakeBearish, patternsFound, ref patternCount, initialCapacity);

                            var hikkakeBullish = await HikkakePattern.IsPatternAsync(i, trendLookback, true, prices, metricsCache);
                            if (hikkakeBullish != null)
                                AddPattern(hikkakeBullish, patternsFound, ref patternCount, initialCapacity);

                            var hikkakeBearish = await HikkakePattern.IsPatternAsync(i, trendLookback, false, prices, metricsCache);
                            if (hikkakeBearish != null)
                                AddPattern(hikkakeBearish, patternsFound, ref patternCount, initialCapacity);

                            var identicalThreeCrows = await IdenticalThreeCrowsPattern.IsPatternAsync(i, prices, trendLookback, metricsCache);
                            if (identicalThreeCrows != null)
                                AddPattern(identicalThreeCrows, patternsFound, ref patternCount, initialCapacity);

                            var stickSandwichBullish = await StickSandwichPattern.IsPatternAsync(i, PatternDirection.Bullish, prices, metricsCache, trendLookback);
                            if (stickSandwichBullish != null)
                                AddPattern(stickSandwichBullish, patternsFound, ref patternCount, initialCapacity);

                            var stickSandwichBearish = await StickSandwichPattern.IsPatternAsync(i, PatternDirection.Bearish, prices, metricsCache, trendLookback);
                            if (stickSandwichBearish != null)
                                AddPattern(stickSandwichBearish, patternsFound, ref patternCount, initialCapacity);

                            var tasukiGapBullish = await TasukiGapPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bullish, prices, metricsCache);
                            if (tasukiGapBullish != null)
                                AddPattern(tasukiGapBullish, patternsFound, ref patternCount, initialCapacity);

                            var tasukiGapBearish = await TasukiGapPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bearish, prices, metricsCache);
                            if (tasukiGapBearish != null)
                                AddPattern(tasukiGapBearish, patternsFound, ref patternCount, initialCapacity);

                            var tristarBullish = await TristarPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bullish, prices, metricsCache);
                            if (tristarBullish != null)
                                AddPattern(tristarBullish, patternsFound, ref patternCount, initialCapacity);

                            var tristarBearish = await TristarPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bearish, prices, metricsCache);
                            if (tristarBearish != null)
                                AddPattern(tristarBearish, patternsFound, ref patternCount, initialCapacity);

                            var threeInsideUp = await ThreeInsidePattern.IsPatternAsync(i, trendLookback, PatternDirection.Bullish, prices, metricsCache);
                            if (threeInsideUp != null)
                                AddPattern(threeInsideUp, patternsFound, ref patternCount, initialCapacity);

                            var threeInsideDown = await ThreeInsidePattern.IsPatternAsync(i, trendLookback, PatternDirection.Bearish, prices, metricsCache);
                            if (threeInsideDown != null)
                                AddPattern(threeInsideDown, patternsFound, ref patternCount, initialCapacity);

                            var threeOutsideUp = await ThreeOutsidePattern.IsPatternAsync(i, trendLookback, PatternDirection.Bullish, prices, metricsCache);
                            if (threeOutsideUp != null)
                                AddPattern(threeOutsideUp, patternsFound, ref patternCount, initialCapacity);

                            var threeOutsideDown = await ThreeOutsidePattern.IsPatternAsync(i, trendLookback, PatternDirection.Bearish, prices, metricsCache);
                            if (threeOutsideDown != null)
                                AddPattern(threeOutsideDown, patternsFound, ref patternCount, initialCapacity);

                            var shortLineCandleBullish = await ShortLineCandlePattern.IsPatternAsync(i, trendLookback, PatternDirection.Bullish, metricsCache, prices);
                            if (shortLineCandleBullish != null)
                                AddPattern(shortLineCandleBullish, patternsFound, ref patternCount, initialCapacity);

                            var shortLineCandleBearish = await ShortLineCandlePattern.IsPatternAsync(i, trendLookback, PatternDirection.Bearish, metricsCache, prices);
                            if (shortLineCandleBearish != null)
                                AddPattern(shortLineCandleBearish, patternsFound, ref patternCount, initialCapacity);

                            var stalledPattern = await StalledPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (stalledPattern != null)
                                AddPattern(stalledPattern, patternsFound, ref patternCount, initialCapacity);

                            var threeAdvancingWhiteSoldiers = await ThreeAdvancingWhiteSoldiersPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (threeAdvancingWhiteSoldiers != null)
                                AddPattern(threeAdvancingWhiteSoldiers, patternsFound, ref patternCount, initialCapacity);

                            var threeBlackCrows = await ThreeBlackCrowsPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (threeBlackCrows != null)
                                AddPattern(threeBlackCrows, patternsFound, ref patternCount, initialCapacity);

                            var threeStarsInTheSouth = await ThreeStarsInTheSouthPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (threeStarsInTheSouth != null)
                                AddPattern(threeStarsInTheSouth, patternsFound, ref patternCount, initialCapacity);

                            var upsideGapTwoCrows = await UpsideGapTwoCrowsPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                            if (upsideGapTwoCrows != null)
                                AddPattern(upsideGapTwoCrows, patternsFound, ref patternCount, initialCapacity);

                            var upsideDownsideGapThreeMethodsBullish = await UpsideDownsideGapThreeMethodsPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bullish, prices, metricsCache);
                            if (upsideDownsideGapThreeMethodsBullish != null)
                                AddPattern(upsideDownsideGapThreeMethodsBullish, patternsFound, ref patternCount, initialCapacity);

                            var upsideDownsideGapThreeMethodsBearish = await UpsideDownsideGapThreeMethodsPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bearish, prices, metricsCache);
                            if (upsideDownsideGapThreeMethodsBearish != null)
                                AddPattern(upsideDownsideGapThreeMethodsBearish, patternsFound, ref patternCount, initialCapacity);

                            var upDownGapSideBySideWhiteLinesBullish = await UpDownGapSideBySideWhiteLinesPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bullish, prices, metricsCache);
                            if (upDownGapSideBySideWhiteLinesBullish != null)
                                AddPattern(upDownGapSideBySideWhiteLinesBullish, patternsFound, ref patternCount, initialCapacity);

                            var upDownGapSideBySideWhiteLinesBearish = await UpDownGapSideBySideWhiteLinesPattern.IsPatternAsync(i, trendLookback, PatternDirection.Bearish, prices, metricsCache);
                            if (upDownGapSideBySideWhiteLinesBearish != null)
                                AddPattern(upDownGapSideBySideWhiteLinesBearish, patternsFound, ref patternCount, initialCapacity);

                            // 4-Candle Patterns
                            if (i >= 3)
                            {
                                var concealingBabySwallow = await ConcealingBabySwallowPattern.IsPatternAsync(i, prices, metricsCache, trendLookback);
                                if (concealingBabySwallow != null)
                                    AddPattern(concealingBabySwallow, patternsFound, ref patternCount, initialCapacity);

                                var threeLineStrikeBullish = await ThreeLineStrikePattern.IsPatternAsync(i, trendLookback, PatternDirection.Bullish, prices, metricsCache);
                                if (threeLineStrikeBullish != null)
                                    AddPattern(threeLineStrikeBullish, patternsFound, ref patternCount, initialCapacity);

                                var threeLineStrikeBearish = await ThreeLineStrikePattern.IsPatternAsync(i, trendLookback, PatternDirection.Bearish, prices, metricsCache);
                                if (threeLineStrikeBearish != null)
                                    AddPattern(threeLineStrikeBearish, patternsFound, ref patternCount, initialCapacity);

                                // 5-Candle Patterns
                                if (i >= 4)
                                {
                                    var breakawayBullish = await BreakawayPattern.IsPatternAsync(i, prices, metricsCache, PatternDirection.Bullish, trendLookback);
                                    if (breakawayBullish != null)
                                        AddPattern(breakawayBullish, patternsFound, ref patternCount, initialCapacity);

                                    var breakawayBearish = await BreakawayPattern.IsPatternAsync(i, prices, metricsCache, PatternDirection.Bearish, trendLookback);
                                    if (breakawayBearish != null)
                                        AddPattern(breakawayBearish, patternsFound, ref patternCount, initialCapacity);

                                    var ladderBottom = await LadderBottomPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
                                    if (ladderBottom != null)
                                        AddPattern(ladderBottom, patternsFound, ref patternCount, initialCapacity);

                                    var matHoldBullish = await MatHoldPattern.IsPatternAsync(i, trendLookback, true, prices, metricsCache);
                                    if (matHoldBullish != null)
                                        AddPattern(matHoldBullish, patternsFound, ref patternCount, initialCapacity);

                                    var matHoldBearish = await MatHoldPattern.IsPatternAsync(i, trendLookback, false, prices, metricsCache);
                                    if (matHoldBearish != null)
                                        AddPattern(matHoldBearish, patternsFound, ref patternCount, initialCapacity);

                                    var risingFallingThreeMethods = await RisingFallingThreeMethodsPattern.IsPatternAsync(i, trendLookback, prices, metricsCache);
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

                    if (_enablePerformanceMetrics)
                    {
                        metrics.IncrementCandlesProcessed();
                    }
                }

                if (_enablePerformanceMetrics)
                {
                    metrics.StopDetection();

                    // Record performance metrics to central monitor if provided
                    if (performanceMonitor != null)
                    {
                        var summary = metrics.GetSummary();
                        var metricsDict = new Dictionary<string, object>
                        {
                            ["MethodName"] = "PatternSearch.DetectPatternsAsync",
                            ["TotalDetectionTimeMs"] = summary.TotalDetectionTimeMs,
                            ["TotalCandlesProcessed"] = summary.TotalCandlesProcessed,
                            ["TotalPatternsFound"] = summary.TotalPatternsFound,
                            ["PatternCheckTimes"] = summary.PatternCheckTimes,
                            ["Timestamp"] = DateTime.UtcNow
                        };

                        // Call the new method for speed dial metric
                        performanceMonitor.RecordSpeedDialMetric("PatternSearch", "DetectPatternsAsync", "Pattern Detection Execution Time", "Time taken to detect patterns", (double)summary.TotalDetectionTimeMs, "ms", "Performance", 0, 10000, 5000, _enablePerformanceMetrics);
                    }
                }

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
        private static async Task<PatternDefinition?> CheckPatternWithMetricsAsync(Func<Task<PatternDefinition?>> patternCheck, string patternName, PatternDetectionMetrics metrics)
        {
            if (!_enablePerformanceMetrics)
            {
                return await patternCheck();
            }

            var stopwatch = Stopwatch.StartNew();
            var result = await patternCheck();
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

        /// <summary>
        /// Detects candlestick patterns and generates visualization images (synchronous version).
        /// </summary>
        /// <param name="prices">Array of candle data containing price and volume information.</param>
        /// <param name="trendLookback">Number of candles to look back for trend analysis and pattern context.</param>
        /// <param name="performanceMonitor">Optional performance monitor to record metrics centrally.</param>
        /// <param name="generateImages">Whether to generate and save pattern visualization images.</param>
        /// <param name="imageLookback">Number of candles to include in pattern images for context.</param>
        /// <returns>Dictionary mapping candle indices to lists of pattern visualizations with image paths.</returns>
        public static Dictionary<int, List<PatternVisualization>> DetectPatternsWithVisualization(CandleMids[] prices, int trendLookback, BacklashInterfaces.PerformanceMetrics.IPerformanceMonitor? performanceMonitor = null, bool generateImages = true, int imageLookback = 10)
        {
            var config = new PatternDetectionConfig();
            var metrics = new PatternDetectionMetrics();
            return DetectPatternsWithVisualizationAsync(prices, trendLookback, config, metrics, performanceMonitor, generateImages, imageLookback).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Detects candlestick patterns and generates visualization images asynchronously.
        /// </summary>
        /// <param name="prices">Array of candle data containing price and volume information.</param>
        /// <param name="trendLookback">Number of candles to look back for trend analysis and pattern context.</param>
        /// <param name="config">Configuration for pattern detection thresholds and settings.</param>
        /// <param name="metrics">Metrics collector for performance tracking.</param>
        /// <param name="performanceMonitor">Optional performance monitor to record metrics centrally.</param>
        /// <param name="generateImages">Whether to generate and save pattern visualization images.</param>
        /// <param name="imageLookback">Number of candles to include in pattern images for context.</param>
        /// <returns>Task containing dictionary mapping candle indices to lists of pattern visualizations with image paths.</returns>
        public static async Task<Dictionary<int, List<PatternVisualization>>> DetectPatternsWithVisualizationAsync(CandleMids[] prices,
                int trendLookback, PatternDetectionConfig config, PatternDetectionMetrics metrics,
                IPerformanceMonitor? performanceMonitor = null, bool generateImages = true, int imageLookback = 10)
        {
            // First detect patterns normally
            var patterns = await DetectPatternsAsync(prices, trendLookback, config, metrics, performanceMonitor);

            if (!generateImages)
            {
                // Convert to PatternVisualization without images
                var result = new Dictionary<int, List<PatternVisualization>>();
                foreach (var kvp in patterns)
                {
                    result[kvp.Key] = kvp.Value.Select(p => new PatternVisualization(p)).ToList();
                }
                return result;
            }

            // Generate images for each pattern
            var resultWithImages = new Dictionary<int, List<PatternVisualization>>();
            foreach (var kvp in patterns)
            {
                var visualizations = PatternVisualizer.GeneratePatternImages(kvp.Value, prices, imageLookback);
                resultWithImages[kvp.Key] = visualizations;
            }

            return resultWithImages;
        }
    }


}








