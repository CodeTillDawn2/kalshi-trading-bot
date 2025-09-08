using SmokehouseDTOs;
using SmokehousePatterns;
using SmokehousePatterns.PatternDefinitions;
using static SmokehousePatterns.PatternUtils;

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
    public const string BaseName = "DownsideGapThreeMethods";
    public override string Name => BaseName;
    public override double Strength { get; protected set; }
    public override double Certainty { get; protected set; }
    public override double Uncertainty { get; protected set; }
    public DownsideGapThreeMethodsPattern(List<int> candles) : base(candles)
    {
    }

    public static DownsideGapThreeMethodsPattern IsPattern(
        int index,
        CandleMids[] prices,
        int trendLookback,
        Dictionary<int, CandleMetrics> metricsCache)
    {
        if (index < 2) return null;

        int c1 = index - 2;
        int c2 = index - 1;
        int c3 = index;

        var metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
        var metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
        var metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

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