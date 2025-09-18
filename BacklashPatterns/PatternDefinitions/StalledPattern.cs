using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Identifies a Stalled Pattern, a three-candle reversal pattern signaling a weakening uptrend,
    /// often indicating a potential top or pause in bullish momentum.
    /// 
    /// Requirements (Source: BabyPips, "Stalled Pattern"):
    /// - First candle: Strong bullish candle with a large body.
    /// - Second candle: Bullish candle with a smaller body, closing above the first.
    /// - Third candle: Small body (bullish or bearish), closing below the second but near the first,
    ///   showing momentum stalling.
    /// - Occurs in an established uptrend.
    /// - Suggests a potential reversal or consolidation.
    /// </summary>
    public class StalledPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first candle to qualify as a strong bullish candle.
        /// Strictest: 2.0 (strong trend); Loosest: 1.0 (minimal trend strength).
        /// </summary>
        public static double MinBodySize { get; set; } = 1.5;

        /// <summary>
        /// Maximum body size for the second and third candles to indicate stalling momentum.
        /// Strictest: 1.0 (very small body); Loosest: 2.0 (allows broader stalling).
        /// </summary>
        public static double SmallBodyMax { get; set; } = 1.5;

        /// <summary>
        /// Minimum mean trend value to confirm an established uptrend.
        /// Strictest: 0.5 (strong uptrend); Loosest: 0.1 (weak uptrend).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;

        /// <summary>
        /// Minimum trend consistency to ensure a reliable uptrend context.
        /// Strictest: 0.75 (highly consistent); Loosest: 0.25 (minimally consistent).
        /// </summary>
        public static double ConsistencyThreshold { get; set; } = 0.5;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Stalled";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bearish reversal pattern in an uptrend with three candles where the first two are bullish with ascending closes, but the third opens higher and closes lower, signaling weakening momentum.";
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
        /// Initializes a new instance of the StalledPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public StalledPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Stalled pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<StalledPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            // Early exit if there aren t enough prior candles for a three-candle pattern
            if (index < 2) return null;

            // Define candle indices
            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            // Check array bounds
            if (c1 < 0 || c3 >= prices.Length) return null;

            // Lazy load metrics for all three candles
            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // First candle: Must be bullish with a significant body
            if (!metrics1.IsBullish || metrics1.BodySize < MinBodySize) return null;

            // Second candle: Must be bullish, have a small body, and close above the first candle
            if (!metrics2.IsBullish || metrics2.BodySize > SmallBodyMax ||
                prices[c2].Close <= prices[c1].Close) return null;

            // Third candle: Must have a small body and close below the second but above or near the first
            if (metrics3.BodySize > SmallBodyMax || prices[c3].Close >= prices[c2].Close ||
                prices[c3].Close < prices[c1].Close - 0.5) return null;

            // Ensure the pattern occurs in an uptrend
            if (metrics3.GetLookbackMeanTrend(3) <= TrendThreshold || metrics3.GetLookbackTrendConsistency(3) < ConsistencyThreshold) return null;

            // Define the candle indices for the pattern (three candles)
            var candles = new List<int> { c1, c2, c3 };

            // Return the pattern instance if all conditions are satisfied
            return new StalledPattern(candles);
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
                throw new InvalidOperationException("StalledPattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on body sizes, stalling progression, trend
            double firstBodyScore = metrics1.BodySize / MinBodySize;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double secondBodyScore = 1 - (metrics2.BodySize / SmallBodyMax);
            secondBodyScore = Math.Clamp(secondBodyScore, 0, 1);

            double thirdBodyScore = 1 - (metrics3.BodySize / SmallBodyMax);
            thirdBodyScore = Math.Clamp(thirdBodyScore, 0, 1);

            double progressionScore = (prices2.Close - prices1.Close) / MinBodySize;
            progressionScore = Math.Min(progressionScore, 1);

            double stallingScore = 1 - ((prices2.Close - prices3.Close) / MinBodySize);
            stallingScore = Math.Clamp(stallingScore, 0, 1);

            double trendStrength = metrics3.GetLookbackMeanTrend(3);
            double trendConsistency = metrics3.GetLookbackTrendConsistency(3);

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.15, wSecond = 0.15, wThird = 0.15, wProgression = 0.15, wStalling = 0.15, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wFirst * firstBodyScore + wSecond * secondBodyScore + wThird * thirdBodyScore +
                                 wProgression * progressionScore + wStalling * stallingScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirst + wSecond + wThird + wProgression + wStalling + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double firstDeviation = Math.Abs(metrics1.BodySize - MinBodySize) / MinBodySize;
            double secondDeviation = metrics2.BodySize / SmallBodyMax;
            double thirdDeviation = metrics3.BodySize / SmallBodyMax;
            double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
            double consistencyDeviation = Math.Abs(trendConsistency - ConsistencyThreshold) / ConsistencyThreshold;
            double matchScore = 1 - (firstDeviation + secondDeviation + thirdDeviation + trendDeviation + consistencyDeviation) / 5;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








