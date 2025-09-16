using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Three Black Crows pattern.
    /// - A bearish reversal pattern with three consecutive bearish candles, each opening near the previous close and closing lower, with small wicks, signaling a strong downtrend after an uptrend.
    /// Source: Investopedia (https://www.investopedia.com/terms/t/three_black_crows.asp)
    /// </summary>
    public class ThreeBlackCrowsPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for each candle. Ensures strong bearish momentum.
        /// Strictest: 2.0 (very strong), Loosest: 0.5 (minimal momentum).
        /// </summary>
        public static double MinBodySize { get; } = 1.5;

        /// <summary>
        /// Maximum wick size for each candle. Ensures small wicks for strong candles.
        /// Strictest: 0.5 (tiny wicks), Loosest: 2.0 (larger wicks allowed).
        /// </summary>
        public static double MaxWickSize { get; } = 1.5;

        /// <summary>
        /// Maximum difference between a candle s open and the previous close. Ensures continuity.
        /// Strictest: 0.1 (very tight), Loosest: 1.0 (loose continuity).
        /// </summary>
        public static double MaxOpenCloseDiff { get; } = 0.5;

        /// <summary>
        /// Threshold for confirming a prior uptrend. Positive value indicates uptrend.
        /// Strictest: 0.5 (strong uptrend), Loosest: 0.1 (weak uptrend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ThreeBlackCrows";
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
        /// Initializes a new instance of the ThreeBlackCrowsPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public ThreeBlackCrowsPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Three Black Crows pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<ThreeBlackCrowsPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2 || index >= prices.Length) return null;

            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // All bearish
            if (!metrics1.IsBearish || !metrics2.IsBearish || !metrics3.IsBearish) return null;

            // Significant bodies
            if (metrics1.BodySize < MinBodySize || metrics2.BodySize < MinBodySize || metrics3.BodySize < MinBodySize) return null;

            // Descending closes
            if (prices[c2].Close >= prices[c1].Close || prices[c3].Close >= prices[c2].Close) return null;

            // Opens near previous close
            if (Math.Abs(prices[c2].Open - prices[c1].Close) > MaxOpenCloseDiff ||
                Math.Abs(prices[c3].Open - prices[c2].Close) > MaxOpenCloseDiff) return null;

            // Small wicks
            if (metrics1.UpperWick > MaxWickSize || metrics2.UpperWick > MaxWickSize || metrics3.UpperWick > MaxWickSize) return null;
            if (metrics1.LowerWick > MaxWickSize || metrics2.LowerWick > MaxWickSize || metrics3.LowerWick > MaxWickSize) return null;

            // Uptrend check using CandleMetrics
            if (metrics3.GetLookbackMeanTrend(3) <= TrendThreshold) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new ThreeBlackCrowsPattern(candles);
        }
    }
}








