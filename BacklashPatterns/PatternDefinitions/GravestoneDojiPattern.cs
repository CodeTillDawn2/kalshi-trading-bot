using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

public class GravestoneDojiPattern : PatternDefinition
{
    /// <summary>
    /// Minimum total range of the candle to qualify as a significant move.
    /// Purpose: Ensures the candle has enough volatility to be meaningful.
    /// Strictest: 2.0 (highly volatile candle), Loosest: 0.5 (minimal range per loose technical analysis).
    /// </summary>
    public static double MinRange { get; set; } = 1.5;

    /// <summary>
    /// Maximum body size allowed to still classify as a doji.
    /// Purpose: Ensures the candle represents indecision with a small body.
    /// Strictest: 0.5 (very tight doji), Loosest: 2.0 (allows larger bodies per loose doji definitions).
    /// </summary>
    public static double BodyMax { get; set; } = 1.5;

    /// <summary>
    /// Maximum ratio of body size to total range.
    /// Purpose: Ensures the body remains small relative to the candle’s range.
    /// Strictest: 0.05 (extremely small body), Loosest: 0.25 (larger body allowed per loose standards).
    /// </summary>
    public static double BodyRangeRatio { get; set; } = 0.1;

    /// <summary>
    /// Minimum ratio of upper wick to total range.
    /// Purpose: Ensures a significant upper wick, characteristic of a gravestone doji.
    /// Strictest: 0.6 (very prominent wick), Loosest: 0.2 (minimal wick per loose definitions).
    /// </summary>
    public static double WickRangeRatio { get; set; } = 0.4;

    /// <summary>
    /// Minimum ratio of upper wick to body size.
    /// Purpose: Reinforces the prominence of the upper wick over the body.
    /// Strictest: 2.0 (very long wick), Loosest: 1.0 (wick just exceeds body per loose standards).
    /// </summary>
    public static double WickBodyRatio { get; set; } = 1.5;

    /// <summary>
    /// Maximum ratio of lower wick to total range.
    /// Purpose: Ensures the lower wick is minimal, a key feature of gravestone doji.
    /// Strictest: 0.05 (almost no lower wick), Loosest: 0.2 (small lower wick per loose definitions).
    /// </summary>
    public static double LowerWickMaxRatio { get; set; } = 0.1;

    /// <summary>
    /// Minimum trend strength threshold for the prior uptrend.
    /// Purpose: Confirms the pattern occurs after a significant bullish move.
    /// Strictest: 0.7 (strong trend), Loosest: 0.1 (minimal trend per loose technical analysis).
    /// </summary>
    public static double TrendThreshold { get; set; } = 0.3;
    public const string BaseName = "GravestoneDoji";
    public override string Name => BaseName;
    public override double Strength { get; protected set; }
    public override double Certainty { get; protected set; }
    public override double Uncertainty { get; protected set; }
    public GravestoneDojiPattern(List<int> candles) : base(candles)
    {
    }

    public static GravestoneDojiPattern IsPattern(
        Dictionary<int, CandleMetrics> metricsCache,
        int index,
        int trendLookback,
        CandleMids[] prices)
    {
        var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

        if (metrics.TotalRange < MinRange || metrics.BodySize > BodyMax ||
            metrics.BodySize > BodyRangeRatio * metrics.TotalRange ||
            metrics.UpperWick < WickRangeRatio * metrics.TotalRange ||
            metrics.UpperWick < WickBodyRatio * metrics.BodySize ||
            metrics.LowerWick > LowerWickMaxRatio * metrics.TotalRange) return null;

        if (metrics.GetLookbackMeanTrend(1) <= TrendThreshold) return null;

        var candles = new List<int> { index };
        return new GravestoneDojiPattern(candles);
    }
}
