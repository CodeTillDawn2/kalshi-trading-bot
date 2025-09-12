using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Identifies a Short Line Candle pattern, a single candlestick with a small body and range, 
    /// indicating indecision or a potential reversal after a strong trend.
    /// 
    /// Requirements (Source: Investopedia, "Short Line Candle"):
    /// - Small body (close near open), typically less than average candle size.
    /// - Small total range (high to low), showing low volatility.
    /// - Occurs after a defined trend (bullish after downtrend, bearish after uptrend).
    /// - Indicates potential reversal or continuation depending on context.
    /// </summary>
    public class ShortLineCandlePattern : PatternDefinition
    {
        /// <summary>
        /// Maximum body size for the candle, ensuring it remains small.
        /// Loosest: 2.5 (slightly larger body); Strictest: 1.0 (very small body).
        /// </summary>
        public static double MaxBodySize { get; } = 2.0;

        /// <summary>
        /// Maximum total range (high to low) for the candle, indicating low volatility.
        /// Loosest: 3.0 (wider range); Strictest: 1.5 (tight range).
        /// </summary>
        public static double MaxRange { get; } = 2.5;

        /// <summary>
        /// Minimum trend strength threshold to confirm the prior trend direction.
        /// Loosest: 0.3 (weaker trend); Strictest: 0.7 (strong trend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.5;

        /// <summary>
        /// Minimum consistency of the prior trend direction.
        /// Loosest: 0.4 (less consistent); Strictest: 0.8 (highly consistent).
        /// </summary>
        public static double TrendConsistencyThreshold { get; } = 0.6;
        public const string BaseName = "ShortLineCandle";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        private readonly bool IsBullish;

        public ShortLineCandlePattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        public static ShortLineCandlePattern IsPattern(
            int index,
            int trendLookback,
            bool isBullish,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices)
        {
            // Early exit if there aren’t enough prior candles
            if (index < 1) return null;

            // Retrieve metrics for the current candle
            var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Check if the candle has a small body (original logic: <= 2.0)
            bool isShort = metrics.BodySize <= MaxBodySize;

            // Verify the total range is within the acceptable limit (original logic: <= 2.5)
            bool smallRange = metrics.TotalRange <= MaxRange;

            // Confirm the candle direction matches the specified trend (original logic)
            bool direction = isBullish ? metrics.IsBullish : metrics.IsBearish;

            // If any condition fails, it’s not the pattern
            if (!isShort || !smallRange || !direction) return null;

            // Adjusted trend conditions from original: Use CandleMetrics methods
            bool priorTrend = isBullish
                ? (metrics.GetLookbackAverageTrend(1) <= -TrendThreshold && metrics.GetLookbackTrendStability(1) <= -TrendConsistencyThreshold) // Consistent downtrend
                : (metrics.GetLookbackAverageTrend(1) >= TrendThreshold && metrics.GetLookbackTrendStability(1) >= TrendConsistencyThreshold);   // Consistent uptrend

            // If the trend condition isn’t met, return null
            if (!priorTrend) return null;

            // Define the candle indices for the pattern (single candle in this case)
            var candles = new List<int> { index };

            // Return the pattern instance with the specified direction
            return new ShortLineCandlePattern(candles, isBullish);
        }
    }
}






