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

        public const string BaseName = "AbandonedBaby";

        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        private readonly bool IsBullish;

        public AbandonedBabyPattern(List<int> candles, CandleMids[] prices,
            double avgVolume, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

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
        public void CalculateStrength(
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            double avgVolume)
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
            double totalWeight = wDoji + wGap + wCandle + wVolume;
            double strength = (wDoji * dojiStrength + wGap * gapStrength +
                              wCandle * candleStrength + wVolume * volumeStrength) / totalWeight;

            Strength = Math.Clamp(strength, 0, 1);
        }
    }
}








