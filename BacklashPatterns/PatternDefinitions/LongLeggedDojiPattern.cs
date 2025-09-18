using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Long-Legged Doji candlestick pattern.
    /// </summary>
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


        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "LongLeggedDoji";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A Doji candle with both long upper and lower wicks and a very small body, indicating high market indecision and potential for a significant price move in either direction.";
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
        /// Initializes a new instance of the LongLeggedDojiPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
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
        public static async Task<LongLeggedDojiPattern?> IsPatternAsync(
             Dictionary<int, CandleMetrics> metricsCache,
             int index,
             int trendLookback,
             CandleMids[] prices)
        {
            // Lazy load metrics for the current candle
            var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

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
                throw new InvalidOperationException("LongLeggedDojiPattern must have exactly 1 candle.");

            int index = Candles[0];
            var metrics = metricsCache[index];

            // Power Score: Based on range, body smallness, wick length, symmetry
            double rangeScore = metrics.TotalRange / MinRange;
            rangeScore = Math.Min(rangeScore, 1);

            double bodyScore = 1 - (metrics.BodySize / BodyMax);
            bodyScore = Math.Clamp(bodyScore, 0, 1);

            double upperWickScore = metrics.UpperWick / (WickRangeRatio * metrics.TotalRange);
            upperWickScore = Math.Min(upperWickScore, 1);

            double lowerWickScore = metrics.LowerWick / (WickRangeRatio * metrics.TotalRange);
            lowerWickScore = Math.Min(lowerWickScore, 1);

            double wickBodyScore = (metrics.UpperWick / (WickBodyRatio * metrics.BodySize) +
                                   metrics.LowerWick / (WickBodyRatio * metrics.BodySize)) / 2;
            wickBodyScore = Math.Min(wickBodyScore, 1);

            double symmetryScore = 0;
            if (metrics.UpperWick > 0 && metrics.LowerWick > 0)
            {
                double wickRatio = metrics.UpperWick / (metrics.LowerWick + Epsilon);
                if (wickRatio >= WickSymmetryMin && wickRatio <= WickSymmetryMax)
                {
                    // Closer to 1.0 is better symmetry
                    symmetryScore = 1 - Math.Abs(wickRatio - 1.0) / Math.Max(1.0, wickRatio);
                    symmetryScore = Math.Clamp(symmetryScore, 0, 1);
                }
            }

            double volumeScore = 0.5; // Placeholder

            double wRange = 0.15, wBody = 0.15, wUpperWick = 0.15, wLowerWick = 0.15, wWickBody = 0.15, wSymmetry = 0.15, wVolume = 0.1;
            double powerScore = (wRange * rangeScore + wBody * bodyScore + wUpperWick * upperWickScore +
                                 wLowerWick * lowerWickScore + wWickBody * wickBodyScore + wSymmetry * symmetryScore + wVolume * volumeScore) /
                                (wRange + wBody + wUpperWick + wLowerWick + wWickBody + wSymmetry + wVolume);

            // Match Score: Deviation from thresholds
            double rangeDeviation = Math.Abs(metrics.TotalRange - MinRange) / MinRange;
            double bodyDeviation = Math.Abs(metrics.BodySize - BodyMax) / BodyMax;
            double wickDeviation = Math.Abs(metrics.UpperWick - WickRangeRatio * metrics.TotalRange) / (WickRangeRatio * metrics.TotalRange);
            double matchScore = 1 - (rangeDeviation + bodyDeviation + wickDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








