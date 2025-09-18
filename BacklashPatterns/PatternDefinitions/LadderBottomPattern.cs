using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Ladder Bottom candlestick pattern.
    /// </summary>
    public class LadderBottomPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for candles in the pattern (relaxed check).
        /// Loosest: 0.1 (very small bodies); Strictest: 1.0 (larger bodies required).
        /// </summary>
        public static double MinBodySize { get; } = 0.5;

        /// <summary>
        /// Maximum difference between lows of the first four candles for similarity.
        /// Loosest: 2.0 (more variation allowed); Strictest: 0.5 (very tight similarity).
        /// </summary>
        public static double MaxLowDifference { get; } = 1.5;

        /// <summary>
        /// Threshold for trend strength to confirm prior downtrend.
        /// Loosest: 0.1 (weak trend); Strictest: 0.5 (strong trend required).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Minimum total price drop over the first four candles.
        /// Loosest: 0.1 (small drop); Strictest: 1.0 (significant drop required).
        /// </summary>
        public static double MinTotalDrop { get; } = 0.5;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "LadderBottom";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bullish reversal pattern in a downtrend with five candles where the first four have higher highs and higher lows, and the fifth is a long bullish candle that breaks the pattern, signaling potential reversal.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Bullish;
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
        /// Initializes a new instance of the LadderBottomPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public LadderBottomPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies a Ladder Bottom, a five-candle bullish reversal pattern.
        /// Requirements (sourced from StockCharts and technical analysis texts):
        /// - Four bearish candles with descending closes, often with similar lows.
        /// - Fifth candle is bullish, closing above the fourth candle s close.
        /// - Occurs after a downtrend, signaling exhaustion and reversal.
        /// Indicates: Potential bullish reversal as selling pressure weakens.
        /// </summary>
        public static async Task<LadderBottomPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 4) return null; // Need five candles

            int startIndex = index - 4;
            var asks = prices.Skip(startIndex).Take(5).ToArray();

            // Lazy load metrics for each of the five candles
            var metrics = new CandleMetrics[5];
            for (int i = 0; i < 5; i++)
            {
                metrics[i] = await GetCandleMetricsAsync(metricsCache, startIndex + i, prices, trendLookback, true);
            }

            // Relaxed descending closes (from original)
            int bearishCount = (metrics[0].IsBearish ? 1 : 0) + (metrics[1].IsBearish ? 1 : 0) +
                               (metrics[2].IsBearish ? 1 : 0) + (metrics[3].IsBearish ? 1 : 0);
            bool descendingCloses = bearishCount >= 3 && asks[0].Close > asks[3].Close;
            if (!descendingCloses) return null;

            // Relaxed total drop (from original, using lookbackAvgRange unavailable, hardcoded)
            double totalDrop = asks[0].Close - asks[3].Close;
            if (totalDrop < MinTotalDrop) return null; // Original used 0.3 * lookbackAvgRange, simplified

            // Reversal candle (from original)
            bool reversalCandle = metrics[4].IsBullish && asks[4].Close > asks[3].Close;
            if (!reversalCandle) return null;

            // Relaxed similar lows (from original)
            double minLow = asks.Take(4).Min(c => c.Low);
            double maxLow = asks.Take(4).Max(c => c.Low);
            bool similarLows = (maxLow - minLow) <= MaxLowDifference;
            if (!similarLows) return null;

            // Relaxed trend (from original)
            bool hasDowntrend = metrics[4].GetLookbackMeanTrend(5) <= -TrendThreshold;
            if (!hasDowntrend) return null;

            // Define the candle indices for the pattern (five candles)
            var candles = new List<int> { startIndex, startIndex + 1, startIndex + 2, startIndex + 3, startIndex + 4 };

            return new LadderBottomPattern(candles);
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
                throw new InvalidOperationException("LadderBottomPattern must have exactly 5 candles.");

            int startIndex = Candles[0];
            var metrics = new CandleMetrics[5];
            var asks = new CandleMids[5];

            for (int i = 0; i < 5; i++)
            {
                metrics[i] = metricsCache[Candles[i]];
                asks[i] = prices[Candles[i]];
            }

            // Power Score: Based on body sizes, low similarity, total drop, reversal strength, trend
            double bodyScore = (metrics[0].BodySize + metrics[1].BodySize + metrics[2].BodySize + metrics[3].BodySize + metrics[4].BodySize) / (5 * MinBodySize);
            bodyScore = Math.Min(bodyScore, 1);

            double lowSimilarityScore = 1 - ((asks.Take(4).Max(c => c.Low) - asks.Take(4).Min(c => c.Low)) / MaxLowDifference);
            lowSimilarityScore = Math.Clamp(lowSimilarityScore, 0, 1);

            double totalDropScore = (asks[0].Close - asks[3].Close) / MinTotalDrop;
            totalDropScore = Math.Min(totalDropScore, 1);

            double reversalScore = metrics[4].BodySize / MinBodySize;
            reversalScore = Math.Min(reversalScore, 1);

            double trendStrength = Math.Abs(metrics[4].GetLookbackMeanTrend(5) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.15, wLowSimilarity = 0.2, wTotalDrop = 0.2, wReversal = 0.2, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wBody * bodyScore + wLowSimilarity * lowSimilarityScore + wTotalDrop * totalDropScore +
                                 wReversal * reversalScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wBody + wLowSimilarity + wTotalDrop + wReversal + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(metrics[0].BodySize - MinBodySize) / MinBodySize;
            double lowDeviation = (asks.Take(4).Max(c => c.Low) - asks.Take(4).Min(c => c.Low)) / MaxLowDifference;
            double dropDeviation = Math.Abs((asks[0].Close - asks[3].Close) - MinTotalDrop) / MinTotalDrop;
            double trendDeviation = Math.Abs(metrics[4].GetLookbackMeanTrend(5) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (bodyDeviation + lowDeviation + dropDeviation + trendDeviation) / 4;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








