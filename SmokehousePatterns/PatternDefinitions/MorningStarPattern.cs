using SmokehousePatterns.Helpers;
using static SmokehousePatterns.Helpers.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    /*
     * Morning Star Pattern:
     * - Description: A three-candle bullish reversal pattern signaling the end of a downtrend, with a bearish candle, 
     *   a small-bodied candle indicating indecision, and a bullish candle confirming the reversal.
     * - Requirements (Source: Investopedia, TradingView):
     *   - Occurs in a downtrend.
     *   - First candle: Bearish with a significant body.
     *   - Second candle: Small body (bullish or bearish), often gapped down, showing indecision.
     *   - Third candle: Bullish, closes well into the first candle’s body (typically above its close or midpoint).
     *   - Indicates: Potential reversal from bearish to bullish momentum.
     */
    public class MorningStarPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size required for the first bearish candle. Ensures it has significant downward movement.
        /// - Strictest: 1.0 (significant body relative to range).
        /// - Loosest: 0.5 (allows smaller but still noticeable bearish candles, per TradingView’s loose interpretations).
        /// </summary>
        public static double MinBodySize { get; } = 1.0;

        /// <summary>
        /// Maximum body size for the second candle, enforcing its indecision nature.
        /// - Strictest: 0.5 (very small body, close to a Doji).
        /// - Loosest: 2.0 (allows larger indecision candles, per Investopedia’s broader Morning Star definitions).
        /// </summary>
        public static double SmallBodyMax { get; } = 1.5;

        /// <summary>
        /// Threshold for confirming a downtrend prior to the pattern. Negative values indicate a bearish trend.
        /// - Strictest: -0.5 (strong downtrend).
        /// - Loosest: -0.1 (minimal downtrend, per loose interpretations from BabyPips).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;

        /// <summary>
        /// Minimum consistency of the downtrend over the lookback period.
        /// - Strictest: 0.8 (highly consistent downtrend).
        /// - Loosest: 0.3 (allows more variability, per TradingView’s flexible pattern recognition).
        /// </summary>
        public static double MinTrendConsistency { get; } = 0.5;

        /// <summary>
        /// Factor determining the minimum size of the third candle’s body relative to the first candle’s body.
        /// - Strictest: 0.5 (third candle closes at least at the midpoint of the first).
        /// - Loosest: 0.1 (minimal penetration into the first candle’s body, per loose reversal definitions).
        /// </summary>
        public static double ThirdBodyFactor { get; } = 0.3;

        /// <summary>
        /// Maximum allowable gap between the first candle’s close and the second candle’s open.
        /// - Strictest: 0.2 (tight gap for clear indecision).
        /// - Loosest: 1.0 (allows larger gaps, per Investopedia’s relaxed Morning Star variants).
        /// </summary>
        public static double MaxOpenGap { get; } = 0.5;
        public const string BaseName = "MorningStar";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public MorningStarPattern(List<int> candles) : base(candles)
        {
        }

        public static MorningStarPattern IsPattern(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            CandleMetrics metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
            CandleMetrics metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
            CandleMetrics metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

            // First candle: Must be bearish with a significant body
            if (!metrics1.IsBearish || metrics1.BodySize < MinBodySize) return null;

            // Second candle: Must have a small body and open at or below the first candle’s close + gap
            if (metrics2.BodySize > SmallBodyMax || prices[c2].Open > prices[c1].Close + MaxOpenGap) return null;

            // Third candle: Must be bullish, body at least a portion of the first, closes above first’s close
            if (!metrics3.IsBullish ||
                metrics3.BodySize < ThirdBodyFactor * metrics1.BodySize ||
                prices[c3].Close <= prices[c1].Close) return null;

            // Require a downtrend based on the mean trend and consistency
            if (metrics3.GetLookbackMeanTrend(3) > TrendThreshold ||
                metrics3.GetLookbackTrendConsistency(3) < MinTrendConsistency) return null;

            // Define the candle indices for the pattern (three candles)
            var candles = new List<int> { c1, c2, c3 };

            // Return the pattern instance if all conditions are met
            return new MorningStarPattern(candles);
        }
    }
}