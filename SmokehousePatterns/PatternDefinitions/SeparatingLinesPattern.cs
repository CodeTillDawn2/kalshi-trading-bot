using SmokehouseDTOs;
using SmokehousePatterns.Helpers;
using static SmokehousePatterns.Helpers.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    public class SeparatingLinesPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for both candles, ensuring they are significant, as a percentage of the pattern's total range.
        /// Loosest: 15% (smaller body); Strictest: 50% (larger body).
        /// </summary>
        public static double MinBodySizePercentage => 25.0;

        /// <summary>
        /// Maximum difference between the open prices of the two candles, allowing near-equality, as a percentage of the pattern's total range.
        /// Loosest: 100% (wider tolerance); Strictest: 25% (very close opens).
        /// </summary>
        public static double MaxOpenDifferencePercentage => 75.0;

        /// <summary>
        /// Minimum trend strength threshold to confirm the prior trend direction, as a percentage of the pattern's total range.
        /// Loosest: 10% (weaker trend); Strictest: 25% (strong trend).
        /// </summary>
        public static double TrendThresholdPercentage => 15.0;

        public const string BaseName = "SeparatingLines";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        private readonly bool IsBullish;

        public SeparatingLinesPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Separating Lines pattern, a two-candle continuation pattern.
        /// Requirements (source: TradingView, Investopedia):
        /// - Bullish: Occurs in a downtrend; first candle is bearish, second is bullish, both open at nearly the same price.
        /// - Bearish: Occurs in an uptrend; first candle is bullish, second is bearish, both open at nearly the same price.
        /// Indicates: Continuation of the existing trend (bullish or bearish) with a strong move after a brief pause.
        /// </summary>
        public static SeparatingLinesPattern IsPattern(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices)
        {
            // Early exit if index is invalid
            if (index < 1 || index >= prices.Length) return null;

            // Lazy load metrics for the two candles
            var prevMetrics = GetCandleMetrics(ref metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Calculate the pattern's total range (highest high - lowest low across both candles)
            double patternHigh = Math.Max(prices[index].High, prices[index - 1].High);
            double patternLow = Math.Min(prices[index].Low, prices[index - 1].Low);
            double totalRange = patternHigh - patternLow;

            // Convert percentage-based thresholds to absolute values
            double minBodySize = totalRange * (MinBodySizePercentage / 100.0);
            double maxOpenDifference = totalRange * (MaxOpenDifferencePercentage / 100.0);
            double trendThreshold = totalRange * (TrendThresholdPercentage / 100.0);

            // Both candles must have significant bodies
            if (prevMetrics.BodySize < minBodySize || currMetrics.BodySize < minBodySize) return null;

            // Check if the open prices of the two candles are sufficiently close
            bool sameOpen = Math.Abs(prices[index].Open - prices[index - 1].Open) <= maxOpenDifference;
            if (!sameOpen) return null;

            // Determine if the candle directions align with the pattern type
            bool direction = isBullish
                ? (currMetrics.IsBullish && prevMetrics.IsBearish)
                : (currMetrics.IsBearish && prevMetrics.IsBullish);

            // Validate the trend direction using CandleMetrics method
            bool trendValid = isBullish
                ? currMetrics.GetLookbackMeanTrend(2) <= -trendThreshold // Downtrend before bullish continuation
                : currMetrics.GetLookbackMeanTrend(2) >= trendThreshold;  // Uptrend before bearish continuation

            // Combine conditions to confirm the pattern
            if (!direction || !trendValid) return null;

            // Define the candle indices for the pattern (two candles)
            var candles = new List<int> { index - 1, index };

            // Return the pattern instance with the specified direction
            return new SeparatingLinesPattern(candles, isBullish);
        }
    }
}