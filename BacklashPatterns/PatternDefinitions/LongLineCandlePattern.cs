using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Long Line Candle candlestick pattern.
    /// </summary>
    public class LongLineCandlePattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range for the candle to be significant.
        /// Loosest: 1.0 (smaller moves); Strictest: 3.0 (large moves required).
        /// </summary>
        public static double MinRange { get; } = 2.0;

        /// <summary>
        /// Minimum ratio of body size to total range for a prominent body.
        /// Loosest: 0.5 (smaller body); Strictest: 0.8 (very large body).
        /// </summary>
        public static double BodyRangeRatio { get; } = 0.6;

        /// <summary>
        /// Maximum ratio of wick size to total range for small wicks.
        /// Loosest: 0.3 (larger wicks); Strictest: 0.1 (very small wicks).
        /// </summary>
        public static double WickRangeRatio { get; } = 0.2;

        /// <summary>
        /// Threshold for trend strength to validate prior trend context.
        /// Loosest: 0.1 (weak trend); Strictest: 0.5 (strong trend required).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Multiplier for body size relative to average range in lookback.
        /// Loosest: 1.0 (equal to average); Strictest: 1.5 (much larger than average).
        /// </summary>
        public static double AvgRangeMultiplier { get; } = 1.2;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "LongLineCandle";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish candle with a large body and small wicks, indicating strong buying momentum and conviction in an uptrend."
            : "A bearish candle with a large body and small wicks, indicating strong selling momentum and conviction in a downtrend.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => IsBullish ? PatternDirection.Bullish : PatternDirection.Bearish;
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
        private readonly bool IsBullish;

        /// <summary>
        /// Initializes a new instance of the LongLineCandlePattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public LongLineCandlePattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Long Line Candle pattern, a single-candle pattern indicating strong momentum.
        /// Requirements (sourced from TradingView and adapted to your logic):
        /// - A candle with a large body relative to its range (typically &gt; 60-70%).
        /// - Small upper and lower wicks (typically &lt; 20-30% of range).
        /// - Appears in a trend context (bullish after downtrend, bearish after uptrend).
        /// - Indicates strong directional momentum.
        /// Your original logic includes a contextual range check against lookback average.
        /// </summary>
        public static async Task<LongLineCandlePattern?> IsPatternAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            bool isBullish,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Check if the total range meets the minimum requirement for significance
            if (metrics.TotalRange < MinRange) return null;

            // Determine if the candle direction matches the expected trend
            bool direction = isBullish ? metrics.IsBullish : metrics.IsBearish;

            // Validate the trend direction using CandleMetrics method
            bool trendValid = isBullish
                ? metrics.GetLookbackMeanTrend(1) < -TrendThreshold // Downtrend before bullish
                : metrics.GetLookbackMeanTrend(1) > TrendThreshold;  // Uptrend before bearish

            // Check contextual range against lookback average
            double avgRange = metrics.GetLookbackAvgRange(1);
            if (avgRange <= 0) return null; // Avoid invalid average range

            // Check if the body size, upper wick, and lower wick meet the pattern criteria
            bool isPatternValid = metrics.BodySize >= BodyRangeRatio * metrics.TotalRange && // Body must be significant
                                  metrics.UpperWick <= WickRangeRatio * metrics.TotalRange && // Upper wick limited
                                  metrics.LowerWick <= WickRangeRatio * metrics.TotalRange && // Lower wick limited
                                  metrics.BodySize >= AvgRangeMultiplier * avgRange && // Contextual size check
                                  direction && trendValid;

            if (!isPatternValid) return null;

            // Define the candle index for the pattern (single candle)
            var candles = new List<int> { index };

            // Return the pattern instance with the specified direction
            return new LongLineCandlePattern(candles, isBullish);
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
                throw new InvalidOperationException("LongLineCandlePattern must have exactly 1 candle.");

            int index = Candles[0];
            var metrics = metricsCache[index];

            // Power Score: Based on range, body dominance, wick minimization, contextual size, trend
            double rangeScore = metrics.TotalRange / MinRange;
            rangeScore = Math.Min(rangeScore, 1);

            double bodyDominanceScore = metrics.BodySize / (BodyRangeRatio * metrics.TotalRange);
            bodyDominanceScore = Math.Min(bodyDominanceScore, 1);

            double wickScore = 1 - ((metrics.UpperWick + metrics.LowerWick) / (2 * WickRangeRatio * metrics.TotalRange));
            wickScore = Math.Clamp(wickScore, 0, 1);

            double contextualScore = metrics.BodySize / (AvgRangeMultiplier * metrics.GetLookbackAvgRange(1));
            contextualScore = Math.Min(contextualScore, 1);

            double trendStrength = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wRange = 0.15, wBodyDominance = 0.2, wWick = 0.2, wContextual = 0.2, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wRange * rangeScore + wBodyDominance * bodyDominanceScore + wWick * wickScore +
                                 wContextual * contextualScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wRange + wBodyDominance + wWick + wContextual + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double rangeDeviation = Math.Abs(metrics.TotalRange - MinRange) / MinRange;
            double bodyDeviation = Math.Abs(metrics.BodySize - BodyRangeRatio * metrics.TotalRange) / (BodyRangeRatio * metrics.TotalRange);
            double wickDeviation = (metrics.UpperWick + metrics.LowerWick) / (2 * WickRangeRatio * metrics.TotalRange);
            double contextualDeviation = Math.Abs(metrics.BodySize - AvgRangeMultiplier * metrics.GetLookbackAvgRange(1)) / (AvgRangeMultiplier * metrics.GetLookbackAvgRange(1));
            double trendDeviation = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (rangeDeviation + bodyDeviation + wickDeviation + contextualDeviation + trendDeviation) / 5;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








