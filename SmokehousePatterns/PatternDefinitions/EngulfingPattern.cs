using SmokehousePatterns;
using SmokehousePatterns.Helpers;
using SmokehousePatterns.PatternDefinitions;
using static SmokehousePatterns.Helpers.PatternUtils;

public class EngulfingPattern : PatternDefinition
{
    /// <summary>
    /// Minimum threshold for the absolute value of the trend strength to consider a valid trend.
    /// Strictest: 0.5 (strong trend required), Loosest: 0.1 (minimal trend).
    /// </summary>
    public static double TrendThreshold { get; set; } = 0.3;

    /// <summary>
    /// Threshold for trend consistency over the lookback period.
    /// Strictest: 0.7 (highly consistent), Loosest: 0.2 (minimally consistent).
    /// </summary>
    public static double TrendConsistencyThreshold { get; set; } = 0.43;
    public const string BaseName = "Engulfing";
    public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
    private readonly bool IsBullish;
    public override double Strength { get; protected set; }
    public override double Certainty { get; protected set; }
    public override double Uncertainty { get; protected set; }
    public EngulfingPattern(List<int> candles, bool isBullish) : base(candles)
    {
        IsBullish = isBullish;
    }

    public static EngulfingPattern IsPattern(
        int index,
        Dictionary<int, CandleMetrics> metricsCache,
        CandleMids[] prices,
        double meanTrend,
        bool isBullish)
    {
        if (index < 1) return null;

        var currMetrics = GetCandleMetrics(ref metricsCache, index, prices, 2, true);
        var prevMetrics = GetCandleMetrics(ref metricsCache, index - 1, prices, 2, false);

        bool hasTrend = isBullish
            ? currMetrics.GetLookbackMeanTrend(2) < 0 && Math.Abs(currMetrics.GetLookbackMeanTrend(2)) >= TrendThreshold &&
              currMetrics.GetLookbackTrendConsistency(2) <= -TrendConsistencyThreshold
            : currMetrics.GetLookbackMeanTrend(2) > 0 && currMetrics.GetLookbackMeanTrend(2) >= TrendThreshold &&
              currMetrics.GetLookbackTrendConsistency(2) >= TrendConsistencyThreshold;
        if (!hasTrend) return null;

        if (prevMetrics.BodySize <= 0 || currMetrics.BodySize <= prevMetrics.BodySize) return null;

        var previousPrice = prices[index - 1];
        var currentPrice = prices[index];

        bool engulf = isBullish
            ? (currentPrice.Open < previousPrice.Close && currentPrice.Close > previousPrice.Open)
            : (currentPrice.Open > previousPrice.Close && currentPrice.Close < previousPrice.Open);

        bool direction = isBullish ? currMetrics.IsBullish : currMetrics.IsBearish;
        bool prevDirection = isBullish ? prevMetrics.IsBearish : prevMetrics.IsBullish;

        if (!(engulf && direction && prevDirection)) return null;

        var candles = new List<int> { index - 1, index };
        return new EngulfingPattern(candles, isBullish);
    }
}