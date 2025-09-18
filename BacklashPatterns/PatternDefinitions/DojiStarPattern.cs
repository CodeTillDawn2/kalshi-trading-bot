using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

public class DojiStarPattern : PatternDefinition
{
    /// <summary>
    /// Maximum allowable body size for the Doji candle (difference between open and close).
    /// Strictest: 0.5 (very small body), Loosest: 2.0 (still recognizably small relative to range).
    /// </summary>
    public static double DojiBodyMax { get; set; } = 1.5;

    /// <summary>
    /// Minimum total range (high to low) for the Doji candle to ensure significance.
    /// Strictest: 1.0 (small but notable range), Loosest: 2.5 (larger range still valid).
    /// </summary>
    public static double DojiRangeMin { get; set; } = 1.5;

    /// <summary>
    /// Minimum gap size between the first candle's close and the Doji's open.
    /// Strictest: 0.2 (small gap), Loosest: 1.0 (larger gap still indicates separation).
    /// </summary>
    public static double GapSize { get; set; } = 0.5;

    /// <summary>
    /// Threshold for determining trend strength in the lookback period.
    /// Strictest: 0.1 (weak trend), Loosest: 0.5 (stronger trend required).
    /// </summary>
    public static double TrendThreshold { get; set; } = 0.3;

    /// <summary>
    /// Minimum body size of the first candle to establish a strong prior trend.
    /// Strictest: 1.0 (moderate body), Loosest: 3.0 (very strong trend).
    /// </summary>
    public static double FirstCandleMinBodySize { get; set; } = 2.0;
    public const string BaseName = "DojiStar";
    public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
    /// <summary>
    /// Gets the description of the pattern.
    /// </summary>
    public override string Description => "A three-candle pattern with a Doji in the middle, signaling indecision and potential reversal after a strong trend.";
    /// <summary>
    /// Gets the direction of the pattern.
    /// </summary>
    public override PatternDirection Direction => IsBullish ? PatternDirection.Bullish : PatternDirection.Bearish;
    private readonly bool IsBullish;
    public override double Strength { get; protected set; }
    public override double Certainty { get; protected set; }
    public override double Uncertainty { get; protected set; }
    public DojiStarPattern(List<int> candles, bool isBullish) : base(candles)
    {
        IsBullish = isBullish;
    }

    public static async Task<DojiStarPattern?> IsPatternAsync(
        int index,
        int trendLookback,
        CandleMids[] prices,
        Dictionary<int, CandleMetrics> metricsCache)
    {
        if (index < 1) return null;

        var firstMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
        var secondMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
        var firstAsk = prices[index - 1];
        var secondAsk = prices[index];

        bool isDoji = secondMetrics.BodySize <= DojiBodyMax && secondMetrics.TotalRange >= DojiRangeMin;
        if (!isDoji) return null;

        bool hasGap = Math.Abs(secondAsk.Open - firstAsk.Close) >= GapSize;
        if (!hasGap) return null;

        bool strongTrend = firstMetrics.BodySize >= FirstCandleMinBodySize;
        if (!strongTrend) return null;

        bool isBullishDojiStar = secondMetrics.GetLookbackMeanTrend(2) < -TrendThreshold && firstMetrics.IsBearish;
        bool isBearishDojiStar = secondMetrics.GetLookbackMeanTrend(2) > TrendThreshold && firstMetrics.IsBullish;

        if (isBullishDojiStar)
            return new DojiStarPattern(new List<int> { index - 1, index }, true);
        else if (isBearishDojiStar)
            return new DojiStarPattern(new List<int> { index - 1, index }, false);
        return null;
    }
}








