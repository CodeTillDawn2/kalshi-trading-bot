using SmokehouseDTOs;
using SmokehousePatterns.Helpers;
using static SmokehousePatterns.Helpers.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Doji candlestick pattern, a single-candle pattern indicating indecision.
    /// Suggests potential reversal or continuation depending on prior trend.
    /// Requirements (Source: Investopedia, DailyFX):
    /// - Single candle with a very small body (open and close nearly equal).
    /// - Shadows can vary, but total range should be significant relative to prior volatility.
    /// - Meaningful only after a clear trend, measured via TrendDirectionRatio.
    /// Optimized for ML: Relative scaling based on lookback average range, no volume dependency.
    /// </summary>
    public class DojiPattern : PatternDefinition
    {
        /// <summary>
        /// Number of candles required to form the pattern.
        /// Purpose: Defines the fixed size of the Doji pattern (single candle).
        /// Default: 1 (standard for Doji pattern).
        /// </summary>
        public static int PatternSize { get; } = 1;

        /// <summary>
        /// Maximum body size relative to the candle's total range.
        /// Purpose: Ensures the body is small compared to the candle's range, indicating indecision.
        /// Default: 0.1 (10% of total range).
        /// Range: 0.05–0.15 (0.05 for stricter Doji, 0.15 for broader interpretation).
        /// </summary>
        public static double BodyToRangeMax { get; set; } = 0.1;

        /// <summary>
        /// Maximum body size relative to the lookback average range.
        /// Purpose: Caps body size against prior volatility for consistency across timeframes.
        /// Default: 0.05 (5% of average range).
        /// Range: 0.03–0.07 (0.03 for tighter control, 0.07 for looser fit).
        /// </summary>
        public static double BodyToAvgRangeMax { get; set; } = 0.05;

        /// <summary>
        /// Minimum total range relative to the lookback average range.
        /// Purpose: Ensures the candle is significant compared to prior volatility, filtering stagnation.
        /// Default: 1.5 (1.5x average range).
        /// Range: 1.2–2.0 (1.2 for more frequent detection, 2.0 for stricter significance).
        /// </summary>
        public static double MinRangeToAvgRangeRatio { get; set; } = 1.5;

        /// <summary>
        /// Maximum wick ratio (larger wick / smaller wick).
        /// Purpose: Ensures upper and lower wicks are reasonably balanced for a classic Doji.
        /// Default: 1.5 (larger wick ≤ 1.5x smaller wick).
        /// Range: 1.2–2.0 (1.2 for stricter balance, 2.0 for looser balance).
        /// </summary>
        public static double MaxWickRatio { get; set; } = 1.5;

        /// <summary>
        /// Minimum trend direction ratio in the lookback period to confirm a prior trend.
        /// Purpose: Ensures a consistent prior trend (bullish or bearish) for meaningful indecision.
        /// Default: 0.6 (60% of candles in one direction).
        /// Range: 0.5–0.8 (0.5 for moderate trends, 0.8 for strong trends).
        /// </summary>
        public static double TrendDirectionRatioMin { get; set; } = 0.6;

        public const string BaseName = "Doji";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the DojiPattern class.
        /// </summary>
        /// <param name="candles">List of candle indices forming the pattern (single candle).</param>
        public DojiPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Doji pattern exists at the specified index.
        /// </summary>
        /// <param name="metricsCache">Cache of candle metrics.</param>
        /// <param name="index">The index of the candle to check (final candle of the pattern).</param>
        /// <param name="trendLookback">Number of candles to look back for trend and average range.</param>
        /// <param name="prices">Array of candle price data.</param>
        /// <returns>A DojiPattern instance if detected, otherwise null.</returns>
        public static DojiPattern IsPattern(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            // Skip if not enough lookback data
            if (index < PatternSize + trendLookback - 1) return null;

            // Define candle index (single candle pattern)
            var candles = new List<int> { index };

            // Get metrics for the candle
            CandleMetrics indexMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Calculate lookback average range before the pattern
            double avgRange = indexMetrics.LookbackAvgRange[PatternSize - 1]; // Use PatternSize for consistency
            if (avgRange == 0) return null; // Avoid division by zero

            // Check minimum range relative to lookback average
            if (indexMetrics.TotalRange < MinRangeToAvgRangeRatio * avgRange) return null;

            // Check body size constraints (both relative to range and lookback average)
            bool isDoji = indexMetrics.BodySize <= BodyToRangeMax * indexMetrics.TotalRange &&
                          indexMetrics.BodySize <= BodyToAvgRangeMax * avgRange;
            if (!isDoji) return null;

            // Check prior trend using TrendDirectionRatio (bullish or bearish)
            double bullishRatio = indexMetrics.BullishRatio[PatternSize - 1];
            double bearishRatio = indexMetrics.BearishRatio[PatternSize - 1];
            if (bullishRatio < TrendDirectionRatioMin && bearishRatio < TrendDirectionRatioMin) return null;

            // Check wick balance
            double wickRatio = indexMetrics.UpperWick > 0 && indexMetrics.LowerWick > 0
                ? Math.Max(indexMetrics.UpperWick, indexMetrics.LowerWick) / Math.Min(indexMetrics.UpperWick, indexMetrics.LowerWick)
                : double.MaxValue;
            if (wickRatio > MaxWickRatio) return null;

            return new DojiPattern(candles);
        }
    }
}