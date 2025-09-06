using SmokehouseDTOs;
using SmokehousePatterns.Helpers;
using static SmokehousePatterns.Helpers.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    public class Unique3RiverPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first bearish candle to ensure a strong down move.
        /// Strictest: 1.0 (current), Loosest: 0.5 (still shows significant bearish momentum).
        /// </summary>
        public static double MinFirstBodySize { get; } = 1.0;

        /// <summary>
        /// Maximum body size for the second and third candles to maintain a slowing/reversal shape.
        /// Strictest: 1.0 (small bodies), Loosest: 2.5 (allows larger bodies but retains pattern intent).
        /// </summary>
        public static double MaxBodySize { get; } = 2.0;

        /// <summary>
        /// Tolerance for how close the second candle’s close must be to the first candle’s low.
        /// Strictest: 0.1 (very close), Loosest: 1.0 (broader range for nearness).
        /// </summary>
        public static double LowCloseTolerance { get; } = 0.5;

        /// <summary>
        /// Minimum negative trend strength to confirm a downtrend before the pattern.
        /// Strictest: -0.5 (strong downtrend), Loosest: -0.1 (minimal downtrend still present).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;
        public const string BaseName = "Unique3River";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public Unique3RiverPattern(List<int> candles) : base(candles)
        {
        }

        /*
         * Unique 3 River Pattern:
         * - Description: A three-candle bullish reversal pattern in a downtrend. 
         *   Suggests a bottoming process with slowing bearish momentum.
         * - Requirements (Source: TradingView):
         *   1. First candle: Strong bearish candle in a downtrend.
         *   2. Second candle: Bearish, smaller body, often a new low or close near first low.
         *   3. Third candle: Bullish, opens below second close, closes above it.
         * - Indication: Indicates potential exhaustion of sellers, possible trend reversal upward.
         */
        public static Unique3RiverPattern IsPattern(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;
            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle
            if (c1 < 0 || c3 >= prices.Length) return null;

            var metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

            // First candle: Bearish, significant body
            if (metrics1.BodySize < MinFirstBodySize || !metrics1.IsBearish) return null;

            // Second candle: Bearish, smaller body, new low or close near first low
            if (metrics2.BodySize > MaxBodySize || !metrics2.IsBearish) return null;

            // Third candle: Bullish, smaller body, reversal shape
            if (metrics3.BodySize > MaxBodySize || !metrics3.IsBullish) return null;

            var ask1 = prices[c1];
            var ask2 = prices[c2];
            var ask3 = prices[c3];

            // Shape conditions
            bool shape = (ask2.Low < ask1.Low || Math.Abs(ask2.Close - ask1.Low) <= LowCloseTolerance) &&
                         ask3.Open < ask2.Close &&
                         ask3.Close > ask2.Close;
            if (!shape) return null;

            // Downtrend check
            if (metrics3.GetLookbackMeanTrend(3) > TrendThreshold) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new Unique3RiverPattern(candles);
        }
    }
}