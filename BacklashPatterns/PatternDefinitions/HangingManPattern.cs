using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The Hanging Man is a single-candle bearish reversal pattern appearing after an uptrend.
    /// - Small body, long lower wick, minimal upper wick.
    /// - Suggests selling pressure after a rise, potential top.
    /// Indicates: Bearish reversal if confirmed by subsequent decline.
    /// Source: https://www.investopedia.com/terms/h/hangingman.asp
    /// </summary>
    public class HangingManPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range of the candle. Ensures sufficient volatility for pattern significance.
        /// Strictest: 3.0 (original), Loosest: 2.0 (still notable range per general Hanging Man descriptions).
        /// </summary>
        public static double MinRange { get; } = 3.0;

        /// <summary>
        /// Maximum body size of the candle. Keeps the body small relative to wicks.
        /// Strictest: 1.0 (original), Loosest: 1.5 (allows slightly larger body, per loose definitions).
        /// </summary>
        public static double BodyMax { get; } = 1.0;

        /// <summary>
        /// Minimum ratio of lower wick to body size. Emphasizes the long lower wick.
        /// Strictest: 2.0 (original), Loosest: 1.5 (still prominent wick per broad Hanging Man logic).
        /// </summary>
        public static double WickBodyRatio { get; } = 2.0;

        /// <summary>
        /// Maximum upper wick as a percentage of total range. Minimizes upper wick presence.
        /// Strictest: 0.1 (original), Loosest: 0.25 (allows small upper wick, per loose definitions).
        /// </summary>
        public static double UpperWickMaxRatio { get; } = 0.1;

        /// <summary>
        /// Minimum mean trend strength for the uptrend. Confirms the preceding uptrend.
        /// Strictest: 0.5 (original), Loosest: 0.2 (minimal uptrend still valid, per reversal patterns).
        /// </summary>
        public static double TrendThreshold { get; } = 0.5;

        /// <summary>
        /// Minimum trend consistency for the uptrend. Ensures a reliable trend context.
        /// Strictest: 0.6 (original), Loosest: 0.4 (allows less consistent uptrend, per loose logic).
        /// </summary>
        public static double ConsistencyThreshold { get; } = 0.6;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "HangingMan";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the strength of the pattern.
        /// </summary>
        public override double Strength { get; protected set; }
        /// <summary>
        /// Gets the certainty of the pattern.
        /// </summary>
        public override double Certainty { get; protected set; }
        /// <summary>
        /// Gets the uncertainty of the pattern.
        /// </summary>
        public override double Uncertainty { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the HangingManPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public HangingManPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Hanging Man pattern exists at the specified index.
        /// </summary>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="index">The index of the candle.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<HangingManPattern?> IsPatternAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            CandleMids[] prices,
            int trendLookback)
        {
            if (index < 1) return null;

            var currentMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            if (currentMetrics.TotalRange < MinRange) return null;
            if (currentMetrics.BodySize > BodyMax) return null;
            if (currentMetrics.LowerWick < WickBodyRatio * currentMetrics.BodySize) return null;
            if (currentMetrics.UpperWick > UpperWickMaxRatio * currentMetrics.TotalRange) return null;

            bool hasUptrend = currentMetrics.GetLookbackMeanTrend(1) > TrendThreshold &&
                              currentMetrics.GetLookbackTrendConsistency(1) >= ConsistencyThreshold;
            if (!hasUptrend) return null;

            var candles = new List<int> { index };
            return new HangingManPattern(candles);
        }
    }
}








