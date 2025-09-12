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
        public const string BaseName = "ClosingMarubozu";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        private readonly bool IsBullish;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public ClosingMarubozuPattern_old(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        public static ClosingMarubozuPattern_old IsPattern(
            int index,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            int trendLookback,
            bool isBullish)
        {
            CandleMetrics candleMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);
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







