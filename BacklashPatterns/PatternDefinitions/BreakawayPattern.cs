using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Breakaway candlestick pattern, a five-candle reversal pattern.
    /// Bullish: Indicates a potential reversal from a downtrend to an uptrend.
    /// Bearish: Indicates a potential reversal from an uptrend to a downtrend.
    /// Requirements (Source: BabyPips, TradingView, Investopedia):
    /// - First candle: Strong move in the prior trend direction (bearish for bullish pattern, bullish for bearish pattern).
    /// - Middle three candles: Consolidation with small bodies and tight range.
    /// - Fifth candle: Strong breakout in the reversal direction, closing beyond the consolidation range.
    /// - Occurs after a clear trend (downtrend for bullish, uptrend for bearish), measured via TrendDirectionRatio.
    /// Optimized for ML: Relative scaling based on lookback average range, gap relaxed to 0 for markets without gaps.
    /// </summary>
    public class BreakawayPattern : PatternDefinition
    {
        /// <summary>
        /// Number of candles required to form the pattern.
        /// Purpose: Defines the fixed size of the Breakaway pattern (five candles).
        /// Default: 5 (standard for Breakaway pattern)
        /// </summary>
        public static int PatternSize { get; } = 5;

        /// <summary>
        /// Minimum body size for the first and fifth candles relative to the lookback average range.
        /// Purpose: Ensures the first and fifth candles are significant compared to prior volatility.
        /// Default: 1.0 (equal to average range)
        /// Range: 0.5�2.0 (0.5 for less strict significance, 2.0 for very strong signals).
        /// </summary>
        public static double MinBodyToAvgRangeRatio { get; set; } = 0.5;

        /// <summary>
        /// Maximum body size for the middle three consolidation candles relative to the lookback average range.
        /// Purpose: Ensures the consolidation candles have small bodies, indicating low volatility.
        /// Default: 0.5 (half the average range)
        /// Range: 0.3�1.0 (0.3 for very tight consolidation, 1.0 for slightly broader consolidation).
        /// </summary>
        public static double ConsolidationBodyToAvgRangeMax { get; set; } = 1.0;

        /// <summary>
        /// Maximum total range for each of the middle three consolidation candles relative to the lookback average range.
        /// Purpose: Ensures each consolidation candle has a small range, reinforcing a tight consolidation.
        /// Default: 1.0 (equal to average range)
        /// Range: 0.5�1.5 (0.5 for tighter ranges, 1.5 for broader acceptable ranges).
        /// </summary>
        public static double ConsolidationRangeToAvgRangeMax { get; set; } = 2.0;

        /// <summary>
        /// Maximum consolidation range (max high - min low of middle three candles) relative to the lookback average range.
        /// Purpose: Ensures the consolidation period is tight compared to prior volatility.
        /// Default: 2.0 (twice the average range)
        /// Range: 1.5�3.0 (1.5 for very tight consolidation, 3.0 for slightly broader consolidation).
        /// </summary>
        public static double ConsolidationTightRangeMax { get; set; } = 4.0;

        /// <summary>
        /// Minimum trend direction ratio in the lookback period to confirm a prior trend.
        /// Purpose: Ensures a consistent prior trend (downtrend for bullish, uptrend for bearish) before the pattern.
        /// Default: 0.6 (60% of candles in trend direction)
        /// Range: 0.5�0.8 (0.5 for moderate consistency, 0.8 for strong, steady trends).
        /// </summary>
        public static double TrendDirectionRatioMin { get; set; } = 0.3;

        public const string BaseName = "Breakaway";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish reversal pattern with a strong bearish candle, three consolidation candles, and a bullish breakout candle. Signals potential reversal from downtrend to uptrend."
            : "A bearish reversal pattern with a strong bullish candle, three consolidation candles, and a bearish breakout candle. Signals potential reversal from uptrend to downtrend.";
        private readonly bool IsBullish;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public BreakawayPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Determines if a Breakaway pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the fifth candle in the pattern.</param>
        /// <param name="prices">Array of candle price data.</param>
        /// <param name="metricsCache">Cache of candle metrics.</param>
        /// <param name="isBullish">True for bullish pattern, false for bearish.</param>
        /// <param name="trendLookback">Number of candles to look back for trend and average range.</param>
        /// <returns>A BreakawayPattern instance if detected, otherwise null.</returns>
        public static async Task<BreakawayPattern?> IsPatternAsync(
             int index,
             CandleMids[] prices,
             Dictionary<int, CandleMetrics> metricsCache,
             bool isBullish,
             int trendLookback)
        {
            if (index < PatternSize + trendLookback - 1) return null;
            int startIndex = index - (PatternSize - 1);

            // Define candle indices
            var candles = new List<int> { startIndex, startIndex + 1, startIndex + 2, startIndex + 3, index };

            // Get metrics for all five candles
            var metrics1 = await GetCandleMetricsAsync(metricsCache, startIndex, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, startIndex + 1, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, startIndex + 2, prices, trendLookback, false);
            var metrics4 = await GetCandleMetricsAsync(metricsCache, startIndex + 3, prices, trendLookback, false);
            var metrics5 = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Calculate lookback average range before the pattern
            double avgRange = metrics5.LookbackAvgRange[PatternSize - 1];

            // Check prior trend using TrendDirectionRatio (unchanged from original)
            double trendDirectionRatio = isBullish ? metrics5.BearishRatio[PatternSize - 1] : metrics5.BullishRatio[PatternSize - 1];
            if (trendDirectionRatio < TrendDirectionRatioMin) return null;

            // Check first candle: direction and size (unchanged from original)
            bool firstCandleDirection = isBullish ? metrics1.IsBearish : metrics1.IsBullish;
            if (!firstCandleDirection || metrics1.BodySize < MinBodyToAvgRangeRatio * avgRange) return null;

            // Check middle three candles: consolidation (unchanged from original)
            bool isConsolidation = true;
            foreach (var metrics in new[] { metrics2, metrics3, metrics4 })
            {
                if (metrics.BodySize > ConsolidationBodyToAvgRangeMax * avgRange ||
                    metrics.TotalRange > ConsolidationRangeToAvgRangeMax * avgRange)
                {
                    isConsolidation = false;
                    break;
                }
            }
            if (!isConsolidation) return null;

            // Calculate consolidation range
            double consolidationMaxHigh = Math.Max(prices[startIndex + 1].High,
                                          Math.Max(prices[startIndex + 2].High, prices[startIndex + 3].High));
            double consolidationMinLow = Math.Min(prices[startIndex + 1].Low,
                                         Math.Min(prices[startIndex + 2].Low, prices[startIndex + 3].Low));
            double consolidationRange = consolidationMaxHigh - consolidationMinLow;
            if (consolidationRange > ConsolidationTightRangeMax * avgRange) return null;

            // Step 4: Check fifth candle with relaxed requirements
            bool fifthCandleDirection = isBullish ? metrics5.IsBullish : metrics5.IsBearish;
            double minBreakoutBodySize = 0.5 * avgRange; // Relaxed from 1.0 to 0.5
            if (!fifthCandleDirection || metrics5.BodySize < minBreakoutBodySize) return null;

            // Step 4: Relaxed breakout condition (within 10% of consolidation boundary)
            bool breaksConsolidation = isBullish
                ? prices[index].Close > consolidationMaxHigh * 0.9  // 10% below max high
                : prices[index].Close < consolidationMinLow * 1.1;  // 10% above min low
            if (!breaksConsolidation) return null;

            // Step 5: Gap check removed entirely (no hasGap variable or condition)

            return new BreakawayPattern(candles, isBullish);
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
            if (Candles.Count != 5)
                throw new InvalidOperationException("BreakawayPattern must have exactly 5 candles.");

            int startIndex = Candles[0];
            int index = Candles[4]; // Fifth candle

            var metrics1 = metricsCache[startIndex];
            var metrics2 = metricsCache[startIndex + 1];
            var metrics3 = metricsCache[startIndex + 2];
            var metrics4 = metricsCache[startIndex + 3];
            var metrics5 = metricsCache[index];

            double avgRange = metrics5.LookbackAvgRange[PatternSize - 1];

            // Power Score: Based on first candle strength, consolidation tightness, fifth candle breakout, trend
            double firstBodyScore = metrics1.BodySize / (MinBodyToAvgRangeRatio * avgRange);
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double consolidationScore = 0;
            foreach (var metrics in new[] { metrics2, metrics3, metrics4 })
            {
                double bodyScore = 1 - (metrics.BodySize / (ConsolidationBodyToAvgRangeMax * avgRange));
                double rangeScore = 1 - (metrics.TotalRange / (ConsolidationRangeToAvgRangeMax * avgRange));
                consolidationScore += (bodyScore + rangeScore) / 2;
            }
            consolidationScore /= 3;

            double fifthBodyScore = metrics5.BodySize / avgRange;
            fifthBodyScore = Math.Min(fifthBodyScore, 1);

            double trendDirectionRatio = IsBullish ? metrics5.BearishRatio[PatternSize - 1] : metrics5.BullishRatio[PatternSize - 1];

            double volumeScore = 0.5; // Placeholder for volume if needed

            double wFirst = 0.25, wConsolidation = 0.25, wFifth = 0.25, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wFirst * firstBodyScore + wConsolidation * consolidationScore +
                                 wFifth * fifthBodyScore + wTrend * trendDirectionRatio + wVolume * volumeScore) /
                                (wFirst + wConsolidation + wFifth + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double firstDeviation = Math.Abs(metrics1.BodySize - MinBodyToAvgRangeRatio * avgRange) / (MinBodyToAvgRangeRatio * avgRange);
            double consolidationDeviation = 0;
            foreach (var metrics in new[] { metrics2, metrics3, metrics4 })
            {
                consolidationDeviation += Math.Abs(metrics.BodySize - ConsolidationBodyToAvgRangeMax * avgRange) / (ConsolidationBodyToAvgRangeMax * avgRange);
            }
            consolidationDeviation /= 3;
            double trendDeviation = Math.Abs(trendDirectionRatio - TrendDirectionRatioMin) / TrendDirectionRatioMin;
            double matchScore = 1 - (firstDeviation + consolidationDeviation + trendDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








