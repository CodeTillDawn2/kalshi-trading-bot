using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /*
     * Morning Doji Star Pattern:
     * - Description: A three-candle bullish reversal pattern, a variant of the Morning Star, where the middle candle is 
     *   a Doji, signaling stronger indecision before a bullish reversal.
     * - Requirements (Source: Investopedia, BabyPips):
     *   - Occurs in a downtrend.
     *   - First candle: Bearish with a significant body.
     *   - Second candle: Doji (very small body), typically gapped down from the first candle.
     *   - Third candle: Bullish, closes above the first candle s midpoint or close, confirming reversal.
     *   - Indicates: Stronger bullish reversal potential due to the Doji s indecision.
     */
/// <summary>MorningDojiStarPattern</summary>
/// <summary>MorningDojiStarPattern</summary>
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
    public class MorningDojiStarPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first bearish candle, ensuring a significant downward move.
        /// - Strictest: 1.0 (significant body).
        /// - Loosest: 0.5 (smaller but noticeable bearish candle, per BabyPips  loose reversal patterns).
        /// </summary>
        public static double MinBodySize { get; } = 1.0;
/// <summary>Gets or sets the Strength.</summary>
/// <summary>Gets or sets the Name.</summary>
/// <summary>
/// </summary>

/// <summary>MorningDojiStarPattern</summary>
/// <summary>
/// </summary>
        /// <summary>
        /// Threshold for confirming a downtrend prior to the pattern. Negative values indicate a bearish trend.
        /// - Strictest: -0.5 (strong downtrend).
        /// - Loosest: -0.1 (minimal downtrend, per Investopedia s relaxed Morning Doji Star).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;
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
        /// Maximum allowable gap between the first candle s close and the second candle s open.
        /// - Strictest: 0.2 (tight gap for clear Doji indecision).
        /// - Loosest: 1.0 (larger gap allowed, per loose Doji Star definitions).
        /// </summary>
        public static double MaxOpenGap { get; } = 0.5;
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>

/// <summary>
/// </summary>
        /// <summary>
        /// Factor determining the minimum size of the third candle s body relative to the first candle s body.
        /// - Strictest: 0.5 (third candle closes at least at the midpoint).
        /// - Loosest: 0.1 (minimal penetration, per relaxed reversal standards).
        /// </summary>
        public static double ThirdBodyFactor { get; } = 0.3;
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
        /// Minimum absolute body size for the third candle, ensuring a significant bullish move.
        /// - Strictest: 1.0 (significant bullish candle).
        /// - Loosest: 0.5 (smaller but noticeable bullish move, per BabyPips  flexibility).
        /// </summary>
        public static double MinThirdBody { get; } = 1.0;
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public const string BaseName = "MorningDojiStar";
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bullish reversal pattern in a downtrend with three candles: a large bearish candle, a Doji star, and a large bullish candle.";
        public override double Strength { get; protected set; }
/// <summary>
/// </summary>
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public MorningDojiStarPattern(List<int> candles) : base(candles)
        {
        }

        public static async Task<MorningDojiStarPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            CandleMetrics firstMetrics = await GetCandleMetricsAsync(metricsCache, index - 2, prices, trendLookback, false);
            CandleMetrics secondMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            CandleMetrics thirdMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Require a downtrend based on the mean trend
            if (thirdMetrics.GetLookbackMeanTrend(3) > TrendThreshold) return null;

            // Extract the three relevant candles
            var asks = prices.Skip(index - 2).Take(3).ToArray();

            // Check if the second candle is a Doji (restored original strictness)
            bool isDoji = secondMetrics.TotalRange >= 1 &&
                          secondMetrics.BodySize <= 1 &&
                          secondMetrics.BodySize <= 0.1 * secondMetrics.TotalRange;

            // Calculate body sizes for first and third candles
            double body1 = Math.Abs(asks[0].Close - asks[0].Open); // First candle
            double body3 = Math.Abs(asks[2].Close - asks[2].Open); // Third candle

            // Combine conditions to confirm the pattern
            bool isPatternValid = asks[0].Close < asks[0].Open &&      // First candle must be bearish
                                 body1 >= MinBodySize &&               // First candle must have a significant body
                                 isDoji &&                             // Second candle must be a Doji
                                 asks[1].Open <= asks[0].Close + MaxOpenGap && // Second candle opens near first close
                                 asks[2].Close > asks[2].Open &&       // Third candle must be bullish
                                 (body3 >= ThirdBodyFactor * body1 || body3 >= MinThirdBody) && // Third candle size
                                 asks[2].Close > asks[0].Close;        // Third candle closes above first close

            if (!isPatternValid) return null;

            // Define the candle indices for the pattern (three candles)
            var candles = new List<int> { index - 2, index - 1, index };

            // Return the pattern instance if all conditions are met
            return new MorningDojiStarPattern(candles);
        }
    }
}








