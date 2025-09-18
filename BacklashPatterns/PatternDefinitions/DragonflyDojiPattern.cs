using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

/// <summary>
/// Represents a Dragonfly Doji candlestick pattern.
/// </summary>
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
/// <summary>
/// <summary>
/// Gets the name of the pattern.
/// </summary>
/// Gets the base name of the pattern.
/// </summary>
    /// </summary>
    public static double WickBodyRatio { get; set; } = 2.0;

    /// <summary>
    /// Gets the base name of the pattern.
    /// </summary>
    public const string BaseName = "DragonflyDoji";
    /// <summary>
    /// Gets the name of the pattern.
    /// </summary>
    public override string Name => BaseName;
    /// <summary>
    /// Gets the description of the pattern.
    /// </summary>
    public override string Description => "A bullish reversal pattern in a downtrend with a Doji candle that has a long lower wick and no upper wick, signaling rejection of lower prices.";
    /// <summary>
    /// Gets the direction of the pattern.
    /// </summary>
    public override PatternDirection Direction => PatternDirection.Bullish;
    /// <summary>
    /// Gets the strength of the pattern.
    /// </summary>
    /// <summary>
    /// Gets the strength of the pattern.
    /// </summary>
    public override double Strength { get; protected set; }
    /// <summary>
    /// Gets the certainty of the pattern.
    /// </summary>
    /// <summary>
    /// Gets the certainty of the pattern.
    /// </summary>
    public override double Certainty { get; protected set; }
    /// <summary>
    /// Gets the uncertainty of the pattern.
    /// </summary>
    /// <summary>
    /// Gets the uncertainty of the pattern.
    /// </summary>
    public override double Uncertainty { get; protected set; }
    /// <summary>
    /// Initializes a new instance of the DragonflyDojiPattern class.
    /// </summary>
    /// <param name="candles">The list of candle indices that form the pattern.</param>
    public DragonflyDojiPattern(List<int> candles) : base(candles)
    {
    }

    /// <summary>
    /// Asynchronously determines if a Dragonfly Doji pattern is present at the specified index.
    /// </summary>
    /// <param name="metricsCache">The cache of candle metrics.</param>
    /// <param name="index">The index of the candle to check.</param>
    /// <param name="trendLookback">The number of candles to look back for trend analysis.</param>
    /// <param name="prices">The array of candle mid prices.</param>
    /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
    public static async Task<DragonflyDojiPattern?> IsPatternAsync(
        Dictionary<int, CandleMetrics> metricsCache,
        int index,
        int trendLookback,
        CandleMids[] prices)
    {
        if (index < 0 || index >= prices.Length) return null;

        var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

        if (!(metrics.TotalRange >= MinRange && metrics.BodySize <= BodyMax &&
              metrics.BodySize <= BodyRangeRatio * metrics.TotalRange &&
              metrics.LowerWick >= WickRangeRatio * metrics.TotalRange &&
              metrics.LowerWick >= WickBodyRatio * metrics.BodySize &&
              metrics.UpperWick == 0)) return null;

        var candles = new List<int> { index };
        return new DragonflyDojiPattern(candles);
    }
}








