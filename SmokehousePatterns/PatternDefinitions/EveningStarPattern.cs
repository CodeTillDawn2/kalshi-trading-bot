using SmokehouseDTOs;
using SmokehousePatterns;
using SmokehousePatterns.Helpers;
using SmokehousePatterns.PatternDefinitions;
using static SmokehousePatterns.Helpers.PatternUtils;

public class EveningStarPattern : PatternDefinition
{
    /// <summary>
    /// Minimum body size for the first bullish candle.
    /// Strictest: 1.5 (significant move); Loosest: 0.5 (minimal noticeable body).
    /// </summary>
    public static double MinBodySize { get; set; } = 1.0;

    /// <summary>
    /// Maximum body size for the second candle to ensure it’s small.
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
    public const string BaseName = "EveningStar";
    public override string Name => BaseName;
    public override double Strength { get; protected set; }
    public override double Certainty { get; protected set; }
    public override double Uncertainty { get; protected set; }
    public EveningStarPattern(List<int> candles) : base(candles)
    {
    }

    public static EveningStarPattern IsPattern(
        int index,
        CandleMids[] prices,
        int trendLookback,
        Dictionary<int, CandleMetrics> metricsCache)
    {
        if (index < 2) return null;

        int firstIndex = index - 2;
        int secondIndex = index - 1;
        int thirdIndex = index;

        var firstMetrics = GetCandleMetrics(ref metricsCache, firstIndex, prices, trendLookback, false);
        var secondMetrics = GetCandleMetrics(ref metricsCache, secondIndex, prices, trendLookback, false);
        var thirdMetrics = GetCandleMetrics(ref metricsCache, thirdIndex, prices, trendLookback, true);

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