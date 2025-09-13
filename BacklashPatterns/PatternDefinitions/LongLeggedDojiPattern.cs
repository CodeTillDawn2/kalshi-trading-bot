using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class LongLeggedDojiPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range for the candle to be significant.
        /// Loosest: 1.0 (smaller range); Strictest: 2.0 (larger range required).
        /// </summary>
        public static double MinRange { get; } = 1.0;

        /// <summary>
        /// Maximum body size to ensure a small body (indecision).
        /// Loosest: 2.0 (larger body allowed); Strictest: 0.5 (very small body).
        /// </summary>
        public static double BodyMax { get; } = 2.0;

        /// <summary>
        /// Minimum ratio of wick size to total range for long wicks.
        /// Loosest: 0.3 (shorter wicks); Strictest: 0.45 (very long wicks).
        /// </summary>
        public static double WickRangeRatio { get; } = 0.25;

        /// <summary>
        /// Minimum ratio of wick size to body size for long wicks relative to body.
        /// Loosest: 1.0 (equal to body); Strictest: 2.0 (much larger than body).
        /// </summary>
        public static double WickBodyRatio { get; } = 1.0;

        /// <summary>
        /// Minimum symmetry ratio between upper and lower wicks.
        /// Loosest: 0.25 (more asymmetry); Strictest: 0.9 (near perfect symmetry).
        /// </summary>
        public static double WickSymmetryMin { get; } = 0.2;

        /// <summary>
        /// Maximum symmetry ratio between upper and lower wicks.
        /// Loosest: 4.0 (more asymmetry); Strictest: 1.1 (near perfect symmetry).
        /// </summary>
        public static double WickSymmetryMax { get; } = 5.0;

        // Small constant to prevent division-by-zero
        private const double Epsilon = 0.001;


        public const string BaseName = "LongLeggedDoji";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public LongLeggedDojiPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies a Long-Legged Doji, a single-candle pattern.
        /// Requirements (sourced from BabyPips and Investopedia):
        /// - Small body (near equal open/close), indicating indecision.
        /// - Long upper and lower wicks, showing volatility and rejection at extremes.
        /// - Wicks roughly balanced, though slight asymmetry is allowed.
        /// Indicates: Potential reversal or continuation depending on prior trend, due to high indecision.
        /// </summary>
        public static LongLeggedDojiPattern? IsPattern(
             Dictionary<int, CandleMetrics> metricsCache,
             int index,
             int trendLookback,
             CandleMids[] prices)
        {
            // Lazy load metrics for the current candle
            var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            if (metrics.TotalRange <= 0) return null; // Reject if no range (invalid candle)

            if (metrics.UpperWick <= 0 || metrics.LowerWick <= 0) return null; // Must have both wicks

            if (metrics.TotalRange < MinRange) return null;

            double maxBodyRatio = 0.25 * metrics.TotalRange;
            if (metrics.BodySize > BodyMax || metrics.BodySize > maxBodyRatio) return null;

            double minWickLength = WickRangeRatio * metrics.TotalRange;
            if (metrics.UpperWick < minWickLength || metrics.LowerWick < minWickLength) return null;

            double wickRatio = metrics.UpperWick / (metrics.LowerWick + Epsilon);
            if (wickRatio < WickSymmetryMin || wickRatio > WickSymmetryMax) return null;


            bool hasLongWicks = metrics.UpperWick >= WickBodyRatio * metrics.BodySize &&
                                metrics.LowerWick >= WickBodyRatio * metrics.BodySize;
            if (!hasLongWicks) return null;

            // Pattern identified: single candle
            var candles = new List<int> { index };
            return new LongLeggedDojiPattern(candles);
        }
    }
}








