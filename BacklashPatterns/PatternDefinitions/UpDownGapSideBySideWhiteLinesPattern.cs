using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents an Up Down Gap Side By Side White Lines candlestick pattern.
    /// </summary>
    public class UpDownGapSideBySideWhiteLinesPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for all candles to ensure significant movement in the trend direction.
        /// Strictest: 0.5 (current), Loosest: 0.2 (still shows clear bullish/bearish intent).
        /// </summary>
        public static double MinBodySize { get; } = 0.1;

        /// <summary>
        /// Maximum overlap allowed in the gap between candles, ensuring a visible separation.
        /// Strictest: 0.1 (minimal overlap), Loosest: 0.4 (allows more overlap but maintains gap).
        /// </summary>
        public static double GapOverlapTolerance { get; } = 0.2;

        /// <summary>
        /// Maximum difference allowed between the open prices of the second and third candles.
        /// Strictest: 0.5 (near identical opens), Loosest: 2.0 (still side-by-side appearance).
        /// </summary>
        public static double MaxOpenDifference { get; } = 3.0;

        /// <summary>
        /// Maximum difference in body sizes between the second and third candles for similarity.
        /// Strictest: 0.5 (very similar), Loosest: 2.0 (allows variation but retains pattern shape).
        /// </summary>
        public static double MaxBodyDifference { get; } = 3.0;

        /// <summary>
        /// Maximum difference in high/low ranges between the second and third candles.
        /// Strictest: 0.5 (tight alignment), Loosest: 2.0 (still visually aligned).
        /// </summary>
        public static double MaxRangeDifference { get; } = 3.0;

        /// <summary>
        /// Minimum trend strength to confirm a prior trend (positive for bullish, negative for bearish).
        /// Strictest: 0.5 (strong trend), Loosest: 0.1 (minimal trend still present).
        /// </summary>
        public static double TrendThreshold { get; } = 0.2;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "UpDownGapSideBySideWhiteLines";
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
        /// Initializes a new instance of the UpDownGapSideBySideWhiteLinesPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public UpDownGapSideBySideWhiteLinesPattern(List<int> candles) : base(candles)
        {
        }

        /*
         * Up/Down Gap Side-by-Side White Lines Pattern:
         * - Description: A three-candle continuation pattern in a trending market. 
         *   Indicates sustained momentum after a gap.
         * - Requirements (Source: BabyPips):
         *   1. First candle: Strong candle in trend direction (bullish or bearish).
         *   2. Second candle: Same direction, gaps from first candle.
         *   3. Third candle: Same direction, opens near second candle s open, similar size.
         * - Indication: Bullish version confirms uptrend continuation; bearish version confirms downtrend continuation.
         */
/// <summary>IsPatternAsync</summary>
/// <summary>IsPatternAsync</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public static async Task<UpDownGapSideBySideWhiteLinesPattern?> IsPatternAsync(
                    int index,
                    int trendLookback,
                    bool isBullish,
                    CandleMids[] prices,
                    Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;
            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle
            if (c1 < 0 || c3 >= prices.Length) return null;

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            var ask1 = prices[c1];
            var ask2 = prices[c2];
            var ask3 = prices[c3];

            if (isBullish)
            {
                // All candles bullish, minimal body size
                if (!metrics1.IsBullish || metrics1.BodySize < MinBodySize ||
                    !metrics2.IsBullish || metrics2.BodySize < MinBodySize ||
                    !metrics3.IsBullish || metrics3.BodySize < MinBodySize) return null;

                // No gap requirement (removed)
                // Previously: if (ask2.Open <= ask1.Close || ask2.Low <= ask1.Close - GapOverlapTolerance) return null;

                // Third candle loosely aligns with second
                if (Math.Abs(ask3.Open - ask2.Open) > MaxOpenDifference ||
                    Math.Abs(metrics3.BodySize - metrics2.BodySize) > MaxBodyDifference ||
                    Math.Abs(ask3.High - ask2.High) > MaxRangeDifference) return null;

                // Weak uptrend check
                if (metrics3.GetLookbackMeanTrend(3) <= TrendThreshold) return null;
            }
            else
            {
                // All candles bearish, minimal body size
                if (!metrics1.IsBearish || metrics1.BodySize < MinBodySize ||
                    !metrics2.IsBearish || metrics2.BodySize < MinBodySize ||
                    !metrics3.IsBearish || metrics3.BodySize < MinBodySize) return null;

                // No gap requirement (removed)
                // Previously: if (ask2.Open >= ask1.Close || ask2.High >= ask1.Close + GapOverlapTolerance) return null;

                // Third candle loosely aligns with second
                if (Math.Abs(ask3.Open - ask2.Open) > MaxOpenDifference ||
                    Math.Abs(metrics3.BodySize - metrics2.BodySize) > MaxBodyDifference ||
                    Math.Abs(ask3.Low - ask2.Low) > MaxRangeDifference) return null;

                // Weak downtrend check
                if (metrics3.GetLookbackMeanTrend(3) >= -TrendThreshold) return null;
            }

            var candles = new List<int> { c1, c2, c3 };
            return new UpDownGapSideBySideWhiteLinesPattern(candles);
        }
    }
}








