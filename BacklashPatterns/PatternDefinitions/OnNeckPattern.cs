using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents an On Neck candlestick pattern.
    /// </summary>
    public class OnNeckPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size of the first (bearish) candle, as a normalized value.
        /// Purpose: Ensures the first candle has a significant body to establish bearish momentum.
        /// Loosest: 0.3 (smaller body still shows direction); Strictest: 0.5 (standard requirement).
        /// </summary>
        public static double MinBodySize { get; } = 0.5;

        /// <summary>
        /// Maximum difference between the second candle s open/close and the first candle s close.
        /// Purpose: Ensures the second candle closes very near the first candle s close for continuation.
        /// Loosest: 2.0 (wider tolerance for proximity); Strictest: 0.5 (very tight proximity).
        /// </summary>
        public static double MaxOpenCloseDifference { get; } = 1.5;

        /// <summary>
        /// Threshold for identifying a downtrend based on lookback mean trend.
        /// Purpose: Confirms the pattern occurs in a bearish context.
        /// Loosest: -0.1 (very weak downtrend); Strictest: -0.5 (strong downtrend).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;

        /// <summary>
        /// Maximum upper wick size of the second candle, as a normalized value.
        /// Purpose: Limits the bullish wick to emphasize weak buying pressure.
        /// Loosest: 3.0 (allows larger wicks); Strictest: 1.0 (minimal wick).
        /// </summary>
        public static double MaxUpperWick { get; } = 2.0;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "OnNeck";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bearish continuation pattern in a downtrend where a bullish candle opens below the previous bearish candle's close but closes near it, showing continued downward pressure.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Bearish;
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
        /// Initializes a new instance of the OnNeckPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public OnNeckPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if an On Neck pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the second candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<OnNeckPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices)
        {
            if (index < 1) return null;

            CandleMids currentPrices = prices[index];
            CandleMids previousPrices = prices[index - 1];

            CandleMetrics currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
            CandleMetrics prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);

            // First candle must be bearish with a significant body, second must be bullish
            if (!prevMetrics.IsBearish || prevMetrics.BodySize < MinBodySize || !currMetrics.IsBullish)
                return null;

            // Require a downtrend based on the mean trend (restored original threshold)
            if (currMetrics.GetLookbackMeanTrend(2) > TrendThreshold) return null;

            // Check if the second candle s open and close are sufficiently close to the first candle s close
            bool isPatternValid = Math.Abs(currentPrices.Open - previousPrices.Close) <= MaxOpenCloseDifference &&
                                 Math.Abs(currentPrices.Close - previousPrices.Close) <= MaxOpenCloseDifference &&
                                 currMetrics.UpperWick <= MaxUpperWick; // Restored wick check

            if (!isPatternValid) return null;

            // Define the candle indices for the pattern (two candles)
            var candles = new List<int> { index - 1, index };

            // Return the pattern instance if all conditions are met
            return new OnNeckPattern(candles);
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
                throw new InvalidOperationException("OnNeckPattern must have exactly 2 candles.");

            int prevIndex = Candles[0];
            int currIndex = Candles[1];

            var previousMetrics = metricsCache[prevIndex];
            var currentMetrics = metricsCache[currIndex];

            var previousPrices = prices[prevIndex];
            var currentPrices = prices[currIndex];

            // Power Score: Based on body sizes, proximity, wick control, trend
            double firstBodyScore = previousMetrics.BodySize / MinBodySize;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double openProximityScore = 1 - (Math.Abs(currentPrices.Open - previousPrices.Close) / MaxOpenCloseDifference);
            openProximityScore = Math.Clamp(openProximityScore, 0, 1);

            double closeProximityScore = 1 - (Math.Abs(currentPrices.Close - previousPrices.Close) / MaxOpenCloseDifference);
            closeProximityScore = Math.Clamp(closeProximityScore, 0, 1);

            double wickScore = 1 - (currentMetrics.UpperWick / MaxUpperWick);
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
            double openDeviation = Math.Abs(currentPrices.Open - previousPrices.Close) / MaxOpenCloseDifference;
            double closeDeviation = Math.Abs(currentPrices.Close - previousPrices.Close) / MaxOpenCloseDifference;
            double wickDeviation = currentMetrics.UpperWick / MaxUpperWick;
            double trendDeviation = Math.Abs(currentMetrics.GetLookbackMeanTrend(2) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (bodyDeviation + openDeviation + closeDeviation + wickDeviation + trendDeviation) / 5;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








