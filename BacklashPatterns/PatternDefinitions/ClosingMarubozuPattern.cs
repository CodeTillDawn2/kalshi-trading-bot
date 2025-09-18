using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
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

/// <summary>Gets or sets the BaseName.</summary>
/// <summary>Gets or sets the BaseName.</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public const string BaseName = "ClosingMarubozu";
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish pattern with a single candle that opens at its low and closes at its high, showing strong buying momentum. Can indicate continuation or reversal depending on trend context."
            : "A bearish pattern with a single candle that opens at its high and closes at its low, showing strong selling momentum. Can indicate continuation or reversal depending on trend context.";
/// <summary>
/// </summary>
        public override string Name => $"{BaseName}_{(IsBullish ? "Bullish" : "Bearish")}{(IsContinuation.HasValue ? (IsContinuation.Value ? "_Continuation" : "_Reversal") : "")}";
/// <summary>
/// </summary>
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
        public static async Task<ClosingMarubozuPattern?> IsPatternAsync(
            int index,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            int trendLookback,
            bool isBullish)
        {
            var candles = new List<int> { index };
            CandleMetrics candleMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

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
            if (Candles.Count != 1)
                throw new InvalidOperationException("ClosingMarubozuPattern must have exactly 1 candle.");

            int index = Candles[0];
            var candleMetrics = metricsCache[index];
            var currentPrices = prices[index];

            double avgRange = candleMetrics.LookbackAvgRange[PatternSize - 1];

            // Power Score: Based on body size, trend strength, continuation/reversal clarity
            double bodyScore = candleMetrics.BodySize / (MinBodyToAvgRangeRatio * avgRange);
            bodyScore = Math.Min(bodyScore, 1);

            double trendDirectionRatio = IsBullish ? candleMetrics.BullishRatio[PatternSize - 1] : candleMetrics.BearishRatio[PatternSize - 1];

            double continuationScore = 0.5; // Neutral if no trend
            if (IsContinuation.HasValue)
            {
                continuationScore = IsContinuation.Value ? 1.0 : 0.0; // 1 for continuation, 0 for reversal
            }

            // Check if it's a perfect Marubozu (no shadows)
            bool isPerfectMarubozu = IsBullish
                ? (currentPrices.Open == currentPrices.Low && currentPrices.Close == currentPrices.High)
                : (currentPrices.Open == currentPrices.High && currentPrices.Close == currentPrices.Low);
            double marubozuScore = isPerfectMarubozu ? 1.0 : 0.5;

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.3, wTrend = 0.3, wContinuation = 0.2, wMarubozu = 0.1, wVolume = 0.1;
            double powerScore = (wBody * bodyScore + wTrend * trendDirectionRatio +
                                 wContinuation * continuationScore + wMarubozu * marubozuScore + wVolume * volumeScore) /
                                (wBody + wTrend + wContinuation + wMarubozu + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(candleMetrics.BodySize - MinBodyToAvgRangeRatio * avgRange) / (MinBodyToAvgRangeRatio * avgRange);
            double trendDeviation = Math.Abs(trendDirectionRatio - OptionalTrendDirectionRatioMin) / OptionalTrendDirectionRatioMin;
            double matchScore = 1 - (bodyDeviation + trendDeviation) / 2;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








