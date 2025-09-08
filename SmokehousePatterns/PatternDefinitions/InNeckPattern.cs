using SmokehouseDTOs;
using static SmokehousePatterns.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    /// <summary>
    /// The In Neck Pattern is a two-candle continuation pattern in a downtrend.
    /// - First candle bearish, second bullish with close near first close.
    /// - Indicates temporary pause in downtrend, likely to continue downward.
    /// Source: https://www.babypips.com/learn/forex/in-neck-pattern
    /// </summary>
    public class InNeckPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for first candle.
        /// Purpose: Ensures first candle has notable bearish strength.
        /// Strictest: 0.5 (current default, significant body).
        /// Loosest: 0.3 (minimal body still indicating direction per BabyPips).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Maximum difference between second open and first close.
        /// Purpose: Ensures second candle opens near first candle's close.
        /// Strictest: 1.0 (tight proximity).
        /// Loosest: 3.0 (relaxed proximity still within pattern per web sources).
        /// </summary>
        public static double MaxOpenDifference { get; set; } = 2.0;

        /// <summary>
        /// Maximum difference between second close and first close.
        /// Purpose: Ensures closes are close, indicating continuation.
        /// Strictest: 0.5 (very tight alignment).
        /// Loosest: 2.0 (still close enough per BabyPips).
        /// </summary>
        public static double MaxCloseDifference { get; set; } = 1.5;

        /// <summary>
        /// Maximum upper wick for second candle.
        /// Purpose: Limits upper wick to maintain pattern shape.
        /// Strictest: 1.0 (tight wick control).
        /// Loosest: 3.0 (allows flexibility per trading forums).
        /// </summary>
        public static double MaxWickSize { get; set; } = 2.0;

        /// <summary>
        /// Maximum mean trend for downtrend confirmation.
        /// Purpose: Confirms preceding downtrend.
        /// Strictest: -0.5 (strong downtrend).
        /// Loosest: -0.1 (weak downtrend still valid).
        /// </summary>
        public static double TrendThreshold { get; set; } = -0.3;
        public const string BaseName = "InNeck";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public InNeckPattern(List<int> candles) : base(candles)
        {
        }

        public static InNeckPattern IsPattern(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices)
        {
            if (index < 1) return null;

            CandleMids previousPrice = prices[index - 1];
            CandleMids currentPrice = prices[index];

            var previousMetrics = GetCandleMetrics(ref metricsCache, index - 1, prices, trendLookback, false);
            var currentMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);

            // Direction and body size check
            if (!previousMetrics.IsBearish || previousMetrics.BodySize < MinBodySize) return null;
            if (!currentMetrics.IsBullish) return null;

            // Open and close proximity
            if (Math.Abs(currentPrice.Open - previousPrice.Close) > MaxOpenDifference) return null;
            if (Math.Abs(currentPrice.Close - previousPrice.Close) > MaxCloseDifference) return null;

            // Upper wick limit
            if (currentMetrics.UpperWick > MaxWickSize) return null;

            // Downtrend check
            if (currentMetrics.GetLookbackMeanTrend(2) > TrendThreshold) return null;

            var candles = new List<int> { index - 1, index };
            return new InNeckPattern(candles);
        }
    }
}