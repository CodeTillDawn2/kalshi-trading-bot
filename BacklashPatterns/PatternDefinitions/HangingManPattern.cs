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
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bearish reversal pattern in an uptrend with a small body, long lower wick, and minimal upper wick. The long lower wick shows rejection of lower prices, signaling potential reversal from uptrend to downtrend.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Bearish;
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

        /// <summary>
        /// Calculates the strength of the pattern using historical cache for comparison.
        /// </summary>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="avgVolume">The average volume.</param>
        /// <param name="historicalCache">The historical pattern cache.</param>
        public void CalculateStrength(
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            double avgVolume,
            HistoricalPatternCache historicalCache)
        {
            if (Candles.Count != 1)
                throw new InvalidOperationException("HangingManPattern must have exactly 1 candle.");

            int index = Candles[0];
            var currentMetrics = metricsCache[index];

            // Power Score: Based on range, body smallness, wick strength, trend
            double rangeScore = currentMetrics.TotalRange / MinRange;
            rangeScore = Math.Min(rangeScore, 1);

            double bodyScore = 1 - (currentMetrics.BodySize / BodyMax);
            bodyScore = Math.Clamp(bodyScore, 0, 1);

            double wickBodyScore = currentMetrics.LowerWick / (WickBodyRatio * currentMetrics.BodySize);
            wickBodyScore = Math.Min(wickBodyScore, 1);

            double upperWickScore = 1 - (currentMetrics.UpperWick / (UpperWickMaxRatio * currentMetrics.TotalRange));
            upperWickScore = Math.Clamp(upperWickScore, 0, 1);

            double trendScore = currentMetrics.GetLookbackMeanTrend(1) / TrendThreshold;
            trendScore = Math.Min(trendScore, 1);

            double consistencyScore = currentMetrics.GetLookbackTrendConsistency(1) / ConsistencyThreshold;
            consistencyScore = Math.Min(consistencyScore, 1);

            double volumeScore = 0.5; // Placeholder

            double wRange = 0.15, wBody = 0.15, wWickBody = 0.25, wUpperWick = 0.15, wTrend = 0.15, wConsistency = 0.1, wVolume = 0.05;
            double powerScore = (wRange * rangeScore + wBody * bodyScore + wWickBody * wickBodyScore +
                                 wUpperWick * upperWickScore + wTrend * trendScore + wConsistency * consistencyScore + wVolume * volumeScore) /
                                (wRange + wBody + wWickBody + wUpperWick + wTrend + wConsistency + wVolume);

            // Match Score: Deviation from thresholds
            double rangeDeviation = Math.Abs(currentMetrics.TotalRange - MinRange) / MinRange;
            double bodyDeviation = Math.Abs(currentMetrics.BodySize - BodyMax) / BodyMax;
            double wickDeviation = Math.Abs(currentMetrics.LowerWick - WickBodyRatio * currentMetrics.BodySize) / (WickBodyRatio * currentMetrics.BodySize);
            double trendDeviation = Math.Abs(currentMetrics.GetLookbackMeanTrend(1) - TrendThreshold) / TrendThreshold;
            double matchScore = 1 - (rangeDeviation + bodyDeviation + wickDeviation + trendDeviation) / 4;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








