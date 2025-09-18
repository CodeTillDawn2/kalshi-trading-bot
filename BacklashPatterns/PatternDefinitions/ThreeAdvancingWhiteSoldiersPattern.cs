using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Three Advancing White Soldiers candlestick pattern.
    /// </summary>
    public class ThreeAdvancingWhiteSoldiersPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for each candle. Ensures strong bullish momentum.
        /// Strictest: 2.0 (very strong), Loosest: 0.5 (minimal momentum).
        /// </summary>
        public static double MinBodySize { get; } = 1.0;

        /// <summary>
        /// Maximum wick size relative to the total range. Ensures small wicks for strong candles.
        /// Strictest: 0.1 (tiny wicks), Loosest: 0.4 (larger wicks allowed).
        /// </summary>
        public static double WickRangeRatio { get; } = 0.2;

        /// <summary>
        /// Threshold for confirming a prior downtrend. Negative value indicates downtrend.
        /// Strictest: -0.5 (strong downtrend), Loosest: -0.1 (weak downtrend).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ThreeAdvancingWhiteSoldiers";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Bullish;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bullish reversal pattern in a downtrend with three consecutive bullish candles of increasing size and higher closes, signaling strong upward momentum.";
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
        /// Initializes a new instance of the ThreeAdvancingWhiteSoldiersPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public ThreeAdvancingWhiteSoldiersPattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Identifies a Three Advancing White Soldiers pattern, a three-candle bullish reversal pattern.
        /// Occurs after a downtrend; three consecutive bullish candles with large bodies and small wicks,
        /// each closing higher than the previous. Signals strong buying pressure and a potential trend reversal.
        /// Source: https://www.investopedia.com/terms/t/three_white_soldiers.asp
        /// </summary>
        public static async Task<ThreeAdvancingWhiteSoldiersPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null; // Need 3 candles
            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle
            if (c1 < 0 || c3 >= prices.Length) return null;

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // Check each candle: Bullish, significant body, small wicks
            if (metrics1.BodySize < MinBodySize || !metrics1.IsBullish ||
                metrics1.UpperWick > WickRangeRatio * metrics1.TotalRange ||
                metrics1.LowerWick > WickRangeRatio * metrics1.TotalRange) return null;
            if (metrics2.BodySize < MinBodySize || !metrics2.IsBullish ||
                metrics2.UpperWick > WickRangeRatio * metrics2.TotalRange ||
                metrics2.LowerWick > WickRangeRatio * metrics2.TotalRange) return null;
            if (metrics3.BodySize < MinBodySize || !metrics3.IsBullish ||
                metrics3.UpperWick > WickRangeRatio * metrics3.TotalRange ||
                metrics3.LowerWick > WickRangeRatio * metrics3.TotalRange) return null;

            // Ascending closes
            var ask1 = prices[c1];
            var ask2 = prices[c2];
            var ask3 = prices[c3];
            if (ask2.Close <= ask1.Close || ask3.Close <= ask2.Close) return null;

            // Downtrend check using LookbackMeanTrend (3 candles)
            if (metrics3.GetLookbackMeanTrend(3) > TrendThreshold) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new ThreeAdvancingWhiteSoldiersPattern(candles);
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
                throw new InvalidOperationException("ThreeAdvancingWhiteSoldiersPattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on body sizes, wick minimization, ascending progression, trend
            double bodyScore = (metrics1.BodySize + metrics2.BodySize + metrics3.BodySize) / (3 * MinBodySize);
            bodyScore = Math.Min(bodyScore, 1);

            double wickScore = 1 - ((metrics1.UpperWick + metrics1.LowerWick + metrics2.UpperWick + metrics2.LowerWick + metrics3.UpperWick + metrics3.LowerWick) /
                                    (6 * WickRangeRatio * (metrics1.TotalRange + metrics2.TotalRange + metrics3.TotalRange) / 3));
            wickScore = Math.Clamp(wickScore, 0, 1);

            double progressionScore = (prices2.Close - prices1.Close + prices3.Close - prices2.Close) / (2 * MinBodySize);
            progressionScore = Math.Min(progressionScore, 1);

            double trendStrength = Math.Abs(metrics3.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.25, wWick = 0.25, wProgression = 0.25, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wBody * bodyScore + wWick * wickScore + wProgression * progressionScore +
                                 wTrend * trendStrength + wVolume * volumeScore) /
                                (wBody + wWick + wProgression + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(metrics1.BodySize - MinBodySize) / MinBodySize;
            double wickDeviation = (metrics1.UpperWick + metrics1.LowerWick) / (2 * WickRangeRatio * metrics1.TotalRange);
            double trendDeviation = Math.Abs(metrics3.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (bodyDeviation + wickDeviation + trendDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








