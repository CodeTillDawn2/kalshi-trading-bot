using SmokehousePatterns.Helpers;
using static SmokehousePatterns.Helpers.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    public class StickSandwichPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for all candles to ensure significant movement.
        /// Strictest: 1.0 (strong candles); Loosest: 0.3 (minimal significance).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Maximum difference between the closes of the first and third candles.
        /// Strictest: 0.1 (nearly identical); Loosest: 1.0 (broader tolerance).
        /// </summary>
        public static double MaxCloseDifference { get; set; } = 0.5;

        /// <summary>
        /// Minimum trend threshold to confirm the preceding trend direction.
        /// Strictest: 0.5 (strong trend); Loosest: 0.1 (weak trend).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;
        public const string BaseName = "StickSandwich";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        private readonly bool IsBullish;

        public StickSandwichPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Stick Sandwich pattern, a three-candle reversal pattern.
        /// Bullish: Occurs in a downtrend; two bearish candles sandwich a bullish candle, with c1 and c3 closes nearly equal.
        /// Bearish: Occurs in an uptrend; two bullish candles sandwich a bearish candle, with c1 and c3 closes nearly equal.
        /// Indicates a potential reversal as the middle candle’s move is rejected.
        /// Source: https://www.tradingview.com/education/stick-sandwich/
        /// </summary>
        public static StickSandwichPattern IsPattern(
            int index,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            int trendLookback)
        {
            if (index < 2) return null; // Need 3 candles

            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            var metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

            double body1 = metrics1.BodySize;
            double body2 = metrics2.BodySize;
            double body3 = metrics3.BodySize;

            if (isBullish)
            {
                // Directions
                if (!metrics1.IsBearish || !metrics2.IsBullish || !metrics3.IsBearish) return null;

                // Closes of c1 and c3 nearly equal (within tolerance)
                bool closesMatch = Math.Abs(prices[c3].Close - prices[c1].Close) <= MaxCloseDifference;
                if (!closesMatch) return null;

                // Second candle exceeds first candle’s range
                if (prices[c2].High < prices[c1].High || prices[c2].Low > prices[c1].Low) return null;

                // Significant bodies
                if (body1 < MinBodySize || body2 < MinBodySize || body3 < MinBodySize) return null;

                // Downtrend check using LookbackMeanTrend (3 candles)
                if (metrics3.GetLookbackMeanTrend(3) > -TrendThreshold) return null;
            }
            else // Bearish
            {
                // Directions
                if (!metrics1.IsBullish || !metrics2.IsBearish || !metrics3.IsBullish) return null;

                // Closes of c1 and c3 nearly equal (within tolerance)
                bool closesMatch = Math.Abs(prices[c3].Close - prices[c1].Close) <= MaxCloseDifference;
                if (!closesMatch) return null;

                // Second candle exceeds first candle’s range
                if (prices[c2].High < prices[c1].High || prices[c2].Low > prices[c1].Low) return null;

                // Significant bodies
                if (body1 < MinBodySize || body2 < MinBodySize || body3 < MinBodySize) return null;

                // Uptrend check using LookbackMeanTrend (3 candles)
                if (metrics3.GetLookbackMeanTrend(3) < TrendThreshold) return null;
            }

            var candles = new List<int> { c1, c2, c3 };
            return new StickSandwichPattern(candles, isBullish);
        }
    }
}