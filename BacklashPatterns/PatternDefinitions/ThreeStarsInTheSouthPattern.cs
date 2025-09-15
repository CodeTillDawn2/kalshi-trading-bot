using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class ThreeStarsInTheSouthPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size of the first candle to qualify as significant.
        /// Strictest: 1.0 (original logic), Loosest: 0.3 (still shows bearish intent).
        /// </summary>
        public static double MinBodySize { get; set; } = 1.0;

        /// <summary>
        /// Factor by which the second candle's body size is capped relative to the first candle.
        /// Strictest: 1.0 (equal or smaller), Loosest: 1.5 (allows slightly larger second candle).
        /// </summary>
        public static double MaxSecondBodyFactor { get; set; } = 1.2;

        /// <summary>
        /// Absolute cap on the second candle's body size.
        /// Strictest: 2.0 (original cap), Loosest: 3.0 (allows larger but still controlled size).
        /// </summary>
        public static double MaxSecondBodyCap { get; set; } = 2.0;

        /// <summary>
        /// Maximum body size of the third candle.
        /// Strictest: 1.5 (original logic), Loosest: 2.0 (still small but less restrictive).
        /// </summary>
        public static double MaxThirdBodySize { get; set; } = 1.5;

        /// <summary>
        /// Factor requiring the first candle's lower wick to be a proportion of its body.
        /// Strictest: 0.5 (original logic), Loosest: 0.2 (minimal wick still present).
        /// </summary>
        public static double LowerWickFactor { get; set; } = 0.5;

        /// <summary>
        /// Tolerance for how much the third candle's low can drop below the second candle's low.
        /// Strictest: 0.5 (original logic), Loosest: 1.0 (greater flexibility in low positioning).
        /// </summary>
        public static double LowTolerance { get; set; } = 0.5;

        /// <summary>
        /// Tolerance for how much the third candle's high can exceed the second candle's high.
        /// Strictest: 0.5 (original logic), Loosest: 1.0 (allows slight upward deviation).
        /// </summary>
        public static double HighTolerance { get; set; } = 0.5;

        /// <summary>
        /// Maximum lower wick size for the third candle.
        /// Strictest: 1.5 (original logic), Loosest: 2.0 (allows longer wick but still limited).
        /// </summary>
        public static double MaxLowerWick { get; set; } = 1.5;

        /// <summary>
        /// Threshold for confirming a preceding downtrend (negative value).
        /// Strictest: -0.3 (original logic), Loosest: -0.1 (very weak downtrend).
        /// </summary>
        public static double TrendThreshold { get; set; } = -0.3;

        /// <summary>
        /// Represents a Three Stars in the South pattern, a rare bullish reversal pattern in a downtrend.
        /// - Three bearish candles with decreasing size and specific wick/low/high relationships, indicating slowing bearish momentum before a potential reversal.
        /// Requirements sourced from: https://www.tradingview.com/education/threestarsinthesouth/
        /// </summary>
        public const string BaseName = "ThreeStarsInTheSouth";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public ThreeStarsInTheSouthPattern(List<int> candles) : base(candles)
        {
        }

        public static async Task<ThreeStarsInTheSouthPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            int c1 = index - 2;
            int c2 = index - 1;
            int c3 = index;

            if (c1 < 0 || c3 >= prices.Length) return null;

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            if (metrics1.BodySize < MinBodySize || !metrics1.IsBearish || metrics1.LowerWick < LowerWickFactor * metrics1.BodySize) return null;

            double maxSecondBody = Math.Max(MaxSecondBodyCap, MaxSecondBodyFactor * metrics1.BodySize);
            if (metrics2.BodySize > maxSecondBody || !metrics2.IsBearish || prices[c2].Low > prices[c1].Low || metrics2.LowerWick < 0) return null;

            if (metrics3.BodySize > MaxThirdBodySize || !metrics3.IsBearish ||
                prices[c3].Low < prices[c2].Low - LowTolerance || prices[c3].High > prices[c2].High + HighTolerance ||
                metrics3.LowerWick > MaxLowerWick) return null;

            if (metrics3.GetLookbackMeanTrend(3) > TrendThreshold) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new ThreeStarsInTheSouthPattern(candles);
        }
    }
}








