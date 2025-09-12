using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The Harami pattern is a two-candle reversal pattern indicating a potential trend change.
    /// Requirements:
    /// - First candle: Large body in the direction of the prevailing trend (bullish in uptrend, bearish in downtrend).
    /// - Second candle: Smaller body fully contained within the first candle’s body, opposite direction.
    /// - Trend: Occurs after a defined trend (downtrend for Bullish Harami, uptrend for Bearish Harami).
    /// Indicates:
    /// - Bullish Harami: Potential reversal from downtrend to uptrend.
    /// - Bearish Harami: Potential reversal from uptrend to downtrend.
    /// Source: https://www.investopedia.com/terms/h/harami.asp
    /// </summary>
    public class HaramiPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first candle. Ensures the first candle has a significant body.
        /// Strictest: 1.5 (original), Loosest: 1.0 (still notable body per general candlestick analysis).
        /// </summary>
        public static double MinFirstBodySize { get; } = 1.5;

        /// <summary>
        /// Maximum body size for the second candle. Limits the second candle’s body to remain small.
        /// Strictest: 1.5 (original), Loosest: 2.0 (still smaller relative to first, per loose definitions).
        /// </summary>
        public static double MaxSecondBodySize { get; } = 1.5;

        /// <summary>
        /// Maximum second body size as a percentage of the first candle’s body. Ensures proportionality.
        /// Strictest: 0.5 (original), Loosest: 0.75 (allows slightly larger second body, per loose Harami definitions).
        /// </summary>
        public static double BodySizeRatio { get; } = 0.5;

        /// <summary>
        /// Minimum trend strength for the prior trend. Confirms the preceding trend’s validity.
        /// Strictest: 0.3 (original), Loosest: 0.1 (minimal trend still detectable, per broad reversal logic).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        public const string BaseName = "Harami";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        private readonly bool IsBullish;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public HaramiPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        public static HaramiPattern IsPattern(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            bool isBullish)
        {
            if (index < 1) return null;

            var prevMetrics = GetCandleMetrics(ref metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);
            CandleMids previousPrice = prices[index - 1];
            CandleMids currentPrice = prices[index];

            if (prevMetrics.BodySize < MinFirstBodySize) return null;

            if (currMetrics.BodySize > MaxSecondBodySize ||
                currMetrics.BodySize > BodySizeRatio * prevMetrics.BodySize) return null;

            double prevMin = Math.Min(previousPrice.Open, previousPrice.Close);
            double prevMax = Math.Max(previousPrice.Open, previousPrice.Close);
            bool inside = currentPrice.Open >= prevMin &&
                          currentPrice.Open <= prevMax &&
                          currentPrice.Close >= prevMin &&
                          currentPrice.Close <= prevMax;
            if (!inside) return null;

            bool prevDirection = isBullish ? prevMetrics.IsBearish : prevMetrics.IsBullish;
            bool currDirection = isBullish ? currMetrics.IsBullish : currMetrics.IsBearish;
            if (!prevDirection || !currDirection) return null;

            bool trendValid = isBullish
                ? currMetrics.GetLookbackAverageTrend(2) <= -TrendThreshold
                : currMetrics.GetLookbackAverageTrend(2) >= TrendThreshold;
            if (!trendValid) return null;

            var candles = new List<int> { index - 1, index };
            return new HaramiPattern(candles, isBullish);
        }
    }
}






