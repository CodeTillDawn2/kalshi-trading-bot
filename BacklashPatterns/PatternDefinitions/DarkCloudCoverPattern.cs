using BacklashDTOs;
using BacklashPatterns;
using BacklashPatterns.PatternDefinitions;
using static BacklashPatterns.PatternUtils;
/// <summary>
/// Represents a Dark Cloud Cover candlestick pattern, a 2-candle bearish reversal pattern.
/// Indicates a potential reversal from an uptrend to a downtrend.
/// Requirements (Source: Investopedia, BabyPips):
/// - Occurs after a clear uptrend, measured via TrendDirectionRatio.
/// - First candle: Bullish, showing significant buying.
/// - Second candle: Bearish, opens at/near first close, closes into first candle�s body.
/// Optimized for ML: Uses relative scaling based on lookback average range, maximally loosened for detection.
/// </summary>
public class DarkCloudCoverPattern : PatternDefinition
{
    /// <summary>
    /// Number of candles required to form the pattern.
    /// Default: 2 (standard for Dark Cloud Cover).
    /// </summary>
    public static int PatternSize { get; } = 2;

    /// <summary>
    /// Minimum body size for both candles relative to the lookback average range.
    /// Purpose: Ensures detectable price movement; loosened for broader detection.
    /// Default: 0.2 (20% of average range, minimal significance).
    /// Range: 0.1�1.0 (0.1 for tiniest bodies, 1.0 for stronger signals).
    /// </summary>
    public static double MinBodyToAvgRangeRatio { get; set; } = 0.2;

    /// <summary>
    /// Minimum bullish trend direction ratio in the lookback period to confirm a prior uptrend.
    /// Purpose: Ensures a minimal uptrend before the pattern; loosened to weakest viable threshold.
    /// Default: 0.5 (50% bullish candles, bare minimum for an uptrend).
    /// Range: 0.5�0.8 (0.5 for weakest uptrend, 0.8 for strong uptrend).
    /// </summary>
    public static double TrendDirectionRatioMin { get; set; } = 0.5;

    /// <summary>
    /// Minimum penetration of the second candle into the first candle�s body (from open).
    /// Purpose: Confirms bearish reversal; loosened to minimal intrusion.
    /// Default: 0.1 (10% penetration, bare minimum to enter body).
    /// Range: 0.1�0.5 (0.1 for slightest penetration, 0.5 for midpoint rule).
    /// </summary>
    public static double BodyPenetration { get; set; } = 0.1;

    /// <summary>
    /// Tolerance for the second candle�s open relative to the first candle�s close.
    /// Purpose: Relaxes the open condition maximally in markets with rare gaps.
    /// Default: 0.5 (50% of lookback average range, very forgiving).
    /// Range: 0.0�0.5 (0.0 for strict equality, 0.5 for maximum tolerance).
    /// </summary>
    public static double OpenToleranceToAvgRange { get; set; } = 0.5;

    /// <summary>
    /// Gets the base name of the pattern.
    /// </summary>
    public const string BaseName = "DarkCloudCover";
    /// <summary>
    /// Gets the name of the pattern.
    /// </summary>
    public override string Name => BaseName;
    /// <summary>
    /// Gets the description of the pattern.
    /// </summary>
    public override string Description => "A bearish reversal pattern in an uptrend with a bullish candle followed by a bearish candle that opens above the previous high but closes below the midpoint of the previous candle. Signals potential reversal from uptrend to downtrend.";
    /// <summary>
    /// Gets the direction of the pattern.
    /// </summary>
    public override PatternDirection Direction => PatternDirection.Bearish;
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
    /// Initializes a new instance of the DarkCloudCoverPattern2 class.
    /// </summary>
    /// <param name="candles">The list of candle indices.</param>
    public DarkCloudCoverPattern(List<int> candles) : base(candles)
    {
    }

    /// <summary>
    /// Determines if a Dark Cloud Cover pattern exists at the specified index.
    /// </summary>
    /// <param name="index">The index of the second candle.</param>
    /// <param name="prices">The array of candle prices.</param>
    /// <param name="trendLookback">The trend lookback period.</param>
    /// <param name="metricsCache">The metrics cache.</param>
    /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
    public static async Task<DarkCloudCoverPattern?> IsPatternAsync(
        int index,
        CandleMids[] prices,
        int trendLookback,
        Dictionary<int, CandleMetrics> metricsCache)
    {
        if (index < PatternSize - 1 + trendLookback) return null; // Ensure enough lookback
        var candles = new List<int> { index - 1, index };

        var prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
        var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
        var prevPrices = prices[index - 1];
        var currPrices = prices[index];

        // Calculate lookback average range
        double avgRange = currMetrics.LookbackAvgRange[PatternSize - 1];

        // Uptrend check: bare minimum bullish majority
        if (currMetrics.BullishRatio[PatternSize - 1] < TrendDirectionRatioMin) return null;

        // Check minimum body size: smallest detectable movement
        if (prevMetrics.BodySize < MinBodyToAvgRangeRatio * avgRange ||
            currMetrics.BodySize < MinBodyToAvgRangeRatio * avgRange) return null;

        // First candle must be bullish
        if (!prevMetrics.IsBullish) return null;

        // Second candle must be bearish
        if (!currMetrics.IsBearish) return null;

        // Second opens near first close: maximum tolerance
        double openTolerance = OpenToleranceToAvgRange * avgRange;
        if (currPrices.Open < prevPrices.Close - openTolerance) return null;

        // Second closes into first body: minimal penetration
        double penetrationPoint = prevPrices.Open + BodyPenetration * (prevPrices.Close - prevPrices.Open);
        if (currPrices.Close >= penetrationPoint || currPrices.Close <= prevPrices.Open) return null;

        return new DarkCloudCoverPattern(candles);
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
            throw new InvalidOperationException("DarkCloudCoverPattern must have exactly 2 candles.");

        int prevIndex = Candles[0];
        int currIndex = Candles[1];

        var prevMetrics = metricsCache[prevIndex];
        var currMetrics = metricsCache[currIndex];

        var prevPrices = prices[prevIndex];
        var currPrices = prices[currIndex];

        double avgRange = currMetrics.LookbackAvgRange[PatternSize - 1];

        // Power Score: Based on body sizes, penetration, open proximity, trend
        double prevBodyScore = prevMetrics.BodySize / (MinBodyToAvgRangeRatio * avgRange);
        prevBodyScore = Math.Min(prevBodyScore, 1);

        double currBodyScore = currMetrics.BodySize / (MinBodyToAvgRangeRatio * avgRange);
        currBodyScore = Math.Min(currBodyScore, 1);

        // Penetration: how deep into the first body the second closes
        double penetrationPoint = prevPrices.Open + BodyPenetration * (prevPrices.Close - prevPrices.Open);
        double actualPenetration = (penetrationPoint - currPrices.Close) / (prevPrices.Close - prevPrices.Open);
        double penetrationScore = Math.Min(actualPenetration / BodyPenetration, 1);

        // Open proximity: how close second open is to first close
        double openDiff = Math.Abs(currPrices.Open - prevPrices.Close);
        double openScore = 1 - (openDiff / (OpenToleranceToAvgRange * avgRange));
        openScore = Math.Clamp(openScore, 0, 1);

        double trendDirectionRatio = currMetrics.BullishRatio[PatternSize - 1];

        double volumeScore = 0.5; // Placeholder

        double wPrevBody = 0.2, wCurrBody = 0.2, wPenetration = 0.3, wOpen = 0.2, wTrend = 0.05, wVolume = 0.05;
        double powerScore = (wPrevBody * prevBodyScore + wCurrBody * currBodyScore + wPenetration * penetrationScore +
                             wOpen * openScore + wTrend * trendDirectionRatio + wVolume * volumeScore) /
                            (wPrevBody + wCurrBody + wPenetration + wOpen + wTrend + wVolume);

        // Match Score: Deviation from thresholds
        double bodyDeviation = Math.Abs(prevMetrics.BodySize - MinBodyToAvgRangeRatio * avgRange) / (MinBodyToAvgRangeRatio * avgRange);
        double trendDeviation = Math.Abs(trendDirectionRatio - TrendDirectionRatioMin) / TrendDirectionRatioMin;
        double penetrationDeviation = Math.Abs(actualPenetration - BodyPenetration) / BodyPenetration;
        double matchScore = 1 - (bodyDeviation + trendDeviation + penetrationDeviation) / 3;
        matchScore = Math.Clamp(matchScore, 0, 1);

        // Use historical cache for comparative strength
        Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
    }
}








