using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;


/// <summary>
/// Represents a Downside Gap Three Methods pattern, a bearish continuation pattern.
/// This pattern consists of three candles where the second gaps down from the first,
/// and the third partially fills that gap, indicating continued downward momentum in a downtrend.
/// </summary>
public class DownsideGapThreeMethodsPattern : PatternDefinition
{
    /// <summary>
    /// Minimum body size for each candle to ensure significance.
    /// Strictest: 0.3 (small but notable body), Loosest: 1.0 (larger body still valid).
    /// </summary>
    public static double MinBodySize { get; set; } = 0.5;

    /// <summary>
    /// Minimum gap size between the first and second candles.
    /// Strictest: 0.2 (small gap), Loosest: 1.0 (larger gap still indicates separation).
    /// </summary>
    public static double GapSize { get; set; } = 0.5;

    /// <summary>
    /// Threshold for determining a bearish trend in the lookback period.
    /// Strictest: -0.1 (weak bearish trend), Loosest: -0.5 (strong bearish trend).
    /// </summary>
    public static double TrendThreshold { get; set; } = -0.3;

    /// <summary>
    /// Gets the base name identifier for this pattern type.
    /// </summary>
    public const string BaseName = "DownsideGapThreeMethods";

    /// <summary>
    /// Gets the name of this pattern instance.
    /// </summary>
    public override string Name => BaseName;
    /// <summary>
    /// Gets the description of the pattern.
    /// </summary>
    public override string Description => "A bearish continuation pattern in a downtrend with three candles where the second gaps down from the first and the third fills part of that gap, signaling continued downward momentum.";
    /// <summary>
    /// Gets the direction of the pattern.
    /// </summary>
    public override PatternDirection Direction => PatternDirection.Bearish;
    /// <summary>
    /// Gets or sets the strength of this pattern instance, calculated based on various factors.
    /// </summary>
    public override double Strength { get; protected set; }

    /// <summary>
    /// Gets or sets the certainty level of this pattern recognition.
    /// </summary>
    public override double Certainty { get; protected set; }
    /// <summary>
    /// Gets or sets the uncertainty level of this pattern recognition.
    /// </summary>
    public override double Uncertainty { get; protected set; }
    /// <summary>
    /// Initializes a new instance of the DownsideGapThreeMethodsPattern class.
    /// </summary>
    /// <param name="candles">The list of candle indices that form this pattern.</param>
    public DownsideGapThreeMethodsPattern(List<int> candles) : base(candles)
    {
    }

    /// <summary>
    /// Asynchronously determines if a Downside Gap Three Methods pattern exists at the specified index.
    /// The pattern requires three candles where the second gaps down from the first in a downtrend,
    /// and the third partially fills that gap while remaining bearish.
    /// </summary>
    /// <param name="index">The index of the third candle in the potential pattern.</param>
    /// <param name="prices">The array of candle price data.</param>
    /// <param name="trendLookback">The number of candles to look back for trend analysis.</param>
    /// <param name="metricsCache">The cache of pre-calculated candle metrics.</param>
    /// <returns>A DownsideGapThreeMethodsPattern instance if the pattern is detected, otherwise null.</returns>
    public static async Task<DownsideGapThreeMethodsPattern?> IsPatternAsync(
        int index,
        CandleMids[] prices,
        int trendLookback,
        Dictionary<int, CandleMetrics> metricsCache)
    {
        if (index < 2) return null;

        int c1 = index - 2;
        int c2 = index - 1;
        int c3 = index;

        var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
        var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
        var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

        var asks = prices.Skip(index - 2).Take(3).ToArray();

        if (metrics3.GetLookbackMeanTrend(3) > TrendThreshold) return null;

        if (!metrics1.IsBearish || metrics1.BodySize < MinBodySize) return null;
        if (asks[1].Open >= asks[0].Close - GapSize) return null;
        if (!metrics2.IsBearish || metrics2.BodySize < MinBodySize) return null;
        if (!metrics3.IsBullish || metrics3.BodySize < MinBodySize) return null;
        if (asks[2].Close <= asks[1].Open || asks[2].Close > asks[0].Close + GapSize) return null;

        var candles = new List<int> { c1, c2, c3 };
        return new DownsideGapThreeMethodsPattern(candles);
    }
}








