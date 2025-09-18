using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The Inverted Hammer is a single-candle bullish reversal pattern after a downtrend.
    /// - Small body, long upper wick, minimal lower wick, bullish direction.
    /// - Indicates potential buying pressure and reversal to the upside.
    /// Source: https://www.investopedia.com/terms/i/invertedhammer.asp
    /// </summary>
    public class InvertedHammerPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range of the candle.
        /// Purpose: Ensures sufficient volatility for pattern significance.
        /// Strictest: 3.0 (current default, notable range).
        /// Loosest: 1.5 (smaller range still valid per Investopedia).
        /// </summary>
        public static double MinRange { get; set; } = 3.0;

        /// <summary>
        /// Maximum body size as percentage of total range.
        /// Purpose: Ensures small body relative to range.
        /// Strictest: 0.25 (current default, small body).
        /// Loosest: 0.4 (larger body still acceptable per trading forums).
        /// </summary>
        public static double BodyRangeRatio { get; set; } = 0.25;

        /// <summary>
        /// Minimum upper wick size as percentage of total range.
        /// Purpose: Ensures long upper wick for pattern shape.
        /// Strictest: 0.5 (current default, prominent wick).
        /// Loosest: 0.3 (shorter wick still valid).
        /// </summary>
        public static double WickRangeRatio { get; set; } = 0.5;

        /// <summary>
        /// Minimum ratio of upper wick to body size.
        /// Purpose: Ensures upper wick dominates body.
        /// Strictest: 2.0 (current default, strong wick).
        /// Loosest: 1.5 (relaxed but still prominent).
        /// </summary>
        public static double WickBodyRatio { get; set; } = 2.0;

        /// <summary>
        /// Maximum lower wick size as percentage of total range.
        /// Purpose: Limits lower wick to maintain pattern shape.
        /// Strictest: 0.1 (very minimal wick).
        /// Loosest: 0.3 (slightly larger wick per Investopedia).
        /// </summary>
        public static double LowerWickMaxRatio { get; set; } = 0.2;

        /// <summary>
        /// Maximum mean trend for downtrend confirmation.
        /// Purpose: Confirms preceding downtrend.
        /// Strictest: -0.5 (strong downtrend).
        /// Loosest: -0.1 (weak downtrend still valid).
        /// </summary>
        public static double TrendThreshold { get; set; } = -0.3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "InvertedHammer";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bullish reversal pattern in a downtrend with a small body, long upper wick, and minimal lower wick. The long upper wick shows rejection of higher prices, signaling potential reversal from downtrend to uptrend.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Bullish;
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
        /// Initializes a new instance of the InvertedHammerPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public InvertedHammerPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if an Inverted Hammer pattern exists at the specified index.
        /// </summary>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="index">The index of the candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<InvertedHammerPattern?> IsPatternAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            CandleMids[] prices)
        {
            if (index < 1) return null;

            CandleMetrics metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Downtrend check
            if (metrics.GetLookbackMeanTrend(1) > TrendThreshold) return null;

            // Range check
            if (metrics.TotalRange < MinRange) return null;

            // Shape checks
            bool hasSmallBody = metrics.BodySize <= BodyRangeRatio * metrics.TotalRange;
            bool hasLongUpperWick = metrics.UpperWick >= WickRangeRatio * metrics.TotalRange &&
                                    metrics.UpperWick >= WickBodyRatio * metrics.BodySize;
            bool hasMinimalLowerWick = metrics.LowerWick <= LowerWickMaxRatio * metrics.TotalRange;
            bool isBullish = metrics.IsBullish;

            if (!hasSmallBody || !hasLongUpperWick || !hasMinimalLowerWick || !isBullish) return null;

            var candles = new List<int> { index };
            return new InvertedHammerPattern(candles);
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
                throw new InvalidOperationException("InvertedHammerPattern must have exactly 1 candle.");

            int index = Candles[0];
            var metrics = metricsCache[index];

            // Power Score: Based on range, body smallness, wick strength, trend
            double rangeScore = metrics.TotalRange / MinRange;
            rangeScore = Math.Min(rangeScore, 1);

            double bodyScore = 1 - (metrics.BodySize / (BodyRangeRatio * metrics.TotalRange));
            bodyScore = Math.Clamp(bodyScore, 0, 1);

            double wickRangeScore = metrics.UpperWick / (WickRangeRatio * metrics.TotalRange);
            wickRangeScore = Math.Min(wickRangeScore, 1);

            double wickBodyScore = metrics.UpperWick / (WickBodyRatio * metrics.BodySize);
            wickBodyScore = Math.Min(wickBodyScore, 1);

            double lowerWickScore = 1 - (metrics.LowerWick / (LowerWickMaxRatio * metrics.TotalRange));
            lowerWickScore = Math.Clamp(lowerWickScore, 0, 1);

            double trendScore = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendScore = 1 - trendScore; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wRange = 0.15, wBody = 0.15, wWickRange = 0.2, wWickBody = 0.2, wLowerWick = 0.15, wTrend = 0.1, wVolume = 0.05;
            double powerScore = (wRange * rangeScore + wBody * bodyScore + wWickRange * wickRangeScore +
                                 wWickBody * wickBodyScore + wLowerWick * lowerWickScore + wTrend * trendScore + wVolume * volumeScore) /
                                (wRange + wBody + wWickRange + wWickBody + wLowerWick + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double rangeDeviation = Math.Abs(metrics.TotalRange - MinRange) / MinRange;
            double bodyDeviation = Math.Abs(metrics.BodySize - BodyRangeRatio * metrics.TotalRange) / (BodyRangeRatio * metrics.TotalRange);
            double wickDeviation = Math.Abs(metrics.UpperWick - WickRangeRatio * metrics.TotalRange) / (WickRangeRatio * metrics.TotalRange);
            double trendDeviation = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (rangeDeviation + bodyDeviation + wickDeviation + trendDeviation) / 4;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








