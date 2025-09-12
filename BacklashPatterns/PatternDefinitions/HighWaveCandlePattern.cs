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
        public const string BaseName = "HighWaveCandle";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public HighWaveCandlePattern(List<int> candles) : base(candles)
        {
        }

        public static HighWaveCandlePattern IsPattern(int index, int trendLookback, CandleMids[] prices, Dictionary<int, CandleMetrics> metricsCache)
        {

            if (index < 1) return null;

            var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            if (metrics.TotalRange < MinRange) return null;
            if (metrics.TotalRange < MinRangeVsLookback * metrics.GetLookbackAvgRange(1)) return null;

            if (metrics.BodySize > BodyRangeRatio * metrics.TotalRange || metrics.BodySize > BodyMax) return null;

            if (metrics.UpperWick < WickRangeRatio * metrics.TotalRange ||
                metrics.LowerWick < WickRangeRatio * metrics.TotalRange) return null;

            if (Math.Abs(metrics.GetLookbackTrendConsistency(1)) > MaxTrendConsistency) return null;

            var candles = new List<int> { index };
            return new HighWaveCandlePattern(candles);
        }
    }
}







