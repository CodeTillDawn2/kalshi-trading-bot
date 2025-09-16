using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Modified Hikkake candlestick pattern.
    /// </summary>
    public class ModifiedHikkakePattern : PatternDefinition
    {
        /// <summary>
        /// Minimum directional move required for the second candle s breakout or breakdown from the first candle s close.
        /// - Strictest: 0.5 (noticeable breakout/breakdown).
        /// - Loosest: 0.0 (any move qualifies, per your original loose definition and Investopedia s flexibility).
        /// </summary>
        public static double BreakoutThreshold { get; } = 0;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ModifiedHikkake";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
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
        private readonly bool IsBullish;

        /// <summary>
        /// Initializes a new instance of the ModifiedHikkakePattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public ModifiedHikkakePattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Modified Hikkake pattern, a three-candle reversal pattern.
        /// Requirements (sourced from Investopedia and adapted to your logic):
        /// - A three-candle pattern where the first candle sets a reference point.
        /// - Second candle shows a slight breakout (bullish) or breakdown (bearish) from the first candle s close.
        /// - Third candle reverses the second candle s direction, continuing the trend loosely.
        /// - Indicates a potential reversal with minimal trend confirmation.
        /// Your original logic uses a very loose definition compared to the standard Hikkake.
        /// </summary>
        public static async Task<ModifiedHikkakePattern?> IsPatternAsync(
            int index,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            int trendLookback)
        {
            // Ensure there are enough candles for the pattern
            if (index < 2) return null; // Early exit: Need 3 candles

            int firstIdx = index - 2;  // Reference candle
            int secondIdx = index - 1; // Breakout/Breakdown candle
            int thirdIdx = index;      // Reversal candle

            var firstMid = prices[firstIdx];
            var secondMid = prices[secondIdx];
            var thirdMid = prices[thirdIdx];

            // Lazy load metrics for the three candles
            var secondMetrics = await GetCandleMetricsAsync(metricsCache, secondIdx, prices, trendLookback, false);
            var thirdMetrics = await GetCandleMetricsAsync(metricsCache, thirdIdx, prices, trendLookback, true);

            // Very loose breakout/breakdown: Any directional move from first close
            bool breakoutCondition = isBullish
                ? secondMid.Close > firstMid.Close + BreakoutThreshold  // Slight upward move
                : secondMid.Close < firstMid.Close - BreakoutThreshold; // Slight downward move
            if (!breakoutCondition) return null;

            // Very loose reversal: Just directional consistency, no size minimum
            bool reversal = isBullish
                ? thirdMid.Close > secondMid.Close && thirdMetrics.IsBullish
                : thirdMid.Close < secondMid.Close && thirdMetrics.IsBearish;
            if (!reversal) return null;

            // Minimal trend check (almost neutral) using CandleMetrics method
            bool trendCondition = isBullish
                ? thirdMetrics.GetLookbackMeanTrend(3) <= 0  // No strong uptrend before bullish reversal
                : thirdMetrics.GetLookbackMeanTrend(3) >= 0; // No strong downtrend before bearish reversal
            if (!trendCondition) return null;

            // Define the candle indices for the pattern (three candles)
            var candles = new List<int> { firstIdx, secondIdx, thirdIdx };

            // Return the pattern instance with the specified direction
            return new ModifiedHikkakePattern(candles, isBullish);
        }
    }
}








