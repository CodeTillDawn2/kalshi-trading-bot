using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The High Wave Candle is a single-candle pattern indicating indecision in the market.
    /// - Large total range with a small body and long upper and lower wicks.
    /// - Suggests volatility and potential reversal or continuation depending on context.
    /// Indicates: Uncertainty; often appears at tops or bottoms.
    /// Source: https://www.babypips.com/learn/forex/high-wave-candlestick
    /// </summary>
    public class HighWaveCandlePattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range of the candle. Ensures significant volatility.
        /// Strictest: 3.0 (original), Loosest: 2.0 (still notable range per High Wave descriptions).
        /// </summary>
        public static double MinRange { get; } = 3.0;

        /// <summary>
        /// Minimum range compared to average lookback range. Confirms relative volatility.
        /// Strictest: 1.2 (original), Loosest: 1.0 (equal to average still valid, per loose logic).
        /// </summary>
        public static double MinRangeVsLookback { get; } = 1.2;

        /// <summary>
        /// Maximum body size as a percentage of total range. Keeps the body small.
        /// Strictest: 0.2 (original), Loosest: 0.3 (slightly larger body allowed, per loose definitions).
        /// </summary>
        public static double BodyRangeRatio { get; } = 0.2;

        /// <summary>
        /// Maximum absolute body size. Limits body size absolutely.
        /// Strictest: 1.5 (original), Loosest: 2.0 (still small relative to range, per broad logic).
        /// </summary>
        public static double BodyMax { get; } = 1.5;

        /// <summary>
        /// Minimum wick length as a percentage of total range. Ensures long wicks.
        /// Strictest: 0.3 (original), Loosest: 0.2 (shorter wicks still valid, per loose High Wave).
        /// </summary>
        public static double WickRangeRatio { get; } = 0.3;

        /// <summary>
        /// Maximum absolute trend consistency. Confirms indecision by limiting trend strength.
        /// Strictest: 0.5 (original), Loosest: 0.7 (allows slightly more trend, per loose indecision logic).
        /// </summary>
        public static double MaxTrendConsistency { get; } = 0.5;
/// <summary>Gets or sets the BaseName.</summary>
/// <summary>Gets or sets the BaseName.</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public const string BaseName = "HighWaveCandle";
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A candle with a very small body relative to its total range and long wicks, indicating high market volatility and indecision. Can signal potential reversal or continuation depending on context.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Neutral;
/// <summary>
/// </summary>
        public override string Name => BaseName;
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
/// <summary>
/// </summary>
        public override double Uncertainty { get; protected set; }

        public HighWaveCandlePattern(List<int> candles) : base(candles)
        {
        }

        public static async Task<HighWaveCandlePattern?> IsPatternAsync(int index, int trendLookback, CandleMids[] prices, Dictionary<int, CandleMetrics> metricsCache)
        {

            if (index < 1) return null;

            var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            if (metrics.TotalRange < MinRange) return null;
            if (metrics.TotalRange < MinRangeVsLookback * metrics.GetLookbackAvgRange(1)) return null;

            if (metrics.BodySize > BodyRangeRatio * metrics.TotalRange || metrics.BodySize > BodyMax) return null;

            if (metrics.UpperWick < WickRangeRatio * metrics.TotalRange ||
                metrics.LowerWick < WickRangeRatio * metrics.TotalRange) return null;

            if (Math.Abs(metrics.GetLookbackTrendConsistency(1)) > MaxTrendConsistency) return null;

            var candles = new List<int> { index };
            return new HighWaveCandlePattern(candles);
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
                throw new InvalidOperationException("HighWaveCandlePattern must have exactly 1 candle.");

            int index = Candles[0];
            var metrics = metricsCache[index];

            // Power Score: Based on range, body smallness, wick length, trend indecision
            double rangeScore = metrics.TotalRange / MinRange;
            rangeScore = Math.Min(rangeScore, 1);

            double rangeVsLookbackScore = metrics.TotalRange / (MinRangeVsLookback * metrics.GetLookbackAvgRange(1));
            rangeVsLookbackScore = Math.Min(rangeVsLookbackScore, 1);

            double bodyScore = 1 - (metrics.BodySize / (BodyRangeRatio * metrics.TotalRange));
            bodyScore = Math.Clamp(bodyScore, 0, 1);

            double bodyAbsScore = 1 - (metrics.BodySize / BodyMax);
            bodyAbsScore = Math.Clamp(bodyAbsScore, 0, 1);

            double upperWickScore = metrics.UpperWick / (WickRangeRatio * metrics.TotalRange);
            upperWickScore = Math.Min(upperWickScore, 1);

            double lowerWickScore = metrics.LowerWick / (WickRangeRatio * metrics.TotalRange);
            lowerWickScore = Math.Min(lowerWickScore, 1);

            double trendIndecisionScore = 1 - Math.Abs(metrics.GetLookbackTrendConsistency(1)) / MaxTrendConsistency;
            trendIndecisionScore = Math.Clamp(trendIndecisionScore, 0, 1);

            double volumeScore = 0.5; // Placeholder

            double wRange = 0.15, wRangeVsLookback = 0.15, wBody = 0.15, wBodyAbs = 0.15, wUpperWick = 0.15, wLowerWick = 0.15, wTrend = 0.05, wVolume = 0.05;
            double powerScore = (wRange * rangeScore + wRangeVsLookback * rangeVsLookbackScore + wBody * bodyScore +
                                 wBodyAbs * bodyAbsScore + wUpperWick * upperWickScore + wLowerWick * lowerWickScore +
                                 wTrend * trendIndecisionScore + wVolume * volumeScore) /
                                (wRange + wRangeVsLookback + wBody + wBodyAbs + wUpperWick + wLowerWick + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double rangeDeviation = Math.Abs(metrics.TotalRange - MinRange) / MinRange;
            double bodyDeviation = Math.Abs(metrics.BodySize - BodyRangeRatio * metrics.TotalRange) / (BodyRangeRatio * metrics.TotalRange);
            double wickDeviation = Math.Abs(metrics.UpperWick - WickRangeRatio * metrics.TotalRange) / (WickRangeRatio * metrics.TotalRange);
            double matchScore = 1 - (rangeDeviation + bodyDeviation + wickDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








