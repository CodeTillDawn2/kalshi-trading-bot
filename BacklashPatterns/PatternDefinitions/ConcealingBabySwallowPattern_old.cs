using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Concealing Baby Swallow candlestick pattern, a 4-candle bullish reversal pattern.
    /// Indicates a potential reversal from a downtrend to an uptrend.
    /// Requirements (Source: BabyPips, TradingView):
    /// - Occurs in a downtrend.
    /// - First two candles: Strong bearish (near Marubozu) with small wicks, small gap between.
    /// - Third candle: Bearish with upper wick overlapping second, slowing momentum.
    /// - Fourth candle: Bearish, engulfs third, closes below second low, signals reversal.
    /// </summary>
    public class ConcealingBabySwallowPattern_old : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first and second candles to qualify as strong bearish.
        /// Loosest value: 1.0 (still ensures a significant body, per general candlestick analysis).
        /// </summary>
        public static double MinBodySize { get; } = 1.5;

        /// <summary>
        /// Maximum wick size for the first and second candles to maintain near-Marubozu shape.
        /// Loosest value: 1.5 (allows slightly larger wicks while keeping body dominance, per TradingView).
        /// </summary>
        public static double WickMax { get; } = 1.0;

        /// <summary>
        /// Maximum gap allowance between the first and second candles.
        /// Loosest value: 1.0 (allows a wider gap while still appearing consecutive, per BabyPips).
        /// </summary>
        public static double GapSize { get; } = 0.5;

        /// <summary>
        /// Maximum body size for the third candle to indicate slowing momentum.
        /// Loosest value: 4.0 (permits a larger body while still smaller than prior candles, per general analysis).
        /// </summary>
        public static double MaxThirdBody { get; } = 3.0;

        /// <summary>
        /// Maximum downtrend strength threshold to confirm a preceding downtrend.
        /// Loosest value: -0.1 (requires only a mild downtrend, per loose trend definitions).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ConcealingBabySwallow";
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
        /// Initializes a new instance of the ConcealingBabySwallowPattern_old class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public ConcealingBabySwallowPattern_old(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Concealing Baby Swallow pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the fourth candle.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<ConcealingBabySwallowPattern?> IsPatternAsync(
            int index,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            int trendLookback
            )
        {
            if (index < 3 || index >= prices.Length) return null;
            int startIndex = index - 3;
            var candles = new List<int> { startIndex, startIndex + 1, startIndex + 2, index };

            var firstMetrics = await GetCandleMetricsAsync(metricsCache, startIndex, prices, trendLookback, false);
            var secondMetrics = await GetCandleMetricsAsync(metricsCache, startIndex + 1, prices, trendLookback, false);
            var thirdMetrics = await GetCandleMetricsAsync(metricsCache, startIndex + 2, prices, trendLookback, false);
            var fourthMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            CandleMids firstPrices = prices[startIndex];
            CandleMids secondPrices = prices[startIndex + 1];
            CandleMids thirdPrices = prices[startIndex + 2];
            CandleMids fourthPrices = prices[index];

            // Downtrend check
            if (fourthMetrics.GetLookbackMeanTrend(4) > TrendThreshold) return null;

            // First candle: Bearish, large body, small wicks
            if (!firstMetrics.IsBearish || firstMetrics.BodySize < MinBodySize ||
                firstMetrics.UpperWick > WickMax || firstMetrics.LowerWick > WickMax)
                return null;

            // Second candle: Bearish, large body, small wicks, small gap
            if (!secondMetrics.IsBearish || secondMetrics.BodySize < MinBodySize ||
                secondMetrics.UpperWick > WickMax || secondMetrics.LowerWick > WickMax ||
                secondPrices.Open > firstPrices.Close - GapSize)
                return null;

            // Third candle: Bearish, smaller body, upper wick overlaps second
            if (!thirdMetrics.IsBearish || thirdMetrics.BodySize > MaxThirdBody ||
                thirdMetrics.UpperWick <= 0 || thirdPrices.High <= secondPrices.Close)
                return null;

            // Fourth candle: Bearish, closes below second low, simplified engulfing
            if (!fourthMetrics.IsBearish || fourthPrices.Close > thirdPrices.Close ||
                fourthPrices.Close >= secondPrices.Low)
                return null;

            return new ConcealingBabySwallowPattern(candles);
        }
    }
}








