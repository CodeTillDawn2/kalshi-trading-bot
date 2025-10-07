using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Morning Doji Star candlestick pattern, a three-candle bullish reversal pattern.
    /// Occurs in a downtrend with a bearish candle, a Doji star, and a bullish candle.
    /// Requirements: Downtrend, bearish first candle, Doji second, bullish third closing above first.
    /// Optimized for: Loose thresholds for maximum detection in a 0-100 fixed-range market.
    /// </summary>
    public class MorningDojiStarPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first bearish candle, ensuring a significant downward move.
        /// - Strictest: 1.0 (significant body).
        /// - Loosest: 0.5 (smaller but noticeable bearish candle, per BabyPips loose reversal patterns).
        /// </summary>
        public static double MinBodySize { get; } = 1.0;

        /// <summary>
        /// Threshold for confirming a downtrend prior to the pattern. Negative values indicate a bearish trend.
        /// - Strictest: -0.5 (strong downtrend).
        /// - Loosest: -0.1 (minimal downtrend, per Investopedia's relaxed Morning Doji Star).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;
        /// <summary>
        /// Maximum allowable gap between the first candle's close and the second candle's open.
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
        /// Factor determining the minimum size of the third candle's body relative to the first candle's body.
        /// - Strictest: 0.5 (third candle closes at least at the midpoint).
        /// - Loosest: 0.1 (minimal penetration, per relaxed reversal standards).
        /// </summary>
        public static double ThirdBodyFactor { get; } = 0.3;

        /// <summary>
        /// Minimum absolute body size for the third candle, ensuring a significant bullish move.
        /// - Strictest: 1.0 (significant bullish candle).
        /// - Loosest: 0.5 (smaller but noticeable bullish move, per BabyPips flexibility).
        /// </summary>
        public static double MinThirdBody { get; } = 1.0;
        /// <summary>
        /// Gets the base name for the Morning Doji Star pattern.
        /// </summary>
        public const string BaseName = "MorningDojiStar";

        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bullish reversal pattern in a downtrend with three candles: a large bearish candle, a Doji star, and a large bullish candle.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Bullish;

        /// <summary>
        /// Gets the calculated strength of the pattern.
        /// Note: Not calculated in this implementation; reserved for future use.
        /// </summary>
        public override double Strength { get; protected set; }

        /// <summary>
        /// Gets the calculated certainty of the pattern.
        /// Note: Not calculated in this implementation; reserved for future use.
        /// </summary>
        public override double Certainty { get; protected set; }

        /// <summary>
        /// Gets the calculated uncertainty of the pattern.
        /// Note: Not calculated in this implementation; reserved for future use.
        /// </summary>
        public override double Uncertainty { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the MorningDojiStarPattern class.
        /// </summary>
        /// <param name="candles">List of candle indices forming the pattern (three candles).</param>
        /// <summary>
        /// Initializes a new instance of the MorningDojiStarPattern class.
        /// </summary>
        /// <param name="candles">List of candle indices forming the pattern (three candles).</param>
        public MorningDojiStarPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Morning Doji Star pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle in the pattern.</param>
        /// <param name="trendLookback">Number of candles to look back for trend analysis.</param>
        /// <param name="prices">Array of candle price data.</param>
        /// <param name="metricsCache">Cache of precomputed candle metrics.</param>
        /// <returns>A MorningDojiStarPattern instance if detected, otherwise null.</returns>
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








