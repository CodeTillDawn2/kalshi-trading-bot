using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

public class DragonflyDojiPattern : PatternDefinition
{
    /// <summary>
    /// Minimum total range (high - low) of the candle to qualify as a Dragonfly Doji.
    /// Strictest: 3.0 (large range), Loosest: 1.0 (smallest acceptable range).
    /// </summary>
    public static double MinRange { get; set; } = 2.0;

    /// <summary>
    /// Maximum body size (open - close) allowed for the candle to be a Doji.
    /// Strictest: 0.5 (very small body), Loosest: 1.5 (largest acceptable body).
    /// </summary>
    public static double BodyMax { get; set; } = 1.0;

    /// <summary>
    /// Maximum ratio of body size to total range to maintain Doji characteristics.
    /// Strictest: 0.05 (tiny body relative to range), Loosest: 0.2 (larger body allowed).
    /// </summary>
    public static double BodyRangeRatio { get; set; } = 0.1;

    /// <summary>
    /// Minimum ratio of lower wick to total range for the Dragonfly shape.
    /// Strictest: 0.7 (long lower wick), Loosest: 0.3 (shortest acceptable wick).
    /// </summary>
    public static double WickRangeRatio { get; set; } = 0.5;

    /// <summary>
    /// Minimum ratio of lower wick length to body size to emphasize the Dragonfly shape.
    /// Strictest: 3.0 (very long wick relative to body), Loosest: 1.0 (minimal wick dominance).
    /// </summary>
    public static double WickBodyRatio { get; set; } = 2.0;

    public const string BaseName = "DragonflyDoji";
    public override string Name => BaseName;
    public override double Strength { get; protected set; }
    public override double Certainty { get; protected set; }
    public override double Uncertainty { get; protected set; }
    public DragonflyDojiPattern(List<int> candles) : base(candles)
    {
    }

    public static DragonflyDojiPattern IsPattern(
        Dictionary<int, CandleMetrics> metricsCache,
        int index,
        int trendLookback,
        CandleMids[] prices)
    {
        if (index < 0 || index >= prices.Length) return null;

        var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

        if (!(metrics.TotalRange >= MinRange && metrics.BodySize <= BodyMax &&
              metrics.BodySize <= BodyRangeRatio * metrics.TotalRange &&
              metrics.LowerWick >= WickRangeRatio * metrics.TotalRange &&
              metrics.LowerWick >= WickBodyRatio * metrics.BodySize &&
              metrics.UpperWick == 0)) return null;

        var candles = new List<int> { index };
        return new DragonflyDojiPattern(candles);
    }
}







