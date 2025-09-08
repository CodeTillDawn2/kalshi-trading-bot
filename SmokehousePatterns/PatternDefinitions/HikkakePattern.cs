using SmokehouseDTOs;
using static SmokehousePatterns.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
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
        /// Maximum breach above the first candle’s high for the inside bar. Allows slight flexibility.
        /// Strictest: 0.5 (original), Loosest: 1.0 (still inside with buffer, per loose Hikkake logic).
        /// </summary>
        public static double MaxInsideBarHighBuffer { get; } = 0.5;

        /// <summary>
        /// Maximum breach below the first candle’s low for the inside bar. Allows slight flexibility.
        /// Strictest: 0.5 (original), Loosest: 1.0 (still inside with buffer, per loose Hikkake logic).
        /// </summary>
        public static double MaxInsideBarLowBuffer { get; } = 0.5;

        /// <summary>
        /// Minimum body size for the reversal candle. Ensures a significant reversal move.
        /// Strictest: 1.0 (original), Loosest: 0.5 (smaller body still notable, per broad reversal logic).
        /// </summary>
        public static double MinReversalBodySize { get; } = 1.0;

        /// <summary>
        /// Minimum trend strength for the prior trend. Confirms the preceding trend’s validity.
        /// Strictest: 0.3 (original), Loosest: 0.1 (minimal trend still detectable, per loose definitions).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;
        public const string BaseName = "Hikkake";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        private readonly bool IsBullish;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public HikkakePattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        public static HikkakePattern IsPattern(
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

            var firstMetrics = GetCandleMetrics(ref metricsCache, firstIdx, prices, trendLookback, false);
            var secondMetrics = GetCandleMetrics(ref metricsCache, secondIdx, prices, trendLookback, false);
            var thirdMetrics = GetCandleMetrics(ref metricsCache, thirdIdx, prices, trendLookback, true);

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
    }
}