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
    /// Optimized for ML: Relative scaling based on lookback average range, gap relaxed to 0.
    /// </summary>
    public class ConcealingBabySwallowPattern : PatternDefinition
    {
        /// <summary>
        /// Number of candles required to form the pattern.
        /// Purpose: Defines the fixed size of the Concealing Baby Swallow pattern (four candles).
        /// Default: 4 (standard for this pattern)
        /// </summary>
        public static int PatternSize { get; } = 4;

        /// <summary>
        /// Minimum body size for the first and second candles relative to the lookback average range.
        /// Purpose: Ensures the first two candles are strong bearish compared to prior volatility.
        /// Default: 1.5 (1.5 times the average range)
        /// Range: 1.0–2.0 (1.0 for moderate strength, 2.0 for very strong bearish candles).
        /// </summary>
        public static double MinBodyToAvgRangeRatio { get; set; } = 0.5;

        /// <summary>
        /// Maximum wick size for the first and second candles relative to the lookback average range.
        /// Purpose: Maintains near-Marubozu shape by limiting wick size relative to prior volatility.
        /// Default: 0.3 (30% of average range)
        /// Range: 0.2–0.5 (0.2 for strict Marubozu, 0.5 for slightly larger wicks).
        /// </summary>
        public static double WickToAvgRangeMax { get; set; } = 1.0;

        /// <summary>
        /// Maximum gap allowance between the first and second candles relative to the lookback average range.
        /// Purpose: Allows a small gap; set to 0.0 to relax requirement in markets without gaps.
        /// Default: 0.0 (no gap required)
        /// Range: 0.0–0.3 (0.0 for no gap, 0.3 for small gaps relative to volatility).
        /// </summary>
        public static double GapToAvgRangeMax { get; set; } = 0.0;

        /// <summary>
        /// Maximum body size for the third candle relative to the lookback average range.
        /// Purpose: Ensures slowing momentum by limiting the third candle’s body size.
        /// Default: 0.8 (80% of average range)
        /// Range: 0.5–1.0 (0.5 for very slow momentum, 1.0 for moderate slowing).
        /// </summary>
        public static double MaxThirdBodyToAvgRange { get; set; } = 1.5;

        /// <summary>
        /// Minimum bearish trend direction ratio in the lookback period to confirm a prior downtrend.
        /// Purpose: Ensures a consistent downtrend before the pattern forms.
        /// Default: 0.6 (60% bearish candles)
        /// Range: 0.5–0.8 (0.5 for moderate downtrend, 0.8 for strong downtrend).
        /// </summary>
        public static double BearishTrendDirectionRatioMin { get; set; } = 0.3;

        public const string BaseName = "ConcealingBabySwallow";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public ConcealingBabySwallowPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Concealing Baby Swallow pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the fourth candle in the pattern.</param>
        /// <param name="prices">Array of candle price data.</param>
        /// <param name="metricsCache">Cache of candle metrics.</param>
        /// <param name="trendLookback">Number of candles to look back for trend and average range.</param>
        /// <returns>A ConcealingBabySwallowPattern instance if detected, otherwise null.</returns>
        public static ConcealingBabySwallowPattern IsPattern(
            int index,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            int trendLookback = 5)
        {
            if (index < PatternSize + trendLookback - 1) return null;
            int startIndex = index - (PatternSize - 1);
            var candles = new List<int> { startIndex, startIndex + 1, startIndex + 2, index };

            var firstMetrics = GetCandleMetrics(ref metricsCache, startIndex, prices, trendLookback, false);
            var secondMetrics = GetCandleMetrics(ref metricsCache, startIndex + 1, prices, trendLookback, false);
            var thirdMetrics = GetCandleMetrics(ref metricsCache, startIndex + 2, prices, trendLookback, false);
            var fourthMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            CandleMids firstPrices = prices[startIndex];
            CandleMids secondPrices = prices[startIndex + 1];
            CandleMids thirdPrices = prices[startIndex + 2];
            CandleMids fourthPrices = prices[index];

            double avgRange = Math.Max(fourthMetrics.LookbackAvgRange[PatternSize - 1], 0.001 * fourthPrices.Close);

            if (fourthMetrics.BearishRatio[PatternSize - 1] < 0.3) return null;  // 30% bearish

            if (!firstMetrics.IsBearish || firstMetrics.BodySize < 0.5 * avgRange ||
                firstMetrics.UpperWick > avgRange || firstMetrics.LowerWick > avgRange)
                return null;

            if (!secondMetrics.IsBearish || secondMetrics.BodySize < 0.5 * avgRange ||
                secondMetrics.UpperWick > avgRange || secondMetrics.LowerWick > avgRange)
                return null;

            // 3b: Third candle high = second low
            if (!thirdMetrics.IsBearish || thirdMetrics.BodySize > 1.5 * avgRange ||
                thirdPrices.High < secondPrices.Low)
                return null;

            // 4a & 4b: Close < third low, = second close
            if (!fourthMetrics.IsBearish || fourthPrices.Close >= thirdPrices.Low ||
                fourthPrices.Close > secondPrices.Close)
                return null;

            return new ConcealingBabySwallowPattern(candles);
        }
    }
}






