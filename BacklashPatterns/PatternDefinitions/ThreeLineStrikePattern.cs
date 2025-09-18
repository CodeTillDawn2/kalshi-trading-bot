using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Three Line Strike candlestick pattern.
    /// </summary>
    public class ThreeLineStrikePattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for all candles in the pattern to ensure significance.
        /// Strictest: 1.5 (original logic), Loosest: 0.5 (still notable but less pronounced).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Threshold for confirming the strength of the preceding trend (  value).
        /// Strictest: 0.5 (original logic), Loosest: 0.1 (minimal trend still detectable).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.1;

        /// <summary>
        /// Represents a Three Line Strike pattern (Bullish or Bearish).
        /// - Bullish: A reversal pattern in a downtrend. Three bullish candles with ascending closes, followed by a bearish candle that strikes back, closing below the second candle s close.
        /// - Bearish: A reversal pattern in an uptrend. Three bearish candles with descending closes, followed by a bullish candle that strikes back, closing above the second candle s close.
        /// Requirements sourced from: https://www.investopedia.com/terms/t/three-line-strike.asp
        /// </summary>
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "ThreeLineStrike";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName;
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish reversal pattern with three consecutive bearish candles followed by a strong bullish candle that strikes back, closing above the third bearish candle. Signals potential reversal from downtrend to uptrend."
            : "A bearish reversal pattern with three consecutive bullish candles followed by a strong bearish candle that strikes back, closing below the third bullish candle. Signals potential reversal from uptrend to downtrend.";
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
        /// Initializes a new instance of the ThreeLineStrikePattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        public ThreeLineStrikePattern(List<int> candles) : base(candles)
        {
        }

        /// <summary>
        /// Determines if a Three Line Strike pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the fourth candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<ThreeLineStrikePattern?> IsPatternAsync(
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 3) return null;

            int c1 = index - 3;
            int c2 = index - 2;
            int c3 = index - 1;
            int c4 = index;

            if (c4 >= prices.Length) return null;

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, false);
            var metrics4 = await GetCandleMetricsAsync(metricsCache, c4, prices, trendLookback, true);

            if (isBullish)
            {
                if (!metrics1.IsBullish || metrics1.BodySize < MinBodySize) return null;
                if (!metrics2.IsBullish || metrics2.BodySize < MinBodySize) return null;
                if (!metrics3.IsBullish || metrics3.BodySize < MinBodySize) return null;
                if (!metrics4.IsBearish || metrics4.BodySize < MinBodySize) return null;

                // Step 3: Relaxed close checks with tolerance
                if (prices[c2].Close < prices[c1].Close - 0.5 || prices[c3].Close < prices[c2].Close - 0.5) return null;
                // Step 4: Low constraints removed (no code here)
                // Step 5: Simplified strike condition
                if (prices[c4].Close >= prices[c3].Close) return null;
                // Step 6: Trend check only if c1 >= 0
                if (c1 >= 0 && metrics4.GetLookbackMeanTrend(4) >= -TrendThreshold) return null;
            }
            else
            {
                if (!metrics1.IsBearish || metrics1.BodySize < MinBodySize) return null;
                if (!metrics2.IsBearish || metrics2.BodySize < MinBodySize) return null;
                if (!metrics3.IsBearish || metrics3.BodySize < MinBodySize) return null;
                if (!metrics4.IsBullish || metrics4.BodySize < MinBodySize) return null;

                // Step 3: Relaxed close checks with tolerance
                if (prices[c2].Close > prices[c1].Close + 0.5 || prices[c3].Close > prices[c2].Close + 0.5) return null;
                // Step 4: High constraints removed (no code here)
                // Step 5: Simplified strike condition
                if (prices[c4].Close <= prices[c3].Close) return null;
                // Step 6: Trend check only if c1 >= 0
                if (c1 >= 0 && metrics4.GetLookbackMeanTrend(4) <= TrendThreshold) return null;
            }

            var candles = new List<int> { c1, c2, c3, c4 };
            return new ThreeLineStrikePattern(candles);
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
            if (Candles.Count != 4)
                throw new InvalidOperationException("ThreeLineStrikePattern must have exactly 4 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2], c4 = Candles[3];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];
            var metrics4 = metricsCache[c4];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];
            var prices4 = prices[c4];

            // Power Score: Based on body sizes, progression, strike, trend
            double bodyScore = (metrics1.BodySize + metrics2.BodySize + metrics3.BodySize + metrics4.BodySize) / (4 * MinBodySize);
            bodyScore = Math.Min(bodyScore, 1);

            double progressionScore = 0;
            if (metrics1.IsBullish && metrics2.IsBullish && metrics3.IsBullish && metrics4.IsBearish)
            {
                // Bullish pattern: ascending closes
                double asc1 = prices2.Close - prices1.Close;
                double asc2 = prices3.Close - prices2.Close;
                progressionScore = (asc1 + asc2) / (2 * MinBodySize);
                progressionScore = Math.Min(progressionScore, 1);
            }
            else if (metrics1.IsBearish && metrics2.IsBearish && metrics3.IsBearish && metrics4.IsBullish)
            {
                // Bearish pattern: descending closes
                double desc1 = prices1.Close - prices2.Close;
                double desc2 = prices2.Close - prices3.Close;
                progressionScore = (desc1 + desc2) / (2 * MinBodySize);
                progressionScore = Math.Min(progressionScore, 1);
            }

            double strikeScore = 0;
            if (metrics4.IsBearish)
            {
                strikeScore = (prices3.Close - prices4.Close) / MinBodySize;
                strikeScore = Math.Min(strikeScore, 1);
            }
            else if (metrics4.IsBullish)
            {
                strikeScore = (prices4.Close - prices3.Close) / MinBodySize;
                strikeScore = Math.Min(strikeScore, 1);
            }

            double trendStrength = Math.Abs(metrics4.GetLookbackMeanTrend(4));

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.25, wProgression = 0.25, wStrike = 0.3, wTrend = 0.15, wVolume = 0.05;
            double powerScore = (wBody * bodyScore + wProgression * progressionScore + wStrike * strikeScore +
                                 wTrend * trendStrength + wVolume * volumeScore) /
                                (wBody + wProgression + wStrike + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(metrics1.BodySize - MinBodySize) / MinBodySize;
            double trendDeviation = Math.Abs(trendStrength - TrendThreshold) / TrendThreshold;
            double matchScore = 1 - (bodyDeviation + trendDeviation) / 2;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








