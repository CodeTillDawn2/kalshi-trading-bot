using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
/// <summary>ThrustingPattern</summary>
/// </summary>
    public class ThrustingPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first candle to qualify as significant.
        /// Strictest: 1.0 (original logic), Loosest: 0.3 (minimal but still notable size).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Maximum body size for the second candle to maintain pattern integrity.
        /// Strictest: 2.0 (original logic), Loosest: 4.0 (allows larger but still controlled thrust).
        /// </summary>
        public static double MaxBodySize { get; set; } = 3.0;

        /// <summary>
        /// Threshold for confirming a downtrend in a bullish thrusting pattern (negative value).
        /// Strictest: -0.5 (strong downtrend), Loosest: 0.0 (neutral or very weak downtrend).
        /// </summary>
        public static double BullishTrendThreshold { get; set; } = -0.2;

        /// <summary>
        /// Threshold for confirming an uptrend in a bearish thrusting pattern (positive value).
        /// Strictest: 0.5 (strong uptrend), Loosest: 0.0 (neutral or very weak uptrend).
        /// </summary>
        public static double BearishTrendThreshold { get; set; } = 0.2;

        /// <summary>
        /// Represents a Thrusting pattern (Bullish or Bearish).
        /// - Bullish Thrusting: A potential continuation or weak reversal in a downtrend. First candle is bearish, second is bullish but doesn t close above the first s open, indicating hesitation.
        /// - Bearish Thrusting: A potential continuation or weak reversal in an uptrend. First candle is bullish, second is bearish but doesn t close below the first s open, showing indecision.
        /// Requirements sourced from: https://www.babypips.com/learn/forex/thrusting-pattern
        /// </summary>
        public const string BaseName = "Thrusting";
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => Direction == PatternDirection.Bullish
            ? "A bullish continuation pattern in a downtrend with a bearish candle followed by a bullish candle that opens below the previous close but closes above it, showing hesitation before continuing the downtrend."
            : "A bearish continuation pattern in an uptrend with a bullish candle followed by a bearish candle that opens above the previous close but closes below it, showing hesitation before continuing the uptrend.";

        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction { get; }

        public override string Name => BaseName + "_" + Direction.ToString();

        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }


        public ThrustingPattern(List<int> candles, PatternDirection direction) : base(candles)
        {
            Direction = direction;
        }

        public static async Task<ThrustingPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            PatternDirection direction,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 1) return null;

            int prevIndex = index - 1;
            int currIndex = index;

            var prevMetrics = await GetCandleMetricsAsync(metricsCache, prevIndex, prices, trendLookback, false);
            var currMetrics = await GetCandleMetricsAsync(metricsCache, currIndex, prices, trendLookback, true);
            CandleMids prevPrices = prices[prevIndex];
            CandleMids currPrices = prices[currIndex];

            if (direction == PatternDirection.Bullish)
            {
                if (currMetrics.GetLookbackMeanTrend(2) > BullishTrendThreshold) return null;

                bool isPatternValid = prevMetrics.BodySize >= MinBodySize &&
                                      prevMetrics.IsBearish &&
                                      currMetrics.BodySize <= MaxBodySize &&
                                      currMetrics.IsBullish &&
                                      currPrices.Open <= prevPrices.Close &&
                                      currPrices.Close > prevPrices.Close &&
                                      currPrices.Close < prevPrices.Open;
                if (!isPatternValid) return null;
            }
            else
            {
                if (currMetrics.GetLookbackMeanTrend(2) <= BearishTrendThreshold) return null;

                bool isPatternValid = prevMetrics.BodySize >= MinBodySize &&
                                      prevMetrics.IsBullish &&
                                      currMetrics.BodySize <= MaxBodySize &&
                                      currMetrics.IsBearish &&
                                      currPrices.Open >= prevPrices.Close &&
                                      currPrices.Close < prevPrices.Close &&
                                      currPrices.Close > prevPrices.Open;
                if (!isPatternValid) return null;
            }

            var candles = new List<int> { prevIndex, currIndex };
            return new ThrustingPattern(candles, direction);
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
                throw new InvalidOperationException("ThrustingPattern must have exactly 2 candles.");

            int prevIndex = Candles[0];
            int currIndex = Candles[1];

            var prevMetrics = metricsCache[prevIndex];
            var currMetrics = metricsCache[currIndex];

            var prevPrices = prices[prevIndex];
            var currPrices = prices[currIndex];

            // Power Score: Based on body sizes, thrust extent, trend
            double firstBodyScore = prevMetrics.BodySize / MinBodySize;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double secondBodyScore = 1 - (currMetrics.BodySize / MaxBodySize);
            secondBodyScore = Math.Clamp(secondBodyScore, 0, 1);

            double thrustExtent = 0;
            if (Direction == PatternDirection.Bullish)
            {
                thrustExtent = (prevPrices.Open - currPrices.Close) / prevMetrics.BodySize;
                thrustExtent = Math.Min(thrustExtent, 1);
            }
            else
            {
                thrustExtent = (currPrices.Close - prevPrices.Open) / prevMetrics.BodySize;
                thrustExtent = Math.Min(thrustExtent, 1);
            }

            double trendThreshold = Direction == PatternDirection.Bullish ? BullishTrendThreshold : BearishTrendThreshold;
            double trendStrength = Math.Abs(currMetrics.GetLookbackMeanTrend(2) - trendThreshold) / Math.Abs(trendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.2, wSecond = 0.2, wThrust = 0.3, wTrend = 0.2, wVolume = 0.1;
            double powerScore = (wFirst * firstBodyScore + wSecond * secondBodyScore + wThrust * thrustExtent +
                                 wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirst + wSecond + wThrust + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double firstDeviation = Math.Abs(prevMetrics.BodySize - MinBodySize) / MinBodySize;
            double secondDeviation = Math.Abs(currMetrics.BodySize - MaxBodySize) / MaxBodySize;
            double trendDeviation = Math.Abs(currMetrics.GetLookbackMeanTrend(2) - trendThreshold) / Math.Abs(trendThreshold);
            double matchScore = 1 - (firstDeviation + secondDeviation + trendDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








