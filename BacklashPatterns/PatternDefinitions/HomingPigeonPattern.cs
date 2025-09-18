using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The Homing Pigeon is a two-candle bullish reversal pattern occurring after a downtrend.
    /// - Two bearish candles where the second is smaller and contained within the first s range.
    /// - Indicates slowing bearish momentum and potential reversal to the upside.
    /// Source: https://www.babypips.com/learn/forex/homing-pigeon
    /// </summary>
    public class HomingPigeonPattern : PatternDefinition
    {
        /// <summary>
        /// Maximum increase in the second candle s open relative to the first s open.
        /// Strictest: 0.2 (tight range); Loosest: 1.0 (allows more separation).
        /// </summary>
        public static double MaxOpenBuffer { get; set; } = 0.5;

        /// <summary>
        /// Maximum decrease in the second candle s close relative to the first s close.
        /// Strictest: 0.2 (tight range); Loosest: 1.0 (allows more separation).
        /// </summary>
        public static double MaxCloseBuffer { get; set; } = 0.5;

        /// <summary>
        /// Base maximum difference between the lows of the two candles.
        /// Strictest: 1.0 (very close lows); Loosest: 5.0 (wider range).
        /// </summary>
        public static double BaseMaxLowDifference { get; set; } = 3.0;

        /// <summary>
        /// Maximum low difference as a percentage of the first candle s range.
        /// Strictest: 0.3 (tight range); Loosest: 0.7 (wider range).
        /// </summary>
        public static double LowRangeRatio { get; set; } = 0.5;

        /// <summary>
        /// Maximum mean trend value to confirm a preceding downtrend.
        /// Strictest: -0.5 (strong downtrend); Loosest: -0.1 (weak downtrend).
        /// </summary>
        public static double TrendThreshold { get; set; } = -0.3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "HomingPigeon";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bullish reversal pattern in a downtrend with two bearish candles where the second is completely contained within the first, signaling slowing bearish momentum and potential reversal.";
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
        /// Initializes a new instance of the HomingPigeonPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public HomingPigeonPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Homing Pigeon pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the second candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<HomingPigeonPattern?> IsPatternAsync(
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

            // Direction check
            if (!previousMetrics.IsBearish || !currentMetrics.IsBearish) return null;

            // Positional relationships
            if (currentPrice.Open > previousPrice.Open + MaxOpenBuffer ||
                currentPrice.Close < previousPrice.Close - MaxCloseBuffer ||
                currentPrice.Close > previousPrice.Open) return null;

            // Second candle smaller
            if (currentMetrics.BodySize >= previousMetrics.BodySize) return null;

            // Low difference check
            double maxLowDifference = Math.Max(BaseMaxLowDifference, LowRangeRatio * previousMetrics.TotalRange);
            if (Math.Abs(currentPrice.Low - previousPrice.Low) > maxLowDifference) return null;

            // Downtrend check
            if (currentMetrics.GetLookbackMeanTrend(2) > TrendThreshold) return null;

            var candles = new List<int> { index - 1, index };
            return new HomingPigeonPattern(candles);
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
                throw new InvalidOperationException("HomingPigeonPattern must have exactly 2 candles.");

            int prevIndex = Candles[0];
            int currIndex = Candles[1];

            var previousMetrics = metricsCache[prevIndex];
            var currentMetrics = metricsCache[currIndex];

            var previousPrice = prices[prevIndex];
            var currentPrice = prices[currIndex];

            // Power Score: Based on containment, body size reduction, low proximity, trend
            double containmentScore = 1 - ((currentPrice.Open - previousPrice.Open) / MaxOpenBuffer +
                                           (previousPrice.Close - currentPrice.Close) / MaxCloseBuffer) / 2;
            containmentScore = Math.Clamp(containmentScore, 0, 1);

            double bodyReductionScore = 1 - (currentMetrics.BodySize / previousMetrics.BodySize);
            bodyReductionScore = Math.Clamp(bodyReductionScore, 0, 1);

            double lowProximityScore = 1 - (Math.Abs(currentPrice.Low - previousPrice.Low) /
                                           Math.Max(BaseMaxLowDifference, LowRangeRatio * previousMetrics.TotalRange));
            lowProximityScore = Math.Clamp(lowProximityScore, 0, 1);

            double trendStrength = Math.Abs(currentMetrics.GetLookbackMeanTrend(2) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wContainment = 0.25, wBodyReduction = 0.25, wLowProximity = 0.25, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wContainment * containmentScore + wBodyReduction * bodyReductionScore +
                                 wLowProximity * lowProximityScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wContainment + wBodyReduction + wLowProximity + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(currentMetrics.BodySize - previousMetrics.BodySize) / previousMetrics.BodySize;
            double trendDeviation = Math.Abs(currentMetrics.GetLookbackMeanTrend(2) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (bodyDeviation + trendDeviation) / 2;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








