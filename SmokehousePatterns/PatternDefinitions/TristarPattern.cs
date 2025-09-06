using SmokehouseDTOs;
using SmokehousePatterns.Helpers;
using static SmokehousePatterns.Helpers.PatternUtils;

namespace SmokehousePatterns.PatternDefinitions
{
    public class TristarPattern : PatternDefinition
    {
        /// <summary>
        /// Maximum body size for a candle to be considered a loosened Doji.
        /// Strictest: 0.5 (tight Doji), Loosest: 2.0 (allows larger bodies while retaining indecision).
        /// </summary>
        public static double DojiBodyMax { get; } = 1.5;

        /// <summary>
        /// Minimum total range (high-low) for a loosened Doji to ensure some price movement.
        /// Strictest: 1.0 (significant range), Loosest: 0.3 (minimal movement still qualifies).
        /// </summary>
        public static double DojiRangeMin { get; } = 0.5;

        /// <summary>
        /// Maximum ratio of body size to total range for a loosened Doji (e.g., 0.15 means body ≤ 15% of range).
        /// Strictest: 0.05 (classic Doji), Loosest: 0.25 (allows larger bodies relative to range).
        /// </summary>
        public static double DojiBodyToRangeRatio { get; } = 0.15;

        /// <summary>
        /// Minimum trend strength prior to the pattern (positive for bearish, negative for bullish).
        /// Strictest: 0.5 (strong trend), Loosest: 0.1 (minimal trend still present).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Minimum gap size between the first and second Doji candles.
        /// Strictest: 0.5 (clear gap), Loosest: 0.1 (minimal separation still visible).
        /// </summary>
        public static double MinGapSize { get; } = 0.3;

        /// <summary>
        /// Base tolerance for matching the closes of the first and third Doji candles.
        /// Strictest: 0.5 (tight match), Loosest: 2.0 (allows broader indecision range).
        /// </summary>
        public static double CloseToleranceBase { get; } = 1.5;

        /// <summary>
        /// Factor applied to the first candle’s range to adjust close tolerance dynamically.
        /// Strictest: 0.1 (tight range-based tolerance), Loosest: 0.3 (broader range allowance).
        /// </summary>
        public static double CloseToleranceRangeFactor { get; } = 0.2;
        public const string BaseName = "Tristar";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }
        private readonly bool IsBullish;

        public TristarPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies the Tristar Pattern, a rare three-candle reversal pattern consisting of three Doji candles.
        /// 
        /// Requirements (sourced from Investopedia and candlestickpattern.net):
        /// - Three consecutive Doji candles (small body relative to range, typically ≤ 5-15% of total range).
        /// - Appears after a strong trend (bullish Tristar after downtrend, bearish Tristar after uptrend).
        /// - Second Doji gaps away from the first (upward in bearish, downward in bullish).
        /// - Third Doji's close is near the first Doji's close, signaling indecision and potential reversal.
        /// - Trend strength prior to pattern should be significant (e.g., downtrend for bullish, uptrend for bearish).
        /// 
        /// Indicates:
        /// - Bullish Tristar: Potential reversal from downtrend to uptrend due to exhaustion of sellers.
        /// - Bearish Tristar: Potential reversal from uptrend to downtrend due to exhaustion of buyers.
        /// </summary>
        public static TristarPattern IsPattern(
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            // Early exit if there aren't enough prior candles (matches original: index < 2)
            if (index < 2) return null;

            int c1 = index - 2; // First Doji
            int c2 = index - 1; // Second Doji
            int c3 = index;     // Third Doji


            // Lazy load metrics (matches original structure)
            var metrics1 = GetCandleMetrics(ref metricsCache, c1, prices, trendLookback, false);
            var metrics2 = GetCandleMetrics(ref metricsCache, c2, prices, trendLookback, false);
            var metrics3 = GetCandleMetrics(ref metricsCache, c3, prices, trendLookback, true);

            // Check all three are Doji using original loosened criteria (from IsLoosenedDoji)
            bool allDoji = IsLoosenedDoji(metrics1, c1, prices) &&
                           IsLoosenedDoji(metrics2, c2, prices) &&
                           IsLoosenedDoji(metrics3, c3, prices);
            if (!allDoji) return null;


            var ask1 = prices[c1];
            var ask2 = prices[c2];
            var ask3 = prices[c3];

            if (ask3.Timestamp == new DateTime(2024, 7, 20, 12, 35, 0, DateTimeKind.Utc) &&
                isBullish == false) // Bearish BeltHold
            {
                // BREAKPOINT HERE: Set your breakpoint on the line below
                Console.WriteLine($"Breakpoint hit: Checking BeltHold_Bearish at {ask3.Timestamp}");
            }

            // Loosened gap condition (restored from original: min 0.3-point gap or body outside range)
            bool gapValid = isBullish
                ? (ask2.Open < ask1.Close - MinGapSize || ask2.Close < ask1.Close - MinGapSize) // Below first close
                : (ask2.Open > ask1.Close + MinGapSize || ask2.Close > ask1.Close + MinGapSize); // Above first close
            if (!gapValid) return null;

            // Loosened closes match (restored from original: within 1.5 points or 20% of first range)
            double tolerance = Math.Max(CloseToleranceBase, CloseToleranceRangeFactor * metrics1.TotalRange);
            bool closesMatch = Math.Abs(ask3.Close - ask1.Close) <= tolerance;
            if (!closesMatch) return null;

            // Loosened trend check using CandleMetrics method (restored from original: ±0.3)
            bool trendValid = isBullish
                ? (metrics3.GetLookbackMeanTrend(3) <= -TrendThreshold) // Downtrend for bullish
                : (metrics3.GetLookbackMeanTrend(3) >= TrendThreshold);  // Uptrend for bearish
            if (!trendValid) return null;

            // Define the candle indices for the pattern (matches original)
            var candles = new List<int> { c1, c2, c3 };

            // Return the pattern instance with the specified direction
            return new TristarPattern(candles, isBullish);
        }

        // Helper method restored from original IsLoosenedDoji, adapted to current structure
        private static bool IsLoosenedDoji(CandleMetrics metricsCache, int index, CandleMids[] prices)
        {
            return metricsCache.TotalRange >= DojiRangeMin &&           // Lowered min range (was 0.5)
                   metricsCache.BodySize <= DojiBodyMax &&             // Larger body allowed (was 1.5)
                   metricsCache.BodySize <= DojiBodyToRangeRatio * metricsCache.TotalRange; // 15% of range (was 0.15)
        }
    }
}