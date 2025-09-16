using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Three Line Strike candlestick pattern.
    /// </summary>
    public class ThreeLineStrikePattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for all candles in the pattern to ensure significance.
        /// Strictest: 1.5 (original logic), Loosest: 0.5 (still notable but less pronounced).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Threshold for confirming the strength of the preceding trend (  value).
        /// Strictest: 0.5 (original logic), Loosest: 0.1 (minimal trend still detectable).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.1;

        /// <summary>
        /// Represents a Three Line Strike pattern (Bullish or Bearish).
        /// - Bullish: A reversal pattern in a downtrend. Three bullish candles with ascending closes, followed by a bearish candle that strikes back, closing below the second candle s close.
        /// - Bearish: A reversal pattern in an uptrend. Three bearish candles with descending closes, followed by a bullish candle that strikes back, closing above the second candle s close.
        /// Requirements sourced from: https://www.investopedia.com/terms/t/three-line-strike.asp
        /// </summary>
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ThreeLineStrike";
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
        /// Initializes a new instance of the ThreeLineStrikePattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public ThreeLineStrikePattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Three Line Strike pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the fourth candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<ThreeLineStrikePattern?> IsPatternAsync(
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 3) return null;

            int c1 = index - 3;
            int c2 = index - 2;
            int c3 = index - 1;
            int c4 = index;

            if (c4 >= prices.Length) return null;

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, false);
            var metrics4 = await GetCandleMetricsAsync(metricsCache, c4, prices, trendLookback, true);

            if (isBullish)
            {
                if (!metrics1.IsBullish || metrics1.BodySize < MinBodySize) return null;
                if (!metrics2.IsBullish || metrics2.BodySize < MinBodySize) return null;
                if (!metrics3.IsBullish || metrics3.BodySize < MinBodySize) return null;
                if (!metrics4.IsBearish || metrics4.BodySize < MinBodySize) return null;

                // Step 3: Relaxed close checks with tolerance
                if (prices[c2].Close < prices[c1].Close - 0.5 || prices[c3].Close < prices[c2].Close - 0.5) return null;
                // Step 4: Low constraints removed (no code here)
                // Step 5: Simplified strike condition
                if (prices[c4].Close >= prices[c3].Close) return null;
                // Step 6: Trend check only if c1 >= 0
                if (c1 >= 0 && metrics4.GetLookbackMeanTrend(4) >= -TrendThreshold) return null;
            }
            else
            {
                if (!metrics1.IsBearish || metrics1.BodySize < MinBodySize) return null;
                if (!metrics2.IsBearish || metrics2.BodySize < MinBodySize) return null;
                if (!metrics3.IsBearish || metrics3.BodySize < MinBodySize) return null;
                if (!metrics4.IsBullish || metrics4.BodySize < MinBodySize) return null;

                // Step 3: Relaxed close checks with tolerance
                if (prices[c2].Close > prices[c1].Close + 0.5 || prices[c3].Close > prices[c2].Close + 0.5) return null;
                // Step 4: High constraints removed (no code here)
                // Step 5: Simplified strike condition
                if (prices[c4].Close <= prices[c3].Close) return null;
                // Step 6: Trend check only if c1 >= 0
                if (c1 >= 0 && metrics4.GetLookbackMeanTrend(4) <= TrendThreshold) return null;
            }

            var candles = new List<int> { c1, c2, c3, c4 };
            return new ThreeLineStrikePattern(candles);
        }
    }
}








