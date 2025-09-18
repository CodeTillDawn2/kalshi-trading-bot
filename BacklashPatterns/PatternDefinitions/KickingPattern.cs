using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Kicking candlestick pattern.
    /// </summary>
    public class KickingPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for both candles.
        /// Purpose: Ensures significant body size for momentum.
        /// Strictest: 1.5 (current default, strong body).
        /// Loosest: 1.0 (relaxed but still notable per Investopedia).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.8;

        /// <summary>
        /// Maximum wick size (upper and lower).
        /// Purpose: Limits wicks to resemble Marubozu.
        /// Strictest: 0.5 (near pure Marubozu).
        /// Loosest: 2.0 (allows larger wicks per trading forums).
        /// </summary>
        public static double MaxWickSize { get; set; } = 2.5;

        /// <summary>
        /// Minimum gap size between candles.
        /// Purpose: Ensures a visible momentum shift.
        /// Strictest: 0.5 (current default, clear gap).
        /// Loosest: 0.1 (minimal gap per technical analysis).
        /// </summary>
        public static double GapSize { get; set; } = 0.5;

        /// <summary>
        /// Minimum trend strength for prior trend.
        /// Purpose: Confirms trend context before reversal.
        /// Strictest: 0.5 (strong trend).
        /// Loosest: 0.1 (weak trend still valid).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.05;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "Kicking";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish reversal pattern with two Marubozu candles where a bearish Marubozu is followed by a bullish Marubozu that gaps up, signaling strong reversal from downtrend to uptrend."
            : "A bearish reversal pattern with two Marubozu candles where a bullish Marubozu is followed by a bearish Marubozu that gaps down, signaling strong reversal from uptrend to downtrend.";
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
        /// Initializes a new instance of the KickingPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public KickingPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Kicking pattern, a two-candle reversal pattern.
        /// Requirements (sourced from Investopedia and other technical analysis resources):
        /// - Two large-bodied candles with opposite directions.
        /// - A gap between the candles (up for bullish, down for bearish).
        /// - Small or no wicks, resembling Marubozu candles.
        /// - Bullish Kicking follows a downtrend; Bearish Kicking follows an uptrend.
        /// Indicates: Strong reversal signal due to abrupt momentum shift.
        /// </summary>
        public static async Task<KickingPattern?> IsPatternAsync(
        int index, bool isBullish, int trendLookback,
        Dictionary<int, CandleMetrics> metricsCache, CandleMids[] prices)
        {
            if (index < 1) return null;

            var prevMetrics = await GetCandleMetricsAsync(metricsCache, index - 1, prices, trendLookback, false);
            var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);

            // Trend check
            bool trendValid = isBullish ? currMetrics.GetLookbackMeanTrend(2) <= -TrendThreshold
                                       : currMetrics.GetLookbackMeanTrend(2) >= TrendThreshold;
            if (!trendValid) return null;

            // Body size check
            if (prevMetrics.BodySize < MinBodySize || currMetrics.BodySize < MinBodySize) return null;

            // Wick limits check
            if (prevMetrics.UpperWick > MaxWickSize || prevMetrics.LowerWick > MaxWickSize) return null;
            if (currMetrics.UpperWick > MaxWickSize || currMetrics.LowerWick > MaxWickSize) return null;

            // Direction check
            bool directions = isBullish ? (prevMetrics.IsBearish && currMetrics.IsBullish)
                                       : (prevMetrics.IsBullish && currMetrics.IsBearish);
            if (!directions) return null;

            return new KickingPattern(new List<int> { index - 1, index }, isBullish);
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
                throw new InvalidOperationException("KickingPattern must have exactly 2 candles.");

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








