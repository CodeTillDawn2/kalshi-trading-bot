using SmokehouseDTOs;
using static SmokehousePatterns.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Closing Marubozu candlestick pattern, a single-candle pattern indicating strong momentum.
    /// Bullish Continuation: Strong buying pressure in an uptrend, potential continuation.
    /// Bullish Reversal: Strong buying pressure after a downtrend, potential reversal.
    /// Bearish Continuation: Strong selling pressure in a downtrend, potential continuation.
    /// Bearish Reversal: Strong selling pressure after an uptrend, potential reversal.
    /// Requirements (Source: Investopedia, DailyFX):
    /// - Single candle with no shadows (open equals low and close equals high for bullish;
    ///   open equals high and close equals low for bearish).
    /// - Significant body size relative to lookback average range, showing decisive movement.
    /// - Continuation or reversal determined by prior trend via TrendDirectionRatio.
    /// Optimized for ML: Relative scaling based on lookback average range, no gap reliance.
    /// </summary>
    public class ClosingMarubozuPattern : PatternDefinition
    {
        /// <summary>
        /// Number of candles required to form the pattern.
        /// Purpose: Defines the fixed size of the Closing Marubozu pattern (single candle).
        /// Default: 1 (standard for Closing Marubozu pattern)
        /// </summary>
        public static int PatternSize { get; } = 1;

        /// <summary>
        /// Minimum body size for the candle relative to the lookback average range.
        /// Purpose: Ensures the candle indicates strong momentum compared to prior volatility.
        /// Default: 1.5 (1.5 times the average range)
        /// Range: 1.0–2.0 (1.0 for moderate significance, 2.0 for very strong signals).
        /// </summary>
        public static double MinBodyToAvgRangeRatio { get; set; } = 1.0;

        /// <summary>
        /// Minimum trend direction ratio in the lookback period to confirm a prior trend.
        /// Purpose: Ensures a consistent prior trend for continuation/reversal classification.
        /// Default: 0.6 (60% of candles in trend direction)
        /// Range: 0.5–0.8 (0.5 for moderate consistency, 0.8 for strong, steady trends).
        /// </summary>
        public static double OptionalTrendDirectionRatioMin { get; set; } = 0.6;

        public const string BaseName = "ClosingMarubozu";
        public override string Name => $"{BaseName}_{(IsBullish ? "Bullish" : "Bearish")}{(IsContinuation.HasValue ? (IsContinuation.Value ? "_Continuation" : "_Reversal") : "")}";
        private readonly bool IsBullish;
        private readonly bool? IsContinuation; // Nullable to allow unclassified cases
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the ClosingMarubozuPattern class.
        /// </summary>
        /// <param name="candles">List of candle indices forming the pattern.</param>
        /// <param name="isBullish">True for bullish pattern, false for bearish.</param>
        /// <param name="isContinuation">True for continuation, false for reversal, null if trend unavailable.</param>
        public ClosingMarubozuPattern(List<int> candles, bool isBullish, bool? isContinuation = null) : base(candles)
        {
            IsBullish = isBullish;
            IsContinuation = isContinuation;
        }

        /// <summary>
        /// Determines if a Closing Marubozu pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the candle in the pattern.</param>
        /// <param name="metricsCache">Cache of candle metrics.</param>
        /// <param name="prices">Array of candle price data.</param>
        /// <param name="trendLookback">Number of candles to look back for trend and average range.</param>
        /// <param name="isBullish">True for bullish pattern, false for bearish.</param>
        /// <returns>A ClosingMarubozuPattern instance if detected, otherwise null.</returns>
        public static ClosingMarubozuPattern IsPattern(
            int index,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            int trendLookback,
            bool isBullish)
        {
            var candles = new List<int> { index };
            CandleMetrics candleMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            double avgRange = candleMetrics.LookbackAvgRange[PatternSize - 1];
            double minBodySize = MinBodyToAvgRangeRatio * avgRange;
            if (candleMetrics.BodySize < minBodySize) return null;

            if (isBullish && !candleMetrics.IsBullish) return null;
            if (!isBullish && !candleMetrics.IsBearish) return null;

            var currentPrices = prices[index];
            bool meetsCriteria = isBullish
                ? (currentPrices.Open == currentPrices.Low && currentPrices.Close == currentPrices.High)
                : (currentPrices.Open == currentPrices.High && currentPrices.Close == currentPrices.Low);
            if (!meetsCriteria) return null;

            bool? isContinuation = null;
            if (index >= trendLookback + PatternSize - 1)
            {
                bool priorTrendIsBullish = candleMetrics.BullishRatio[PatternSize - 1] >= OptionalTrendDirectionRatioMin;
                bool priorTrendIsBearish = candleMetrics.BearishRatio[PatternSize - 1] >= OptionalTrendDirectionRatioMin;

                if (isBullish)
                {
                    if (priorTrendIsBullish) isContinuation = true;
                    else if (priorTrendIsBearish) isContinuation = false;
                }
                else
                {
                    if (priorTrendIsBearish) isContinuation = true;
                    else if (priorTrendIsBullish) isContinuation = false;
                }
                if (!priorTrendIsBullish && !priorTrendIsBearish) isContinuation = null; // Explicit ambiguity
            }

            return new ClosingMarubozuPattern(candles, isBullish, isContinuation);
        }
    }
}