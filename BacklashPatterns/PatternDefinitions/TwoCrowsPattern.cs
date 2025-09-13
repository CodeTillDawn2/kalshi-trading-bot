using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class TwoCrowsPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size required for the first candle's real body (difference between open and close).
        /// Ensures the first candle is a significant bullish move.
        /// Strictest: 0.5 (current), Loosest: 0.3 (still shows notable bullishness).
        /// </summary>
        public static double MinBodySize { get; } = 0.1;

        /// <summary>
        /// Tolerance for the gap or overlap between candles, allowing slight deviations from a perfect gap.
        /// Strictest: 0.1 (almost no overlap), Loosest: 0.7 (allows more overlap while maintaining pattern intent).
        /// </summary>
        public static double GapTolerance { get; } = 2.0;

        /// <summary>
        /// Minimum trend strength required to confirm an uptrend before the pattern.
        /// Strictest: 0.5 (strong uptrend), Loosest: 0.1 (minimal uptrend still present).
        /// </summary>
        public static double TrendThreshold { get; } = 0.05;
        public const string BaseName = "TwoCrows";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public TwoCrowsPattern(List<int> candles) : base(candles)
        {
        }

        /*
         * Two Crows Pattern:
         * - Description: A three-candle bearish reversal pattern occurring in an uptrend. 
         *   Indicates a potential top as two bearish candles ("crows") follow a strong bullish candle.
         * - Requirements (Source: Investopedia):
         *   1. First candle: Strong bullish candle in an uptrend.
         *   2. Second candle: Bearish, gaps up from the first candle�s close.
         *   3. Third candle: Bearish, opens within the second candle�s range, closes near or below the first candle�s close.
         * - Indication: Suggests selling pressure overcoming buying momentum, potential reversal to downtrend.
         */
        public static TwoCrowsPattern? IsPattern(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null; // Need 3 candles
            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle
            if (c1 < 0 || c3 >= prices.Length) return null;

            var metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

            // First candle: Bullish, minimal body size
            if (!metrics1.IsBullish || metrics1.BodySize < 0.1) return null;

            // Second candle: Bearish, opens above c1 close (with tolerance), closes with flexibility
            if (!metrics2.IsBearish ||
                prices[c2].Open <= prices[c1].Close - GapTolerance ||
                prices[c2].Close <= prices[c1].Close - GapTolerance) return null;

            // Third candle: Bearish, opens below c2 open, closes below c1 open (with tolerance)
            if (!metrics3.IsBearish ||
                prices[c3].Open > prices[c2].Open + GapTolerance ||
                prices[c3].Close > prices[c1].Open + GapTolerance) return null;

            // Minimal uptrend check
            if (metrics3.GetLookbackMeanTrend(3) <= 0.05) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new TwoCrowsPattern(candles);
        }
    }
}








