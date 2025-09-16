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
    /// - Second candle: Bearish, opens above first close (often gapped up), closes significantly into first candle s body (typically below midpoint but above open).
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
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "DarkCloudCover";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
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
        /// Initializes a new instance of the DarkCloudCoverPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public DarkCloudCoverPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Dark Cloud Cover pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the second candle.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<DarkCloudCoverPattern?> IsPatternAsync(
            int index,
            CandleMids[] prices,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 1 || index >= prices.Length) return null;
            var candles = new List<int> { index - 1, index };

            var prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
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








