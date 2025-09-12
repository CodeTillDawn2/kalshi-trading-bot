using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class RisingFallingThreeMethodsPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first and fifth candles, ensuring they are significant.
        /// Loosest: 1.0 (small but notable body); Strictest: 2.0 (larger, prominent candle).
        /// </summary>
        public static double MinBodySizeMajor { get; } = 1.5;

        /// <summary>
        /// Maximum body size for the middle three candles, keeping them small relative to the major candles.
        /// Loosest: 2.0 (allows slightly larger consolidation); Strictest: 1.0 (very small bodies).
        /// </summary>
        public static double MaxBodySizeMinor { get; } = 1.5;

        /// <summary>
        /// Maximum proportion of the fifth candle’s range that can be wicks, ensuring a strong body.
        /// Loosest: 0.6 (more wick allowed); Strictest: 0.3 (minimal wicks).
        /// </summary>
        public static double WickRangeRatio { get; } = 0.5;

        /// <summary>
        /// Minimum trend strength threshold to confirm the prior trend direction.
        /// Loosest: 0.2 (weak trend); Strictest: 0.5 (strong trend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Buffer factor as a percentage of the first candle’s range for middle candle containment.
        /// Loosest: 0.15 (wider range); Strictest: 0.05 (tight containment).
        /// </summary>
        public static double RangeBufferFactor { get; } = 0.1;

        /// <summary>
        /// Minimum buffer size for range containment when the first candle’s range is small.
        /// Loosest: 0.3 (smaller buffer); Strictest: 1.0 (larger buffer).
        /// </summary>
        public static double MinRangeBuffer { get; } = 0.5;
        public const string BaseName = "RisingFallingThreeMethods";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public RisingFallingThreeMethodsPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies Rising/Falling Three Methods, a five-candle continuation pattern.
        /// Requirements (source: Investopedia, TradingView):
        /// - Rising: Occurs in a downtrend; first candle is bullish with a large body, followed by 
        ///   three smaller bearish candles within the first candle’s range, and a final bullish candle 
        ///   closing above the first.
        /// - Falling: Occurs in an uptrend; first candle is bearish with a large body, followed by 
        ///   three smaller bullish candles within the first candle’s range, and a final bearish candle 
        ///   closing below the first.
        /// Indicates: Continuation of the prior trend (bullish for Rising, bearish for Falling) after a brief consolidation.
        /// </summary>
        public static RisingFallingThreeMethodsPattern IsPattern(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            // Early exit if there aren't enough prior candles
            if (index < 4) return null;

            int startIndex = index - 4;
            var asks = prices.Skip(startIndex).Take(5).ToArray();

            // Lazy load metrics for all 5 candles
            CandleMetrics metrics1 = GetCandleMetrics(ref metricsCache, startIndex, prices, trendLookback, false);
            CandleMetrics metrics2 = GetCandleMetrics(ref metricsCache, startIndex + 1, prices, trendLookback, false);
            CandleMetrics metrics3 = GetCandleMetrics(ref metricsCache, startIndex + 2, prices, trendLookback, false);
            CandleMetrics metrics4 = GetCandleMetrics(ref metricsCache, startIndex + 3, prices, trendLookback, false);
            CandleMetrics metrics5 = GetCandleMetrics(ref metricsCache, startIndex + 4, prices, trendLookback, true);

            // Check if first candle has a significant body
            if (metrics1.BodySize < MinBodySizeMajor) return null;

            // Calculate range buffer as in original
            double rangeBuffer = Math.Max(MinRangeBuffer, RangeBufferFactor * metrics1.TotalRange);

            if (metrics1.IsBullish) // Rising
            {
                // Require a downtrend
                if (metrics5.GetLookbackAverageTrend(5) >= -TrendThreshold) return null;

                // Fifth candle: Must be bullish, significant body, close higher
                if (!metrics5.IsBullish ||
                    metrics5.BodySize < MinBodySizeMajor ||
                    asks[4].Close <= asks[0].Close) return null;

                // Fifth candle: Must have small wicks
                if (metrics5.UpperWick > WickRangeRatio * metrics5.TotalRange ||
                    metrics5.LowerWick > WickRangeRatio * metrics5.TotalRange ||
                    metrics5.TotalRange <= 0) return null;

                // Middle candles: Small bodies
                if (metrics2.BodySize > MaxBodySizeMinor ||
                    metrics3.BodySize > MaxBodySizeMinor ||
                    metrics4.BodySize > MaxBodySizeMinor) return null;

                // Count bearish middle candles and those within range
                int bearishCount = (metrics2.IsBearish ? 1 : 0) + (metrics3.IsBearish ? 1 : 0) + (metrics4.IsBearish ? 1 : 0);
                int inRangeCount = (asks[1].High <= asks[0].High + rangeBuffer && asks[1].Low >= asks[0].Low - rangeBuffer ? 1 : 0) +
                                   (asks[2].High <= asks[0].High + rangeBuffer && asks[2].Low >= asks[0].Low - rangeBuffer ? 1 : 0) +
                                   (asks[3].High <= asks[0].High + rangeBuffer && asks[3].Low >= asks[0].Low - rangeBuffer ? 1 : 0);

                if (bearishCount < 2 || inRangeCount < 2) return null;
            }
            else if (metrics1.IsBearish) // Falling
            {
                // Require an uptrend
                if (metrics5.GetLookbackAverageTrend(5) <= TrendThreshold) return null;

                // Fifth candle: Must be bearish, significant body, close lower
                if (!metrics5.IsBearish ||
                    metrics5.BodySize < MinBodySizeMajor ||
                    asks[4].Close >= asks[0].Close) return null;

                // Fifth candle: Must have small wicks
                if (metrics5.UpperWick > WickRangeRatio * metrics5.TotalRange ||
                    metrics5.LowerWick > WickRangeRatio * metrics5.TotalRange ||
                    metrics5.TotalRange <= 0) return null;

                // Middle candles: Small bodies
                if (metrics2.BodySize > MaxBodySizeMinor ||
                    metrics3.BodySize > MaxBodySizeMinor ||
                    metrics4.BodySize > MaxBodySizeMinor) return null;

                // Count bullish middle candles and those within range
                int bullishCount = (metrics2.IsBullish ? 1 : 0) + (metrics3.IsBullish ? 1 : 0) + (metrics4.IsBullish ? 1 : 0);
                int inRangeCount = (asks[1].High <= asks[0].High + rangeBuffer && asks[1].Low >= asks[0].Low - rangeBuffer ? 1 : 0) +
                                   (asks[2].High <= asks[0].High + rangeBuffer && asks[2].Low >= asks[0].Low - rangeBuffer ? 1 : 0) +
                                   (asks[3].High <= asks[0].High + rangeBuffer && asks[3].Low >= asks[0].Low - rangeBuffer ? 1 : 0);

                if (bullishCount < 2 || inRangeCount < 2) return null;
            }
            else
            {
                return null; // Neither rising nor falling
            }

            // Define the candle indices for the pattern (five candles)
            var candles = new List<int> { startIndex, startIndex + 1, startIndex + 2, startIndex + 3, startIndex + 4 };

            // Return the pattern instance if all conditions are met
            return new RisingFallingThreeMethodsPattern(candles);
        }
    }
}






