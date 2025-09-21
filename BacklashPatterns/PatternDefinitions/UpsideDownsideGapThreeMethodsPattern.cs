using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;

/// <summary>
/// Represents an Upside Downside Gap Three Methods candlestick pattern.
/// </summary>
public class UpsideDownsideGapThreeMethodsPattern : PatternDefinition
{
    /// <summary>
    /// Minimum body size for the first and third candles as a percentage of their total range.
    /// Purpose: Ensures significant directional movement in the trend and reversal candles.
    /// Strictest: 1.0 (very large body), Loosest: 0.1 (minimal body still showing direction).
    /// </summary>
    public static double MinBodySize { get; set; } = 1.0;

    /// <summary>
    /// Threshold for confirming the directional trend (positive for bullish, negative for bearish).
    /// Purpose: Confirms the pattern occurs in a strong trend context for continuation or reversal.
    /// Strictest: 0.5 (strong trend), Loosest: 0.1 (minimal trend strength).
    /// </summary>
    public static double TrendThreshold { get; set; } = 0.5;
    /// <summary>
    /// Gets the base name of the pattern.
    /// </summary>
    public const string BaseName = "UpsideDownsideGapThreeMethods";
    /// <summary>
    /// Gets the name of the pattern.
    /// </summary>
    public override string Name => BaseName + "_" + Direction.ToString();
    /// <summary>
    /// Gets the description of the pattern.
    /// </summary>
    public override string Description => Direction == PatternDirection.Bullish
        ? "A bullish continuation pattern in an uptrend with a bullish candle followed by a bearish candle that gaps down and a third bullish candle that closes above the first, signaling continued upward momentum."
        : "A bearish continuation pattern in a downtrend with a bearish candle followed by a bullish candle that gaps up and a third bearish candle that closes below the first, signaling continued downward momentum.";
    /// <summary>
    /// Gets the direction of the pattern.
    /// </summary>
    public override PatternDirection Direction { get; }
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
    /// Initializes a new instance of the UpsideDownsideGapThreeMethodsPattern class.
    /// </summary>
    /// <param name="candles">The list of candle indices.</param>
    /// <param name="direction">The direction of the pattern.</param>
    public UpsideDownsideGapThreeMethodsPattern(List<int> candles, PatternDirection direction) : base(candles)
    {
        Direction = direction;
    }

    /// <summary>
    /// Determines if an Upside Downside Gap Three Methods pattern exists at the specified index.
    /// </summary>
    /// <param name="index">The index of the third candle.</param>
    /// <param name="trendLookback">The trend lookback period.</param>
    /// <param name="direction">The direction of the pattern to check for.</param>
    /// <param name="prices">The array of candle prices.</param>
    /// <param name="metricsCache">The metrics cache.</param>
    /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
    public static async Task<UpsideDownsideGapThreeMethodsPattern?> IsPatternAsync(
    int index,
    int trendLookback,
    PatternDirection direction,
    CandleMids[] prices,
    Dictionary<int, CandleMetrics> metricsCache)
    {
        if (index < 2 || index >= prices.Length) return null;

        int c1 = index - 2; // First candle
        int c2 = index - 1; // Second candle
        int c3 = index;     // Third candle

        CandleMetrics metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
        CandleMetrics metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
        CandleMetrics metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

        if (direction == PatternDirection.Bullish)
        {
            if (metrics1.BodySize < MinBodySize || !metrics1.IsBullish) return null;
            if (!metrics2.IsBullish || prices[c2].Open <= prices[c1].High) return null;
            if (!metrics3.IsBearish ||
                prices[c3].Close < prices[c1].Open ||
                prices[c3].Close > prices[c1].Close) return null;
            if (metrics3.BodySize < MinBodySize ||
                metrics3.TotalRange <= 0 ||
                metrics3.LowerWick > 0.1 * metrics3.TotalRange) return null;
            if (metrics3.GetLookbackMeanTrend(3) <= TrendThreshold) return null;
        }
        else
        {
            if (metrics1.BodySize < MinBodySize || !metrics1.IsBearish) return null;
            if (!metrics2.IsBearish ||
                prices[c2].Open >= prices[c1].Low ||
                prices[c2].High >= prices[c1].Low) return null;
            if (!metrics3.IsBullish ||
                prices[c3].Close < prices[c1].Close ||
                prices[c3].Close > prices[c1].Open) return null;
            if (metrics3.BodySize < MinBodySize ||
                metrics3.TotalRange <= 0 ||
                metrics3.UpperWick > 0.1 * metrics3.TotalRange) return null;
            if (metrics3.GetLookbackMeanTrend(3) >= -TrendThreshold) return null;
        }

        var candles = new List<int> { c1, c2, c3 };
        return new UpsideDownsideGapThreeMethodsPattern(candles, direction);
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
        if (Candles.Count != 3)
            throw new InvalidOperationException("UpsideDownsideGapThreeMethodsPattern must have exactly 3 candles.");

        int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

        var metrics1 = metricsCache[c1];
        var metrics2 = metricsCache[c2];
        var metrics3 = metricsCache[c3];

        var prices1 = prices[c1];
        var prices2 = prices[c2];
        var prices3 = prices[c3];

        // Power Score: Based on body sizes, gap, positioning, wick minimization, trend
        double firstBodyScore = metrics1.BodySize / MinBodySize;
        firstBodyScore = Math.Min(firstBodyScore, 1);

        double thirdBodyScore = metrics3.BodySize / MinBodySize;
        thirdBodyScore = Math.Min(thirdBodyScore, 1);

        double gapScore = 0;
        if (metrics1.IsBullish && metrics2.IsBullish && metrics3.IsBearish)
        {
            gapScore = (prices2.Open - prices1.High) / metrics1.BodySize;
            gapScore = Math.Min(gapScore, 1);
        }
        else if (metrics1.IsBearish && metrics2.IsBearish && metrics3.IsBullish)
        {
            gapScore = (prices1.Low - prices2.Open) / metrics1.BodySize;
            gapScore = Math.Min(gapScore, 1);
        }

        double positioningScore = 0;
        if (metrics1.IsBullish && metrics3.IsBearish)
        {
            positioningScore = 1 - ((prices1.Close - prices3.Close) / metrics1.BodySize);
            positioningScore = Math.Clamp(positioningScore, 0, 1);
        }
        else if (metrics1.IsBearish && metrics3.IsBullish)
        {
            positioningScore = (prices3.Close - prices1.Close) / metrics1.BodySize;
            positioningScore = Math.Min(positioningScore, 1);
        }

        double wickScore = 1 - ((metrics3.UpperWick + metrics3.LowerWick) / (0.2 * metrics3.TotalRange));
        wickScore = Math.Clamp(wickScore, 0, 1);

        double trendStrength = Math.Abs(metrics3.GetLookbackMeanTrend(3));

        double volumeScore = 0.5; // Placeholder

        double wFirst = 0.15, wThird = 0.15, wGap = 0.2, wPositioning = 0.2, wWick = 0.15, wTrend = 0.1, wVolume = 0.05;
        double powerScore = (wFirst * firstBodyScore + wThird * thirdBodyScore + wGap * gapScore +
                             wPositioning * positioningScore + wWick * wickScore + wTrend * trendStrength + wVolume * volumeScore) /
                            (wFirst + wThird + wGap + wPositioning + wWick + wTrend + wVolume);

        // Match Score: Deviation from thresholds
        double bodyDeviation = Math.Abs(metrics1.BodySize - MinBodySize) / MinBodySize;
        double thirdDeviation = Math.Abs(metrics3.BodySize - MinBodySize) / MinBodySize;
        double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
        double matchScore = 1 - (bodyDeviation + thirdDeviation + trendDeviation) / 3;
        matchScore = Math.Clamp(matchScore, 0, 1);

        // Use historical cache for comparative strength
        Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
    }
}








