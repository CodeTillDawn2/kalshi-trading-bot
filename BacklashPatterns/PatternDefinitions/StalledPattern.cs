using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Identifies a Stalled Pattern, a three-candle reversal pattern signaling a weakening uptrend,
    /// often indicating a potential top or pause in bullish momentum.
    /// 
    /// Requirements (Source: BabyPips, "Stalled Pattern"):
    /// - First candle: Strong bullish candle with a large body.
    /// - Second candle: Bullish candle with a smaller body, closing above the first.
    /// - Third candle: Small body (bullish or bearish), closing below the second but near the first,
    ///   showing momentum stalling.
    /// - Occurs in an established uptrend.
    /// - Suggests a potential reversal or consolidation.
    /// </summary>
    public class StalledPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first candle to qualify as a strong bullish candle.
        /// Strictest: 2.0 (strong trend); Loosest: 1.0 (minimal trend strength).
        /// </summary>
        public static double MinBodySize { get; set; } = 1.5;

        /// <summary>
        /// Maximum body size for the second and third candles to indicate stalling momentum.
        /// Strictest: 1.0 (very small body); Loosest: 2.0 (allows broader stalling).
        /// </summary>
        public static double SmallBodyMax { get; set; } = 1.5;

        /// <summary>
        /// Minimum mean trend value to confirm an established uptrend.
        /// Strictest: 0.5 (strong uptrend); Loosest: 0.1 (weak uptrend).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;

        /// <summary>
        /// Minimum trend consistency to ensure a reliable uptrend context.
        /// Strictest: 0.75 (highly consistent); Loosest: 0.25 (minimally consistent).
        /// </summary>
        public static double ConsistencyThreshold { get; set; } = 0.5;
        public const string BaseName = "Stalled";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public StalledPattern(List<int> candles) : base(candles)
        {
        }

        public static StalledPattern IsPattern(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            // Early exit if there aren’t enough prior candles for a three-candle pattern
            if (index < 2) return null;

            // Define candle indices
            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            // Check array bounds
            if (c1 < 0 || c3 >= prices.Length) return null;

            // Lazy load metrics for all three candles
            var metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

            // First candle: Must be bullish with a significant body
            if (!metrics1.IsBullish || metrics1.BodySize < MinBodySize) return null;

            // Second candle: Must be bullish, have a small body, and close above the first candle
            if (!metrics2.IsBullish || metrics2.BodySize > SmallBodyMax ||
                prices[c2].Close <= prices[c1].Close) return null;

            // Third candle: Must have a small body and close below the second but above or near the first
            if (metrics3.BodySize > SmallBodyMax || prices[c3].Close >= prices[c2].Close ||
                prices[c3].Close < prices[c1].Close - 0.5) return null;

            // Ensure the pattern occurs in an uptrend
            if (metrics3.GetLookbackMeanTrend(3) <= TrendThreshold || metrics3.GetLookbackTrendConsistency(3) < ConsistencyThreshold) return null;

            // Define the candle indices for the pattern (three candles)
            var candles = new List<int> { c1, c2, c3 };

            // Return the pattern instance if all conditions are satisfied
            return new StalledPattern(candles);
        }
    }
}
