using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
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
        /// Range: 0.05�0.15 (0.05 for stricter Doji, 0.15 for broader interpretation).
        /// </summary>
        public static double BodyToRangeMax { get; set; } = 0.1;

        /// <summary>
        /// Maximum body size relative to the lookback average range.
        /// Purpose: Caps body size against prior volatility for consistency across timeframes.
        /// Default: 0.05 (5% of average range).
        /// Range: 0.03�0.07 (0.03 for tighter control, 0.07 for looser fit).
        /// </summary>
        public static double BodyToAvgRangeMax { get; set; } = 0.05;

        /// <summary>
        /// Minimum total range relative to the lookback average range.
        /// Purpose: Ensures the candle is significant compared to prior volatility, filtering stagnation.
        /// Default: 1.5 (1.5x average range).
        /// Range: 1.2�2.0 (1.2 for more frequent detection, 2.0 for stricter significance).
        /// </summary>
        public static double MinRangeToAvgRangeRatio { get; set; } = 1.5;

        /// <summary>
        /// Maximum wick ratio (larger wick / smaller wick).
        /// Purpose: Ensures upper and lower wicks are reasonably balanced for a classic Doji.
        /// Default: 1.5 (larger wick = 1.5x smaller wick).
        /// Range: 1.2�2.0 (1.2 for stricter balance, 2.0 for looser balance).
        /// </summary>
        public static double MaxWickRatio { get; set; } = 1.5;

        /// <summary>
        /// Minimum trend direction ratio in the lookback period to confirm a prior trend.
        /// Purpose: Ensures a consistent prior trend (bullish or bearish) for meaningful indecision.
        /// Default: 0.6 (60% of candles in one direction).
        /// Range: 0.5�0.8 (0.5 for moderate trends, 0.8 for strong trends).
        /// </summary>
        public static double TrendDirectionRatioMin { get; set; } = 0.6;

        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Doji";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A single candle with a very small body relative to its total range, indicating market indecision. Can signal potential reversal or continuation depending on context and surrounding candles.";
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
        public static async Task<DojiPattern?> IsPatternAsync(
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
            CandleMetrics indexMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

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
                throw new InvalidOperationException("DojiPattern must have exactly 1 candle.");

            int index = Candles[0];
            var indexMetrics = metricsCache[index];

            double avgRange = indexMetrics.LookbackAvgRange[PatternSize - 1];

            // Power Score: Based on body smallness, range significance, wick balance, trend
            double bodySmallness = 1 - (indexMetrics.BodySize / (BodyToRangeMax * indexMetrics.TotalRange));
            bodySmallness = Math.Clamp(bodySmallness, 0, 1);

            double rangeSignificance = indexMetrics.TotalRange / (MinRangeToAvgRangeRatio * avgRange);
            rangeSignificance = Math.Min(rangeSignificance, 1);

            double wickBalance = 1;
            if (indexMetrics.UpperWick > 0 && indexMetrics.LowerWick > 0)
            {
                double wickRatio = Math.Max(indexMetrics.UpperWick, indexMetrics.LowerWick) / Math.Min(indexMetrics.UpperWick, indexMetrics.LowerWick);
                wickBalance = 1 - (wickRatio - 1) / (MaxWickRatio - 1);
                wickBalance = Math.Clamp(wickBalance, 0, 1);
            }

            double trendDirectionRatio = Math.Max(indexMetrics.BullishRatio[PatternSize - 1], indexMetrics.BearishRatio[PatternSize - 1]);

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.3, wRange = 0.3, wWick = 0.2, wTrend = 0.1, wVolume = 0.1;
            double powerScore = (wBody * bodySmallness + wRange * rangeSignificance + wWick * wickBalance +
                                 wTrend * trendDirectionRatio + wVolume * volumeScore) /
                                (wBody + wRange + wWick + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(indexMetrics.BodySize - BodyToAvgRangeMax * avgRange) / (BodyToAvgRangeMax * avgRange);
            double rangeDeviation = Math.Abs(indexMetrics.TotalRange - MinRangeToAvgRangeRatio * avgRange) / (MinRangeToAvgRangeRatio * avgRange);
            double trendDeviation = Math.Abs(trendDirectionRatio - TrendDirectionRatioMin) / TrendDirectionRatioMin;
            double matchScore = 1 - (bodyDeviation + rangeDeviation + trendDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








