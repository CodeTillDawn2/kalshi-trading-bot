using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents an Upside Gap Two Crows candlestick pattern.
    /// </summary>
    public class UpsideGapTwoCrowsPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size of the first candle as a percentage of its total range.
        /// Purpose: Ensures the first candle has a significant bullish move to establish the uptrend context.
        /// Strictest: 0.5 (requires a strong body), Loosest: 0.1 (minimal body to still be considered bullish).
        /// </summary>
        public static double FirstCandleMinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Threshold for confirming an uptrend based on lookback mean trend.
        /// Purpose: Validates that the pattern occurs in an uptrend, critical for reversal significance.
        /// Strictest: 0.5 (strong uptrend), Loosest: 0.1 (barely trending up).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;

        /// <summary>
        /// Tolerance for overlap in the gap between the first and second candles.
        /// Purpose: Allows some flexibility in the "gap up" definition while maintaining separation.
        /// Strictest: 0 (no overlap), Loosest: 1.0 (significant overlap still considered a gap).
        /// </summary>
        public static double GapOverlapTolerance { get; set; } = 0.5;

        /// <summary>
        /// Tolerance for how far the third candle s close can deviate above the first candle s close.
        /// Purpose: Ensures the third candle closes near the first to signal weakening momentum.
        /// Strictest: 0 (exact match), Loosest: 2.0 (allows wider deviation while still bearish).
        /// </summary>
        public static double ThirdCandleCloseTolerance { get; set; } = 1.0;

        /// <summary>
        /// Represents the Upside Gap Two Crows pattern, a three-candle bearish reversal pattern.
        /// Occurs in an uptrend with a bullish candle, followed by two bearish candles ("crows") 
        /// where the second gaps up from the first and the third engulfs part of the second, 
        /// closing below it but not too far from the first.
        /// Indicates a potential reversal from bullish to bearish momentum.
        /// Source: https://www.investopedia.com/terms/u/upside-gap-two-crows.asp
        /// </summary>
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "UpsideGapTwoCrows";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bearish reversal pattern in an uptrend with a strong bullish candle followed by two bearish candles where the second gaps up from the first and the third closes below the second but near the first, signaling potential reversal from uptrend to downtrend.";
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
        /// Initializes a new instance of the UpsideGapTwoCrowsPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public UpsideGapTwoCrowsPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if an Upside Gap Two Crows pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<UpsideGapTwoCrowsPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            int c1 = index - 2; // First candle (bullish)
            int c2 = index - 1; // Second candle (first crow)
            int c3 = index;     // Third candle (second crow)

            if (c1 < 0 || c3 >= prices.Length) return null;

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // Uptrend check
            if (metrics3.GetLookbackMeanTrend(3) <= TrendThreshold) return null;

            // First candle: Bullish with sufficient body
            if (metrics1.BodySize < FirstCandleMinBodySize || !metrics1.IsBullish) return null;

            // Second candle: Bearish, gaps up from first (with tolerance), closes above first open
            if (!metrics2.IsBearish ||
                prices[c2].Open < prices[c1].Close - GapOverlapTolerance ||
                prices[c2].Close <= prices[c1].Open) return null;

            // Third candle: Bearish, opens between second s range, closes below second, near first close
            if (!metrics3.IsBearish ||
                prices[c3].Open > prices[c2].Open + GapOverlapTolerance ||
                prices[c3].Open < prices[c2].Close - GapOverlapTolerance ||
                prices[c3].Close >= prices[c2].Close ||
                prices[c3].Close > prices[c1].Close + ThirdCandleCloseTolerance) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new UpsideGapTwoCrowsPattern(candles);
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
                throw new InvalidOperationException("UpsideGapTwoCrowsPattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on body size, gap, positioning, close proximity, trend
            double firstBodyScore = metrics1.BodySize / FirstCandleMinBodySize;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double gapScore = (prices2.Open - prices1.Close) / GapOverlapTolerance;
            gapScore = Math.Min(gapScore, 1);

            double secondPositionScore = (prices2.Close - prices1.Open) / metrics1.BodySize;
            secondPositionScore = Math.Min(secondPositionScore, 1);

            double thirdPositionScore = 1 - ((prices3.Close - prices1.Close) / ThirdCandleCloseTolerance);
            thirdPositionScore = Math.Clamp(thirdPositionScore, 0, 1);

            double engulfScore = (prices2.Close - prices3.Close) / metrics2.BodySize;
            engulfScore = Math.Min(engulfScore, 1);

            double trendStrength = metrics3.GetLookbackMeanTrend(3);

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.15, wGap = 0.15, wSecond = 0.15, wThird = 0.15, wEngulf = 0.2, wTrend = 0.1, wVolume = 0.05;
            double powerScore = (wFirst * firstBodyScore + wGap * gapScore + wSecond * secondPositionScore +
                                 wThird * thirdPositionScore + wEngulf * engulfScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirst + wGap + wSecond + wThird + wEngulf + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(metrics1.BodySize - FirstCandleMinBodySize) / FirstCandleMinBodySize;
            double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
            double matchScore = 1 - (bodyDeviation + trendDeviation) / 2;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








