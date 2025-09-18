using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Breakaway candlestick pattern, a 5-candle reversal pattern.
    /// Bullish: Indicates a reversal from a downtrend to an uptrend after consolidation.
    /// Bearish: Indicates a reversal from an uptrend to a downtrend after consolidation.
    /// Requirements (Source: Investopedia, TradingView):
    /// - First candle: Strong move in trend direction (bearish for bullish pattern, bullish for bearish).
    /// - Middle 3 candles: Consolidation with small bodies and ranges.
    /// - Fifth candle: Strong move opposite to first, breaks out of consolidation with a gap.
    /// </summary>
    public class BreakawayPattern_old : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first and fifth candles to indicate a strong trend or breakout.
        /// Loosest value: 0.5 (smaller bodies can still indicate a trend, depending on volatility and timeframe).
        /// </summary>
        public static double MinBodySize { get; set; } = 1.0;

        /// <summary>
        /// Maximum body size for the middle three consolidation candles.
        /// Loosest value: 2.0 (allows slightly larger bodies while still resembling consolidation).
        /// </summary>
        public static double ConsolidationBodyMax { get; set; } = 1.5;

        /// <summary>
        /// Maximum total range (high - low) for the middle three consolidation candles.
        /// Loosest value: 4.0 (permits wider ranges in volatile markets while maintaining consolidation).
        /// </summary>
        public static double ConsolidationRangeMax { get; set; } = 3.0;

        /// <summary>
        /// Threshold for the mean trend strength over the lookback period to confirm a prior trend.
        /// Loosest value: 0.1 (weaker trend still qualifies as long as direction is clear).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;

        /// <summary>
        /// Minimum gap size between the fourth and fifth candles to confirm a breakout.
        /// Loosest value: 0.0 (no gap required in some interpretations, though a small gap is preferred).
        /// </summary>
        public static double GapSize { get; set; } = 0.2;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Breakaway";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish reversal pattern with five candles: a strong bearish first candle, three consolidation candles, and a strong bullish breakout candle with a gap."
            : "A bearish reversal pattern with five candles: a strong bullish first candle, three consolidation candles, and a strong bearish breakout candle with a gap.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => IsBullish ? PatternDirection.Bullish : PatternDirection.Bearish;
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
        /// Initializes a new instance of the BreakawayPattern_old class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public BreakawayPattern_old(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Determines if a Breakaway pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the fifth candle.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<BreakawayPattern_old?> IsPatternAsync(
            int index,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            bool isBullish,
            int trendLookback)
        {
            if (index < 4) return null;
            int startIndex = index - 4;

            var candles = new List<int> { startIndex, startIndex + 1, startIndex + 2, startIndex + 3, startIndex + 4 };

            var metrics1 = await GetCandleMetricsAsync(metricsCache, startIndex, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, startIndex + 1, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, startIndex + 2, prices, trendLookback, false);
            var metrics4 = await GetCandleMetricsAsync(metricsCache, startIndex + 3, prices, trendLookback, false);
            var metrics5 = await GetCandleMetricsAsync(metricsCache, startIndex + 4, prices, trendLookback, true);

            // Check trend and first candle
            double meanTrend = metrics5.GetLookbackMeanTrend(5);
            bool hasTrend = isBullish
                ? (metrics1.IsBearish && metrics1.BodySize >= MinBodySize && meanTrend <= -TrendThreshold)
                : (metrics1.IsBullish && metrics1.BodySize >= MinBodySize && meanTrend >= TrendThreshold);
            if (!hasTrend) return null;

            // Check consolidation (candles 2-4)
            bool isConsolidation = true;
            for (int i = startIndex + 1; i <= startIndex + 3; i++)
            {
                var metrics = await GetCandleMetricsAsync(metricsCache, i, prices, trendLookback, false);
                if (metrics.BodySize > ConsolidationBodyMax || metrics.TotalRange > ConsolidationRangeMax)
                {
                    isConsolidation = false;
                    break;
                }
            }
            if (!isConsolidation) return null;

            // Check breakout candle (5th)
            bool isLong = metrics5.BodySize >= MinBodySize;
            if (!isLong) return null;

            bool isBreakoutDirection = isBullish ? metrics5.IsBullish : metrics5.IsBearish;
            if (!isBreakoutDirection) return null;

            bool closesCorrectly = isBullish
                ? (prices[index].Close > prices[startIndex + 3].Close)
                : (prices[index].Close < prices[startIndex + 3].Close);
            if (!closesCorrectly) return null;

            bool hasGap = isBullish
                ? (prices[index].Open > prices[startIndex + 3].Close + GapSize)
                : (prices[index].Open < prices[startIndex + 3].Close - GapSize);
            if (!hasGap) return null;

            return new BreakawayPattern_old(candles, isBullish);
        }
    }
}








