using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /*
     * On-Neck Pattern:
     * - Description: A two-candle continuation pattern in a downtrend where a bearish candle is followed by a bullish candle 
     *   that opens near the previous close and closes at or very near it, indicating weak buying pressure and continuation 
     *   of the downtrend.
     * - Requirements (Source: Investopedia, BabyPips):
     *   - Occurs in a downtrend.
     *   - First candle: Bearish with a significant body.
     *   - Second candle: Bullish, opens near the first candle s close, closes at or near the first candle s close.
     *   - Indicates: Continuation of the downtrend due to inability of bulls to push prices higher.
     */
    public class OnNeckPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size of the first (bearish) candle, as a normalized value.
        /// Purpose: Ensures the first candle has a significant body to establish bearish momentum.
        /// Loosest: 0.3 (smaller body still shows direction); Strictest: 0.5 (standard requirement).
        /// </summary>
        public static double MinBodySize { get; } = 0.5;

        /// <summary>
        /// Maximum difference between the second candle s open/close and the first candle s close.
        /// Purpose: Ensures the second candle closes very near the first candle s close for continuation.
        /// Loosest: 2.0 (wider tolerance for proximity); Strictest: 0.5 (very tight proximity).
        /// </summary>
        public static double MaxOpenCloseDifference { get; } = 1.5;

        /// <summary>
        /// Threshold for identifying a downtrend based on lookback mean trend.
        /// Purpose: Confirms the pattern occurs in a bearish context.
        /// Loosest: -0.1 (very weak downtrend); Strictest: -0.5 (strong downtrend).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;

        /// <summary>
        /// Maximum upper wick size of the second candle, as a normalized value.
        /// Purpose: Limits the bullish wick to emphasize weak buying pressure.
        /// Loosest: 3.0 (allows larger wicks); Strictest: 1.0 (minimal wick).
        /// </summary>
        public static double MaxUpperWick { get; } = 2.0;
        public const string BaseName = "OnNeck";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public OnNeckPattern(List<int> candles) : base(candles)
        {
        }

        public static async Task<OnNeckPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices)
        {
            if (index < 1) return null;

            CandleMids currentPrices = prices[index];
            CandleMids previousPrices = prices[index - 1];

            CandleMetrics currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
            CandleMetrics prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);

            // First candle must be bearish with a significant body, second must be bullish
            if (!prevMetrics.IsBearish || prevMetrics.BodySize < MinBodySize || !currMetrics.IsBullish)
                return null;

            // Require a downtrend based on the mean trend (restored original threshold)
            if (currMetrics.GetLookbackMeanTrend(2) > TrendThreshold) return null;

            // Check if the second candle s open and close are sufficiently close to the first candle s close
            bool isPatternValid = Math.Abs(currentPrices.Open - previousPrices.Close) <= MaxOpenCloseDifference &&
                                 Math.Abs(currentPrices.Close - previousPrices.Close) <= MaxOpenCloseDifference &&
                                 currMetrics.UpperWick <= MaxUpperWick; // Restored wick check

            if (!isPatternValid) return null;

            // Define the candle indices for the pattern (two candles)
            var candles = new List<int> { index - 1, index };

            // Return the pattern instance if all conditions are met
            return new OnNeckPattern(candles);
        }
    }
}








