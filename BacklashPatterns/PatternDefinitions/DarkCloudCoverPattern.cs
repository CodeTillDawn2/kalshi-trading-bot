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
/// - Second candle: Bearish, opens at/near first close, closes into first candle’s body.
/// Optimized for ML: Uses relative scaling based on lookback average range, maximally loosened for detection.
/// </summary>
public class DarkCloudCoverPattern2 : PatternDefinition
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
    /// Range: 0.1–1.0 (0.1 for tiniest bodies, 1.0 for stronger signals).
    /// </summary>
    public static double MinBodyToAvgRangeRatio { get; set; } = 0.2;

    /// <summary>
    /// Minimum bullish trend direction ratio in the lookback period to confirm a prior uptrend.
    /// Purpose: Ensures a minimal uptrend before the pattern; loosened to weakest viable threshold.
    /// Default: 0.5 (50% bullish candles, bare minimum for an uptrend).
    /// Range: 0.5–0.8 (0.5 for weakest uptrend, 0.8 for strong uptrend).
    /// </summary>
    public static double TrendDirectionRatioMin { get; set; } = 0.5;

    /// <summary>
    /// Minimum penetration of the second candle into the first candle’s body (from open).
    /// Purpose: Confirms bearish reversal; loosened to minimal intrusion.
    /// Default: 0.1 (10% penetration, bare minimum to enter body).
    /// Range: 0.1–0.5 (0.1 for slightest penetration, 0.5 for midpoint rule).
    /// </summary>
    public static double BodyPenetration { get; set; } = 0.1;

    /// <summary>
    /// Tolerance for the second candle’s open relative to the first candle’s close.
    /// Purpose: Relaxes the open condition maximally in markets with rare gaps.
    /// Default: 0.5 (50% of lookback average range, very forgiving).
    /// Range: 0.0–0.5 (0.0 for strict equality, 0.5 for maximum tolerance).
    /// </summary>
    public static double OpenToleranceToAvgRange { get; set; } = 0.5;

    public const string BaseName = "DarkCloudCover";
    public override string Name => BaseName;
    public override double Strength { get; protected set; }
    public override double Certainty { get; protected set; }
    public override double Uncertainty { get; protected set; }

    public DarkCloudCoverPattern2(List<int> candles) : base(candles)
    {
    }

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
}








