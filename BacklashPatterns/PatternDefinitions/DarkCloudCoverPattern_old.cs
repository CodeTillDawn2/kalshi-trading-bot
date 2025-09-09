using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Dark Cloud Cover candlestick pattern, a 2-candle bearish reversal pattern.
    /// Indicates a potential reversal from an uptrend to a downtrend.
    /// Requirements (Source: Investopedia, BabyPips):
    /// - Occurs after an uptrend.
    /// - First candle: Bullish, showing strong buying.
    /// - Second candle: Bearish, opens above first close (often gapped up), closes significantly into first candle’s body (typically below midpoint but above open).
    /// </summary>
    public class DarkCloudCoverPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for both candles.
        /// Purpose: Ensures both candles have significant price movement.
        /// Loosest value: 0.3 (some sources allow smaller bodies for broader detection).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Minimum uptrend strength over the lookback period.
        /// Purpose: Confirms the pattern occurs after a notable uptrend.
        /// Loosest value: 0.1 (weaker uptrend still valid per some trading forums).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;

        /// <summary>
        /// Minimum penetration of the second candle into the first candle's body (from open).
        /// Purpose: Ensures bearish reversal strength by closing well into the first candle.
        /// Loosest value: 0.3 (30% penetration acceptable per BabyPips relaxed rules).
        /// </summary>
        public static double BodyPenetration { get; set; } = 0.4;
        public const string BaseName = "DarkCloudCover";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public DarkCloudCoverPattern(List<int> candles) : base(candles)
        {
        }

        public static DarkCloudCoverPattern IsPattern(
            int index,
            CandleMids[] prices,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 1 || index >= prices.Length) return null;
            var candles = new List<int> { index - 1, index };

            var prevMetrics = GetCandleMetrics(ref metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);
            var prevPrices = prices[index - 1];
            var currPrices = prices[index];

            // Uptrend check
            if (currMetrics.GetLookbackMeanTrend(2) <= TrendThreshold) return null;

            // Check minimum body size
            if (prevMetrics.BodySize < MinBodySize || currMetrics.BodySize < MinBodySize) return null;

            // First candle must be bullish
            if (!prevMetrics.IsBullish) return null;

            // Second candle must be bearish
            if (!currMetrics.IsBearish) return null;

            // Second opens at or above first close
            if (currPrices.Open < prevPrices.Close) return null;

            // Second closes below 60% of first body (from open), but above first open
            double relaxedMidPoint = prevPrices.Open + BodyPenetration * (prevPrices.Close - prevPrices.Open);
            if (currPrices.Close >= relaxedMidPoint || currPrices.Close <= prevPrices.Open) return null;

            return new DarkCloudCoverPattern(candles);
        }
    }
}
