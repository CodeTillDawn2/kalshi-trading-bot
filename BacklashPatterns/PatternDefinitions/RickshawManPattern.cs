using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class RickshawManPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range of the candle, as a normalized value.
        /// Purpose: Ensures significant volatility to qualify as a notable pattern.
        /// Loosest: 1.0 (minimal volatility); Strictest: 2.0 (high volatility).
        /// </summary>
        public static double MinRange { get; } = 1.5;

        /// <summary>
        /// Maximum body size as a proportion of the total range.
        /// Purpose: Ensures the body remains small relative to the range, indicating indecision.
        /// Loosest: 0.2 (allows slightly larger bodies); Strictest: 0.1 (very small body).
        /// </summary>
        public static double MaxBodyFactor { get; } = 0.15;

        /// <summary>
        /// Absolute maximum body size, as a normalized value.
        /// Purpose: Caps the body size independently of range for consistency.
        /// Loosest: 2.0 (larger absolute body); Strictest: 1.0 (small absolute body).
        /// </summary>
        public static double MaxBodyAbsolute { get; } = 1.5;

        /// <summary>
        /// Minimum wick length as a proportion of the total range.
        /// Purpose: Ensures long wicks to indicate volatility and indecision.
        /// Loosest: 0.15 (shorter wicks); Strictest: 0.3 (longer wicks).
        /// </summary>
        public static double MinWickRatio { get; } = 0.2;

        /// <summary>
        /// Minimum ratio of upper wick to lower wick for symmetry.
        /// Purpose: Ensures wicks are roughly balanced (lower bound).
        /// Loosest: 0.3 (less symmetry); Strictest: 0.8 (near-perfect symmetry).
        /// </summary>
        public static double WickSymmetryMin { get; } = 0.5;

        /// <summary>
        /// Maximum ratio of upper wick to lower wick for symmetry.
        /// Purpose: Ensures wicks are roughly balanced (upper bound).
        /// Loosest: 3.0 (less symmetry); Strictest: 1.2 (near-perfect symmetry).
        /// </summary>
        public static double WickSymmetryMax { get; } = 2.0;

        /// <summary>
        /// Tolerance factor for the close relative to the midpoint, as a proportion of range.
        /// Purpose: Allows flexibility in how close the close must be to the midpoint.
        /// Loosest: 0.15 (wider tolerance); Strictest: 0.05 (narrow tolerance).
        /// </summary>
        public static double CloseToleranceFactor { get; } = 0.1;

        /// <summary>
        /// Minimum absolute tolerance for the close relative to the midpoint.
        /// Purpose: Ensures a baseline tolerance regardless of range.
        /// Loosest: 1.5 (wider absolute tolerance); Strictest: 0.5 (narrow tolerance).
        /// </summary>
        public static double MinCloseTolerance { get; } = 1.0;
        public const string BaseName = "RickshawMan";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public RickshawManPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies a Rickshaw Man, a single-candle pattern similar to a Long-Legged Doji.
        /// Requirements (source: BabyPips, TradingView):
        /// - A small body (near-equal open and close) with long upper and lower wicks of roughly equal length.
        /// - Total range is significant, indicating volatility.
        /// - Close is near the candle’s midpoint.
        /// Indicates: Indecision in the market, often appearing at tops or bottoms, suggesting a potential reversal.
        /// </summary>
        public static RickshawManPattern IsPattern(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            // Lazy load metrics for the current candle
            var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Check if total range and body size meet requirements
            if (metrics.TotalRange < MinRange ||
                metrics.BodySize > Math.Max(MaxBodyAbsolute, MaxBodyFactor * metrics.TotalRange)) return null;

            // Calculate minimum wick length and wick ratio
            double minWickLength = MinWickRatio * metrics.TotalRange;
            double wickRatio = metrics.UpperWick / (metrics.LowerWick + 0.001); // Avoid division by zero

            // Verify wick conditions
            if (metrics.UpperWick < minWickLength ||
                metrics.LowerWick < minWickLength ||
                wickRatio < WickSymmetryMin ||
                wickRatio > WickSymmetryMax) return null;

            // Ensure close is near the midpoint
            if (Math.Abs(prices[index].Close - metrics.MidPoint) > Math.Max(MinCloseTolerance, CloseToleranceFactor * metrics.TotalRange)) return null;

            // Define the candle indices for the pattern (single candle)
            var candles = new List<int> { index };

            // Return the pattern instance if all conditions are met
            return new RickshawManPattern(candles);
        }
    }
}







