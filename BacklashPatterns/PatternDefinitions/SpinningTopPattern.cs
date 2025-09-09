using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Identifies a Spinning Top pattern, a single candlestick with a small body and long wicks,
    /// indicating indecision in the market.
    /// 
    /// Requirements (Source: TradingView, "Spinning Top"):
    /// - Small body (close near open), showing balance between buyers and sellers.
    /// - Long upper and lower wicks, roughly equal in length, exceeding the body size.
    /// - Total range indicates volatility, but body remains small_iterations

    public class SpinningTopPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range of the candle to ensure sufficient volatility.
        /// Strictest: 3.0 (high volatility); Loosest: 1.5 (minimal volatility).
        /// </summary>
        public static double MinRange { get; set; } = 2.5;

        /// <summary>
        /// Maximum ratio of body size to total range to define a small body.
        /// Strictest: 0.1 (tiny body); Loosest: 0.3 (broader small body).
        /// </summary>
        public static double BodyRangeRatio { get; set; } = 0.2;

        /// <summary>
        /// Maximum absolute body size to maintain indecision characteristics.
        /// Strictest: 1.0 (very small); Loosest: 2.0 (broader indecision).
        /// </summary>
        public static double BodyMax { get; set; } = 1.5;

        /// <summary>
        /// Minimum ratio of wick length to total range for significant wicks.
        /// Strictest: 0.35 (long wicks); Loosest: 0.2 (moderate wicks).
        /// </summary>
        public static double WickRangeRatio { get; set; } = 0.25;

        /// <summary>
        /// Minimum wick symmetry ratio to ensure balanced wicks.
        /// Strictest: 0.75 (near equal); Loosest: 0.4 (slight imbalance).
        /// </summary>
        public static double WickSymmetryMin { get; set; } = 0.5;

        /// <summary>
        /// Maximum wick symmetry ratio to ensure balanced wicks.
        /// Strictest: 1.25 (near equal); Loosest: 2.5 (slight imbalance).
        /// </summary>
        public static double WickSymmetryMax { get; set; } = 2.0;
        public const string BaseName = "SpinningTop";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public SpinningTopPattern(List<int> candles) : base(candles)
        {
        }

        public static SpinningTopPattern IsPattern(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            // Retrieve metrics for the current candle with lazy loading
            var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Early exit if the total range doesn’t meet the minimum requirement
            if (metrics.TotalRange < MinRange) return null;

            // Check if the candle meets the pattern criteria: small body and significant wicks
            bool isPatternValid = metrics.BodySize <= BodyRangeRatio * metrics.TotalRange &&
                                 metrics.BodySize <= BodyMax &&
                                 metrics.UpperWick >= WickRangeRatio * metrics.TotalRange &&
                                 metrics.LowerWick >= WickRangeRatio * metrics.TotalRange;

            // Strengthen wick requirements with symmetry check
            double wickRatio = metrics.UpperWick / (metrics.LowerWick + 0.001); // Avoid division by zero
            if (!isPatternValid || wickRatio < WickSymmetryMin || wickRatio > WickSymmetryMax) return null;

            // Define the candle indices for the pattern (single candle in this case)
            var candles = new List<int> { index };

            // Return the pattern instance if all conditions are satisfied
            return new SpinningTopPattern(candles);
        }
    }
}
