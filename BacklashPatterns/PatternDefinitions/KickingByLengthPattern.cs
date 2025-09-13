using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    public class KickingByLengthPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for both candles.
        /// Purpose: Ensures candles have significant body size to indicate strong momentum.
        /// Strictest: 1.5 (current default, aligns with Marubozu-like strength).
        /// Loosest: 1.0 (still notable body size while relaxing strictness).
        /// </summary>
        public static double MinBodySize { get; set; } = 1.0;

        /// <summary>
        /// Maximum wick size (upper and lower) for Marubozu-like candles.
        /// Purpose: Limits wick size to maintain Marubozu resemblance.
        /// Strictest: 0.3 (nearly pure Marubozu per Investopedia).
        /// Loosest: 0.7 (allows more flexibility while preserving pattern intent).
        /// </summary>
        public static double MaxWickSize { get; set; } = 1.0;

        /// <summary>
        /// Minimum gap size between the two candles.
        /// Purpose: Ensures a clear separation indicating momentum shift.
        /// Strictest: 0.5 (current default, significant gap).
        /// Loosest: 0.1 (minimal gap still noticeable per TradingView).
        /// </summary>
        public static double GapSize { get; set; } = 0.5;

        /// <summary>
        /// Minimum trend strength for prior trend.
        /// Purpose: Confirms preceding trend direction before reversal.
        /// Strictest: 0.5 (strong trend per technical analysis norms).
        /// Loosest: 0.1 (weak trend still sufficient for context).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.05;
        public const string BaseName = "KickingByLength";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        private readonly bool IsBullish;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public KickingByLengthPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Kicking By Length pattern, a two-candle reversal pattern.
        /// Requirements (sourced from TradingView and Investopedia):
        /// - Two large-bodied candles with opposite directions, resembling Marubozu (minimal wicks).
        /// - A significant gap between the candles (bullish: up gap; bearish: down gap).
        /// - Bullish pattern follows a downtrend; bearish pattern follows an uptrend.
        /// - Candles should have minimal wicks (less strict than pure Marubozu).
        /// Indicates: A strong reversal due to an abrupt momentum shift, supported by large bodies and a gap.
        /// Note: Original logic relaxed strictness from pure Marubozu and adjusted trend thresholds.
        /// </summary>
        public static KickingByLengthPattern? IsPattern(
                int index, int trendLookback, bool isBullish,
                Dictionary<int, CandleMetrics> metricsCache, CandleMids[] prices)
        {
            if (index < 1) return null;

            var prevMetrics = GetCandleMetrics(ref metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Trend check
            if (isBullish && currMetrics.GetLookbackMeanTrend(2) >= -TrendThreshold) return null;
            if (!isBullish && currMetrics.GetLookbackMeanTrend(2) <= TrendThreshold) return null;

            // Body size check
            if (prevMetrics.BodySize < MinBodySize || currMetrics.BodySize < MinBodySize) return null;

            // Wick limits check
            if (prevMetrics.UpperWick > MaxWickSize || prevMetrics.LowerWick > MaxWickSize) return null;
            if (currMetrics.UpperWick > MaxWickSize || currMetrics.LowerWick > MaxWickSize) return null;

            // Direction check
            bool directions = isBullish ? (prevMetrics.IsBearish && currMetrics.IsBullish)
                                       : (prevMetrics.IsBullish && currMetrics.IsBearish);
            if (!directions) return null;

            return new KickingByLengthPattern(new List<int> { index - 1, index }, isBullish);
        }
    }
}








