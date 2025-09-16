using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Morning Star candlestick pattern.
    /// </summary>
    public class MorningStarPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size required for the first bearish candle. Ensures it has significant downward movement.
        /// - Strictest: 1.0 (significant body relative to range).
        /// - Loosest: 0.5 (allows smaller but still noticeable bearish candles, per TradingView s loose interpretations).
        /// </summary>
        public static double MinBodySize { get; } = 1.0;

        /// <summary>
        /// Maximum body size for the second candle, enforcing its indecision nature.
        /// - Strictest: 0.5 (very small body, close to a Doji).
        /// - Loosest: 2.0 (allows larger indecision candles, per Investopedia s broader Morning Star definitions).
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
        /// - Loosest: 0.3 (allows more variability, per TradingView s flexible pattern recognition).
        /// </summary>
        public static double MinTrendConsistency { get; } = 0.5;

        /// <summary>
        /// Factor determining the minimum size of the third candle s body relative to the first candle s body.
        /// - Strictest: 0.5 (third candle closes at least at the midpoint of the first).
        /// - Loosest: 0.1 (minimal penetration into the first candle s body, per loose reversal definitions).
        /// </summary>
        public static double ThirdBodyFactor { get; } = 0.3;

        /// <summary>
        /// Maximum allowable gap between the first candle s close and the second candle s open.
        /// - Strictest: 0.2 (tight gap for clear indecision).
        /// - Loosest: 1.0 (allows larger gaps, per Investopedia s relaxed Morning Star variants).
        /// </summary>
        public static double MaxOpenGap { get; } = 0.5;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "MorningStar";
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
        /// Initializes a new instance of the MorningStarPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public MorningStarPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Morning Star pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<MorningStarPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            CandleMetrics metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            CandleMetrics metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            CandleMetrics metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // First candle: Must be bearish with a significant body
            if (!metrics1.IsBearish || metrics1.BodySize < MinBodySize) return null;

            // Second candle: Must have a small body and open at or below the first candle s close + gap
            if (metrics2.BodySize > SmallBodyMax || prices[c2].Open > prices[c1].Close + MaxOpenGap) return null;

            // Third candle: Must be bullish, body at least a portion of the first, closes above first s close
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








