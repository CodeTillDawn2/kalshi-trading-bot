using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

/// <summary>
/// Represents a Hammer candlestick pattern.
/// </summary>
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
    /// <summary>
    /// Gets the base name of the pattern.
    /// </summary>
    public const string BaseName = "Hammer";
    /// <summary>
    /// Gets the name of the pattern.
    /// </summary>
    public override string Name => BaseName;
    /// <summary>
    /// Gets the description of the pattern.
    /// </summary>
    public override string Description => "A bullish reversal pattern in a downtrend with a small body, long lower wick, and minimal upper wick. The long lower wick shows rejection of lower prices, signaling potential reversal from downtrend to uptrend.";
    /// <summary>
    /// Gets the direction of the pattern.
    /// </summary>
    public override PatternDirection Direction => PatternDirection.Bullish;
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
    /// Initializes a new instance of the HammerPattern class.
    /// </summary>
    /// <param name="candles">The list of candle indices.</param>
    public HammerPattern(List<int> candles) : base(candles)
    {
    }

    /// <summary>
    /// Asynchronously determines if a Hammer pattern is present at the specified index.
    /// </summary>
    /// <param name="metricsCache">The metrics cache.</param>
    /// <param name="index">The index of the candle.</param>
    /// <param name="trendLookback">The trend lookback period.</param>
    /// <param name="prices">The array of candle prices.</param>
    /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
    public static async Task<HammerPattern?> IsPatternAsync(
        Dictionary<int, CandleMetrics> metricsCache,
        int index,
        int trendLookback,
        CandleMids[] prices)
    {
        if (index < 1) return null;

        var candleMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

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
        if (Candles.Count != 1)
            throw new InvalidOperationException("HammerPattern must have exactly 1 candle.");

        int index = Candles[0];
        var candleMetrics = metricsCache[index];

        // Power Score: Based on range, body smallness, wick strength, trend
        double rangeScore = candleMetrics.TotalRange / MinRange;
        rangeScore = Math.Min(rangeScore, 1);

        double bodyScore = 1 - (candleMetrics.BodySize / (BodyRangeRatio * candleMetrics.TotalRange));
        bodyScore = Math.Clamp(bodyScore, 0, 1);

        double wickRangeScore = candleMetrics.LowerWick / (WickRangeRatio * candleMetrics.TotalRange);
        wickRangeScore = Math.Min(wickRangeScore, 1);

        double wickBodyScore = candleMetrics.LowerWick / (WickBodyRatio * candleMetrics.BodySize);
        wickBodyScore = Math.Min(wickBodyScore, 1);

        double trendScore = Math.Abs(candleMetrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
        trendScore = 1 - trendScore; // Closer to threshold is better

        double volumeScore = 0.5; // Placeholder

        double wRange = 0.2, wBody = 0.2, wWickRange = 0.2, wWickBody = 0.2, wTrend = 0.1, wVolume = 0.1;
        double powerScore = (wRange * rangeScore + wBody * bodyScore + wWickRange * wickRangeScore +
                             wWickBody * wickBodyScore + wTrend * trendScore + wVolume * volumeScore) /
                            (wRange + wBody + wWickRange + wWickBody + wTrend + wVolume);

        // Match Score: Deviation from thresholds
        double rangeDeviation = Math.Abs(candleMetrics.TotalRange - MinRange) / MinRange;
        double bodyDeviation = Math.Abs(candleMetrics.BodySize - BodyRangeRatio * candleMetrics.TotalRange) / (BodyRangeRatio * candleMetrics.TotalRange);
        double wickDeviation = Math.Abs(candleMetrics.LowerWick - WickRangeRatio * candleMetrics.TotalRange) / (WickRangeRatio * candleMetrics.TotalRange);
        double trendDeviation = Math.Abs(candleMetrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
        double matchScore = 1 - (rangeDeviation + bodyDeviation + wickDeviation + trendDeviation) / 4;
        matchScore = Math.Clamp(matchScore, 0, 1);

        // Use historical cache for comparative strength
        Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
    }
}








