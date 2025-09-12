using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Three Inside pattern (Up or Down).
    /// - Three Inside Up: A bullish reversal pattern with a long bearish candle, followed by a smaller bullish candle contained within its body, and a third bullish candle closing above the first candle's open, indicating a potential uptrend after a downtrend.
    /// - Three Inside Down: A bearish reversal pattern with a long bullish candle, followed by a smaller bearish candle contained within its body, and a third bearish candle closing below the first candle's open, signaling a potential downtrend after an uptrend.
    /// Source: Investopedia (https://www.investopedia.com/terms/t/three-inside-up.asp, https://www.investopedia.com/terms/t/three-inside-down.asp)
    /// </summary>
    public class ThreeInsidePattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first candle. Ensures the initial move is significant.
        /// Strictest: 2.0 (strong trend), Loosest: 0.5 (minimal trend).
        /// </summary>
        public static double MinBodySizeFirst { get; } = 1.0;

        /// <summary>
        /// Maximum body size for the second candle. Ensures it’s smaller and contained.
        /// Strictest: 1.0 (tight containment), Loosest: 3.0 (allows larger retracement).
        /// </summary>
        public static double MaxBodySizeSecond { get; } = 2.0;

        /// <summary>
        /// Minimum body size for the third candle. Confirms the reversal strength.
        /// Strictest: 1.0 (strong confirmation), Loosest: 0.3 (minimal confirmation).
        /// </summary>
        public static double MinBodySizeThird { get; } = 0.5;

        /// <summary>
        /// Threshold for determining prior trend strength. Positive for uptrend, negative for downtrend.
        /// Strictest: 0.5 (clear trend), Loosest: 0.1 (weak trend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Buffer for containment of the second candle within the first. Allows flexibility in containment.
        /// Strictest: 0.0 (exact containment), Loosest: 1.0 (significant overlap).
        /// </summary>
        public static double ContainmentBuffer { get; } = 0.5;

        /// <summary>
        /// Minimum number of lookback candles to assess the trend.
        /// Strictest: 5 (longer context), Loosest: 2 (short context).
        /// </summary>
        public static int MinLookbackCount { get; } = 3;
        public const string BaseName = "ThreeInside";
        public override string Name => BaseName;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        public ThreeInsidePattern(List<int> candles) : base(candles)
        {
        }

        public static ThreeInsidePattern IsPattern(
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null; // Need 3 candles

            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            var metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

            if (isBullish) // Three Inside Up
            {
                double body1 = metrics1.BodySize;
                double body2 = metrics2.BodySize;
                double body3 = metrics3.BodySize;

                // First candle: Bearish, significant body
                if (!metrics1.IsBearish || body1 < MinBodySizeFirst) return null;

                // Second candle: Bullish, smaller, contained within first’s body (with buffer)
                if (!metrics2.IsBullish || body2 > MaxBodySizeSecond) return null;
                bool c2Inside = prices[c2].Open >= Math.Min(prices[c1].Open, prices[c1].Close) - ContainmentBuffer &&
                                prices[c2].Open <= Math.Max(prices[c1].Open, prices[c1].Close) + ContainmentBuffer &&
                                prices[c2].Close >= Math.Min(prices[c1].Open, prices[c1].Close) - ContainmentBuffer &&
                                prices[c2].Close <= Math.Max(prices[c1].Open, prices[c1].Close) + ContainmentBuffer;
                if (!c2Inside) return null;

                // Third candle: Bullish, closes above second’s high (relaxed from first’s open)
                if (!metrics3.IsBullish || body3 < MinBodySizeThird) return null;
                if (prices[c3].Close <= prices[c2].High) return null;

                // Downtrend check using CandleMetrics
                int lookbackCount = Math.Min(10, index - 2);
                if (lookbackCount < MinLookbackCount) return null;
                if (metrics3.GetLookbackAverageTrend(1) > -TrendThreshold ||
                    metrics3.GetLookbackTrendStability(1) < 0.4) return null;
            }
            else // Three Inside Down
            {
                double body1 = metrics1.BodySize;
                double body2 = metrics2.BodySize;
                double body3 = metrics3.BodySize;

                // First candle: Bullish, significant body
                if (!metrics1.IsBullish || body1 < MinBodySizeFirst) return null;

                // Second candle: Bearish, smaller, contained within first’s body (with buffer)
                if (!metrics2.IsBearish || body2 > MaxBodySizeSecond) return null;
                bool c2Inside = prices[c2].Open >= Math.Min(prices[c1].Open, prices[c1].Close) - ContainmentBuffer &&
                                prices[c2].Open <= Math.Max(prices[c1].Open, prices[c1].Close) + ContainmentBuffer &&
                                prices[c2].Close >= Math.Min(prices[c1].Open, prices[c1].Close) - ContainmentBuffer &&
                                prices[c2].Close <= Math.Max(prices[c1].Open, prices[c1].Close) + ContainmentBuffer;
                if (!c2Inside) return null;

                // Third candle: Bearish, closes below second’s low (relaxed from first’s open)
                if (!metrics3.IsBearish || body3 < MinBodySizeThird) return null;
                if (prices[c3].Close >= prices[c2].Low) return null;

                // Uptrend check using CandleMetrics
                int lookbackCount = Math.Min(10, index - 2);
                if (lookbackCount < MinLookbackCount) return null;
                if (metrics3.GetLookbackAverageTrend(1) < TrendThreshold ||
                    metrics3.GetLookbackTrendStability(1) < 0.4) return null;
            }

            var candles = new List<int> { c1, c2, c3 };
            return new ThreeInsidePattern(candles);
        }
    }
}






