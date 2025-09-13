using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

public class HammerPattern : PatternDefinition
{
    /// <summary>
    /// Minimum total range (high to low) for the candle to qualify as a hammer.
    /// Strictest: 2.5 (large range); Loosest: 1.0 (minimal range).
    /// </summary>
    public static double MinRange { get; set; } = 2.0;

    /// <summary>
    /// Maximum ratio of body size to total range to ensure a small body.
    /// Strictest: 0.2 (tiny body); Loosest: 0.4 (larger but still small body).
    /// </summary>
    public static double BodyRangeRatio { get; set; } = 0.25;

    /// <summary>
    /// Minimum ratio of lower wick to total range for hammer shape.
    /// Strictest: 0.5 (long wick); Loosest: 0.3 (shorter wick).
    /// </summary>
    public static double WickRangeRatio { get; set; } = 0.4;

    /// <summary>
    /// Minimum ratio of lower wick to body size for hammer shape.
    /// Strictest: 2.0 (very long wick); Loosest: 1.0 (balanced wick).
    /// </summary>
    public static double WickBodyRatio { get; set; } = 1.5;

    /// <summary>
    /// Maximum mean trend value to confirm a preceding downtrend.
    /// Strictest: -0.5 (strong downtrend); Loosest: -0.1 (weak downtrend).
    /// </summary>
    public static double TrendThreshold { get; set; } = -0.3;
    public const string BaseName = "Hammer";
    public override string Name => BaseName;
    public override double Strength { get; protected set; }
    public override double Certainty { get; protected set; }
    public override double Uncertainty { get; protected set; }
    public HammerPattern(List<int> candles) : base(candles)
    {
    }

    public static HammerPattern? IsPattern(
        Dictionary<int, CandleMetrics> metricsCache,
        int index,
        int trendLookback,
        CandleMids[] prices)
    {
        if (index < 1) return null;

        var candleMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

        if (candleMetrics.TotalRange < MinRange) return null;
        if (candleMetrics.BodySize > BodyRangeRatio * candleMetrics.TotalRange) return null;
        if (!candleMetrics.IsBullish) return null;

        bool shape = candleMetrics.LowerWick >= WickRangeRatio * candleMetrics.TotalRange &&
                     candleMetrics.LowerWick >= WickBodyRatio * candleMetrics.BodySize &&
                     candleMetrics.UpperWick <= candleMetrics.BodySize;

        bool hasDowntrend = candleMetrics.GetLookbackMeanTrend(1) <= TrendThreshold;

        if (!(shape && hasDowntrend)) return null;

        var candles = new List<int> { index };
        return new HammerPattern(candles);
    }
}








