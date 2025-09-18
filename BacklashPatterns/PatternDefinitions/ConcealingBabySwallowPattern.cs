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
        /// Range: 1.0�2.0 (1.0 for moderate strength, 2.0 for very strong bearish candles).
        /// </summary>
        public static double MinBodyToAvgRangeRatio { get; set; } = 0.5;

        /// <summary>
        /// Maximum wick size for the first and second candles relative to the lookback average range.
        /// Purpose: Maintains near-Marubozu shape by limiting wick size relative to prior volatility.
        /// Default: 0.3 (30% of average range)
        /// Range: 0.2�0.5 (0.2 for strict Marubozu, 0.5 for slightly larger wicks).
        /// </summary>
        public static double WickToAvgRangeMax { get; set; } = 1.0;

        /// <summary>
        /// Maximum gap allowance between the first and second candles relative to the lookback average range.
        /// Purpose: Allows a small gap; set to 0.0 to relax requirement in markets without gaps.
        /// Default: 0.0 (no gap required)
        /// Range: 0.0�0.3 (0.0 for no gap, 0.3 for small gaps relative to volatility).
        /// </summary>
        public static double GapToAvgRangeMax { get; set; } = 0.0;

        /// <summary>
        /// Maximum body size for the third candle relative to the lookback average range.
        /// Purpose: Ensures slowing momentum by limiting the third candle�s body size.
        /// Default: 0.8 (80% of average range)
        /// Range: 0.5�1.0 (0.5 for very slow momentum, 1.0 for moderate slowing).
        /// </summary>
        public static double MaxThirdBodyToAvgRange { get; set; } = 1.5;

        /// <summary>
        /// Minimum bearish trend direction ratio in the lookback period to confirm a prior downtrend.
        /// Purpose: Ensures a consistent downtrend before the pattern forms.
        /// Default: 0.6 (60% bearish candles)
        /// Range: 0.5�0.8 (0.5 for moderate downtrend, 0.8 for strong downtrend).
        /// </summary>
        public static double BearishTrendDirectionRatioMin { get; set; } = 0.3;

        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ConcealingBabySwallow";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A rare bullish reversal pattern in a downtrend with two strong bearish candles followed by a smaller bearish candle that engulfs the third candle. Signals potential reversal from downtrend to uptrend.";
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
        /// Initializes a new instance of the ConcealingBabySwallowPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
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
        public static async Task<ConcealingBabySwallowPattern?> IsPatternAsync(
            int index,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            int trendLookback = 5)
        {
            if (index < PatternSize + trendLookback - 1) return null;
            int startIndex = index - (PatternSize - 1);
            var candles = new List<int> { startIndex, startIndex + 1, startIndex + 2, index };

            var firstMetrics = await GetCandleMetricsAsync(metricsCache, startIndex, prices, trendLookback, false);
            var secondMetrics = await GetCandleMetricsAsync(metricsCache, startIndex + 1, prices, trendLookback, false);
            var thirdMetrics = await GetCandleMetricsAsync(metricsCache, startIndex + 2, prices, trendLookback, false);
            var fourthMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

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

        /// <summary>
        /// Calculates the strength of the pattern using historical cache for comparison.
        /// </summary>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="avgVolume">The average volume.</param>
        /// <param name="historicalCache">The historical pattern cache.</param>
        public void CalculateStrength(
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            double avgVolume,
            HistoricalPatternCache historicalCache)
        {
            if (Candles.Count != 4)
                throw new InvalidOperationException("ConcealingBabySwallowPattern must have exactly 4 candles.");

            int startIndex = Candles[0];
            int index = Candles[3]; // Fourth candle

            var firstMetrics = metricsCache[startIndex];
            var secondMetrics = metricsCache[startIndex + 1];
            var thirdMetrics = metricsCache[startIndex + 2];
            var fourthMetrics = metricsCache[index];

            var firstPrices = prices[startIndex];
            var secondPrices = prices[startIndex + 1];
            var thirdPrices = prices[startIndex + 2];
            var fourthPrices = prices[index];

            double avgRange = Math.Max(fourthMetrics.LookbackAvgRange[PatternSize - 1], 0.001 * fourthPrices.Close);

            // Power Score: Based on body sizes, wick minimization, engulfing, trend
            double firstBodyScore = firstMetrics.BodySize / (MinBodyToAvgRangeRatio * avgRange);
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double secondBodyScore = secondMetrics.BodySize / (MinBodyToAvgRangeRatio * avgRange);
            secondBodyScore = Math.Min(secondBodyScore, 1);

            double wickScore = 1 - ((firstMetrics.UpperWick + firstMetrics.LowerWick + secondMetrics.UpperWick + secondMetrics.LowerWick) / (4 * WickToAvgRangeMax * avgRange));
            wickScore = Math.Clamp(wickScore, 0, 1);

            double thirdBodyScore = 1 - (thirdMetrics.BodySize / (MaxThirdBodyToAvgRange * avgRange));
            thirdBodyScore = Math.Clamp(thirdBodyScore, 0, 1);

            double engulfingScore = (fourthPrices.Close < thirdPrices.Low && fourthPrices.Close <= secondPrices.Close) ? 1.0 : 0.5;

            double trendDirectionRatio = fourthMetrics.BearishRatio[PatternSize - 1];

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.15, wSecond = 0.15, wWick = 0.2, wThird = 0.15, wEngulfing = 0.2, wTrend = 0.1, wVolume = 0.05;
            double powerScore = (wFirst * firstBodyScore + wSecond * secondBodyScore + wWick * wickScore +
                                 wThird * thirdBodyScore + wEngulfing * engulfingScore + wTrend * trendDirectionRatio + wVolume * volumeScore) /
                                (wFirst + wSecond + wWick + wThird + wEngulfing + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(firstMetrics.BodySize - MinBodyToAvgRangeRatio * avgRange) / (MinBodyToAvgRangeRatio * avgRange);
            double wickDeviation = ((firstMetrics.UpperWick + firstMetrics.LowerWick) / (2 * WickToAvgRangeMax * avgRange) +
                                   (secondMetrics.UpperWick + secondMetrics.LowerWick) / (2 * WickToAvgRangeMax * avgRange)) / 2;
            double trendDeviation = Math.Abs(trendDirectionRatio - BearishTrendDirectionRatioMin) / BearishTrendDirectionRatioMin;
            double matchScore = 1 - (bodyDeviation + wickDeviation + trendDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








