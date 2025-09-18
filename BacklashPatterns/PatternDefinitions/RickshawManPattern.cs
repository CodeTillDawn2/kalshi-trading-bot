using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Rickshaw Man candlestick pattern.
    /// </summary>
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
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "RickshawMan";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A Doji candle where the open and close are at the exact midpoint of the high-low range, indicating perfect market indecision and potential for a significant directional move.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Neutral;
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
        /// Initializes a new instance of the RickshawManPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public RickshawManPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies a Rickshaw Man, a single-candle pattern similar to a Long-Legged Doji.
        /// Requirements (source: BabyPips, TradingView):
        /// - A small body (near-equal open and close) with long upper and lower wicks of roughly equal length.
        /// - Total range is significant, indicating volatility.
        /// - Close is near the candle�s midpoint.
        /// Indicates: Indecision in the market, often appearing at tops or bottoms, suggesting a potential reversal.
        /// </summary>
        public static async Task<RickshawManPattern?> IsPatternAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            // Lazy load metrics for the current candle
            var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

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
                throw new InvalidOperationException("RickshawManPattern must have exactly 1 candle.");

            int index = Candles[0];
            var metrics = metricsCache[index];
            var currentPrice = prices[index];

            // Power Score: Based on range, body smallness, wick length, symmetry, close proximity to midpoint
            double rangeScore = metrics.TotalRange / MinRange;
            rangeScore = Math.Min(rangeScore, 1);

            double bodyScore = 1 - (metrics.BodySize / Math.Max(MaxBodyAbsolute, MaxBodyFactor * metrics.TotalRange));
            bodyScore = Math.Clamp(bodyScore, 0, 1);

            double upperWickScore = metrics.UpperWick / (MinWickRatio * metrics.TotalRange);
            upperWickScore = Math.Min(upperWickScore, 1);

            double lowerWickScore = metrics.LowerWick / (MinWickRatio * metrics.TotalRange);
            lowerWickScore = Math.Min(lowerWickScore, 1);

            double symmetryScore = 0;
            if (metrics.UpperWick > 0 && metrics.LowerWick > 0)
            {
                double wickRatio = metrics.UpperWick / (metrics.LowerWick + 0.001);
                if (wickRatio >= WickSymmetryMin && wickRatio <= WickSymmetryMax)
                {
                    // Closer to 1.0 is better symmetry
                    symmetryScore = 1 - Math.Abs(wickRatio - 1.0) / Math.Max(1.0, wickRatio);
                    symmetryScore = Math.Clamp(symmetryScore, 0, 1);
                }
            }

            double closeProximityScore = 1 - (Math.Abs(currentPrice.Close - metrics.MidPoint) /
                                             Math.Max(MinCloseTolerance, CloseToleranceFactor * metrics.TotalRange));
            closeProximityScore = Math.Clamp(closeProximityScore, 0, 1);

            double volumeScore = 0.5; // Placeholder

            double wRange = 0.15, wBody = 0.15, wUpperWick = 0.15, wLowerWick = 0.15, wSymmetry = 0.15, wClose = 0.15, wVolume = 0.1;
            double powerScore = (wRange * rangeScore + wBody * bodyScore + wUpperWick * upperWickScore +
                                 wLowerWick * lowerWickScore + wSymmetry * symmetryScore + wClose * closeProximityScore + wVolume * volumeScore) /
                                (wRange + wBody + wUpperWick + wLowerWick + wSymmetry + wClose + wVolume);

            // Match Score: Deviation from thresholds
            double rangeDeviation = Math.Abs(metrics.TotalRange - MinRange) / MinRange;
            double bodyDeviation = Math.Abs(metrics.BodySize - Math.Max(MaxBodyAbsolute, MaxBodyFactor * metrics.TotalRange)) /
                                   Math.Max(MaxBodyAbsolute, MaxBodyFactor * metrics.TotalRange);
            double wickDeviation = Math.Abs(metrics.UpperWick - MinWickRatio * metrics.TotalRange) / (MinWickRatio * metrics.TotalRange);
            double matchScore = 1 - (rangeDeviation + bodyDeviation + wickDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








