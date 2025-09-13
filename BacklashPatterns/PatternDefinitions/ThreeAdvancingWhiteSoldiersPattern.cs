using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class ThreeAdvancingWhiteSoldiersPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for each candle. Ensures strong bullish momentum.
        /// Strictest: 2.0 (very strong), Loosest: 0.5 (minimal momentum).
        /// </summary>
        public static double MinBodySize { get; } = 1.0;

        /// <summary>
        /// Maximum wick size relative to the total range. Ensures small wicks for strong candles.
        /// Strictest: 0.1 (tiny wicks), Loosest: 0.4 (larger wicks allowed).
        /// </summary>
        public static double WickRangeRatio { get; } = 0.2;

        /// <summary>
        /// Threshold for confirming a prior downtrend. Negative value indicates downtrend.
        /// Strictest: -0.5 (strong downtrend), Loosest: -0.1 (weak downtrend).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;
        public const string BaseName = "ThreeAdvancingWhiteSoldiers";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public ThreeAdvancingWhiteSoldiersPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies a Three Advancing White Soldiers pattern, a three-candle bullish reversal pattern.
        /// Occurs after a downtrend; three consecutive bullish candles with large bodies and small wicks,
        /// each closing higher than the previous. Signals strong buying pressure and a potential trend reversal.
        /// Source: https://www.investopedia.com/terms/t/three_white_soldiers.asp
        /// </summary>
        public static ThreeAdvancingWhiteSoldiersPattern? IsPattern(
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

            // Check each candle: Bullish, significant body, small wicks
            if (metrics1.BodySize < MinBodySize || !metrics1.IsBullish ||
                metrics1.UpperWick > WickRangeRatio * metrics1.TotalRange ||
                metrics1.LowerWick > WickRangeRatio * metrics1.TotalRange) return null;
            if (metrics2.BodySize < MinBodySize || !metrics2.IsBullish ||
                metrics2.UpperWick > WickRangeRatio * metrics2.TotalRange ||
                metrics2.LowerWick > WickRangeRatio * metrics2.TotalRange) return null;
            if (metrics3.BodySize < MinBodySize || !metrics3.IsBullish ||
                metrics3.UpperWick > WickRangeRatio * metrics3.TotalRange ||
                metrics3.LowerWick > WickRangeRatio * metrics3.TotalRange) return null;

            // Ascending closes
            var ask1 = prices[c1];
            var ask2 = prices[c2];
            var ask3 = prices[c3];
            if (ask2.Close <= ask1.Close || ask3.Close <= ask2.Close) return null;

            // Downtrend check using LookbackMeanTrend (3 candles)
            if (metrics3.GetLookbackMeanTrend(3) > TrendThreshold) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new ThreeAdvancingWhiteSoldiersPattern(candles);
        }
    }
}








