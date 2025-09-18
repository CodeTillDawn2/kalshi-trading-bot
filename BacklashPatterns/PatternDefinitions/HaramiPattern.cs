using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// The Harami pattern is a two-candle reversal pattern indicating a potential trend change.
    /// Requirements:
    /// - First candle: Large body in the direction of the prevailing trend (bullish in uptrend, bearish in downtrend).
    /// - Second candle: Smaller body fully contained within the first candle s body, opposite direction.
    /// - Trend: Occurs after a defined trend (downtrend for Bullish Harami, uptrend for Bearish Harami).
    /// Indicates:
    /// - Bullish Harami: Potential reversal from downtrend to uptrend.
    /// - Bearish Harami: Potential reversal from uptrend to downtrend.
    /// Source: https://www.investopedia.com/terms/h/harami.asp
    /// </summary>
    public class HaramiPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first candle. Ensures the first candle has a significant body.
        /// Strictest: 1.5 (original), Loosest: 1.0 (still notable body per general candlestick analysis).
        /// </summary>
        public static double MinFirstBodySize { get; } = 1.5;

        /// <summary>
        /// Maximum body size for the second candle. Limits the second candle s body to remain small.
        /// Strictest: 1.5 (original), Loosest: 2.0 (still smaller relative to first, per loose definitions).
        /// </summary>
        public static double MaxSecondBodySize { get; } = 1.5;

        /// <summary>
        /// Maximum second body size as a percentage of the first candle s body. Ensures proportionality.
        /// Strictest: 0.5 (original), Loosest: 0.75 (allows slightly larger second body, per loose Harami definitions).
        /// </summary>
        public static double BodySizeRatio { get; } = 0.5;

        /// <summary>
        /// Minimum trend strength for the prior trend. Confirms the preceding trend s validity.
        /// Strictest: 0.3 (original), Loosest: 0.1 (minimal trend still detectable, per broad reversal logic).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Harami";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish reversal pattern with a large bearish candle followed by a smaller bullish candle completely contained within the first candle's body. Signals potential reversal from downtrend to uptrend."
            : "A bearish reversal pattern with a large bullish candle followed by a smaller bearish candle completely contained within the first candle's body. Signals potential reversal from uptrend to downtrend.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => IsBullish ? PatternDirection.Bullish : PatternDirection.Bearish;
        private readonly bool IsBullish;
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
        /// Initializes a new instance of the HaramiPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public HaramiPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Determines if a Harami pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the second candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<HaramiPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            bool isBullish)
        {
            if (index < 1) return null;

            var prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
            CandleMids previousPrice = prices[index - 1];
            CandleMids currentPrice = prices[index];

            if (prevMetrics.BodySize < MinFirstBodySize) return null;

            if (currMetrics.BodySize > MaxSecondBodySize ||
                currMetrics.BodySize > BodySizeRatio * prevMetrics.BodySize) return null;

            double prevMin = Math.Min(previousPrice.Open, previousPrice.Close);
            double prevMax = Math.Max(previousPrice.Open, previousPrice.Close);
            bool inside = currentPrice.Open >= prevMin &&
                          currentPrice.Open <= prevMax &&
                          currentPrice.Close >= prevMin &&
                          currentPrice.Close <= prevMax;
            if (!inside) return null;

            bool prevDirection = isBullish ? prevMetrics.IsBearish : prevMetrics.IsBullish;
            bool currDirection = isBullish ? currMetrics.IsBullish : currMetrics.IsBearish;
            if (!prevDirection || !currDirection) return null;

            bool trendValid = isBullish
                ? currMetrics.GetLookbackMeanTrend(2) <= -TrendThreshold
                : currMetrics.GetLookbackMeanTrend(2) >= TrendThreshold;
            if (!trendValid) return null;

            var candles = new List<int> { index - 1, index };
            return new HaramiPattern(candles, isBullish);
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
            if (Candles.Count != 2)
                throw new InvalidOperationException("HaramiPattern must have exactly 2 candles.");

            int prevIndex = Candles[0];
            int currIndex = Candles[1];

            var prevMetrics = metricsCache[prevIndex];
            var currMetrics = metricsCache[currIndex];

            var previousPrice = prices[prevIndex];
            var currentPrice = prices[currIndex];

            // Power Score: Based on body sizes, ratio, containment, trend
            double firstBodyScore = prevMetrics.BodySize / MinFirstBodySize;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double secondBodyScore = 1 - (currMetrics.BodySize / MaxSecondBodySize);
            secondBodyScore = Math.Clamp(secondBodyScore, 0, 1);

            double bodyRatioScore = 1 - (currMetrics.BodySize / (BodySizeRatio * prevMetrics.BodySize));
            bodyRatioScore = Math.Clamp(bodyRatioScore, 0, 1);

            // Containment score
            double prevMin = Math.Min(previousPrice.Open, previousPrice.Close);
            double prevMax = Math.Max(previousPrice.Open, previousPrice.Close);
            bool inside = currentPrice.Open >= prevMin && currentPrice.Open <= prevMax &&
                          currentPrice.Close >= prevMin && currentPrice.Close <= prevMax;
            double containmentScore = inside ? 1.0 : 0.5;

            double trendStrength = Math.Abs(currMetrics.GetLookbackMeanTrend(2));

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.2, wSecond = 0.2, wRatio = 0.2, wContainment = 0.2, wTrend = 0.1, wVolume = 0.1;
            double powerScore = (wFirst * firstBodyScore + wSecond * secondBodyScore + wRatio * bodyRatioScore +
                                 wContainment * containmentScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirst + wSecond + wRatio + wContainment + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double firstDeviation = Math.Abs(prevMetrics.BodySize - MinFirstBodySize) / MinFirstBodySize;
            double secondDeviation = Math.Abs(currMetrics.BodySize - MaxSecondBodySize) / MaxSecondBodySize;
            double ratioDeviation = Math.Abs(currMetrics.BodySize - BodySizeRatio * prevMetrics.BodySize) / (BodySizeRatio * prevMetrics.BodySize);
            double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
            double matchScore = 1 - (firstDeviation + secondDeviation + ratioDeviation + trendDeviation) / 4;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








