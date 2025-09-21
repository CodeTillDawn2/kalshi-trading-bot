using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

/// <summary>
/// Represents an Evening Star candlestick pattern.
/// </summary>
public class EveningStarPattern : PatternDefinition
{
    /// <summary>
    /// Minimum body size for the first bullish candle.
    /// Strictest: 1.5 (significant move); Loosest: 0.5 (minimal noticeable body).
    /// </summary>
    public static double MinBodySize { get; set; } = 1.0;

    /// <summary>
    /// Maximum body size for the second candle to ensure it s small.
    /// Strictest: 0.5 (very small); Loosest: 1.5 (moderate size).
    /// </summary>
    public static double SmallBodyMax { get; set; } = 1.0;

    /// <summary>
    /// Minimum body size for the third bearish candle.
    /// Strictest: 2.0 (strong move); Loosest: 1.0 (noticeable bearish move).
    /// </summary>
    public static double LargeBodyMin { get; set; } = 1.5;

    /// <summary>
    /// Minimum mean trend value to confirm a preceding uptrend.
    /// Strictest: 0.8 (strong uptrend); Loosest: 0.3 (weak uptrend).
    /// </summary>
    public static double TrendThreshold { get; set; } = 0.5;

    /// <summary>
    /// Minimum trend consistency to ensure reliability of the uptrend.
    /// Strictest: 0.9 (very consistent); Loosest: 0.4 (minimally consistent).
    /// </summary>
    public static double TrendConsistencyThreshold { get; set; } = 0.6;
    /// <summary>
    /// Gets the base name of the pattern.
    /// </summary>
    public const string BaseName = "EveningStar";
    /// <summary>
    /// Gets the name of the pattern.
    /// </summary>
    public override string Name => BaseName;
    /// <summary>
    /// Gets the description of the pattern.
    /// </summary>
    public override string Description => "A bearish reversal pattern in an uptrend with three candles: a large bullish candle, a small star candle, and a large bearish candle that closes below the midpoint of the first candle.";
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
    /// Initializes a new instance of the EveningStarPattern class.
    /// </summary>
    /// <param name="candles">The list of candle indices.</param>
    public EveningStarPattern(List<int> candles) : base(candles)
    {
    }

    /// <summary>
    /// Determines if an Evening Star pattern exists at the specified index.
    /// </summary>
    /// <param name="index">The index of the third candle.</param>
    /// <param name="prices">The array of candle prices.</param>
    /// <param name="trendLookback">The trend lookback period.</param>
    /// <param name="metricsCache">The metrics cache.</param>
    /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
    public static async Task<EveningStarPattern?> IsPatternAsync(
        int index,
        CandleMids[] prices,
        int trendLookback,
        Dictionary<int, CandleMetrics> metricsCache)
    {
        if (index < 2) return null;

        int firstIndex = index - 2;
        int secondIndex = index - 1;
        int thirdIndex = index;

        var firstMetrics = await GetCandleMetricsAsync(metricsCache, firstIndex, prices, trendLookback, false);
        var secondMetrics = await GetCandleMetricsAsync(metricsCache, secondIndex, prices, trendLookback, false);
        var thirdMetrics = await GetCandleMetricsAsync(metricsCache, thirdIndex, prices, trendLookback, true);

        if (thirdMetrics.GetLookbackMeanTrend(3) <= TrendThreshold ||
            thirdMetrics.GetLookbackTrendConsistency(3) < TrendConsistencyThreshold) return null;

        if (!firstMetrics.IsBullish || firstMetrics.BodySize < MinBodySize) return null;
        if (secondMetrics.BodySize > SmallBodyMax) return null;
        if (prices[secondIndex].Open < prices[firstIndex].Close) return null;
        if (!thirdMetrics.IsBearish) return null;
        if (thirdMetrics.BodySize < LargeBodyMin) return null;
        if (prices[thirdIndex].Close >= firstMetrics.BodyMidPoint) return null;

        var candles = new List<int> { firstIndex, secondIndex, thirdIndex };
        return new EveningStarPattern(candles);
    }
}








