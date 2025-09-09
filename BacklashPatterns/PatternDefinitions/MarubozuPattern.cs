using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class MarubozuPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range for the candle to be considered significant.
        /// Loosest: 1.0 (allows smaller significant moves); Strictest: 5.0 (requires large moves).
        /// </summary>
        public static double MinRange { get; } = 3.0;

        /// <summary>
        /// Threshold for trend strength to validate prior trend context.
        /// Loosest: 0.1 (very weak trend); Strictest: 0.8 (strong trend required).
        /// </summary>
        public static double TrendThreshold { get; } = 0.5;
        public const string BaseName = "Marubozu";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        private readonly bool IsBullish;

        public MarubozuPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Marubozu pattern, a single-candle pattern of strong trend continuation or reversal.
        /// Requirements (sourced from Investopedia):
        /// - A long-bodied candle with no upper or lower wicks (open equals low and close equals high for bullish,
        ///   open equals high and close equals low for bearish).
        /// - Indicates strong buying (bullish) or selling (bearish) pressure.
        /// - Typically appears in a trend context (bullish after downtrend, bearish after uptrend).
        /// Your original logic matches this strict definition with a minimum range and trend check.
        /// </summary>
        public static MarubozuPattern IsPattern(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices)
        {
            // Lazy load metrics for the current candle
            var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Check if the total range meets the minimum requirement for significance
            if (metrics.TotalRange < MinRange) return null;

            // Determine if the candle direction matches the expected trend
            bool direction = isBullish ? metrics.IsBullish : metrics.IsBearish;

            // Strict wick check using raw price values to ensure no wicks
            var ask = prices[index];
            bool strictBullish = isBullish && ask.Open == ask.Low && ask.Close == ask.High;
            bool strictBearish = !isBullish && ask.Open == ask.High && ask.Close == ask.Low;

            // Validate the trend direction using CandleMetrics method
            bool trendValid = isBullish
                ? metrics.GetLookbackMeanTrend(1) < -TrendThreshold // Downtrend before bullish
                : metrics.GetLookbackMeanTrend(1) > TrendThreshold;  // Uptrend before bearish

            // Combine conditions to confirm the pattern
            bool isPatternValid = (strictBullish || strictBearish) && direction && trendValid;

            if (!isPatternValid) return null;

            // Define the candle index for the pattern (single candle)
            var candles = new List<int> { index };

            // Return the pattern instance with the specified direction
            return new MarubozuPattern(candles, isBullish);
        }
    }
}
