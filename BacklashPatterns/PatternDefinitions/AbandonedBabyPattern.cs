using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents an Abandoned Baby candlestick pattern, a rare 3-candle reversal pattern.
    /// Bullish: Indicates a potential reversal from a downtrend to an uptrend.
    /// Bearish: Indicates a potential reversal from an uptrend to a downtrend.
    /// Requirements (Source: Investopedia, BabyPips):
    /// - First candle: Strong directional move (bullish for bearish pattern, bearish for bullish).
    /// - Middle candle: Doji (small body, shows indecision), gapped from first candle.
    /// - Third candle: Opposite direction of first, gapped from Doji, confirms reversal.
    /// </summary>
    public class AbandonedBabyPattern : PatternDefinition
    {
        /// <summary>
        /// Maximum body size for the middle Doji candle in absolute terms.
        /// Purpose: Ensures the Doji has a small body relative to its range, indicating indecision.
        /// Default: 1.0
        /// Loosest Value: 2.0 (Based on broader interpretations from TradingView forums, where a slightly larger body is still considered a Doji if shadows dominate).
        /// </summary>
        public static double DojiBodyMax { get; set; } = 2;

        /// <summary>
        /// Maximum body size as a percentage of the Doji's total range.
        /// Purpose: Ensures the Doji's body remains small compared to its wicks, reinforcing indecision.
        /// Default: 0.1 (10%)
        /// Loosest Value: 0.3 (30%) (Per BabyPips, a Doji can have a body up to 30% of range in less strict definitions).
        /// </summary>
        public static double DojiBodyRangeRatio { get; set; } = 0.5;

        /// <summary>
        /// Minimum total range for the Doji candle to be significant.
        /// Purpose: Prevents trivial candles from qualifying, ensuring the pattern has market impact.
        /// Default: 1.0
        /// Loosest Value: 0.5 (Investopedia suggests smaller ranges are acceptable in volatile markets if gaps are clear).
        /// </summary>
        public static double MinRange { get; set; } = 0.5;

        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "AbandonedBaby";

        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A rare bullish reversal pattern with a bearish candle, Doji, and bullish candle, each gapped from the previous. Signals potential reversal from downtrend to uptrend."
            : "A rare bearish reversal pattern with a bullish candle, Doji, and bearish candle, each gapped from the previous. Signals potential reversal from uptrend to downtrend.";
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
        private readonly bool IsBullish;

        /// <summary>
        /// Initializes a new instance of the AbandonedBabyPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="avgVolume">The average volume.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public AbandonedBabyPattern(List<int> candles, CandleMids[] prices,
            double avgVolume, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Asynchronously determines if an Abandoned Baby pattern is present at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<AbandonedBabyPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            bool isBullish)
        {
            if (index < 2) return null; // Need 3 candles
            var candles = new List<int> { index }; // Third candle
            var thirdPrices = prices[index];
            int middleIndex = index - 1;

            // Doji check: Body = 1 or = 10% of range, range = 1
            var middleMetrics = await GetCandleMetricsAsync(metricsCache, middleIndex, prices, trendLookback, false);
            if (middleMetrics.TotalRange < MinRange ||
                (middleMetrics.BodySize > DojiBodyMax &&
                 middleMetrics.BodySize > DojiBodyRangeRatio * middleMetrics.TotalRange))
                return null;
            candles.Insert(0, middleIndex);

            int firstIndex = middleIndex - 1;
            if (firstIndex < 0) return null;
            candles.Insert(0, firstIndex);
            CandleMids firstPrices = prices[firstIndex];
            CandleMids middlePrices = prices[middleIndex];
            var firstMetrics = await GetCandleMetricsAsync(metricsCache, firstIndex, prices, trendLookback, false);
            var thirdMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            double trendThreshold = 0.5; // Adjustable threshold for trend flexibility

            if (isBullish)
            {
                bool isFirstBearish = firstMetrics.IsBearish;
                bool hasInitialGap = middlePrices.High < firstPrices.Low;
                bool hasSecondGap = thirdPrices.Low > middlePrices.High;
                bool isFinalReversal = thirdMetrics.IsBullish;
                bool isPrecedingDowntrend = firstMetrics.LookbackMeanTrend[0] < trendThreshold; // Trend check: downtrend before bullish pattern

                if (isFirstBearish && hasInitialGap && hasSecondGap && isFinalReversal && isPrecedingDowntrend)
                    return new AbandonedBabyPattern(candles, prices,
                        thirdMetrics.GetAvgVolumeVsLookback(candles.Count), true);
            }
            else
            {
                bool isFirstBullish = firstMetrics.IsBullish;
                bool hasInitialGap = middlePrices.Low > firstPrices.High;
                bool hasSecondGap = thirdPrices.High < middlePrices.Low;
                bool isFinalReversal = thirdMetrics.IsBearish;
                bool isPrecedingUptrend = firstMetrics.LookbackMeanTrend[0] > -trendThreshold; // Trend check: uptrend before bearish pattern

                if (isFirstBullish && hasInitialGap && hasSecondGap && isFinalReversal && isPrecedingUptrend)
                    return new AbandonedBabyPattern(candles, prices,
                        thirdMetrics.GetAvgVolumeVsLookback(candles.Count), false);
            }

            return null;
        }

        // Keeping CalculateStrength as its an addition, not in original
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
            if (Candles.Count != 3)
                throw new InvalidOperationException("AbandonedBabyPattern must have exactly 3 candles.");

            int firstIndex = Candles[0];
            int middleIndex = Candles[1];
            int thirdIndex = Candles[2];

            var firstMetrics = metricsCache[firstIndex];
            var middleMetrics = metricsCache[middleIndex];
            var thirdMetrics = metricsCache[thirdIndex];

            var firstPrices = prices[firstIndex];
            var middlePrices = prices[middleIndex];
            var thirdPrices = prices[thirdIndex];

            // Power Score: Based on doji, gap, candle, volume strengths
            double dojiRatio = middleMetrics.BodySize / middleMetrics.TotalRange;
            double dojiStrength = Math.Max(1 - (dojiRatio / DojiBodyRangeRatio), 0);

            double gap1Size = IsBullish ? (firstPrices.Low - middlePrices.High) : (middlePrices.Low - firstPrices.High);
            double gap2Size = IsBullish ? (thirdPrices.Low - middlePrices.High) : (middlePrices.Low - thirdPrices.High);
            double normFactor = 0.01 * firstPrices.Close;
            double gapStrength = (gap1Size + gap2Size) / (2 * normFactor);
            gapStrength = Math.Min(gapStrength, 1);

            double firstBodyRatio = firstMetrics.BodySize / firstMetrics.TotalRange;
            double thirdBodyRatio = thirdMetrics.BodySize / thirdMetrics.TotalRange;
            double candleStrength = (firstBodyRatio + thirdBodyRatio) / 2;

            double volumeStrength = 0.5;
            if (avgVolume > 0)
            {
                double firstVolume = prices[firstIndex].Volume;
                double thirdVolume = prices[thirdIndex].Volume;
                double firstVolScore = Math.Min(firstVolume / avgVolume, 1);
                double thirdVolScore = Math.Min(thirdVolume / avgVolume, 1);
                volumeStrength = (firstVolScore + thirdVolScore) / 2;
            }

            double wDoji = 0.3, wGap = 0.3, wCandle = 0.3, wVolume = 0.1;
            double powerScore = (wDoji * dojiStrength + wGap * gapStrength +
                                 wCandle * candleStrength + wVolume * volumeStrength) / (wDoji + wGap + wCandle + wVolume);

            // Match Score: Deviation from thresholds
            double dojiDeviation = Math.Abs(middleMetrics.BodySize - DojiBodyMax) / DojiBodyMax;
            double rangeDeviation = Math.Abs(middleMetrics.TotalRange - MinRange) / MinRange;
            double matchScore = 1 - (dojiDeviation + rangeDeviation) / 2;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








