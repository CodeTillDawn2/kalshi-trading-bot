using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
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
        public const string BaseName = "LadderBottom";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

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
    }
}








