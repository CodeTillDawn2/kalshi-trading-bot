using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class KickingPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for both candles.
        /// Purpose: Ensures significant body size for momentum.
        /// Strictest: 1.5 (current default, strong body).
        /// Loosest: 1.0 (relaxed but still notable per Investopedia).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.8;

        /// <summary>
        /// Maximum wick size (upper and lower).
        /// Purpose: Limits wicks to resemble Marubozu.
        /// Strictest: 0.5 (near pure Marubozu).
        /// Loosest: 2.0 (allows larger wicks per trading forums).
        /// </summary>
        public static double MaxWickSize { get; set; } = 2.5;

        /// <summary>
        /// Minimum gap size between candles.
        /// Purpose: Ensures a visible momentum shift.
        /// Strictest: 0.5 (current default, clear gap).
        /// Loosest: 0.1 (minimal gap per technical analysis).
        /// </summary>
        public static double GapSize { get; set; } = 0.5;

        /// <summary>
        /// Minimum trend strength for prior trend.
        /// Purpose: Confirms trend context before reversal.
        /// Strictest: 0.5 (strong trend).
        /// Loosest: 0.1 (weak trend still valid).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.05;
        public const string BaseName = "Kicking";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        private readonly bool IsBullish;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public KickingPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Kicking pattern, a two-candle reversal pattern.
        /// Requirements (sourced from Investopedia and other technical analysis resources):
        /// - Two large-bodied candles with opposite directions.
        /// - A gap between the candles (up for bullish, down for bearish).
        /// - Small or no wicks, resembling Marubozu candles.
        /// - Bullish Kicking follows a downtrend; Bearish Kicking follows an uptrend.
        /// Indicates: Strong reversal signal due to abrupt momentum shift.
        /// </summary>
        public static async Task<KickingPattern?> IsPatternAsync(
        int index, bool isBullish, int trendLookback,
        Dictionary<int, CandleMetrics> metricsCache, CandleMids[] prices)
        {
            if (index < 1) return null;

            var prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Trend check
            bool trendValid = isBullish ? currMetrics.GetLookbackMeanTrend(2) <= -TrendThreshold
                                       : currMetrics.GetLookbackMeanTrend(2) >= TrendThreshold;
            if (!trendValid) return null;

            // Body size check
            if (prevMetrics.BodySize < MinBodySize || currMetrics.BodySize < MinBodySize) return null;

            // Wick limits check
            if (prevMetrics.UpperWick > MaxWickSize || prevMetrics.LowerWick > MaxWickSize) return null;
            if (currMetrics.UpperWick > MaxWickSize || currMetrics.LowerWick > MaxWickSize) return null;

            // Direction check
            bool directions = isBullish ? (prevMetrics.IsBearish && currMetrics.IsBullish)
                                       : (prevMetrics.IsBullish && currMetrics.IsBearish);
            if (!directions) return null;

            return new KickingPattern(new List<int> { index - 1, index }, isBullish);
        }
    }
}








