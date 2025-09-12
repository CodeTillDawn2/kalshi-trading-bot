using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class UpsideGapTwoCrowsPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size of the first candle as a percentage of its total range.
        /// Purpose: Ensures the first candle has a significant bullish move to establish the uptrend context.
        /// Strictest: 0.5 (requires a strong body), Loosest: 0.1 (minimal body to still be considered bullish).
        /// </summary>
        public static double FirstCandleMinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Threshold for confirming an uptrend based on lookback mean trend.
        /// Purpose: Validates that the pattern occurs in an uptrend, critical for reversal significance.
        /// Strictest: 0.5 (strong uptrend), Loosest: 0.1 (barely trending up).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;

        /// <summary>
        /// Tolerance for overlap in the gap between the first and second candles.
        /// Purpose: Allows some flexibility in the "gap up" definition while maintaining separation.
        /// Strictest: 0 (no overlap), Loosest: 1.0 (significant overlap still considered a gap).
        /// </summary>
        public static double GapOverlapTolerance { get; set; } = 0.5;

        /// <summary>
        /// Tolerance for how far the third candle�s close can deviate above the first candle�s close.
        /// Purpose: Ensures the third candle closes near the first to signal weakening momentum.
        /// Strictest: 0 (exact match), Loosest: 2.0 (allows wider deviation while still bearish).
        /// </summary>
        public static double ThirdCandleCloseTolerance { get; set; } = 1.0;

        /// <summary>
        /// Represents the Upside Gap Two Crows pattern, a three-candle bearish reversal pattern.
        /// Occurs in an uptrend with a bullish candle, followed by two bearish candles ("crows") 
        /// where the second gaps up from the first and the third engulfs part of the second, 
        /// closing below it but not too far from the first.
        /// Indicates a potential reversal from bullish to bearish momentum.
        /// Source: https://www.investopedia.com/terms/u/upside-gap-two-crows.asp
        /// </summary>
        public const string BaseName = "UpsideGapTwoCrows";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public UpsideGapTwoCrowsPattern(List<int> candles) : base(candles)
        {
        }

        public static UpsideGapTwoCrowsPattern IsPattern(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            int c1 = index - 2; // First candle (bullish)
            int c2 = index - 1; // Second candle (first crow)
            int c3 = index;     // Third candle (second crow)

            if (c1 < 0 || c3 >= prices.Length) return null;

            var metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

            // Uptrend check
            if (metrics3.GetLookbackMeanTrend(3) <= TrendThreshold) return null;

            // First candle: Bullish with sufficient body
            if (metrics1.BodySize < FirstCandleMinBodySize || !metrics1.IsBullish) return null;

            // Second candle: Bearish, gaps up from first (with tolerance), closes above first open
            if (!metrics2.IsBearish ||
                prices[c2].Open < prices[c1].Close - GapOverlapTolerance ||
                prices[c2].Close <= prices[c1].Open) return null;

            // Third candle: Bearish, opens between second�s range, closes below second, near first close
            if (!metrics3.IsBearish ||
                prices[c3].Open > prices[c2].Open + GapOverlapTolerance ||
                prices[c3].Open < prices[c2].Close - GapOverlapTolerance ||
                prices[c3].Close >= prices[c2].Close ||
                prices[c3].Close > prices[c1].Close + ThirdCandleCloseTolerance) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new UpsideGapTwoCrowsPattern(candles);
        }
    }
}







