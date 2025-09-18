using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
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
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "IdenticalThreeCrows";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bearish reversal pattern in an uptrend with three bearish candles of similar size and decreasing closes, signaling strong selling pressure and potential reversal from uptrend to downtrend.";
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
        /// Initializes a new instance of the IdenticalThreeCrowsPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public IdenticalThreeCrowsPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if an Identical Three Crows pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<IdenticalThreeCrowsPattern?> IsPatternAsync(
            int index,
            CandleMids[] prices,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;

            int i1 = index - 2;
            int i2 = index - 1;
            int i3 = index;

            var metrics1 = await GetCandleMetricsAsync(metricsCache, i1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, i2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, i3, prices, trendLookback, true);

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
                throw new InvalidOperationException("IdenticalThreeCrowsPattern must have exactly 3 candles.");

            int i1 = Candles[0], i2 = Candles[1], i3 = Candles[2];

            var metrics1 = metricsCache[i1];
            var metrics2 = metricsCache[i2];
            var metrics3 = metricsCache[i3];

            var prices1 = prices[i1];
            var prices2 = prices[i2];
            var prices3 = prices[i3];

            // Power Score: Based on body sizes, consistency, wick minimization, continuity, trend
            double bodyScore = (metrics1.BodySize + metrics2.BodySize + metrics3.BodySize) / (3 * MinBodySize);
            bodyScore = Math.Min(bodyScore, 1);

            double consistencyScore = 1 - ((Math.Max(metrics1.BodySize, Math.Max(metrics2.BodySize, metrics3.BodySize)) -
                                           Math.Min(metrics1.BodySize, Math.Min(metrics2.BodySize, metrics3.BodySize))) / MaxBodySizeDifference);
            consistencyScore = Math.Clamp(consistencyScore, 0, 1);

            double wickScore = 1 - ((metrics1.UpperWick + metrics1.LowerWick + metrics2.UpperWick + metrics2.LowerWick + metrics3.UpperWick + metrics3.LowerWick) / (6 * MaxWickSize));
            wickScore = Math.Clamp(wickScore, 0, 1);

            double continuityScore = 1 - ((Math.Abs(prices2.Open - prices1.Close) + Math.Abs(prices3.Open - prices2.Close)) / (2 * MaxOpenCloseDifference));
            continuityScore = Math.Clamp(continuityScore, 0, 1);

            double descendingScore = ((prices1.Close - prices2.Close) + (prices2.Close - prices3.Close)) / (2 * MinBodySize);
            descendingScore = Math.Min(descendingScore, 1);

            double trendStrength = metrics3.GetLookbackMeanTrend(3);
            double trendConsistency = metrics3.GetLookbackTrendConsistency(3);

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.15, wConsistency = 0.15, wWick = 0.15, wContinuity = 0.15, wDescending = 0.15, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wBody * bodyScore + wConsistency * consistencyScore + wWick * wickScore +
                                 wContinuity * continuityScore + wDescending * descendingScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wBody + wConsistency + wWick + wContinuity + wDescending + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(metrics1.BodySize - MinBodySize) / MinBodySize;
            double wickDeviation = (metrics1.UpperWick + metrics1.LowerWick) / (2 * MaxWickSize);
            double continuityDeviation = (Math.Abs(prices2.Open - prices1.Close) + Math.Abs(prices3.Open - prices2.Close)) / (2 * MaxOpenCloseDifference);
            double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
            double consistencyDeviation = Math.Abs(trendConsistency - ConsistencyThreshold) / ConsistencyThreshold;
            double matchScore = 1 - (bodyDeviation + wickDeviation + continuityDeviation + trendDeviation + consistencyDeviation) / 5;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








