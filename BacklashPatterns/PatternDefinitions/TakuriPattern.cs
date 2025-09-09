using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class TakuriPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range of the candle to ensure sufficient volatility.
        /// Strictest: 3.0 (high volatility); Loosest: 1.0 (minimal range).
        /// </summary>
        public static double MinRange { get; set; } = 2.0;

        /// <summary>
        /// Maximum ratio of body size to total range for a small body.
        /// Strictest: 0.1 (tiny body); Loosest: 0.3 (broader small body).
        /// </summary>
        public static double BodyRangeRatio { get; set; } = 0.2;

        /// <summary>
        /// Minimum ratio of lower wick to total range for a significant wick.
        /// Strictest: 0.6 (very long wick); Loosest: 0.3 (moderate wick).
        /// </summary>
        public static double WickRangeRatio { get; set; } = 0.4;

        /// <summary>
        /// Minimum ratio of lower wick to body size to emphasize buying pressure.
        /// Strictest: 3.0 (strong pressure); Loosest: 1.5 (minimal pressure).
        /// </summary>
        public static double WickBodyRatio { get; set; } = 2.0;

        /// <summary>
        /// Maximum trend value to confirm a downtrend context.
        /// Strictest: -0.5 (strong downtrend); Loosest: -0.1 (weak downtrend).
        /// </summary>
        public static double TrendThreshold { get; set; } = -0.3;

        /// <summary>
        /// Minimum trend consistency to ensure a reliable downtrend.
        /// Strictest: 0.6 (highly consistent); Loosest: 0.3 (minimally consistent).
        /// </summary>
        public static double MinTrendConsistency { get; set; } = 0.4;
        public const string BaseName = "Takuri";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public TakuriPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies a Takuri pattern, a single-candle bullish reversal pattern.
        /// Occurs in a downtrend; features a small body near the top, a long lower wick (at least twice the body),
        /// and little to no upper wick. Indicates strong buying pressure after a decline, suggesting a potential reversal.
        /// Source: https://www.babypips.com/learn/forex/takuri-line-candlestick-pattern
        /// </summary>
        public static TakuriPattern IsPattern(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices)
        {
            var metrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Downtrend check using LookbackMeanTrend and LookbackTrendConsistency
            if (metrics.GetLookbackMeanTrend(1) > TrendThreshold ||
                metrics.GetLookbackTrendConsistency(1) < MinTrendConsistency) return null;

            // Range and shape conditions
            if (metrics.TotalRange < MinRange) return null;
            bool shape = metrics.BodySize <= BodyRangeRatio * metrics.TotalRange &&
                         metrics.LowerWick >= WickRangeRatio * metrics.TotalRange &&
                         metrics.LowerWick >= WickBodyRatio * metrics.BodySize &&
                         metrics.UpperWick <= metrics.BodySize &&
                         metrics.IsBullish;

            if (!shape) return null;

            var candles = new List<int> { index };
            return new TakuriPattern(candles);
        }
    }
}
