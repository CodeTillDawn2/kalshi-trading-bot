using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Identifies a Short Line Candle pattern, a single candlestick with a small body and range, 
    /// indicating indecision or a potential reversal after a strong trend.
    /// 
    /// Requirements (Source: Investopedia, "Short Line Candle"):
    /// - Small body (close near open), typically less than average candle size.
    /// - Small total range (high to low), showing low volatility.
    /// - Occurs after a defined trend (bullish after downtrend, bearish after uptrend).
    /// - Indicates potential reversal or continuation depending on context.
    /// </summary>
    public class ShortLineCandlePattern : PatternDefinition
    {
        /// <summary>
        /// Maximum body size for the candle, ensuring it remains small.
        /// Loosest: 2.5 (slightly larger body); Strictest: 1.0 (very small body).
        /// </summary>
        public static double MaxBodySize { get; } = 2.0;

        /// <summary>
        /// Maximum total range (high to low) for the candle, indicating low volatility.
        /// Loosest: 3.0 (wider range); Strictest: 1.5 (tight range).
        /// </summary>
        public static double MaxRange { get; } = 2.5;

        /// <summary>
        /// Minimum trend strength threshold to confirm the prior trend direction.
        /// Loosest: 0.3 (weaker trend); Strictest: 0.7 (strong trend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.5;

        /// <summary>
        /// Minimum consistency of the prior trend direction.
        /// Loosest: 0.4 (less consistent); Strictest: 0.8 (highly consistent).
        /// </summary>
        public static double TrendConsistencyThreshold { get; } = 0.6;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ShortLineCandle";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish candle with a small body and minimal wicks, indicating weak buying momentum and potential continuation or reversal depending on context."
            : "A bearish candle with a small body and minimal wicks, indicating weak selling momentum and potential continuation or reversal depending on context.";
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
        private readonly bool IsBullish;

        /// <summary>
        /// Initializes a new instance of the ShortLineCandlePattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public ShortLineCandlePattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Determines if a Short Line Candle pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<ShortLineCandlePattern?> IsPatternAsync(
            int index,
            int trendLookback,
            bool isBullish,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices)
        {
            // Early exit if there aren t enough prior candles
            if (index < 1) return null;

            // Retrieve metrics for the current candle
            var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Check if the candle has a small body (original logic: <= 2.0)
            bool isShort = metrics.BodySize <= MaxBodySize;

            // Verify the total range is within the acceptable limit (original logic: <= 2.5)
            bool smallRange = metrics.TotalRange <= MaxRange;

            // Confirm the candle direction matches the specified trend (original logic)
            bool direction = isBullish ? metrics.IsBullish : metrics.IsBearish;

            // If any condition fails, it s not the pattern
            if (!isShort || !smallRange || !direction) return null;

            // Adjusted trend conditions from original: Use CandleMetrics methods
            bool priorTrend = isBullish
                ? (metrics.GetLookbackMeanTrend(1) <= -TrendThreshold && metrics.GetLookbackTrendConsistency(1) <= -TrendConsistencyThreshold) // Consistent downtrend
                : (metrics.GetLookbackMeanTrend(1) >= TrendThreshold && metrics.GetLookbackTrendConsistency(1) >= TrendConsistencyThreshold);   // Consistent uptrend

            // If the trend condition isn t met, return null
            if (!priorTrend) return null;

            // Define the candle indices for the pattern (single candle in this case)
            var candles = new List<int> { index };

            // Return the pattern instance with the specified direction
            return new ShortLineCandlePattern(candles, isBullish);
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
            if (Candles.Count != 1)
                throw new InvalidOperationException("ShortLineCandlePattern must have exactly 1 candle.");

            int index = Candles[0];
            var metrics = metricsCache[index];

            // Power Score: Based on smallness (body and range), trend strength
            double bodyScore = 1 - (metrics.BodySize / MaxBodySize);
            bodyScore = Math.Clamp(bodyScore, 0, 1);

            double rangeScore = 1 - (metrics.TotalRange / MaxRange);
            rangeScore = Math.Clamp(rangeScore, 0, 1);

            double trendStrength = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double trendConsistency = Math.Abs(metrics.GetLookbackTrendConsistency(1) - TrendConsistencyThreshold) / TrendConsistencyThreshold;
            trendConsistency = 1 - trendConsistency; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.25, wRange = 0.25, wTrend = 0.25, wConsistency = 0.15, wVolume = 0.1;
            double powerScore = (wBody * bodyScore + wRange * rangeScore + wTrend * trendStrength +
                                 wConsistency * trendConsistency + wVolume * volumeScore) /
                                (wBody + wRange + wTrend + wConsistency + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = metrics.BodySize / MaxBodySize;
            double rangeDeviation = metrics.TotalRange / MaxRange;
            double trendDeviation = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            double consistencyDeviation = Math.Abs(metrics.GetLookbackTrendConsistency(1) - TrendConsistencyThreshold) / TrendConsistencyThreshold;
            double matchScore = 1 - (bodyDeviation + rangeDeviation + trendDeviation + consistencyDeviation) / 4;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








