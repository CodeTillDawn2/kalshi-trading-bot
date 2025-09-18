using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Separating Lines candlestick pattern.
    /// </summary>
    public class SeparatingLinesPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for both candles, ensuring they are significant, as a percentage of the pattern's total range.
        /// Loosest: 15% (smaller body); Strictest: 50% (larger body).
        /// </summary>
        public static double MinBodySizePercentage => 25.0;

        /// <summary>
        /// Maximum difference between the open prices of the two candles, allowing near-equality, as a percentage of the pattern's total range.
        /// Loosest: 100% (wider tolerance); Strictest: 25% (very close opens).
        /// </summary>
        public static double MaxOpenDifferencePercentage => 75.0;

        /// <summary>
        /// Minimum trend strength threshold to confirm the prior trend direction, as a percentage of the pattern's total range.
        /// Loosest: 10% (weaker trend); Strictest: 25% (strong trend).
        /// </summary>
        public static double TrendThresholdPercentage => 15.0;

        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "SeparatingLines";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => Direction == PatternDirection.Bullish
            ? "A bullish reversal pattern with two candles where the second bullish candle opens at the same level as the first bearish candle's close, signaling a reversal from downtrend to uptrend."
            : "A bearish reversal pattern with two candles where the second bearish candle opens at the same level as the first bullish candle's close, signaling a reversal from uptrend to downtrend.";
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
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction { get; }

        /// <summary>
        /// Initializes a new instance of the SeparatingLinesPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="direction">The direction of the pattern.</param>
        public SeparatingLinesPattern(List<int> candles, PatternDirection direction) : base(candles)
        {
            Direction = direction;
        }

        /// <summary>
        /// Identifies a Separating Lines pattern, a two-candle continuation pattern.
        /// Requirements (source: TradingView, Investopedia):
        /// - Bullish: Occurs in a downtrend; first candle is bearish, second is bullish, both open at nearly the same price.
        /// - Bearish: Occurs in an uptrend; first candle is bullish, second is bearish, both open at nearly the same price.
        /// Indicates: Continuation of the existing trend (bullish or bearish) with a strong move after a brief pause.
        /// </summary>
        public static async Task<SeparatingLinesPattern?> IsPatternAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            PatternDirection direction,
            CandleMids[] prices)
        {
            // Early exit if index is invalid
            if (index < 1 || index >= prices.Length) return null;

            // Lazy load metrics for the two candles
            var prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Calculate the pattern's total range (highest high - lowest low across both candles)
            double patternHigh = Math.Max(prices[index].High, prices[index - 1].High);
            double patternLow = Math.Min(prices[index].Low, prices[index - 1].Low);
            double totalRange = patternHigh - patternLow;

            // Convert percentage-based thresholds to absolute values
            double minBodySize = totalRange * (MinBodySizePercentage / 100.0);
            double maxOpenDifference = totalRange * (MaxOpenDifferencePercentage / 100.0);
            double trendThreshold = totalRange * (TrendThresholdPercentage / 100.0);

            // Both candles must have significant bodies
            if (prevMetrics.BodySize < minBodySize || currMetrics.BodySize < minBodySize) return null;

            // Check if the open prices of the two candles are sufficiently close
            bool sameOpen = Math.Abs(prices[index].Open - prices[index - 1].Open) <= maxOpenDifference;
            if (!sameOpen) return null;

            // Determine if the candle directions align with the pattern type
            bool directionMatch = direction == PatternDirection.Bullish
                ? (currMetrics.IsBullish && prevMetrics.IsBearish)
                : (currMetrics.IsBearish && prevMetrics.IsBullish);

            // Validate the trend direction using CandleMetrics method
            bool trendValid = direction == PatternDirection.Bullish
                ? currMetrics.GetLookbackMeanTrend(2) <= -trendThreshold // Downtrend before bullish continuation
                : currMetrics.GetLookbackMeanTrend(2) >= trendThreshold;  // Uptrend before bearish continuation

            // Combine conditions to confirm the pattern
            if (!directionMatch || !trendValid) return null;

            // Define the candle indices for the pattern (two candles)
            var candles = new List<int> { index - 1, index };

            // Return the pattern instance with the specified direction
            return new SeparatingLinesPattern(candles, direction);
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
            if (Candles.Count != 2)
                throw new InvalidOperationException("SeparatingLinesPattern must have exactly 2 candles.");

            int prevIndex = Candles[0];
            int currIndex = Candles[1];

            var prevMetrics = metricsCache[prevIndex];
            var currMetrics = metricsCache[currIndex];

            var prevPrice = prices[prevIndex];
            var currPrice = prices[currIndex];

            // Calculate pattern range
            double patternHigh = Math.Max(currPrice.High, prevPrice.High);
            double patternLow = Math.Min(currPrice.Low, prevPrice.Low);
            double totalRange = patternHigh - patternLow;

            // Convert thresholds
            double minBodySize = totalRange * (MinBodySizePercentage / 100.0);
            double maxOpenDifference = totalRange * (MaxOpenDifferencePercentage / 100.0);
            double trendThreshold = totalRange * (TrendThresholdPercentage / 100.0);

            // Power Score: Based on body sizes, open proximity, trend
            double bodyScore = (prevMetrics.BodySize + currMetrics.BodySize) / (2 * minBodySize);
            bodyScore = Math.Min(bodyScore, 1);

            double openProximityScore = 1 - (Math.Abs(currPrice.Open - prevPrice.Open) / maxOpenDifference);
            openProximityScore = Math.Clamp(openProximityScore, 0, 1);

            double trendStrength = Math.Abs(currMetrics.GetLookbackMeanTrend(2) - trendThreshold) / Math.Abs(trendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.4, wOpenProximity = 0.4, wTrend = 0.1, wVolume = 0.1;
            double powerScore = (wBody * bodyScore + wOpenProximity * openProximityScore +
                                 wTrend * trendStrength + wVolume * volumeScore) /
                                (wBody + wOpenProximity + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(prevMetrics.BodySize - minBodySize) / minBodySize;
            double openDeviation = Math.Abs(currPrice.Open - prevPrice.Open) / maxOpenDifference;
            double trendDeviation = Math.Abs(currMetrics.GetLookbackMeanTrend(2) - trendThreshold) / Math.Abs(trendThreshold);
            double matchScore = 1 - (bodyDeviation + openDeviation + trendDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








