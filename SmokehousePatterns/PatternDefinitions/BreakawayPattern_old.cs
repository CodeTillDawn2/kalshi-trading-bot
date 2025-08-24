using SmokehousePatterns.Helpers;
using static SmokehousePatterns.Helpers.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
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
        public const string BaseName = "Breakaway";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        private readonly bool IsBullish;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public BreakawayPattern_old(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        public static BreakawayPattern IsPattern(
            int index,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            bool isBullish,
            int trendLookback)
        {
            if (index < 4) return null;
            int startIndex = index - 4;

            var candles = new List<int> { startIndex, startIndex + 1, startIndex + 2, startIndex + 3, startIndex + 4 };

            var metrics1 = GetCandleMetrics(ref metricsCache, startIndex, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, startIndex + 1, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, startIndex + 2, prices, trendLookback, false);
            var metrics4 = GetCandleMetrics(ref metricsCache, startIndex + 3, prices, trendLookback, false);
            var metrics5 = GetCandleMetrics(ref metricsCache, startIndex + 4, prices, trendLookback, true);

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
                var metrics = GetCandleMetrics(ref metricsCache, i, prices, trendLookback, false);
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

            return new BreakawayPattern(candles, isBullish);
        }
    }
}