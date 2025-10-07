using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents the Unique 3 River pattern, a three-candle bullish reversal pattern in a downtrend.
    /// Consists of a strong bearish candle followed by a smaller bearish candle and a bullish candle that opens below the second close and closes above it.
    /// Indicates potential exhaustion of sellers and possible trend reversal upward.
    /// </summary>
    public class Unique3RiverPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first bearish candle to ensure a strong down move.
        /// Strictest: 1.0 (current), Loosest: 0.5 (still shows significant bearish momentum).
        /// </summary>
        public static double MinFirstBodySize { get; } = 1.0;

        /// <summary>
        /// Maximum body size for the second and third candles to maintain a slowing/reversal shape.
        /// Strictest: 1.0 (small bodies), Loosest: 2.5 (allows larger bodies but retains pattern intent).
        /// </summary>
        public static double MaxBodySize { get; } = 2.0;

        /// <summary>
        /// Tolerance for how close the second candle s close must be to the first candle s low.
        /// Strictest: 0.1 (very close), Loosest: 1.0 (broader range for nearness).
        /// </summary>
        public static double LowCloseTolerance { get; } = 0.5;

        /// <summary>
        /// Minimum negative trend strength to confirm a downtrend before the pattern.
        /// Strictest: -0.5 (strong downtrend), Loosest: -0.1 (minimal downtrend still present).
        /// </summary>
        public static double TrendThreshold { get; } = -0.3;

        /// <summary>
        /// Gets the base name for the Unique 3 River pattern.
        /// </summary>
        public const string BaseName = "Unique3River";

        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();

        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => "A bullish reversal pattern in a downtrend with a strong bearish candle followed by a smaller bearish candle and a bullish candle that opens below the second close and closes above it, signaling slowing bearish momentum.";

        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction => PatternDirection.Bullish;

        /// <summary>
        /// Gets the calculated strength of the pattern.
        /// Note: Not calculated in this implementation; reserved for future use.
        /// </summary>
        public override double Strength { get; protected set; }

        /// <summary>
        /// Gets the calculated certainty of the pattern.
        /// Note: Not calculated in this implementation; reserved for future use.
        /// </summary>
        public override double Certainty { get; protected set; }

        /// <summary>
        /// Gets the calculated uncertainty of the pattern.
        /// Note: Not calculated in this implementation; reserved for future use.
        /// </summary>
        public override double Uncertainty { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the Unique3RiverPattern class.
        /// </summary>
        /// <param name="candles">List of candle indices forming the pattern (three candles).</param>
        public Unique3RiverPattern(List<int> candles) : base(candles)
        {
        }

        /*
         * Unique 3 River Pattern:
         * - Description: A three-candle bullish reversal pattern in a downtrend. 
         *   Suggests a bottoming process with slowing bearish momentum.
         * - Requirements (Source: TradingView):
         *   1. First candle: Strong bearish candle in a downtrend.
         *   2. Second candle: Bearish, smaller body, often a new low or close near first low.
         *   3. Third candle: Bullish, opens below second close, closes above it.
         * - Indication: Indicates potential exhaustion of sellers, possible trend reversal upward.
         */
        /// <summary>
        /// Determines if a Unique 3 River pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle in the pattern.</param>
        /// <param name="trendLookback">Number of candles to look back for trend analysis.</param>
        /// <param name="prices">Array of candle price data.</param>
        /// <param name="metricsCache">Cache of precomputed candle metrics.</param>
        /// <returns>A Unique3RiverPattern instance if detected, otherwise null.</returns>
        public static async Task<Unique3RiverPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null;
            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle
            if (c1 < 0 || c3 >= prices.Length) return null;

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // First candle: Bearish, significant body
            if (metrics1.BodySize < MinFirstBodySize || !metrics1.IsBearish) return null;

            // Second candle: Bearish, smaller body, new low or close near first low
            if (metrics2.BodySize > MaxBodySize || !metrics2.IsBearish) return null;

            // Third candle: Bullish, smaller body, reversal shape
            if (metrics3.BodySize > MaxBodySize || !metrics3.IsBullish) return null;

            var ask1 = prices[c1];
            var ask2 = prices[c2];
            var ask3 = prices[c3];

            // Shape conditions
            bool shape = (ask2.Low < ask1.Low || Math.Abs(ask2.Close - ask1.Low) <= LowCloseTolerance) &&
                         ask3.Open < ask2.Close &&
                         ask3.Close > ask2.Close;
            if (!shape) return null;

            // Downtrend check
            if (metrics3.GetLookbackMeanTrend(3) > TrendThreshold) return null;

            var candles = new List<int> { c1, c2, c3 };
            return new Unique3RiverPattern(candles);
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
                throw new InvalidOperationException("Unique3RiverPattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on body sizes, low proximity, reversal, trend
            double firstBodyScore = metrics1.BodySize / MinFirstBodySize;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double secondBodyScore = 1 - (metrics2.BodySize / MaxBodySize);
            secondBodyScore = Math.Clamp(secondBodyScore, 0, 1);

            double thirdBodyScore = 1 - (metrics3.BodySize / MaxBodySize);
            thirdBodyScore = Math.Clamp(thirdBodyScore, 0, 1);

            double lowProximityScore = 1 - (Math.Min(Math.Abs(prices2.Close - prices1.Low), LowCloseTolerance) / LowCloseTolerance);
            lowProximityScore = Math.Clamp(lowProximityScore, 0, 1);

            double reversalScore = (prices3.Close - prices2.Close) / metrics1.BodySize;
            reversalScore = Math.Min(reversalScore, 1);

            double trendStrength = Math.Abs(metrics3.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.15, wSecond = 0.15, wThird = 0.15, wLow = 0.2, wReversal = 0.2, wTrend = 0.1, wVolume = 0.05;
            double powerScore = (wFirst * firstBodyScore + wSecond * secondBodyScore + wThird * thirdBodyScore +
                                 wLow * lowProximityScore + wReversal * reversalScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirst + wSecond + wThird + wLow + wReversal + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double firstDeviation = Math.Abs(metrics1.BodySize - MinFirstBodySize) / MinFirstBodySize;
            double secondDeviation = Math.Abs(metrics2.BodySize - MaxBodySize) / MaxBodySize;
            double thirdDeviation = Math.Abs(metrics3.BodySize - MaxBodySize) / MaxBodySize;
            double trendDeviation = Math.Abs(metrics3.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (firstDeviation + secondDeviation + thirdDeviation + trendDeviation) / 4;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








