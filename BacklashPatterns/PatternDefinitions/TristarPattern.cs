using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents the Tristar pattern, a rare three-candle reversal pattern consisting of three Doji candles.
    /// Indicates potential reversal from downtrend to uptrend (bullish) or uptrend to downtrend (bearish).
    /// </summary>
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
        /// Maximum ratio of body size to total range for a loosened Doji (e.g., 0.15 means body = 15% of range).
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
        /// Factor applied to the first candle's range to adjust close tolerance dynamically.
        /// Strictest: 0.1 (tight range-based tolerance), Loosest: 0.3 (broader range allowance).
        /// </summary>
        public static double CloseToleranceRangeFactor { get; } = 0.2;

        /// <summary>
        /// Gets the base name for the Tristar pattern.
        /// </summary>
        public const string BaseName = "Tristar";

        /// <summary>
        /// Gets the name of the pattern, appending "_Bullish" or "_Bearish" based on direction.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => Direction == PatternDirection.Bullish
            ? "A rare bullish reversal pattern with three Doji candles where the second gaps below the first and the third closes near the first, signaling potential reversal from downtrend to uptrend."
            : "A rare bearish reversal pattern with three Doji candles where the second gaps above the first and the third closes near the first, signaling potential reversal from uptrend to downtrend.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction { get; }

        /// <summary>
        /// Gets the calculated strength of the pattern.
        /// Note: Not calculated in this implementation; reserved for future use.
        /// </summary>
        public override double Strength { get; protected set; }

        /// <summary>
        /// Gets the calculated certainty of the pattern.
        /// Note: Not calculated in this implementation; reserved for future use.
        /// </summary>
        public override double Certainty { get; protected set; }

        /// <summary>
        /// Gets the calculated uncertainty of the pattern.
        /// Note: Not calculated in this implementation; reserved for future use.
        /// </summary>
        public override double Uncertainty { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the TristarPattern class.
        /// </summary>
        /// <param name="candles">List of candle indices forming the pattern (three candles).</param>
        /// <param name="direction">The direction of the pattern.</param>
        public TristarPattern(List<int> candles, PatternDirection direction) : base(candles)
        {
            Direction = direction;
        }

        /// <summary>
        /// Identifies the Tristar Pattern, a rare three-candle reversal pattern consisting of three Doji candles.
        /// 
        /// Requirements (sourced from Investopedia and candlestickpattern.net):
        /// - Three consecutive Doji candles (small body relative to range, typically = 5-15% of total range).
        /// - Appears after a strong trend (bullish Tristar after downtrend, bearish Tristar after uptrend).
        /// - Second Doji gaps away from the first (upward in bearish, downward in bullish).
        /// - Third Doji's close is near the first Doji's close, signaling indecision and potential reversal.
        /// - Trend strength prior to pattern should be significant (e.g., downtrend for bullish, uptrend for bearish).
        /// 
        /// Indicates:
        /// - Bullish Tristar: Potential reversal from downtrend to uptrend due to exhaustion of sellers.
        /// - Bearish Tristar: Potential reversal from uptrend to downtrend due to exhaustion of buyers.
        /// </summary>
        public static async Task<TristarPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            PatternDirection direction,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            // Early exit if there aren't enough prior candles (matches original: index < 2)
            if (index < 2) return null;

            int c1 = index - 2; // First Doji
            int c2 = index - 1; // Second Doji
            int c3 = index;     // Third Doji


            // Lazy load metrics (matches original structure)
            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // Check all three are Doji using original loosened criteria (from IsLoosenedDoji)
            bool allDoji = IsLoosenedDoji(metrics1, c1, prices) &&
                           IsLoosenedDoji(metrics2, c2, prices) &&
                           IsLoosenedDoji(metrics3, c3, prices);
            if (!allDoji) return null;


            var ask1 = prices[c1];
            var ask2 = prices[c2];
            var ask3 = prices[c3];


            // Loosened gap condition (restored from original: min 0.3-point gap or body outside range)
            bool gapValid = direction == PatternDirection.Bullish
                ? (ask2.Open < ask1.Close - MinGapSize || ask2.Close < ask1.Close - MinGapSize) // Below first close
                : (ask2.Open > ask1.Close + MinGapSize || ask2.Close > ask1.Close + MinGapSize); // Above first close
            if (!gapValid) return null;

            // Loosened closes match (restored from original: within 1.5 points or 20% of first range)
            double tolerance = Math.Max(CloseToleranceBase, CloseToleranceRangeFactor * metrics1.TotalRange);
            bool closesMatch = Math.Abs(ask3.Close - ask1.Close) <= tolerance;
            if (!closesMatch) return null;

            // Loosened trend check using CandleMetrics method (restored from original:  0.3)
            bool trendValid = direction == PatternDirection.Bullish
                ? (metrics3.GetLookbackMeanTrend(3) <= -TrendThreshold) // Downtrend for bullish
                : (metrics3.GetLookbackMeanTrend(3) >= TrendThreshold);  // Uptrend for bearish
            if (!trendValid) return null;

            // Define the candle indices for the pattern (matches original)
            var candles = new List<int> { c1, c2, c3 };

            // Return the pattern instance with the specified direction
            return new TristarPattern(candles, direction);
        }

        /// <summary>
        /// Calculates the strength of the pattern using historical cache for comparison.
        /// </summary>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="avgVolume">The average volume.</param>
        /// <param name="historicalCache">The historical pattern cache.</param>
        public void CalculateStrength(
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            double avgVolume,
            HistoricalPatternCache historicalCache)
        {
            if (Candles.Count != 3)
                throw new InvalidOperationException("TristarPattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on Doji smallness, gap, close match, trend
            double dojiScore = (1 - (metrics1.BodySize / DojiBodyMax) + 1 - (metrics2.BodySize / DojiBodyMax) + 1 - (metrics3.BodySize / DojiBodyMax)) / 3;
            dojiScore = Math.Clamp(dojiScore, 0, 1);

            double rangeScore = (metrics1.TotalRange / DojiRangeMin + metrics2.TotalRange / DojiRangeMin + metrics3.TotalRange / DojiRangeMin) / 3;
            rangeScore = Math.Min(rangeScore, 1);

            double gapSize = Direction == PatternDirection.Bullish ? (prices1.Close - prices2.Close) : (prices2.Close - prices1.Close);
            double gapScore = gapSize / MinGapSize;
            gapScore = Math.Min(gapScore, 1);

            double closeMatch = 1 - (Math.Abs(prices3.Close - prices1.Close) / (CloseToleranceBase + CloseToleranceRangeFactor * metrics1.TotalRange));
            closeMatch = Math.Clamp(closeMatch, 0, 1);

            double trendStrength = Math.Abs(metrics3.GetLookbackMeanTrend(3));

            double volumeScore = 0.5; // Placeholder

            double wDoji = 0.2, wRange = 0.2, wGap = 0.2, wClose = 0.2, wTrend = 0.1, wVolume = 0.1;
            double powerScore = (wDoji * dojiScore + wRange * rangeScore + wGap * gapScore +
                                 wClose * closeMatch + wTrend * trendStrength + wVolume * volumeScore) /
                                (wDoji + wRange + wGap + wClose + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = (metrics1.BodySize + metrics2.BodySize + metrics3.BodySize) / (3 * DojiBodyMax);
            double rangeDeviation = (DojiRangeMin - (metrics1.TotalRange + metrics2.TotalRange + metrics3.TotalRange) / 3) / DojiRangeMin;
            rangeDeviation = Math.Max(rangeDeviation, 0);
            double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
            double matchScore = 1 - (bodyDeviation + rangeDeviation + trendDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
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


