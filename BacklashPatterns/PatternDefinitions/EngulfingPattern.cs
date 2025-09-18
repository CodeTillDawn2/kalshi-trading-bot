using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

/// <summary>
/// Represents an Engulfing candlestick pattern.
/// </summary>
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
    /// <summary>
    /// Gets the base name of the pattern.
    /// </summary>
    public const string BaseName = "Engulfing";
    /// <summary>
    /// Gets the name of the pattern.
    /// </summary>
    public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
    /// <summary>
    /// Gets the description of the pattern.
    /// </summary>
    public override string Description => IsBullish
        ? "A bullish reversal pattern where a small bearish candle is followed by a larger bullish candle that completely engulfs it. Signals potential reversal from downtrend to uptrend."
        : "A bearish reversal pattern where a small bullish candle is followed by a larger bearish candle that completely engulfs it. Signals potential reversal from uptrend to downtrend.";
    private readonly bool IsBullish;
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
    /// Initializes a new instance of the EngulfingPattern class.
    /// </summary>
    /// <param name="candles">The list of candle indices.</param>
    /// <param name="isBullish">Whether the pattern is bullish.</param>
    public EngulfingPattern(List<int> candles, bool isBullish) : base(candles)
    {
        IsBullish = isBullish;
    }

    /// <summary>
    /// Asynchronously determines if an Engulfing pattern is present at the specified index.
    /// </summary>
    /// <param name="index">The index of the second candle.</param>
    /// <param name="metricsCache">The metrics cache.</param>
    /// <param name="prices">The array of candle prices.</param>
    /// <param name="meanTrend">The mean trend.</param>
    /// <param name="isBullish">Whether to check for bullish pattern.</param>
    /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
    public static async Task<EngulfingPattern?> IsPatternAsync(
        int index,
        Dictionary<int, CandleMetrics> metricsCache,
        CandleMids[] prices,
        double meanTrend,
        bool isBullish)
    {
        if (index < 1) return null;

        var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, 2, true);
        var prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, 2, false);

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
        if (Candles.Count != 2)
            throw new InvalidOperationException("EngulfingPattern must have exactly 2 candles.");

        int prevIndex = Candles[0];
        int currIndex = Candles[1];

        var prevMetrics = metricsCache[prevIndex];
        var currMetrics = metricsCache[currIndex];

        var previousPrice = prices[prevIndex];
        var currentPrice = prices[currIndex];

        // Power Score: Based on engulfing extent, trend strength, consistency
        double bodyRatio = currMetrics.BodySize / prevMetrics.BodySize;
        double engulfingExtent = Math.Min(bodyRatio, 2); // Cap at 2x for scoring

        double trendStrength = Math.Abs(currMetrics.GetLookbackMeanTrend(2));
        double trendConsistency = currMetrics.GetLookbackTrendConsistency(2);

        // Engulfing completeness
        double engulfingScore = 0;
        if (IsBullish)
        {
            if (currentPrice.Open < previousPrice.Close && currentPrice.Close > previousPrice.Open)
            {
                engulfingScore = 1.0;
            }
        }
        else
        {
            if (currentPrice.Open > previousPrice.Close && currentPrice.Close < previousPrice.Open)
            {
                engulfingScore = 1.0;
            }
        }

        double volumeScore = 0.5; // Placeholder

        double wEngulfing = 0.4, wTrendStrength = 0.3, wTrendConsistency = 0.2, wVolume = 0.1;
        double powerScore = (wEngulfing * engulfingExtent + wTrendStrength * trendStrength +
                             wTrendConsistency * Math.Abs(trendConsistency) + wVolume * volumeScore) /
                            (wEngulfing + wTrendStrength + wTrendConsistency + wVolume);

        // Match Score: Deviation from thresholds
        double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
        double consistencyDeviation = Math.Abs(Math.Abs(trendConsistency) - TrendConsistencyThreshold) / TrendConsistencyThreshold;
        double matchScore = 1 - (trendDeviation + consistencyDeviation) / 2;
        matchScore = Math.Clamp(matchScore, 0, 1);

        // Use historical cache for comparative strength
        Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
    }
}








