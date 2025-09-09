using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The Homing Pigeon is a two-candle bullish reversal pattern occurring after a downtrend.
    /// - Two bearish candles where the second is smaller and contained within the first’s range.
    /// - Indicates slowing bearish momentum and potential reversal to the upside.
    /// Source: https://www.babypips.com/learn/forex/homing-pigeon
    /// </summary>
    public class HomingPigeonPattern : PatternDefinition
    {
        /// <summary>
        /// Maximum increase in the second candle’s open relative to the first’s open.
        /// Strictest: 0.2 (tight range); Loosest: 1.0 (allows more separation).
        /// </summary>
        public static double MaxOpenBuffer { get; set; } = 0.5;

        /// <summary>
        /// Maximum decrease in the second candle’s close relative to the first’s close.
        /// Strictest: 0.2 (tight range); Loosest: 1.0 (allows more separation).
        /// </summary>
        public static double MaxCloseBuffer { get; set; } = 0.5;

        /// <summary>
        /// Base maximum difference between the lows of the two candles.
        /// Strictest: 1.0 (very close lows); Loosest: 5.0 (wider range).
        /// </summary>
        public static double BaseMaxLowDifference { get; set; } = 3.0;

        /// <summary>
        /// Maximum low difference as a percentage of the first candle’s range.
        /// Strictest: 0.3 (tight range); Loosest: 0.7 (wider range).
        /// </summary>
        public static double LowRangeRatio { get; set; } = 0.5;

        /// <summary>
        /// Maximum mean trend value to confirm a preceding downtrend.
        /// Strictest: -0.5 (strong downtrend); Loosest: -0.1 (weak downtrend).
        /// </summary>
        public static double TrendThreshold { get; set; } = -0.3;
        public const string BaseName = "HomingPigeon";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public HomingPigeonPattern(List<int> candles) : base(candles)
        {
        }

        public static HomingPigeonPattern IsPattern(
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

            // Direction check
            if (!previousMetrics.IsBearish || !currentMetrics.IsBearish) return null;

            // Positional relationships
            if (currentPrice.Open > previousPrice.Open + MaxOpenBuffer ||
                currentPrice.Close < previousPrice.Close - MaxCloseBuffer ||
                currentPrice.Close > previousPrice.Open) return null;

            // Second candle smaller
            if (currentMetrics.BodySize >= previousMetrics.BodySize) return null;

            // Low difference check
            double maxLowDifference = Math.Max(BaseMaxLowDifference, LowRangeRatio * previousMetrics.TotalRange);
            if (Math.Abs(currentPrice.Low - previousPrice.Low) > maxLowDifference) return null;

            // Downtrend check
            if (currentMetrics.GetLookbackMeanTrend(2) > TrendThreshold) return null;

            var candles = new List<int> { index - 1, index };
            return new HomingPigeonPattern(candles);
        }
    }
}
