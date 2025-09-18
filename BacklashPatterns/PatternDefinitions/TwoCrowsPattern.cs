using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Two Crows candlestick pattern.
    /// </summary>
    public class TwoCrowsPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size required for the first candle's real body (difference between open and close).
        /// Ensures the first candle is a significant bullish move.
        /// Strictest: 0.5 (current), Loosest: 0.3 (still shows notable bullishness).
        /// </summary>
        public static double MinBodySize { get; } = 0.1;

        /// <summary>
        /// Tolerance for the gap or overlap between candles, allowing slight deviations from a perfect gap.
        /// Strictest: 0.1 (almost no overlap), Loosest: 0.7 (allows more overlap while maintaining pattern intent).
        /// </summary>
        public static double GapTolerance { get; } = 2.0;

        /// <summary>
        /// Minimum trend strength required to confirm an uptrend before the pattern.
        /// Strictest: 0.5 (strong uptrend), Loosest: 0.1 (minimal uptrend still present).
        /// </summary>
        public static double TrendThreshold { get; } = 0.05;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "TwoCrows";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bearish reversal pattern in an uptrend with a strong bullish candle followed by two bearish candles where the second gaps up and the third closes below the second, signaling potential reversal from uptrend to downtrend.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Bearish;
        /// <summary>
        /// Gets the strength of the pattern.
        /// </summary>
        public override double Strength { get; protected set; }
        /// <summary>
        /// Gets the certainty of the pattern.
        /// </summary>
        public override double Certainty { get; protected set; }
        /// <summary>
        /// Gets the uncertainty of the pattern.
        /// </summary>
        public override double Uncertainty { get; protected set; }
        /// <summary>
        /// Initializes a new instance of the TwoCrowsPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public TwoCrowsPattern(List<int> candles) : base(candles)
        {
        }

        /*
         * Two Crows Pattern:
         * - Description: A three-candle bearish reversal pattern occurring in an uptrend. 
         *   Indicates a potential top as two bearish candles ("crows") follow a strong bullish candle.
         * - Requirements (Source: Investopedia):
         *   1. First candle: Strong bullish candle in an uptrend.
         *   2. Second candle: Bearish, gaps up from the first candle s close.
         *   3. Third candle: Bearish, opens within the second candle s range, closes near or below the first candle s close.
         * - Indication: Suggests selling pressure overcoming buying momentum, potential reversal to downtrend.
         */
        /// <summary>
        /// Determines if a Two Crows pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<TwoCrowsPattern?> IsPatternAsync(
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

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // First candle: Bullish, minimal body size
            if (!metrics1.IsBullish || metrics1.BodySize < 0.1) return null;

            // Second candle: Bearish, opens above c1 close (with tolerance), closes with flexibility
            if (!metrics2.IsBearish ||
                prices[c2].Open <= prices[c1].Close - GapTolerance ||
                prices[c2].Close <= prices[c1].Close - GapTolerance) return null;

            // Third candle: Bearish, opens below c2 open, closes below c1 open (with tolerance)
            if (!metrics3.IsBearish ||
                prices[c3].Open > prices[c2].Open + GapTolerance ||
                prices[c3].Close > prices[c1].Open + GapTolerance) return null;

            // Minimal uptrend check
            if (metrics3.GetLookbackMeanTrend(3) <= 0.05) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new TwoCrowsPattern(candles);
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
                throw new InvalidOperationException("TwoCrowsPattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on body sizes, gap, positioning, trend
            double firstBodyScore = metrics1.BodySize / MinBodySize;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double gapScore = (prices2.Open - prices1.Close) / GapTolerance;
            gapScore = Math.Min(gapScore, 1);

            double secondPositionScore = 1 - ((prices2.Close - prices1.Close) / GapTolerance);
            secondPositionScore = Math.Clamp(secondPositionScore, 0, 1);

            double thirdPositionScore = 1 - ((prices3.Close - prices1.Open) / GapTolerance);
            thirdPositionScore = Math.Clamp(thirdPositionScore, 0, 1);

            double trendStrength = metrics3.GetLookbackMeanTrend(3);

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.2, wGap = 0.2, wSecond = 0.2, wThird = 0.2, wTrend = 0.1, wVolume = 0.1;
            double powerScore = (wFirst * firstBodyScore + wGap * gapScore + wSecond * secondPositionScore +
                                 wThird * thirdPositionScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirst + wGap + wSecond + wThird + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(metrics1.BodySize - MinBodySize) / MinBodySize;
            double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
            double matchScore = 1 - (bodyDeviation + trendDeviation) / 2;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








