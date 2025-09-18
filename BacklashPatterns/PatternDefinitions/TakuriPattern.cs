using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Takuri candlestick pattern.
    /// </summary>
    public class TakuriPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum total range of the candle to ensure sufficient volatility.
        /// Strictest: 3.0 (high volatility); Loosest: 1.0 (minimal range).
        /// </summary>
        public static double MinRange { get; set; } = 2.0;

        /// <summary>
        /// Maximum ratio of body size to total range for a small body.
        /// Strictest: 0.1 (tiny body); Loosest: 0.3 (broader small body).
        /// </summary>
        public static double BodyRangeRatio { get; set; } = 0.2;

        /// <summary>
        /// Minimum ratio of lower wick to total range for a significant wick.
        /// Strictest: 0.6 (very long wick); Loosest: 0.3 (moderate wick).
        /// </summary>
        public static double WickRangeRatio { get; set; } = 0.4;

        /// <summary>
        /// Minimum ratio of lower wick to body size to emphasize buying pressure.
        /// Strictest: 3.0 (strong pressure); Loosest: 1.5 (minimal pressure).
        /// </summary>
        public static double WickBodyRatio { get; set; } = 2.0;

        /// <summary>
        /// Maximum trend value to confirm a downtrend context.
        /// Strictest: -0.5 (strong downtrend); Loosest: -0.1 (weak downtrend).
        /// </summary>
        public static double TrendThreshold { get; set; } = -0.3;

        /// <summary>
        /// Minimum trend consistency to ensure a reliable downtrend.
        /// Strictest: 0.6 (highly consistent); Loosest: 0.3 (minimally consistent).
        /// </summary>
        public static double MinTrendConsistency { get; set; } = 0.4;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Takuri";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bullish reversal pattern in a downtrend with a long bullish candle that opens below the previous bearish candle's low and closes above its high, signaling strong reversal.";
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
        /// Initializes a new instance of the TakuriPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public TakuriPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies a Takuri pattern, a single-candle bullish reversal pattern.
        /// Occurs in a downtrend; features a small body near the top, a long lower wick (at least twice the body),
        /// and little to no upper wick. Indicates strong buying pressure after a decline, suggesting a potential reversal.
        /// Source: https://www.babypips.com/learn/forex/takuri-line-candlestick-pattern
        /// </summary>
        /// <summary>
        /// Determines if a Takuri pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<TakuriPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices)
        {
            var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Downtrend check using LookbackMeanTrend and LookbackTrendConsistency
            if (metrics.GetLookbackMeanTrend(1) > TrendThreshold ||
                metrics.GetLookbackTrendConsistency(1) < MinTrendConsistency) return null;

            // Range and shape conditions
            if (metrics.TotalRange < MinRange) return null;
            bool shape = metrics.BodySize <= BodyRangeRatio * metrics.TotalRange &&
                         metrics.LowerWick >= WickRangeRatio * metrics.TotalRange &&
                         metrics.LowerWick >= WickBodyRatio * metrics.BodySize &&
                         metrics.UpperWick <= metrics.BodySize &&
                         metrics.IsBullish;

            if (!shape) return null;

            var candles = new List<int> { index };
            return new TakuriPattern(candles);
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
                throw new InvalidOperationException("TakuriPattern must have exactly 1 candle.");

            int index = Candles[0];
            var metrics = metricsCache[index];

            // Power Score: Based on range, small body, long wick, wick dominance, trend
            double rangeScore = metrics.TotalRange / MinRange;
            rangeScore = Math.Min(rangeScore, 1);

            double bodyScore = 1 - (metrics.BodySize / (BodyRangeRatio * metrics.TotalRange));
            bodyScore = Math.Clamp(bodyScore, 0, 1);

            double wickScore = metrics.LowerWick / (WickRangeRatio * metrics.TotalRange);
            wickScore = Math.Min(wickScore, 1);

            double wickDominanceScore = metrics.LowerWick / (WickBodyRatio * metrics.BodySize);
            wickDominanceScore = Math.Min(wickDominanceScore, 1);

            double upperWickScore = 1 - (metrics.UpperWick / metrics.BodySize);
            upperWickScore = Math.Clamp(upperWickScore, 0, 1);

            double trendStrength = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double trendConsistency = metrics.GetLookbackTrendConsistency(1) / MinTrendConsistency;
            trendConsistency = Math.Min(trendConsistency, 1);

            double volumeScore = 0.5; // Placeholder

            double wRange = 0.1, wBody = 0.15, wWick = 0.2, wDominance = 0.2, wUpper = 0.1, wTrend = 0.15, wConsistency = 0.05, wVolume = 0.05;
            double powerScore = (wRange * rangeScore + wBody * bodyScore + wWick * wickScore + wDominance * wickDominanceScore +
                                 wUpper * upperWickScore + wTrend * trendStrength + wConsistency * trendConsistency + wVolume * volumeScore) /
                                (wRange + wBody + wWick + wDominance + wUpper + wTrend + wConsistency + wVolume);

            // Match Score: Deviation from thresholds
            double rangeDeviation = Math.Abs(metrics.TotalRange - MinRange) / MinRange;
            double bodyDeviation = metrics.BodySize / (BodyRangeRatio * metrics.TotalRange);
            double wickDeviation = Math.Abs(metrics.LowerWick - WickRangeRatio * metrics.TotalRange) / (WickRangeRatio * metrics.TotalRange);
            double dominanceDeviation = Math.Abs(metrics.LowerWick - WickBodyRatio * metrics.BodySize) / (WickBodyRatio * metrics.BodySize);
            double upperDeviation = metrics.UpperWick / metrics.BodySize;
            double trendDeviation = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            double consistencyDeviation = Math.Abs(metrics.GetLookbackTrendConsistency(1) - MinTrendConsistency) / MinTrendConsistency;
            double matchScore = 1 - (rangeDeviation + bodyDeviation + wickDeviation + dominanceDeviation + upperDeviation + trendDeviation + consistencyDeviation) / 7;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








