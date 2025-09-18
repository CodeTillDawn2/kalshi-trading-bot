using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Three Inside pattern (Up or Down).
    /// - Three Inside Up: A bullish reversal pattern with a long bearish candle, followed by a smaller bullish candle contained within its body, and a third bullish candle closing above the first candle's open, indicating a potential uptrend after a downtrend.
    /// - Three Inside Down: A bearish reversal pattern with a long bullish candle, followed by a smaller bearish candle contained within its body, and a third bearish candle closing below the first candle's open, signaling a potential downtrend after an uptrend.
    /// Source: Investopedia (https://www.investopedia.com/terms/t/three-inside-up.asp, https://www.investopedia.com/terms/t/three-inside-down.asp)
    /// </summary>
    public class ThreeInsidePattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first candle. Ensures the initial move is significant.
        /// Strictest: 2.0 (strong trend), Loosest: 0.5 (minimal trend).
        /// </summary>
        public static double MinBodySizeFirst { get; } = 1.0;

        /// <summary>
        /// Maximum body size for the second candle. Ensures it s smaller and contained.
        /// Strictest: 1.0 (tight containment), Loosest: 3.0 (allows larger retracement).
        /// </summary>
        public static double MaxBodySizeSecond { get; } = 2.0;

        /// <summary>
        /// Minimum body size for the third candle. Confirms the reversal strength.
        /// Strictest: 1.0 (strong confirmation), Loosest: 0.3 (minimal confirmation).
        /// </summary>
        public static double MinBodySizeThird { get; } = 0.5;

        /// <summary>
        /// Threshold for determining prior trend strength. Positive for uptrend, negative for downtrend.
        /// Strictest: 0.5 (clear trend), Loosest: 0.1 (weak trend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Buffer for containment of the second candle within the first. Allows flexibility in containment.
        /// Strictest: 0.0 (exact containment), Loosest: 1.0 (significant overlap).
        /// </summary>
        public static double ContainmentBuffer { get; } = 0.5;

        /// <summary>
        /// Minimum number of lookback candles to assess the trend.
        /// Strictest: 5 (longer context), Loosest: 2 (short context).
        /// </summary>
        public static int MinLookbackCount { get; } = 3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ThreeInside";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish reversal pattern with a large bearish candle followed by a smaller bullish candle contained within it, and a third bullish candle closing above the first. Signals potential reversal from downtrend to uptrend."
            : "A bearish reversal pattern with a large bullish candle followed by a smaller bearish candle contained within it, and a third bearish candle closing below the first. Signals potential reversal from uptrend to downtrend.";
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
        /// Initializes a new instance of the ThreeInsidePattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public ThreeInsidePattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Three Inside pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<ThreeInsidePattern?> IsPatternAsync(
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null; // Need 3 candles

            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            if (isBullish) // Three Inside Up
            {
                double body1 = metrics1.BodySize;
                double body2 = metrics2.BodySize;
                double body3 = metrics3.BodySize;

                // First candle: Bearish, significant body
                if (!metrics1.IsBearish || body1 < MinBodySizeFirst) return null;

                // Second candle: Bullish, smaller, contained within first s body (with buffer)
                if (!metrics2.IsBullish || body2 > MaxBodySizeSecond) return null;
                bool c2Inside = prices[c2].Open >= Math.Min(prices[c1].Open, prices[c1].Close) - ContainmentBuffer &&
                                prices[c2].Open <= Math.Max(prices[c1].Open, prices[c1].Close) + ContainmentBuffer &&
                                prices[c2].Close >= Math.Min(prices[c1].Open, prices[c1].Close) - ContainmentBuffer &&
                                prices[c2].Close <= Math.Max(prices[c1].Open, prices[c1].Close) + ContainmentBuffer;
                if (!c2Inside) return null;

                // Third candle: Bullish, closes above second s high (relaxed from first s open)
                if (!metrics3.IsBullish || body3 < MinBodySizeThird) return null;
                if (prices[c3].Close <= prices[c2].High) return null;

                // Downtrend check using CandleMetrics
                int lookbackCount = Math.Min(10, index - 2);
                if (lookbackCount < MinLookbackCount) return null;
                if (metrics3.GetLookbackMeanTrend(1) > -TrendThreshold ||
                    metrics3.GetLookbackTrendConsistency(1) < 0.4) return null;
            }
            else // Three Inside Down
            {
                double body1 = metrics1.BodySize;
                double body2 = metrics2.BodySize;
                double body3 = metrics3.BodySize;

                // First candle: Bullish, significant body
                if (!metrics1.IsBullish || body1 < MinBodySizeFirst) return null;

                // Second candle: Bearish, smaller, contained within first s body (with buffer)
                if (!metrics2.IsBearish || body2 > MaxBodySizeSecond) return null;
                bool c2Inside = prices[c2].Open >= Math.Min(prices[c1].Open, prices[c1].Close) - ContainmentBuffer &&
                                prices[c2].Open <= Math.Max(prices[c1].Open, prices[c1].Close) + ContainmentBuffer &&
                                prices[c2].Close >= Math.Min(prices[c1].Open, prices[c1].Close) - ContainmentBuffer &&
                                prices[c2].Close <= Math.Max(prices[c1].Open, prices[c1].Close) + ContainmentBuffer;
                if (!c2Inside) return null;

                // Third candle: Bearish, closes below second s low (relaxed from first s open)
                if (!metrics3.IsBearish || body3 < MinBodySizeThird) return null;
                if (prices[c3].Close >= prices[c2].Low) return null;

                // Uptrend check using CandleMetrics
                int lookbackCount = Math.Min(10, index - 2);
                if (lookbackCount < MinLookbackCount) return null;
                if (metrics3.GetLookbackMeanTrend(1) < TrendThreshold ||
                    metrics3.GetLookbackTrendConsistency(1) < 0.4) return null;
            }

            var candles = new List<int> { c1, c2, c3 };
            return new ThreeInsidePattern(candles);
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
            if (Candles.Count != 3)
                throw new InvalidOperationException("ThreeInsidePattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on body sizes, containment, breakout, trend
            double firstBodyScore = metrics1.BodySize / MinBodySizeFirst;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double secondBodyScore = 1 - (metrics2.BodySize / MaxBodySizeSecond);
            secondBodyScore = Math.Clamp(secondBodyScore, 0, 1);

            double thirdBodyScore = metrics3.BodySize / MinBodySizeThird;
            thirdBodyScore = Math.Min(thirdBodyScore, 1);

            // Containment score
            double prevMin = Math.Min(prices1.Open, prices1.Close);
            double prevMax = Math.Max(prices1.Open, prices1.Close);
            bool inside = prices2.Open >= prevMin - ContainmentBuffer && prices2.Open <= prevMax + ContainmentBuffer &&
                          prices2.Close >= prevMin - ContainmentBuffer && prices2.Close <= prevMax + ContainmentBuffer;
            double containmentScore = inside ? 1.0 : 0.5;

            // Breakout score
            double breakoutScore = 0;
            if (metrics1.IsBearish && metrics2.IsBullish && metrics3.IsBullish)
            {
                breakoutScore = (prices3.Close - prices2.High) / metrics1.BodySize;
                breakoutScore = Math.Min(breakoutScore, 1);
            }
            else if (metrics1.IsBullish && metrics2.IsBearish && metrics3.IsBearish)
            {
                breakoutScore = (prices2.Low - prices3.Close) / metrics1.BodySize;
                breakoutScore = Math.Min(breakoutScore, 1);
            }

            double trendStrength = Math.Abs(metrics3.GetLookbackMeanTrend(1));
            double trendConsistency = metrics3.GetLookbackTrendConsistency(1);

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.15, wSecond = 0.15, wThird = 0.15, wContainment = 0.2, wBreakout = 0.2, wTrend = 0.1, wVolume = 0.05;
            double powerScore = (wFirst * firstBodyScore + wSecond * secondBodyScore + wThird * thirdBodyScore +
                                 wContainment * containmentScore + wBreakout * breakoutScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirst + wSecond + wThird + wContainment + wBreakout + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double firstDeviation = Math.Abs(metrics1.BodySize - MinBodySizeFirst) / MinBodySizeFirst;
            double secondDeviation = Math.Abs(metrics2.BodySize - MaxBodySizeSecond) / MaxBodySizeSecond;
            double thirdDeviation = Math.Abs(metrics3.BodySize - MinBodySizeThird) / MinBodySizeThird;
            double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
            double consistencyDeviation = Math.Abs(trendConsistency - 0.4) / 0.4;
            double matchScore = 1 - (firstDeviation + secondDeviation + thirdDeviation + trendDeviation + consistencyDeviation) / 5;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








