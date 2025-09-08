using SmokehouseDTOs;
using static SmokehousePatterns.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    public class ThreeOutsidePattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size of the first candle to qualify for the pattern.
        /// Strictest: 1.0 (original logic), Loosest: 0.5 (allows smaller but still significant candles).
        /// </summary>
        public static double MinBodySize { get; set; } = 1.0;

        /// <summary>
        /// Factor by which the second candle's body must equal or exceed the first candle's body in a bullish pattern.
        /// Strictest: 1.5 (original logic), Loosest: 0.8 (relaxed to allow less aggressive engulfing).
        /// </summary>
        public static double BullishEngulfFactor { get; set; } = 1.0;

        /// <summary>
        /// Factor by which the second candle's body must equal or exceed the first candle's body in a bearish pattern.
        /// Strictest: 1.5 (common strict engulfing), Loosest: 0.5 (allows minimal engulfing).
        /// </summary>
        public static double BearishEngulfFactor { get; set; } = 0.8;

        /// <summary>
        /// Threshold for determining the preceding trend strength (± value).
        /// Strictest: 0.5 (strong trend), Loosest: 0.1 (very weak trend still identifiable).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;

        /// <summary>
        /// Represents a Three Outside pattern (Up or Down).
        /// - ThreeOutsideUp: A bullish reversal pattern after a downtrend. First candle is bearish, second candle is bullish and engulfs the first, third candle is bullish and confirms by closing higher.
        /// - ThreeOutsideDown: A bearish reversal pattern after an uptrend. First candle is bullish, second candle is bearish and engulfs the first, third candle is bearish and confirms by closing lower.
        /// Requirements sourced from: https://www.investopedia.com/terms/t/three-outside-up-down.asp
        /// </summary>
        public const string BaseName = "ThreeOutside";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public ThreeOutsidePattern(List<int> candles) : base(candles)
        {
        }

        public static ThreeOutsidePattern IsPattern(
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null; // Need 3 candles

            int firstIndex = index - 2;
            int secondIndex = index - 1;
            int thirdIndex = index;

            // Lazy load metrics for the three candles
            var metrics1 = GetCandleMetrics(ref metricsCache, firstIndex, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, secondIndex, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, thirdIndex, prices, trendLookback, true);

            double body1 = metrics1.BodySize;
            double body2 = metrics2.BodySize;

            if (isBullish) // ThreeOutsideUp
            {
                bool firstBearish = metrics1.IsBearish && body1 >= MinBodySize;
                if (!firstBearish) return null;

                bool secondEngulfs = metrics2.IsBullish &&
                                     prices[secondIndex].Open <= prices[firstIndex].Close &&
                                     prices[secondIndex].Close >= prices[firstIndex].Open &&
                                     body2 >= body1 * BullishEngulfFactor;
                if (!secondEngulfs) return null;

                bool thirdConfirms = metrics3.IsBullish && prices[thirdIndex].Close > prices[secondIndex].Close;
                if (!thirdConfirms) return null;

                bool hasDowntrend = metrics3.GetLookbackMeanTrend(3) <= -TrendThreshold;
                if (!hasDowntrend) return null;
            }
            else // ThreeOutsideDown
            {
                bool firstBullish = metrics1.IsBullish && body1 >= MinBodySize;
                if (!firstBullish) return null;

                bool secondEngulfs = metrics2.IsBearish &&
                                     prices[secondIndex].Open >= prices[firstIndex].Close &&
                                     prices[secondIndex].Close <= prices[firstIndex].Open &&
                                     body2 >= body1 * BearishEngulfFactor;
                if (!secondEngulfs) return null;

                bool thirdConfirms = metrics3.IsBearish && prices[thirdIndex].Close < prices[secondIndex].Close;
                if (!thirdConfirms) return null;

                bool hasUptrend = metrics3.GetLookbackMeanTrend(3) >= TrendThreshold;
                if (!hasUptrend) return null;
            }

            var candles = new List<int> { firstIndex, secondIndex, thirdIndex };
            return new ThreeOutsidePattern(candles);
        }
    }
}