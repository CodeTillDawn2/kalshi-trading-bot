using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Kicking By Length candlestick pattern.
    /// </summary>
    public class KickingByLengthPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for both candles.
        /// Purpose: Ensures candles have significant body size to indicate strong momentum.
        /// Strictest: 1.5 (current default, aligns with Marubozu-like strength).
        /// Loosest: 1.0 (still notable body size while relaxing strictness).
        /// </summary>
        public static double MinBodySize { get; set; } = 1.0;

        /// <summary>
        /// Maximum wick size (upper and lower) for Marubozu-like candles.
        /// Purpose: Limits wick size to maintain Marubozu resemblance.
        /// Strictest: 0.3 (nearly pure Marubozu per Investopedia).
        /// Loosest: 0.7 (allows more flexibility while preserving pattern intent).
        /// </summary>
        public static double MaxWickSize { get; set; } = 1.0;

        /// <summary>
        /// Minimum gap size between the two candles.
        /// Purpose: Ensures a clear separation indicating momentum shift.
        /// Strictest: 0.5 (current default, significant gap).
        /// Loosest: 0.1 (minimal gap still noticeable per TradingView).
        /// </summary>
        public static double GapSize { get; set; } = 0.5;

        /// <summary>
        /// Minimum trend strength for prior trend.
        /// Purpose: Confirms preceding trend direction before reversal.
        /// Strictest: 0.5 (strong trend per technical analysis norms).
        /// Loosest: 0.1 (weak trend still sufficient for context).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.05;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "KickingByLength";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish reversal pattern with two Marubozu candles where a bearish Marubozu is followed by a longer bullish Marubozu that gaps up, signaling strong reversal from downtrend to uptrend."
            : "A bearish reversal pattern with two Marubozu candles where a bullish Marubozu is followed by a longer bearish Marubozu that gaps down, signaling strong reversal from uptrend to downtrend.";
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
        /// Initializes a new instance of the KickingByLengthPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public KickingByLengthPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Kicking By Length pattern, a two-candle reversal pattern.
        /// Requirements (sourced from TradingView and Investopedia):
        /// - Two large-bodied candles with opposite directions, resembling Marubozu (minimal wicks).
        /// - A significant gap between the candles (bullish: up gap; bearish: down gap).
        /// - Bullish pattern follows a downtrend; bearish pattern follows an uptrend.
        /// - Candles should have minimal wicks (less strict than pure Marubozu).
        /// Indicates: A strong reversal due to an abrupt momentum shift, supported by large bodies and a gap.
        /// Note: Original logic relaxed strictness from pure Marubozu and adjusted trend thresholds.
        /// </summary>
        public static async Task<KickingByLengthPattern?> IsPatternAsync(
                int index, int trendLookback, bool isBullish,
                Dictionary<int, CandleMetrics> metricsCache, CandleMids[] prices)
        {
            if (index < 1) return null;

            var prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Trend check
            if (isBullish && currMetrics.GetLookbackMeanTrend(2) >= -TrendThreshold) return null;
            if (!isBullish && currMetrics.GetLookbackMeanTrend(2) <= TrendThreshold) return null;

            // Body size check
            if (prevMetrics.BodySize < MinBodySize || currMetrics.BodySize < MinBodySize) return null;

            // Wick limits check
            if (prevMetrics.UpperWick > MaxWickSize || prevMetrics.LowerWick > MaxWickSize) return null;
            if (currMetrics.UpperWick > MaxWickSize || currMetrics.LowerWick > MaxWickSize) return null;

            // Direction check
            bool directions = isBullish ? (prevMetrics.IsBearish && currMetrics.IsBullish)
                                       : (prevMetrics.IsBullish && currMetrics.IsBearish);
            if (!directions) return null;

            return new KickingByLengthPattern(new List<int> { index - 1, index }, isBullish);
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
                throw new InvalidOperationException("KickingByLengthPattern must have exactly 2 candles.");

            int prevIndex = Candles[0];
            int currIndex = Candles[1];

            var prevMetrics = metricsCache[prevIndex];
            var currMetrics = metricsCache[currIndex];

            var prevPrice = prices[prevIndex];
            var currPrice = prices[currIndex];

            // Power Score: Based on body sizes, wick minimization, gap, trend
            double bodyScore = (prevMetrics.BodySize + currMetrics.BodySize) / (2 * MinBodySize);
            bodyScore = Math.Min(bodyScore, 1);

            double wickScore = 1 - ((prevMetrics.UpperWick + prevMetrics.LowerWick + currMetrics.UpperWick + currMetrics.LowerWick) / (4 * MaxWickSize));
            wickScore = Math.Clamp(wickScore, 0, 1);

            double gapScore = 0;
            if (IsBullish)
            {
                gapScore = (currPrice.Open - prevPrice.Close) / GapSize;
                gapScore = Math.Min(gapScore, 1);
            }
            else
            {
                gapScore = (prevPrice.Close - currPrice.Open) / GapSize;
                gapScore = Math.Min(gapScore, 1);
            }

            double trendStrength = Math.Abs(currMetrics.GetLookbackMeanTrend(2) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.25, wWick = 0.25, wGap = 0.25, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wBody * bodyScore + wWick * wickScore + wGap * gapScore +
                                 wTrend * trendStrength + wVolume * volumeScore) /
                                (wBody + wWick + wGap + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(prevMetrics.BodySize - MinBodySize) / MinBodySize;
            double wickDeviation = (prevMetrics.UpperWick + prevMetrics.LowerWick) / (2 * MaxWickSize);
            double trendDeviation = Math.Abs(currMetrics.GetLookbackMeanTrend(2) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (bodyDeviation + wickDeviation + trendDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








