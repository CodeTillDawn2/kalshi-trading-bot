using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Closing Marubozu candlestick pattern, a single-candle pattern indicating strong momentum.
    /// Bullish: Strong buying pressure, potential continuation or reversal in a downtrend.
    /// Bearish: Strong selling pressure, potential continuation or reversal in an uptrend.
    /// Requirements (Source: Investopedia, DailyFX):
    /// - Single candle with no shadows (open equals low and close equals high for bullish;
    ///   open equals high and close equals low for bearish).
    /// - Significant body size, showing decisive movement.
    /// </summary>
    public class ClosingMarubozuPattern_old : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the candle to indicate strong momentum.
        /// Loosest value: 0.3 (smaller bodies can still show momentum in context of the timeframe).
        /// </summary>
        public static double MinBodySize { get; set; } = 1.0;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ClosingMarubozu";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
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
        /// Initializes a new instance of the ClosingMarubozuPattern_old class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public ClosingMarubozuPattern_old(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Determines if a Closing Marubozu pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the candle.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<ClosingMarubozuPattern_old?> IsPatternAsync(
            int index,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            int trendLookback,
            bool isBullish)
        {
            CandleMetrics candleMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
            var candles = new List<int> { index };

            // Check minimum body size
            if (candleMetrics.BodySize < MinBodySize) return null;

            // Direction check
            if (isBullish && !candleMetrics.IsBullish) return null;
            if (!isBullish && !candleMetrics.IsBearish) return null;

            // Strict Closing Marubozu criteria using raw price values
            var currentPrices = prices[index];
            bool meetsCriteria = isBullish
                ? (currentPrices.Open == currentPrices.Low && currentPrices.Close == currentPrices.High)
                : (currentPrices.Open == currentPrices.High && currentPrices.Close == currentPrices.Low);

            return meetsCriteria ? new ClosingMarubozuPattern_old(candles, isBullish) : null;
        }
    }
}








