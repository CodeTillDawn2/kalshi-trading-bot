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
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "SpinningTop";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A candle with a small body and long upper and lower wicks of roughly equal length, indicating market indecision and potential for a reversal or continuation depending on context.";
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
        /// Initializes a new instance of the SpinningTopPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public SpinningTopPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Spinning Top pattern exists at the specified index.
        /// </summary>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="index">The index of the candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<SpinningTopPattern?> IsPatternAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            // Retrieve metrics for the current candle with lazy loading
            var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Early exit if the total range doesn�t meet the minimum requirement
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
                throw new InvalidOperationException("SpinningTopPattern must have exactly 1 candle.");

            int index = Candles[0];
            var metrics = metricsCache[index];

            // Power Score: Based on range, body smallness, wick length, symmetry
            double rangeScore = metrics.TotalRange / MinRange;
            rangeScore = Math.Min(rangeScore, 1);

            double bodyScore = 1 - (metrics.BodySize / (BodyRangeRatio * metrics.TotalRange));
            bodyScore = Math.Clamp(bodyScore, 0, 1);

            double bodyAbsScore = 1 - (metrics.BodySize / BodyMax);
            bodyAbsScore = Math.Clamp(bodyAbsScore, 0, 1);

            double upperWickScore = metrics.UpperWick / (WickRangeRatio * metrics.TotalRange);
            upperWickScore = Math.Min(upperWickScore, 1);

            double lowerWickScore = metrics.LowerWick / (WickRangeRatio * metrics.TotalRange);
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

            double volumeScore = 0.5; // Placeholder

            double wRange = 0.15, wBody = 0.15, wBodyAbs = 0.15, wUpperWick = 0.15, wLowerWick = 0.15, wSymmetry = 0.15, wVolume = 0.1;
            double powerScore = (wRange * rangeScore + wBody * bodyScore + wBodyAbs * bodyAbsScore +
                                 wUpperWick * upperWickScore + wLowerWick * lowerWickScore + wSymmetry * symmetryScore + wVolume * volumeScore) /
                                (wRange + wBody + wBodyAbs + wUpperWick + wLowerWick + wSymmetry + wVolume);

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








