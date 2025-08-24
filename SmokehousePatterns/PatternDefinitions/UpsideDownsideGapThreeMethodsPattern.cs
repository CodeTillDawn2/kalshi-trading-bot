using SmokehousePatterns;
using SmokehousePatterns.Helpers;
using SmokehousePatterns.PatternDefinitions;
using static SmokehousePatterns.Helpers.PatternUtils;

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
    public const string BaseName = "UpsideDownsideGapThreeMethods";
    public override string Name => BaseName;
    public override double Strength { get; protected set; }
    public override double Certainty { get; protected set; }
    public override double Uncertainty { get; protected set; }
    public UpsideDownsideGapThreeMethodsPattern(List<int> candles) : base(candles)
    {
    }

    public static UpsideDownsideGapThreeMethodsPattern IsPattern(
        int index,
        int trendLookback,
        bool isBullish,
        CandleMids[] prices,
        Dictionary<int, CandleMetrics> metricsCache)
    {
        if (index < 2 || index >= prices.Length) return null;

        int c1 = index - 2; // First candle
        int c2 = index - 1; // Second candle
        int c3 = index;     // Third candle

        CandleMetrics metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
        CandleMetrics metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
        CandleMetrics metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

        if (isBullish)
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
        return new UpsideDownsideGapThreeMethodsPattern(candles);
    }
}