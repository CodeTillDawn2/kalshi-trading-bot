using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
/// <summary>ThrustingPattern</summary>
/// <summary>ThrustingPattern</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
    public class ThrustingPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first candle to qualify as significant.
        /// Strictest: 1.0 (original logic), Loosest: 0.3 (minimal but still notable size).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.5;
/// <summary>Gets or sets the Uncertainty.</summary>
/// <summary>Gets or sets the Certainty.</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>

/// <summary>IsPatternAsync</summary>
/// <summary>
/// </summary>
        /// <summary>
        /// Maximum body size for the second candle to maintain pattern integrity.
        /// Strictest: 2.0 (original logic), Loosest: 4.0 (allows larger but still controlled thrust).
        /// </summary>
        public static double MaxBodySize { get; set; } = 3.0;
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>

/// <summary>
/// </summary>
/// <summary>
/// </summary>
        /// <summary>
        /// Threshold for confirming a downtrend in a bullish thrusting pattern (negative value).
        /// Strictest: -0.5 (strong downtrend), Loosest: 0.0 (neutral or very weak downtrend).
        /// </summary>
        public static double BullishTrendThreshold { get; set; } = -0.2;
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>

/// <summary>
/// </summary>
/// <summary>
/// </summary>
        /// <summary>
        /// Threshold for confirming an uptrend in a bearish thrusting pattern (positive value).
        /// Strictest: 0.5 (strong uptrend), Loosest: 0.0 (neutral or very weak uptrend).
        /// </summary>
        public static double BearishTrendThreshold { get; set; } = 0.2;
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>

/// <summary>
/// </summary>
/// <summary>
/// </summary>
        /// <summary>
        /// Represents a Thrusting pattern (Bullish or Bearish).
        /// - Bullish Thrusting: A potential continuation or weak reversal in a downtrend. First candle is bearish, second is bullish but doesn t close above the first s open, indicating hesitation.
        /// - Bearish Thrusting: A potential continuation or weak reversal in an uptrend. First candle is bullish, second is bearish but doesn t close below the first s open, showing indecision.
        /// Requirements sourced from: https://www.babypips.com/learn/forex/thrusting-pattern
        /// </summary>
        public const string BaseName = "Thrusting";
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        private readonly bool IsBullish;
/// <summary>
/// </summary>
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
/// <summary>
/// </summary>

        public ThrustingPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        public static async Task<ThrustingPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            bool isBullish,
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

            if (isBullish)
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
            return new ThrustingPattern(candles, isBullish);
        }
    }
}








