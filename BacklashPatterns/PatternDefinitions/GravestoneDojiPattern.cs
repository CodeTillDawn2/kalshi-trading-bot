using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

/// <summary>
/// Represents a Gravestone Doji candlestick pattern.
/// </summary>
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
    /// Purpose: Ensures the body remains small relative to the candle s range.
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
    /// <summary>
    /// Gets the base name of the pattern.
    /// </summary>
    public const string BaseName = "GravestoneDoji";
    /// <summary>
    /// Gets the name of the pattern.
    /// </summary>
    public override string Name => BaseName;
    /// <summary>
    /// Gets the description of the pattern.
    /// </summary>
    public override string Description => "A bearish reversal pattern in an uptrend with a Doji candle that opens and closes near its low with a long upper wick, showing rejection of higher prices and potential reversal from uptrend to downtrend.";
    /// <summary>
    /// Gets the direction of the pattern.
    /// </summary>
    public override PatternDirection Direction => PatternDirection.Bearish;
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
    /// Initializes a new instance of the GravestoneDojiPattern class.
    /// </summary>
    /// <param name="candles">The list of candle indices.</param>
    public GravestoneDojiPattern(List<int> candles) : base(candles)
    {
    }

    /// <summary>
    /// Asynchronously determines if a Gravestone Doji pattern is present at the specified index.
    /// </summary>
    /// <param name="metricsCache">The metrics cache.</param>
    /// <param name="index">The index of the candle.</param>
    /// <param name="trendLookback">The trend lookback period.</param>
    /// <param name="prices">The array of candle prices.</param>
    /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
    public static async Task<GravestoneDojiPattern?> IsPatternAsync(
        Dictionary<int, CandleMetrics> metricsCache,
        int index,
        int trendLookback,
        CandleMids[] prices)
    {
        var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

        if (metrics.TotalRange < MinRange || metrics.BodySize > BodyMax ||
            metrics.BodySize > BodyRangeRatio * metrics.TotalRange ||
            metrics.UpperWick < WickRangeRatio * metrics.TotalRange ||
            metrics.UpperWick < WickBodyRatio * metrics.BodySize ||
            metrics.LowerWick > LowerWickMaxRatio * metrics.TotalRange) return null;

        if (metrics.GetLookbackMeanTrend(1) <= TrendThreshold) return null;

        var candles = new List<int> { index };
        return new GravestoneDojiPattern(candles);
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
            throw new InvalidOperationException("GravestoneDojiPattern must have exactly 1 candle.");

        int index = Candles[0];
        var metrics = metricsCache[index];

        // Power Score: Based on range, body smallness, wick strength, trend
        double rangeScore = metrics.TotalRange / MinRange;
        rangeScore = Math.Min(rangeScore, 1);

        double bodyScore = 1 - (metrics.BodySize / BodyMax);
        bodyScore = Math.Clamp(bodyScore, 0, 1);

        double bodyRangeScore = 1 - (metrics.BodySize / (BodyRangeRatio * metrics.TotalRange));
        bodyRangeScore = Math.Clamp(bodyRangeScore, 0, 1);

        double wickRangeScore = metrics.UpperWick / (WickRangeRatio * metrics.TotalRange);
        wickRangeScore = Math.Min(wickRangeScore, 1);

        double wickBodyScore = metrics.UpperWick / (WickBodyRatio * metrics.BodySize);
        wickBodyScore = Math.Min(wickBodyScore, 1);

        double lowerWickScore = 1 - (metrics.LowerWick / (LowerWickMaxRatio * metrics.TotalRange));
        lowerWickScore = Math.Clamp(lowerWickScore, 0, 1);

        double trendScore = metrics.GetLookbackMeanTrend(1) / TrendThreshold;
        trendScore = Math.Min(trendScore, 1);

        double volumeScore = 0.5; // Placeholder

        double wRange = 0.15, wBody = 0.15, wBodyRange = 0.15, wWickRange = 0.15, wWickBody = 0.15, wLowerWick = 0.15, wTrend = 0.05, wVolume = 0.05;
        double powerScore = (wRange * rangeScore + wBody * bodyScore + wBodyRange * bodyRangeScore +
                             wWickRange * wickRangeScore + wWickBody * wickBodyScore + wLowerWick * lowerWickScore +
                             wTrend * trendScore + wVolume * volumeScore) /
                            (wRange + wBody + wBodyRange + wWickRange + wWickBody + wLowerWick + wTrend + wVolume);

        // Match Score: Deviation from thresholds
        double rangeDeviation = Math.Abs(metrics.TotalRange - MinRange) / MinRange;
        double bodyDeviation = Math.Abs(metrics.BodySize - BodyMax) / BodyMax;
        double wickDeviation = Math.Abs(metrics.UpperWick - WickRangeRatio * metrics.TotalRange) / (WickRangeRatio * metrics.TotalRange);
        double trendDeviation = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / TrendThreshold;
        double matchScore = 1 - (rangeDeviation + bodyDeviation + wickDeviation + trendDeviation) / 4;
        matchScore = Math.Clamp(matchScore, 0, 1);

        // Use historical cache for comparative strength
        Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
    }
}








