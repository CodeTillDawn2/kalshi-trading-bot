using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
/// <summary>MarubozuPattern</summary>
/// <summary>MarubozuPattern</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
    public class MarubozuPattern : PatternDefinition
/// <summary>
/// </summary>
    {
        /// <summary>
        /// Minimum total range for the candle to be considered significant.
        /// Loosest: 1.0 (allows smaller significant moves); Strictest: 5.0 (requires large moves).
        /// </summary>
        public static double MinRange { get; } = 3.0;
/// <summary>
/// </summary>

        /// <summary>
        /// Threshold for trend strength to validate prior trend context.
        /// Loosest: 0.1 (very weak trend); Strictest: 0.8 (strong trend required).
        /// </summary>
        public static double TrendThreshold { get; } = 0.5;
/// <summary>
/// </summary>
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Marubozu";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish candle with no wicks (open equals low, close equals high), showing strong buying momentum with no rejection of higher prices."
            : "A bearish candle with no wicks (open equals high, close equals low), showing strong selling momentum with no rejection of lower prices.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => IsBullish ? PatternDirection.Bullish : PatternDirection.Bearish;
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
        /// Initializes a new instance of the MarubozuPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public MarubozuPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Marubozu pattern, a single-candle pattern of strong trend continuation or reversal.
        /// Requirements (sourced from Investopedia):
        /// - A long-bodied candle with no upper or lower wicks (open equals low and close equals high for bullish,
        ///   open equals high and close equals low for bearish).
        /// - Indicates strong buying (bullish) or selling (bearish) pressure.
        /// - Typically appears in a trend context (bullish after downtrend, bearish after uptrend).
        /// Your original logic matches this strict definition with a minimum range and trend check.
        /// </summary>
        public static async Task<MarubozuPattern?> IsPatternAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices)
        {
            // Lazy load metrics for the current candle
            var metrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Check if the total range meets the minimum requirement for significance
            if (metrics.TotalRange < MinRange) return null;

            // Determine if the candle direction matches the expected trend
            bool direction = isBullish ? metrics.IsBullish : metrics.IsBearish;

            // Strict wick check using raw price values to ensure no wicks
            var ask = prices[index];
            bool strictBullish = isBullish && ask.Open == ask.Low && ask.Close == ask.High;
            bool strictBearish = !isBullish && ask.Open == ask.High && ask.Close == ask.Low;

            // Validate the trend direction using CandleMetrics method
            bool trendValid = isBullish
                ? metrics.GetLookbackMeanTrend(1) < -TrendThreshold // Downtrend before bullish
                : metrics.GetLookbackMeanTrend(1) > TrendThreshold;  // Uptrend before bearish

            // Combine conditions to confirm the pattern
            bool isPatternValid = (strictBullish || strictBearish) && direction && trendValid;

            if (!isPatternValid) return null;

            // Define the candle index for the pattern (single candle)
            var candles = new List<int> { index };

            // Return the pattern instance with the specified direction
            return new MarubozuPattern(candles, isBullish);
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
                throw new InvalidOperationException("MarubozuPattern must have exactly 1 candle.");

            int index = Candles[0];
            var metrics = metricsCache[index];
            var ask = prices[index];

            // Power Score: Based on range, body size, wick absence, trend
            double rangeScore = metrics.TotalRange / MinRange;
            rangeScore = Math.Min(rangeScore, 1);

            double bodyScore = metrics.BodySize / MinRange; // Body should be close to total range
            bodyScore = Math.Min(bodyScore, 1);

            // Wick absence score (perfect Marubozu has no wicks)
            double wickScore = 1.0;
            if (IsBullish)
            {
                if (ask.Open != ask.Low || ask.Close != ask.High) wickScore = 0.5;
            }
            else
            {
                if (ask.Open != ask.High || ask.Close != ask.Low) wickScore = 0.5;
            }

            double trendStrength = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wRange = 0.25, wBody = 0.25, wWick = 0.25, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wRange * rangeScore + wBody * bodyScore + wWick * wickScore +
                                 wTrend * trendStrength + wVolume * volumeScore) /
                                (wRange + wBody + wWick + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double rangeDeviation = Math.Abs(metrics.TotalRange - MinRange) / MinRange;
            double trendDeviation = Math.Abs(metrics.GetLookbackMeanTrend(1) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (rangeDeviation + trendDeviation) / 2;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








