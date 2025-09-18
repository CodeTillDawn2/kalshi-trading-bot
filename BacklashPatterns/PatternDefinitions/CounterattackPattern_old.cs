using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Counterattack candlestick pattern, a 2-candle reversal pattern.
    /// Bullish: Indicates a potential reversal from a downtrend to an uptrend.
    /// Bearish: Indicates a potential reversal from an uptrend to a downtrend.
    /// Requirements (Source: TradingView, BabyPips):
    /// - Occurs after a strong trend (downtrend for bullish, uptrend for bearish).
    /// - First candle: Strong move in trend direction (bearish for bullish, bullish for bearish).
    /// - Second candle: Opposite direction, similar size, closes near first open, not engulfing.
    /// </summary>
    public class CounterattackPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for both candles to ensure significant movement.
        /// Loosest value: 0.5 (allows smaller but still notable bodies, per general candlestick guidelines).
        /// </summary>
        public static double MinBodySize { get; } = 0.5;

        /// <summary>
        /// Minimum ratio of body size to total candle range for both candles.
        /// Loosest value: 0.3 (permits smaller bodies relative to range, per TradingView).
        /// </summary>
        public static double BodyToRangeRatio { get; } = 0.4;

        /// <summary>
        /// Maximum distance between the second candle's close and first candle's open for proximity.
        /// Loosest value: 3.0 (allows a wider range while maintaining counterattack intent, per BabyPips).
        /// </summary>
        public static double NearOpenClose { get; } = 2.5;

        /// <summary>
        /// Minimum trend strength threshold to confirm a preceding trend.
        /// Loosest value: 0.1 (requires only a mild trend, per loose trend definitions).
        /// </summary>
        public static double TrendThreshold { get; } = 0.2;

        /// <summary>
        /// Minimum trend consistency over the lookback period.
        /// Loosest value: 0.3 (allows less consistent trends, per general analysis).
        /// </summary>
        public static double TrendConsistencyMin { get; } = 0.4;

        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Counterattack";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish reversal pattern with two candles of equal size where a bearish candle is followed by a bullish candle that closes at the same level."
            : "A bearish reversal pattern with two candles of equal size where a bullish candle is followed by a bearish candle that closes at the same level.";
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
        /// Initializes a new instance of the CounterattackPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public CounterattackPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Determines if a Counterattack pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the second candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<CounterattackPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            bool isBullish)
        {
            if (index < 1 || index >= prices.Length) return null;
            var candles = new List<int> { index - 1, index };

            CandleMetrics prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            CandleMetrics currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
            CandleMids previousPrices = prices[index - 1];
            CandleMids currentPrices = prices[index];

            // Check candle sizes
            bool isLargeFirst = prevMetrics.BodySize >= MinBodySize &&
                               prevMetrics.BodyToRangeRatio >= BodyToRangeRatio;
            if (!isLargeFirst) return null;

            bool isLargeSecond = currMetrics.BodySize >= MinBodySize &&
                                currMetrics.BodyToRangeRatio >= BodyToRangeRatio;
            if (!isLargeSecond) return null;

            // Direction checks
            bool prevTrend = isBullish ? prevMetrics.IsBearish : prevMetrics.IsBullish;
            if (!prevTrend) return null;

            bool currTrend = isBullish ? currMetrics.IsBullish : currMetrics.IsBearish;
            if (!currTrend) return null;

            // Proximity checks
            bool nearOpen = Math.Abs(currentPrices.Close - previousPrices.Open) <= NearOpenClose;
            if (!nearOpen) return null;

            bool nearClose = Math.Abs(currentPrices.Open - previousPrices.Close) <= NearOpenClose;
            if (!nearClose) return null;

            // Not engulfing
            bool notEngulfing = isBullish
                ? !(currentPrices.Open < previousPrices.Close && currentPrices.Close > previousPrices.Open)
                : !(currentPrices.Open > previousPrices.Close && currentPrices.Close < previousPrices.Open);
            if (!notEngulfing) return null;

            // Trend check
            double meanTrend = currMetrics.GetLookbackMeanTrend(2);
            double trendConsistency = currMetrics.GetLookbackTrendConsistency(2);
            bool hasTrend = isBullish ? (meanTrend <= -TrendThreshold && trendConsistency >= TrendConsistencyMin)
                                     : (meanTrend >= TrendThreshold && trendConsistency >= TrendConsistencyMin);
            if (!hasTrend) return null;

            return new CounterattackPattern(candles, isBullish);
        }
    }
}








