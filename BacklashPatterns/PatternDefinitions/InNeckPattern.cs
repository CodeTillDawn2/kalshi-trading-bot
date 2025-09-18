using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The In Neck Pattern is a two-candle continuation pattern in a downtrend.
    /// - First candle bearish, second bullish with close near first close.
    /// - Indicates temporary pause in downtrend, likely to continue downward.
    /// Source: https://www.babypips.com/learn/forex/in-neck-pattern
    /// </summary>
    public class InNeckPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for first candle.
        /// Purpose: Ensures first candle has notable bearish strength.
        /// Strictest: 0.5 (current default, significant body).
        /// Loosest: 0.3 (minimal body still indicating direction per BabyPips).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Maximum difference between second open and first close.
        /// Purpose: Ensures second candle opens near first candle's close.
        /// Strictest: 1.0 (tight proximity).
        /// Loosest: 3.0 (relaxed proximity still within pattern per web sources).
        /// </summary>
        public static double MaxOpenDifference { get; set; } = 2.0;

        /// <summary>
        /// Maximum difference between second close and first close.
        /// Purpose: Ensures closes are close, indicating continuation.
        /// Strictest: 0.5 (very tight alignment).
        /// Loosest: 2.0 (still close enough per BabyPips).
        /// </summary>
        public static double MaxCloseDifference { get; set; } = 1.5;

        /// <summary>
        /// Maximum upper wick for second candle.
        /// Purpose: Limits upper wick to maintain pattern shape.
        /// Strictest: 1.0 (tight wick control).
        /// Loosest: 3.0 (allows flexibility per trading forums).
        /// </summary>
        public static double MaxWickSize { get; set; } = 2.0;

        /// <summary>
        /// Maximum mean trend for downtrend confirmation.
        /// Purpose: Confirms preceding downtrend.
        /// Strictest: -0.5 (strong downtrend).
        /// Loosest: -0.1 (weak downtrend still valid).
        /// </summary>
        public static double TrendThreshold { get; set; } = -0.3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "InNeck";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bearish continuation pattern in a downtrend where a bullish candle opens below the previous bearish candle's close but closes near it, showing continued downward pressure.";
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
        /// Initializes a new instance of the InNeckPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public InNeckPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if an In Neck pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the second candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<InNeckPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices)
        {
            if (index < 1) return null;

            CandleMids previousPrice = prices[index - 1];
            CandleMids currentPrice = prices[index];

            var previousMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            var currentMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Direction and body size check
            if (!previousMetrics.IsBearish || previousMetrics.BodySize < MinBodySize) return null;
            if (!currentMetrics.IsBullish) return null;

            // Open and close proximity
            if (Math.Abs(currentPrice.Open - previousPrice.Close) > MaxOpenDifference) return null;
            if (Math.Abs(currentPrice.Close - previousPrice.Close) > MaxCloseDifference) return null;

            // Upper wick limit
            if (currentMetrics.UpperWick > MaxWickSize) return null;

            // Downtrend check
            if (currentMetrics.GetLookbackMeanTrend(2) > TrendThreshold) return null;

            var candles = new List<int> { index - 1, index };
            return new InNeckPattern(candles);
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
                throw new InvalidOperationException("InNeckPattern must have exactly 2 candles.");

            int prevIndex = Candles[0];
            int currIndex = Candles[1];

            var previousMetrics = metricsCache[prevIndex];
            var currentMetrics = metricsCache[currIndex];

            var previousPrice = prices[prevIndex];
            var currentPrice = prices[currIndex];

            // Power Score: Based on body sizes, proximity, wick control, trend
            double firstBodyScore = previousMetrics.BodySize / MinBodySize;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double openProximityScore = 1 - (Math.Abs(currentPrice.Open - previousPrice.Close) / MaxOpenDifference);
            openProximityScore = Math.Clamp(openProximityScore, 0, 1);

            double closeProximityScore = 1 - (Math.Abs(currentPrice.Close - previousPrice.Close) / MaxCloseDifference);
            closeProximityScore = Math.Clamp(closeProximityScore, 0, 1);

            double wickScore = 1 - (currentMetrics.UpperWick / MaxWickSize);
            wickScore = Math.Clamp(wickScore, 0, 1);

            double trendStrength = Math.Abs(currentMetrics.GetLookbackMeanTrend(2) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wFirstBody = 0.2, wOpenProximity = 0.2, wCloseProximity = 0.2, wWick = 0.2, wTrend = 0.1, wVolume = 0.1;
            double powerScore = (wFirstBody * firstBodyScore + wOpenProximity * openProximityScore +
                                 wCloseProximity * closeProximityScore + wWick * wickScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirstBody + wOpenProximity + wCloseProximity + wWick + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(previousMetrics.BodySize - MinBodySize) / MinBodySize;
            double openDeviation = Math.Abs(currentPrice.Open - previousPrice.Close) / MaxOpenDifference;
            double closeDeviation = Math.Abs(currentPrice.Close - previousPrice.Close) / MaxCloseDifference;
            double wickDeviation = currentMetrics.UpperWick / MaxWickSize;
            double trendDeviation = Math.Abs(currentMetrics.GetLookbackMeanTrend(2) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (bodyDeviation + openDeviation + closeDeviation + wickDeviation + trendDeviation) / 5;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








