using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The Hikkake pattern is a three-candle reversal pattern used to identify potential reversals after a false breakout.
    /// - First candle: Establishes the range.
    /// - Second candle: An inside bar followed by a breakout (bearish) or breakdown (bullish).
    /// - Third candle: Reverses the breakout direction, signaling a trap for traders.
    /// Indicates: Bullish Hikkake suggests a reversal from downtrend to uptrend; Bearish suggests uptrend to downtrend.
    /// Source: https://www.investopedia.com/terms/h/hikkake-pattern.asp
    /// </summary>
    public class HikkakePattern : PatternDefinition
    {
        /// <summary>
        /// Maximum breach above the first candle s high for the inside bar. Allows slight flexibility.
        /// Strictest: 0.5 (original), Loosest: 1.0 (still inside with buffer, per loose Hikkake logic).
        /// </summary>
        public static double MaxInsideBarHighBuffer { get; } = 0.5;

        /// <summary>
        /// Maximum breach below the first candle s low for the inside bar. Allows slight flexibility.
        /// Strictest: 0.5 (original), Loosest: 1.0 (still inside with buffer, per loose Hikkake logic).
        /// </summary>
        public static double MaxInsideBarLowBuffer { get; } = 0.5;

        /// <summary>
        /// Minimum body size for the reversal candle. Ensures a significant reversal move.
        /// Strictest: 1.0 (original), Loosest: 0.5 (smaller body still notable, per broad reversal logic).
        /// </summary>
        public static double MinReversalBodySize { get; } = 1.0;

        /// <summary>
        /// Minimum trend strength for the prior trend. Confirms the preceding trend s validity.
        /// Strictest: 0.3 (original), Loosest: 0.1 (minimal trend still detectable, per loose definitions).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Hikkake";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish reversal pattern that initially appears as a bearish inside day but then breaks higher, trapping bearish traders. Signals potential strong upward reversal."
            : "A bearish reversal pattern that initially appears as a bullish inside day but then breaks lower, trapping bullish traders. Signals potential strong downward reversal.";
        private readonly bool IsBullish;
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
        /// Initializes a new instance of the HikkakePattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public HikkakePattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Determines if a Hikkake pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<HikkakePattern?> IsPatternAsync(
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            int firstIdx = index - 2;
            int secondIdx = index - 1;
            int thirdIdx = index;

            var firstMetrics = await GetCandleMetricsAsync(metricsCache, firstIdx, prices, trendLookback, false);
            var secondMetrics = await GetCandleMetricsAsync(metricsCache, secondIdx, prices, trendLookback, false);
            var thirdMetrics = await GetCandleMetricsAsync(metricsCache, thirdIdx, prices, trendLookback, true);

            var firstAsk = prices[firstIdx];
            var secondAsk = prices[secondIdx];
            var thirdAsk = prices[thirdIdx];

            bool isInsideBar = secondAsk.High <= firstAsk.High + MaxInsideBarHighBuffer &&
                               secondAsk.Low >= firstAsk.Low - MaxInsideBarLowBuffer &&
                               secondMetrics.TotalRange < firstMetrics.TotalRange;
            if (!isInsideBar) return null;

            bool breakoutCondition = isBullish
                ? secondAsk.Close < firstAsk.Low
                : secondAsk.Close > firstAsk.High;
            if (!breakoutCondition) return null;

            bool reversal;
            if (isBullish)
            {
                reversal = thirdAsk.Close > secondAsk.High &&
                           thirdMetrics.IsBullish &&
                           thirdMetrics.BodySize >= MinReversalBodySize;
            }
            else
            {
                reversal = thirdAsk.Close < secondAsk.Low &&
                           thirdMetrics.IsBearish &&
                           thirdMetrics.BodySize >= MinReversalBodySize;
            }
            if (!reversal) return null;

            bool trendCondition = isBullish
                ? thirdMetrics.GetLookbackMeanTrend(3) <= -TrendThreshold
                : thirdMetrics.GetLookbackMeanTrend(3) >= TrendThreshold;
            if (!trendCondition) return null;

            var candles = new List<int> { firstIdx, secondIdx, thirdIdx };
            return new HikkakePattern(candles, isBullish);
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
                throw new InvalidOperationException("HikkakePattern must have exactly 3 candles.");

            int firstIdx = Candles[0], secondIdx = Candles[1], thirdIdx = Candles[2];

            var firstMetrics = metricsCache[firstIdx];
            var secondMetrics = metricsCache[secondIdx];
            var thirdMetrics = metricsCache[thirdIdx];

            var firstAsk = prices[firstIdx];
            var secondAsk = prices[secondIdx];
            var thirdAsk = prices[thirdIdx];

            // Power Score: Based on inside bar tightness, breakout strength, reversal strength, trend
            double insideBarScore = 1 - ((secondAsk.High - firstAsk.High) / MaxInsideBarHighBuffer +
                                         (firstAsk.Low - secondAsk.Low) / MaxInsideBarLowBuffer) / 2;
            insideBarScore = Math.Clamp(insideBarScore, 0, 1);

            double breakoutScore = 0;
            if (IsBullish)
            {
                breakoutScore = (firstAsk.Low - secondAsk.Close) / firstMetrics.TotalRange;
                breakoutScore = Math.Min(breakoutScore, 1);
            }
            else
            {
                breakoutScore = (secondAsk.Close - firstAsk.High) / firstMetrics.TotalRange;
                breakoutScore = Math.Min(breakoutScore, 1);
            }

            double reversalScore = thirdMetrics.BodySize / MinReversalBodySize;
            reversalScore = Math.Min(reversalScore, 1);

            double reversalDistance = 0;
            if (IsBullish)
            {
                reversalDistance = (thirdAsk.Close - secondAsk.High) / firstMetrics.TotalRange;
                reversalDistance = Math.Min(reversalDistance, 1);
            }
            else
            {
                reversalDistance = (secondAsk.Low - thirdAsk.Close) / firstMetrics.TotalRange;
                reversalDistance = Math.Min(reversalDistance, 1);
            }

            double trendStrength = Math.Abs(thirdMetrics.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wInside = 0.2, wBreakout = 0.2, wReversal = 0.2, wDistance = 0.2, wTrend = 0.1, wVolume = 0.1;
            double powerScore = (wInside * insideBarScore + wBreakout * breakoutScore + wReversal * reversalScore +
                                 wDistance * reversalDistance + wTrend * trendStrength + wVolume * volumeScore) /
                                (wInside + wBreakout + wReversal + wDistance + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(thirdMetrics.BodySize - MinReversalBodySize) / MinReversalBodySize;
            double trendDeviation = Math.Abs(thirdMetrics.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (bodyDeviation + trendDeviation) / 2;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








