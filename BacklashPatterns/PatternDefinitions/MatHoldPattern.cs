using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Mat Hold candlestick pattern.
    /// </summary>
    public class MatHoldPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first and fifth candles, ensuring strong directional moves.
        /// - Strictest: 2.0 (very strong candles).
        /// - Loosest: 1.0 (minimal significant body, per BabyPips  relaxed continuation patterns).
        /// </summary>
        public static double MinBodySize { get; } = 1.5;

        /// <summary>
        /// Maximum body size for the middle three candles, enforcing their small, consolidating nature.
        /// - Strictest: 0.5 (very small bodies).
        /// - Loosest: 2.0 (allows larger consolidation candles, per loose Mat Hold interpretations).
        /// </summary>
        public static double MaxBodySize { get; } = 1.5;

        /// <summary>
        /// Minimum trend strength required prior to the pattern. Positive for bullish, negative for bearish.
        /// - Strictest: 0.5 (strong trend).
        /// - Loosest: 0.1 (minimal trend, per BabyPips  flexible continuation logic).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Buffer allowing middle candles to slightly exceed the first candle s range.
        /// - Strictest: 0.0 (strict containment within first candle s range).
        /// - Loosest: 1.0 (significant buffer, per loose consolidation definitions).
        /// </summary>
        public static double ContainmentBuffer { get; } = 0.5;

        /// <summary>
        /// Minimum difference between the fifth candle s close and the first candle s close, confirming continuation.
        /// - Strictest: 1.0 (strong continuation).
        /// - Loosest: 0.3 (minimal continuation move, per relaxed Mat Hold standards).
        /// </summary>
        public static double CloseDifference { get; } = 0.5;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "MatHold";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish continuation pattern in an uptrend with a long bullish candle followed by three smaller candles that stay above the first candle's low, and a fifth bullish candle that closes above the first candle's high."
            : "A bearish continuation pattern in a downtrend with a long bearish candle followed by three smaller candles that stay below the first candle's high, and a fifth bearish candle that closes below the first candle's low.";
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
        /// Initializes a new instance of the MatHoldPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public MatHoldPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Mat Hold pattern, a five-candle continuation pattern.
        /// Requirements (sourced from BabyPips and adapted to your logic):
        /// - First candle: Strong directional move (bullish or bearish).
        /// - Middle three candles: Small-bodied, at least one opposing the trend, contained within the first candle s range.
        /// - Fifth candle: Strong continuation of the initial trend, closing beyond the first candle.
        /// - Indicates a pause in a trend followed by continuation.
        /// Your original logic relaxes body sizes and containment strictness.
        /// </summary>
        public static async Task<MatHoldPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            // Early exit if there aren t enough candles for the pattern
            if (index < 4) return null;

            // Define candle indices for the five-candle pattern
            int c1 = index - 4; int c2 = index - 3; int c3 = index - 2; int c4 = index - 1; int c5 = index;
            if (c1 < 0 || c5 >= prices.Length) return null;

            // Lazy load metrics for all five candles
            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, false);
            var metrics4 = await GetCandleMetricsAsync(metricsCache, c4, prices, trendLookback, false);
            var metrics5 = await GetCandleMetricsAsync(metricsCache, c5, prices, trendLookback, true);

            // Retrieve price values for all five candles
            var ask1 = prices[c1]; var ask2 = prices[c2]; var ask3 = prices[c3];
            var ask4 = prices[c4]; var ask5 = prices[c5];

            if (isBullish)
            {
                // First candle must be significant and in the expected direction
                if (!metrics1.IsBullish || metrics1.BodySize < MinBodySize) return null;

                // Middle candles must be small
                if (metrics2.BodySize > MaxBodySize || metrics3.BodySize > MaxBodySize || metrics4.BodySize > MaxBodySize) return null;

                // Fifth candle must confirm the trend and be significant
                if (!metrics5.IsBullish || metrics5.BodySize < MinBodySize) return null;

                // Ensure the fifth candle has a significant range
                if (metrics5.TotalRange <= 0) return null;

                // At least one middle candle must oppose the trend
                bool atLeastOneBearish = metrics2.IsBearish || metrics3.IsBearish || metrics4.IsBearish;

                // Middle candles must be contained within the first candle s range with buffer
                bool containment = ask2.High <= ask1.High + ContainmentBuffer && ask2.Low >= ask1.Low - ContainmentBuffer &&
                                  ask3.High <= ask1.High + ContainmentBuffer && ask3.Low >= ask1.Low - ContainmentBuffer &&
                                  ask4.High <= ask1.High + ContainmentBuffer && ask4.Low >= ask1.Low - ContainmentBuffer;

                // Combine all conditions for bullish case
                bool isPatternValid = atLeastOneBearish && containment &&
                                     ask5.Close >= ask1.Close + CloseDifference &&
                                     metrics5.GetLookbackMeanTrend(5) > TrendThreshold; // Requires a prior uptrend

                if (!isPatternValid) return null;
            }
            else // Bearish
            {
                // First candle must be significant and in the expected direction
                if (!metrics1.IsBearish || metrics1.BodySize < MinBodySize) return null;

                // Middle candles must be small
                if (metrics2.BodySize > MaxBodySize || metrics3.BodySize > MaxBodySize || metrics4.BodySize > MaxBodySize) return null;

                // Fifth candle must confirm the trend and be significant
                if (!metrics5.IsBearish || metrics5.BodySize < MinBodySize) return null;

                // Ensure the fifth candle has a significant range
                if (metrics5.TotalRange <= 0) return null;

                // At least one middle candle must oppose the trend
                bool atLeastOneBullish = metrics2.IsBullish || metrics3.IsBullish || metrics4.IsBullish;

                // Middle candles must be contained within the first candle s range with buffer
                bool containment = ask2.High <= ask1.High + ContainmentBuffer && ask2.Low >= ask1.Low - ContainmentBuffer &&
                                  ask3.High <= ask1.High + ContainmentBuffer && ask3.Low >= ask1.Low - ContainmentBuffer &&
                                  ask4.High <= ask1.High + ContainmentBuffer && ask4.Low >= ask1.Low - ContainmentBuffer;

                // Combine all conditions for bearish case
                bool isPatternValid = atLeastOneBullish && containment &&
                                     ask5.Close <= ask1.Close - CloseDifference &&
                                     metrics5.GetLookbackMeanTrend(5) < -TrendThreshold; // Requires a prior downtrend

                if (!isPatternValid) return null;
            }

            // Define the candle indices for the pattern (five candles)
            var candles = new List<int> { c1, c2, c3, c4, c5 };

            // Return the pattern instance with the specified direction
            return new MatHoldPattern(candles, isBullish);
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
            if (Candles.Count != 5)
                throw new InvalidOperationException("MatHoldPattern must have exactly 5 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2], c4 = Candles[3], c5 = Candles[4];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];
            var metrics4 = metricsCache[c4];
            var metrics5 = metricsCache[c5];

            var ask1 = prices[c1];
            var ask2 = prices[c2];
            var ask3 = prices[c3];
            var ask4 = prices[c4];
            var ask5 = prices[c5];

            // Power Score: Based on body sizes, containment, opposition, continuation, trend
            double firstBodyScore = metrics1.BodySize / MinBodySize;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double middleBodyScore = 1 - ((metrics2.BodySize + metrics3.BodySize + metrics4.BodySize) / (3 * MaxBodySize));
            middleBodyScore = Math.Clamp(middleBodyScore, 0, 1);

            double fifthBodyScore = metrics5.BodySize / MinBodySize;
            fifthBodyScore = Math.Min(fifthBodyScore, 1);

            // Containment score
            double containmentScore = 1.0;
            if (ask2.High > ask1.High + ContainmentBuffer || ask2.Low < ask1.Low - ContainmentBuffer ||
                ask3.High > ask1.High + ContainmentBuffer || ask3.Low < ask1.Low - ContainmentBuffer ||
                ask4.High > ask1.High + ContainmentBuffer || ask4.Low < ask1.Low - ContainmentBuffer)
            {
                containmentScore = 0.5;
            }

            // Opposition score (at least one middle candle opposes trend)
            int oppositionCount = 0;
            if (IsBullish)
            {
                if (metrics2.IsBearish) oppositionCount++;
                if (metrics3.IsBearish) oppositionCount++;
                if (metrics4.IsBearish) oppositionCount++;
            }
            else
            {
                if (metrics2.IsBullish) oppositionCount++;
                if (metrics3.IsBullish) oppositionCount++;
                if (metrics4.IsBullish) oppositionCount++;
            }
            double oppositionScore = oppositionCount > 0 ? 1.0 : 0.5;

            // Continuation score
            double continuationScore = 0;
            if (IsBullish)
            {
                continuationScore = (ask5.Close - ask1.Close) / CloseDifference;
                continuationScore = Math.Min(continuationScore, 1);
            }
            else
            {
                continuationScore = (ask1.Close - ask5.Close) / CloseDifference;
                continuationScore = Math.Min(continuationScore, 1);
            }

            double trendStrength = Math.Abs(metrics5.GetLookbackMeanTrend(5) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.15, wMiddle = 0.15, wFifth = 0.15, wContainment = 0.15, wOpposition = 0.15, wContinuation = 0.15, wTrend = 0.05, wVolume = 0.05;
            double powerScore = (wFirst * firstBodyScore + wMiddle * middleBodyScore + wFifth * fifthBodyScore +
                                 wContainment * containmentScore + wOpposition * oppositionScore + wContinuation * continuationScore +
                                 wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirst + wMiddle + wFifth + wContainment + wOpposition + wContinuation + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double firstDeviation = Math.Abs(metrics1.BodySize - MinBodySize) / MinBodySize;
            double middleDeviation = Math.Abs((metrics2.BodySize + metrics3.BodySize + metrics4.BodySize) / 3 - MaxBodySize) / MaxBodySize;
            double fifthDeviation = Math.Abs(metrics5.BodySize - MinBodySize) / MinBodySize;
            double trendDeviation = Math.Abs(metrics5.GetLookbackMeanTrend(5) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (firstDeviation + middleDeviation + fifthDeviation + trendDeviation) / 4;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








