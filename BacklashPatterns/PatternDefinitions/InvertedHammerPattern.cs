using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The Inverted Hammer is a single-candle bullish reversal pattern after a downtrend.
    /// - Small body, long upper wick, minimal lower wick, bullish direction.
    /// - Indicates potential buying pressure and reversal to the upside.
    /// Source: https://www.investopedia.com/terms/i/invertedhammer.asp
    /// </summary>
    public class InvertedHammerPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range of the candle.
        /// Purpose: Ensures sufficient volatility for pattern significance.
        /// Strictest: 3.0 (current default, notable range).
        /// Loosest: 1.5 (smaller range still valid per Investopedia).
        /// </summary>
        public static double MinRange { get; set; } = 3.0;

        /// <summary>
        /// Maximum body size as percentage of total range.
        /// Purpose: Ensures small body relative to range.
        /// Strictest: 0.25 (current default, small body).
        /// Loosest: 0.4 (larger body still acceptable per trading forums).
        /// </summary>
        public static double BodyRangeRatio { get; set; } = 0.25;

        /// <summary>
        /// Minimum upper wick size as percentage of total range.
        /// Purpose: Ensures long upper wick for pattern shape.
        /// Strictest: 0.5 (current default, prominent wick).
        /// Loosest: 0.3 (shorter wick still valid).
        /// </summary>
        public static double WickRangeRatio { get; set; } = 0.5;

        /// <summary>
        /// Minimum ratio of upper wick to body size.
        /// Purpose: Ensures upper wick dominates body.
        /// Strictest: 2.0 (current default, strong wick).
        /// Loosest: 1.5 (relaxed but still prominent).
        /// </summary>
        public static double WickBodyRatio { get; set; } = 2.0;

        /// <summary>
        /// Maximum lower wick size as percentage of total range.
        /// Purpose: Limits lower wick to maintain pattern shape.
        /// Strictest: 0.1 (very minimal wick).
        /// Loosest: 0.3 (slightly larger wick per Investopedia).
        /// </summary>
        public static double LowerWickMaxRatio { get; set; } = 0.2;

        /// <summary>
        /// Maximum mean trend for downtrend confirmation.
        /// Purpose: Confirms preceding downtrend.
        /// Strictest: -0.5 (strong downtrend).
        /// Loosest: -0.1 (weak downtrend still valid).
        /// </summary>
        public static double TrendThreshold { get; set; } = -0.3;
        public const string BaseName = "InvertedHammer";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public InvertedHammerPattern(List<int> candles) : base(candles)
        {
        }

        public static InvertedHammerPattern? IsPattern(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            if (index < 1) return null;

            CandleMetrics metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Downtrend check
            if (metrics.GetLookbackMeanTrend(1) > TrendThreshold) return null;

            // Range check
            if (metrics.TotalRange < MinRange) return null;

            // Shape checks
            bool hasSmallBody = metrics.BodySize <= BodyRangeRatio * metrics.TotalRange;
            bool hasLongUpperWick = metrics.UpperWick >= WickRangeRatio * metrics.TotalRange &&
                                    metrics.UpperWick >= WickBodyRatio * metrics.BodySize;
            bool hasMinimalLowerWick = metrics.LowerWick <= LowerWickMaxRatio * metrics.TotalRange;
            bool isBullish = metrics.IsBullish;

            if (!hasSmallBody || !hasLongUpperWick || !hasMinimalLowerWick || !isBullish) return null;

            var candles = new List<int> { index };
            return new InvertedHammerPattern(candles);
        }
    }
}








