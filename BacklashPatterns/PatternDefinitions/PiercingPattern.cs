using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Piercing candlestick pattern.
    /// </summary>
    public class PiercingPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size of the first (bearish) candle, as a normalized value.
        /// Purpose: Ensures the first candle has a significant body to indicate strong bearish momentum.
        /// Loosest: 0.3 (allows smaller bodies while still showing direction); Strictest: 1.0 (standard requirement).
        /// </summary>
        public static double MinBodySizeFirst { get; } = 0.5;

        /// <summary>
        /// Minimum body size of the second (bullish) candle, as a normalized value.
        /// Purpose: Ensures the second candle has enough bullish strength to suggest a reversal.
        /// Loosest: 0.2 (minimal body for bullish intent); Strictest: 0.5 (standard requirement).
        /// </summary>
        public static double MinBodySizeSecond { get; } = 0.3;

        /// <summary>
        /// Minimum ratio of the second candle s body size to its total range.
        /// Purpose: Ensures the body is significant relative to the candle s volatility.
        /// Loosest: 0.3 (allows smaller bodies in volatile ranges); Strictest: 0.5 (standard requirement).
        /// </summary>
        public static double BodyToRangeRatio { get; } = 0.4;

        /// <summary>
        /// Threshold for identifying a downtrend based on lookback mean trend.
        /// Purpose: Confirms the pattern occurs in a bearish context.
        /// Loosest: -0.1 (very weak downtrend); Strictest: -0.5 (strong downtrend).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;

        /// <summary>
        /// Tolerance for the gap between the first candle s close and second candle s open.
        /// Purpose: Allows flexibility in the gap-down requirement.
        /// Loosest: 1.0 (wide gap tolerance); Strictest: 0.0 (no gap allowed).
        /// </summary>
        public static double OpenTolerance { get; } = 0.5;

        /// <summary>
        /// Tolerance for the second candle s close relative to the first candle s midpoint.
        /// Purpose: Ensures the close is near or above the midpoint while allowing flexibility.
        /// Loosest: 1.0 (wide tolerance); Strictest: 0.0 (exact midpoint).
        /// </summary>
        public static double MidPointTolerance { get; } = 0.5;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Piercing";
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
        /// Initializes a new instance of the PiercingPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public PiercingPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies a Piercing Pattern, a two-candle bullish reversal pattern.
        /// Requirements (source: Investopedia, BabyPips):
        /// - Occurs in a downtrend.
        /// - First candle is bearish with a significant body.
        /// - Second candle is bullish, opens below or near the first candle's close, 
        ///   and closes above the midpoint of the first candle s body but below its open.
        /// Indicates: Potential reversal from bearish to bullish momentum as buyers step in after a gap down.
        /// </summary>
        public static async Task<PiercingPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices)
        {
            // Early exit if indices are invalid
            if (index - 1 < 0 || index >= prices.Length) return null;

            // Lazy load metrics for the two candles
            var prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Require a downtrend using CandleMetrics method
            if (currMetrics.GetLookbackMeanTrend(2) > TrendThreshold) return null;

            // Use precomputed metrics for efficiency
            double body1 = prevMetrics.BodySize;
            double midPoint1 = prevMetrics.BodyMidPoint;
            double body2 = currMetrics.BodySize;
            double range2 = currMetrics.TotalRange;

            // Check shape conditions to confirm the pattern (restored original logic)
            bool shape = prevMetrics.IsBearish &&                   // Bearish first candle
                         body1 >= MinBodySizeFirst &&               // Significant body (relaxed)
                         currMetrics.IsBullish &&                   // Bullish second candle
                         prices[index].Open <= prices[index - 1].Close + OpenTolerance && // Relaxed gap
                         prices[index].Close > midPoint1 - MidPointTolerance && // Above relaxed midpoint
                         prices[index].Close < prices[index - 1].Open && // Below first open
                         body2 >= MinBodySizeSecond &&              // Significant body (relaxed)
                         (range2 > 0 ? body2 >= BodyToRangeRatio * range2 : true); // Body proportion

            if (!shape) return null;

            // Define the candle indices for the pattern (two candles)
            var candles = new List<int> { index - 1, index };

            // Return the pattern instance if all conditions are met
            return new PiercingPattern(candles);
        }
    }
}








