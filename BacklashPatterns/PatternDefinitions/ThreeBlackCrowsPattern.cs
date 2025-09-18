using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Three Black Crows pattern.
    /// - A bearish reversal pattern with three consecutive bearish candles, each opening near the previous close and closing lower, with small wicks, signaling a strong downtrend after an uptrend.
    /// Source: Investopedia (https://www.investopedia.com/terms/t/three_black_crows.asp)
    /// </summary>
    public class ThreeBlackCrowsPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for each candle. Ensures strong bearish momentum.
        /// Strictest: 2.0 (very strong), Loosest: 0.5 (minimal momentum).
        /// </summary>
        public static double MinBodySize { get; } = 1.5;

        /// <summary>
        /// Maximum wick size for each candle. Ensures small wicks for strong candles.
        /// Strictest: 0.5 (tiny wicks), Loosest: 2.0 (larger wicks allowed).
        /// </summary>
        public static double MaxWickSize { get; } = 1.5;

        /// <summary>
        /// Maximum difference between a candle s open and the previous close. Ensures continuity.
        /// Strictest: 0.1 (very tight), Loosest: 1.0 (loose continuity).
        /// </summary>
        public static double MaxOpenCloseDiff { get; } = 0.5;

        /// <summary>
        /// Threshold for confirming a prior uptrend. Positive value indicates uptrend.
        /// Strictest: 0.5 (strong uptrend), Loosest: 0.1 (weak uptrend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ThreeBlackCrows";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bearish reversal pattern in an uptrend with three consecutive bearish candles of decreasing size, each closing lower than the previous. Signals strong selling pressure and potential reversal from uptrend to downtrend.";
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
        /// Initializes a new instance of the ThreeBlackCrowsPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public ThreeBlackCrowsPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Three Black Crows pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<ThreeBlackCrowsPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2 || index >= prices.Length) return null;

            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // All bearish
            if (!metrics1.IsBearish || !metrics2.IsBearish || !metrics3.IsBearish) return null;

            // Significant bodies
            if (metrics1.BodySize < MinBodySize || metrics2.BodySize < MinBodySize || metrics3.BodySize < MinBodySize) return null;

            // Descending closes
            if (prices[c2].Close >= prices[c1].Close || prices[c3].Close >= prices[c2].Close) return null;

            // Opens near previous close
            if (Math.Abs(prices[c2].Open - prices[c1].Close) > MaxOpenCloseDiff ||
                Math.Abs(prices[c3].Open - prices[c2].Close) > MaxOpenCloseDiff) return null;

            // Small wicks
            if (metrics1.UpperWick > MaxWickSize || metrics2.UpperWick > MaxWickSize || metrics3.UpperWick > MaxWickSize) return null;
            if (metrics1.LowerWick > MaxWickSize || metrics2.LowerWick > MaxWickSize || metrics3.LowerWick > MaxWickSize) return null;

            // Uptrend check using CandleMetrics
            if (metrics3.GetLookbackMeanTrend(3) <= TrendThreshold) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new ThreeBlackCrowsPattern(candles);
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
                throw new InvalidOperationException("ThreeBlackCrowsPattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on body sizes, wick minimization, continuity, trend
            double bodyScore = (metrics1.BodySize + metrics2.BodySize + metrics3.BodySize) / (3 * MinBodySize);
            bodyScore = Math.Min(bodyScore, 1);

            double wickScore = 1 - ((metrics1.UpperWick + metrics1.LowerWick + metrics2.UpperWick + metrics2.LowerWick + metrics3.UpperWick + metrics3.LowerWick) / (6 * MaxWickSize));
            wickScore = Math.Clamp(wickScore, 0, 1);

            double continuityScore = 1 - ((Math.Abs(prices2.Open - prices1.Close) + Math.Abs(prices3.Open - prices2.Close)) / (2 * MaxOpenCloseDiff));
            continuityScore = Math.Clamp(continuityScore, 0, 1);

            double descendingScore = ((prices1.Close - prices2.Close) + (prices2.Close - prices3.Close)) / (2 * MinBodySize);
            descendingScore = Math.Min(descendingScore, 1);

            double trendStrength = metrics3.GetLookbackMeanTrend(3);

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.2, wWick = 0.2, wContinuity = 0.2, wDescending = 0.2, wTrend = 0.1, wVolume = 0.1;
            double powerScore = (wBody * bodyScore + wWick * wickScore + wContinuity * continuityScore +
                                 wDescending * descendingScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wBody + wWick + wContinuity + wDescending + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(metrics1.BodySize - MinBodySize) / MinBodySize;
            double wickDeviation = (metrics1.UpperWick + metrics1.LowerWick) / (2 * MaxWickSize);
            double continuityDeviation = (Math.Abs(prices2.Open - prices1.Close) + Math.Abs(prices3.Open - prices2.Close)) / (2 * MaxOpenCloseDiff);
            double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
            double matchScore = 1 - (bodyDeviation + wickDeviation + continuityDeviation + trendDeviation) / 4;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








