using SmokehouseDTOs;
using SmokehousePatterns.Helpers;
using static SmokehousePatterns.Helpers.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    /// <summary>
    /// The Identical Three Crows is a three-candle bearish reversal pattern occurring after an uptrend.
    /// - Three similar-sized bearish candles with descending closes and opens near the previous close.
    /// - Indicates strong selling pressure and a potential trend reversal to the downside.
    /// Source: https://www.investopedia.com/terms/t/three_black_crows.asp (similar but stricter variant)
    /// </summary>
    public class IdenticalThreeCrowsPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for each candle in the pattern.
        /// Strictest: 1.5 (significant move required); Loosest: 0.5 (small but noticeable body).
        /// </summary>
        public static double MinBodySize { get; set; } = 1.5;

        /// <summary>
        /// Maximum allowable difference between body sizes of the three candles for consistency.
        /// Strictest: 0.5 (very uniform); Loosest: 2.0 (allows more variation).
        /// </summary>
        public static double MaxBodySizeDifference { get; set; } = 1.5;

        /// <summary>
        /// Maximum wick size (upper and lower) for each candle to ensure focus on body.
        /// Strictest: 0.5 (minimal wicks); Loosest: 2.0 (allows moderate wicks).
        /// </summary>
        public static double MaxWickSize { get; set; } = 1.5;

        /// <summary>
        /// Maximum difference between a candle's open and the previous candle's close.
        /// Strictest: 0.5 (tight continuation); Loosest: 2.0 (allows minor gaps).
        /// </summary>
        public static double MaxOpenCloseDifference { get; set; } = 1.5;

        /// <summary>
        /// Minimum mean trend value to confirm a preceding uptrend.
        /// Strictest: 0.5 (strong uptrend); Loosest: 0.1 (weak uptrend).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;

        /// <summary>
        /// Minimum trend consistency to ensure reliability of the uptrend.
        /// Strictest: 0.8 (highly consistent); Loosest: 0.3 (minimally consistent).
        /// </summary>
        public static double ConsistencyThreshold { get; set; } = 0.5;
        public const string BaseName = "IdenticalThreeCrows";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public IdenticalThreeCrowsPattern(List<int> candles) : base(candles)
        {
        }

        public static IdenticalThreeCrowsPattern IsPattern(
            int index,
            CandleMids[] prices,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            int i1 = index - 2;
            int i2 = index - 1;
            int i3 = index;

            var metrics1 = GetCandleMetrics(ref metricsCache, i1, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, i2, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, i3, prices, trendLookback, true);

            // Direction check
            if (!metrics1.IsBearish || !metrics2.IsBearish || !metrics3.IsBearish) return null;

            // Body size check
            if (metrics1.BodySize < MinBodySize || metrics2.BodySize < MinBodySize || metrics3.BodySize < MinBodySize) return null;

            // Body size consistency
            double maxBody = Math.Max(Math.Max(metrics1.BodySize, metrics2.BodySize), metrics3.BodySize);
            double minBody = Math.Min(Math.Min(metrics1.BodySize, metrics2.BodySize), metrics3.BodySize);
            if (maxBody - minBody > MaxBodySizeDifference) return null;

            // Descending closes
            if (prices[i1].Close <= prices[i2].Close || prices[i2].Close <= prices[i3].Close) return null;

            // Open near previous close
            if (Math.Abs(prices[i2].Open - prices[i1].Close) > MaxOpenCloseDifference ||
                Math.Abs(prices[i3].Open - prices[i2].Close) > MaxOpenCloseDifference) return null;

            // Wick size limits
            if (metrics1.UpperWick > MaxWickSize || metrics2.UpperWick > MaxWickSize || metrics3.UpperWick > MaxWickSize) return null;
            if (metrics1.LowerWick > MaxWickSize || metrics2.LowerWick > MaxWickSize || metrics3.LowerWick > MaxWickSize) return null;

            // Uptrend check
            bool uptrend = metrics3.GetLookbackMeanTrend(3) > TrendThreshold &&
                           metrics3.GetLookbackTrendConsistency(3) >= ConsistencyThreshold;
            if (!uptrend) return null;

            var candles = new List<int> { i1, i2, i3 };
            return new IdenticalThreeCrowsPattern(candles);
        }
    }
}