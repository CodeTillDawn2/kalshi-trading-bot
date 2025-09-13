using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Identifies a Shooting Star pattern, a single bearish reversal candlestick with a small body
    /// and long upper wick, signaling rejection of higher prices after an uptrend.
    /// 
    /// Requirements (Source: Investopedia, "Shooting Star"):
    /// - Small body near the low of the candle (bearish).
    /// - Long upper wick, typically at least twice the body size.
    /// - Minimal or no lower wick.
    /// - Occurs after an uptrend.
    /// - Indicates potential reversal to the downside.
    /// </summary>
    public class ShootingStarPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range for the candle to ensure sufficient volatility for the pattern.
        /// Loosest: 1.5 (smaller range); Strictest: 2.5 (larger range).
        /// </summary>
        public static double MinRange { get; } = 2.0;

        /// <summary>
        /// Maximum proportion of the total range that the body can occupy, keeping it small.
        /// Loosest: 0.5 (larger body); Strictest: 0.3 (smaller body).
        /// </summary>
        public static double BodyRangeRatio { get; } = 0.4;

        /// <summary>
        /// Minimum proportion of the total range that the upper wick must occupy, ensuring it�s long.
        /// Loosest: 0.3 (shorter wick); Strictest: 0.5 (longer wick).
        /// </summary>
        public static double WickRangeRatio { get; } = 0.4;

        /// <summary>
        /// Minimum ratio of the upper wick to the body size, emphasizing the wick�s dominance.
        /// Loosest: 1.0 (equal to body); Strictest: 2.0 (twice the body).
        /// </summary>
        public static double WickToBodyRatio { get; } = 1.5;

        /// <summary>
        /// Maximum proportion of the total range that the lower wick can occupy, keeping it minimal.
        /// Loosest: 0.3 (slightly larger); Strictest: 0.1 (almost none).
        /// </summary>
        public static double LowerWickMax { get; } = 0.2;

        /// <summary>
        /// Minimum trend strength threshold to confirm an uptrend prior to the pattern.
        /// Loosest: 0.2 (weaker trend); Strictest: 0.5 (strong trend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Minimum consistency of the prior uptrend.
        /// Loosest: 0.3 (less consistent); Strictest: 0.6 (highly consistent).
        /// </summary>
        public static double ConsistencyThreshold { get; } = 0.4;
        public const string BaseName = "ShootingStar";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public ShootingStarPattern(List<int> candles) : base(candles)
        {
        }

        public static ShootingStarPattern? IsPattern(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            // Early exit if there�s no prior candle
            if (index < 1) return null;

            // Lazy load metrics for the current candle
            var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Require an uptrend (original: meanTrend > 0.3, trendConsistency >= 0.4)
            if (metrics.GetLookbackMeanTrend(1) <= TrendThreshold || metrics.GetLookbackTrendConsistency(1) < ConsistencyThreshold) return null;

            // Check if total range meets minimum requirement (original: >= 2)
            if (metrics.TotalRange < MinRange) return null;

            // Verify shape conditions (original logic)
            bool shape = metrics.TotalRange > 0 &&
                         metrics.BodySize <= BodyRangeRatio * metrics.TotalRange && // Small body
                         metrics.UpperWick >= WickRangeRatio * metrics.TotalRange && // Long upper wick
                         metrics.UpperWick >= WickToBodyRatio * metrics.BodySize &&  // Wick-to-body ratio
                         metrics.LowerWick <= LowerWickMax * metrics.TotalRange &&   // Minimal lower wick
                         metrics.IsBearish; // Bearish direction
            if (!shape) return null;

            // Define the candle indices for the pattern (single candle)
            var candles = new List<int> { index };

            // Return the pattern instance if all conditions are met
            return new ShootingStarPattern(candles);
        }
    }
}








