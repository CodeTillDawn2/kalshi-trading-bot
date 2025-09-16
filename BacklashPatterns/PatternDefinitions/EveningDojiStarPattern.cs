using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents an Evening Doji Star candlestick pattern.
    /// </summary>
    public class EveningDojiStarPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first candle to ensure a significant uptrend.
        /// Purpose: Filters out weak initial candles that don t establish a strong bullish move.
        /// Strictest: 3.0 (very strong candle), Loosest: 0.5 (minimal body size per loose interpretations).
        /// </summary>
        public static double MinBodySize { get; set; } = 2.0;

        /// <summary>
        /// Maximum body size for the doji candle to qualify as a doji.
        /// Purpose: Ensures the second candle represents indecision with a small body.
        /// Strictest: 0.5 (very tight doji), Loosest: 1.5 (allows larger bodies per loose doji definitions).
        /// </summary>
        public static double DojiBodyMax { get; set; } = 1.0;

        /// <summary>
        /// Maximum ratio of the doji s body size to its total range.
        /// Purpose: Ensures the doji has a small body relative to its wicks, reinforcing indecision.
        /// Strictest: 0.05 (extremely small body), Loosest: 0.25 (larger body allowed per loose standards).
        /// </summary>
        public static double DojiRangeRatio { get; set; } = 0.1;

        /// <summary>
        /// Minimum trend strength threshold for the prior uptrend.
        /// Purpose: Confirms a significant bullish trend precedes the pattern.
        /// Strictest: 0.7 (strong trend), Loosest: 0.1 (minimal trend per loose technical analysis).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.5;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "EveningDojiStar";
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
        /// Initializes a new instance of the EveningDojiStarPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public EveningDojiStarPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Asynchronously determines if an Evening Doji Star pattern is present at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<EveningDojiStarPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            int firstIdx = index - 2;
            int secondIdx = index - 1;
            int thirdIdx = index;

            var firstMetrics = await GetCandleMetricsAsync(metricsCache, firstIdx, prices, trendLookback, false);
            var secondMetrics = await GetCandleMetricsAsync(metricsCache, secondIdx, prices, trendLookback, false);
            var thirdMetrics = await GetCandleMetricsAsync(metricsCache, thirdIdx, prices, trendLookback, true);

            if (!firstMetrics.IsBullish || firstMetrics.BodySize < MinBodySize) return null;

            if (secondMetrics.BodySize > DojiBodyMax ||
                secondMetrics.BodySize > DojiRangeRatio * secondMetrics.TotalRange) return null;

            if (prices[secondIdx].Open < prices[firstIdx].Close) return null;

            if (!thirdMetrics.IsBearish || prices[thirdIdx].Close >= firstMetrics.BodyMidPoint) return null;

            if (thirdMetrics.GetLookbackMeanTrend(3) <= TrendThreshold) return null;

            var candles = new List<int> { firstIdx, secondIdx, thirdIdx };
            return new EveningDojiStarPattern(candles);
        }
    }
}








