using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

    /// <summary>
    /// Represents an Upside Downside Gap Three Methods candlestick pattern.
    /// </summary>
    public class UpsideDownsideGapThreeMethodsPattern : PatternDefinition
{
    /// <summary>
    /// Minimum body size for the first and third candles as a percentage of their total range.
    /// Purpose: Ensures significant directional movement in the trend and reversal candles.
    /// Strictest: 1.0 (very large body), Loosest: 0.1 (minimal body still showing direction).
    /// </summary>
    public static double MinBodySize { get; set; } = 1.0;

    /// <summary>
    /// Threshold for confirming the directional trend (positive for bullish, negative for bearish).
    /// Purpose: Confirms the pattern occurs in a strong trend context for continuation or reversal.
    /// Strictest: 0.5 (strong trend), Loosest: 0.1 (minimal trend strength).
    /// </summary>
    public static double TrendThreshold { get; set; } = 0.5;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "UpsideDownsideGapThreeMethods";
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
        /// Initializes a new instance of the UpsideDownsideGapThreeMethodsPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public UpsideDownsideGapThreeMethodsPattern(List<int> candles) : base(candles)
    {
    }

        /// <summary>
        /// Determines if an Upside Downside Gap Three Methods pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<UpsideDownsideGapThreeMethodsPattern?> IsPatternAsync(
        int index,
        int trendLookback,
        bool isBullish,
        CandleMids[] prices,
        Dictionary<int, CandleMetrics> metricsCache)
    {
        if (index < 2 || index >= prices.Length) return null;

        int c1 = index - 2; // First candle
        int c2 = index - 1; // Second candle
        int c3 = index;     // Third candle

        CandleMetrics metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
        CandleMetrics metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
        CandleMetrics metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

        if (isBullish)
        {
            if (metrics1.BodySize < MinBodySize || !metrics1.IsBullish) return null;
            if (!metrics2.IsBullish || prices[c2].Open <= prices[c1].High) return null;
            if (!metrics3.IsBearish ||
                prices[c3].Close < prices[c1].Open ||
                prices[c3].Close > prices[c1].Close) return null;
            if (metrics3.BodySize < MinBodySize ||
                metrics3.TotalRange <= 0 ||
                metrics3.LowerWick > 0.1 * metrics3.TotalRange) return null;
            if (metrics3.GetLookbackMeanTrend(3) <= TrendThreshold) return null;
        }
        else
        {
            if (metrics1.BodySize < MinBodySize || !metrics1.IsBearish) return null;
            if (!metrics2.IsBearish ||
                prices[c2].Open >= prices[c1].Low ||
                prices[c2].High >= prices[c1].Low) return null;
            if (!metrics3.IsBullish ||
                prices[c3].Close < prices[c1].Close ||
                prices[c3].Close > prices[c1].Open) return null;
            if (metrics3.BodySize < MinBodySize ||
                metrics3.TotalRange <= 0 ||
                metrics3.UpperWick > 0.1 * metrics3.TotalRange) return null;
            if (metrics3.GetLookbackMeanTrend(3) >= -TrendThreshold) return null;
        }

        var candles = new List<int> { c1, c2, c3 };
        return new UpsideDownsideGapThreeMethodsPattern(candles);
    }
}








