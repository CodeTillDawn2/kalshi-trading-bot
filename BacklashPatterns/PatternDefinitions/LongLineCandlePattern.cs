using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class LongLineCandlePattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range for the candle to be significant.
        /// Loosest: 1.0 (smaller moves); Strictest: 3.0 (large moves required).
        /// </summary>
        public static double MinRange { get; } = 2.0;

        /// <summary>
        /// Minimum ratio of body size to total range for a prominent body.
        /// Loosest: 0.5 (smaller body); Strictest: 0.8 (very large body).
        /// </summary>
        public static double BodyRangeRatio { get; } = 0.6;

        /// <summary>
        /// Maximum ratio of wick size to total range for small wicks.
        /// Loosest: 0.3 (larger wicks); Strictest: 0.1 (very small wicks).
        /// </summary>
        public static double WickRangeRatio { get; } = 0.2;

        /// <summary>
        /// Threshold for trend strength to validate prior trend context.
        /// Loosest: 0.1 (weak trend); Strictest: 0.5 (strong trend required).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Multiplier for body size relative to average range in lookback.
        /// Loosest: 1.0 (equal to average); Strictest: 1.5 (much larger than average).
        /// </summary>
        public static double AvgRangeMultiplier { get; } = 1.2;
        public const string BaseName = "LongLineCandle";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        private readonly bool IsBullish;

        public LongLineCandlePattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Long Line Candle pattern, a single-candle pattern indicating strong momentum.
        /// Requirements (sourced from TradingView and adapted to your logic):
        /// - A candle with a large body relative to its range (typically > 60-70%).
        /// - Small upper and lower wicks (typically < 20-30% of range).
        /// - Appears in a trend context (bullish after downtrend, bearish after uptrend).
        /// - Indicates strong directional momentum.
        /// Your original logic includes a contextual range check against lookback average.
        /// </summary>
        public static LongLineCandlePattern IsPattern(
            Dictionary<int, CandleMetrics> metricsCache,
            bool isBullish,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Check if the total range meets the minimum requirement for significance
            if (metrics.TotalRange < MinRange) return null;

            // Determine if the candle direction matches the expected trend
            bool direction = isBullish ? metrics.IsBullish : metrics.IsBearish;

            // Validate the trend direction using CandleMetrics method
            bool trendValid = isBullish
                ? metrics.GetLookbackMeanTrend(1) < -TrendThreshold // Downtrend before bullish
                : metrics.GetLookbackMeanTrend(1) > TrendThreshold;  // Uptrend before bearish

            // Check contextual range against lookback average
            double avgRange = metrics.GetLookbackAvgRange(1);
            if (avgRange <= 0) return null; // Avoid invalid average range

            // Check if the body size, upper wick, and lower wick meet the pattern criteria
            bool isPatternValid = metrics.BodySize >= BodyRangeRatio * metrics.TotalRange && // Body must be significant
                                  metrics.UpperWick <= WickRangeRatio * metrics.TotalRange && // Upper wick limited
                                  metrics.LowerWick <= WickRangeRatio * metrics.TotalRange && // Lower wick limited
                                  metrics.BodySize >= AvgRangeMultiplier * avgRange && // Contextual size check
                                  direction && trendValid;

            if (!isPatternValid) return null;

            // Define the candle index for the pattern (single candle)
            var candles = new List<int> { index };

            // Return the pattern instance with the specified direction
            return new LongLineCandlePattern(candles, isBullish);
        }
    }
}
