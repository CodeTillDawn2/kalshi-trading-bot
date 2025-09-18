using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Stick Sandwich candlestick pattern.
    /// </summary>
    public class StickSandwichPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for all candles to ensure significant movement.
        /// Strictest: 1.0 (strong candles); Loosest: 0.3 (minimal significance).
        /// </summary>
        public static double MinBodySize { get; set; } = 0.5;

        /// <summary>
        /// Maximum difference between the closes of the first and third candles.
        /// Strictest: 0.1 (nearly identical); Loosest: 1.0 (broader tolerance).
        /// </summary>
        public static double MaxCloseDifference { get; set; } = 0.5;

        /// <summary>
        /// Minimum trend threshold to confirm the preceding trend direction.
        /// Strictest: 0.5 (strong trend); Loosest: 0.1 (weak trend).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.3;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "StickSandwich";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => Direction == PatternDirection.Bullish
            ? "A bullish reversal pattern in a downtrend with three candles where two bearish candles sandwich a bullish candle, and the first and third candles close at nearly the same level."
            : "A bearish reversal pattern in an uptrend with three candles where two bullish candles sandwich a bearish candle, and the first and third candles close at nearly the same level.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction { get; }
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
        /// Initializes a new instance of the StickSandwichPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="direction">The direction of the pattern.</param>
        public StickSandwichPattern(List<int> candles, PatternDirection direction) : base(candles)
        {
            Direction = direction;
        }

        /// <summary>
        /// Identifies a Stick Sandwich pattern, a three-candle reversal pattern.
        /// Bullish: Occurs in a downtrend; two bearish candles sandwich a bullish candle, with c1 and c3 closes nearly equal.
        /// Bearish: Occurs in an uptrend; two bullish candles sandwich a bearish candle, with c1 and c3 closes nearly equal.
        /// Indicates a potential reversal as the middle candle s move is rejected.
        /// Source: https://www.tradingview.com/education/stick-sandwich/
        /// </summary>
        /// <summary>
        /// Determines if a Stick Sandwich pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="direction">The direction of the pattern to check for.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<StickSandwichPattern?> IsPatternAsync(
        int index,
        PatternDirection direction,
        CandleMids[] prices,
        Dictionary<int, CandleMetrics> metricsCache,
        int trendLookback)
        {
            if (index < 2) return null; // Need 3 candles

            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            double body1 = metrics1.BodySize;
            double body2 = metrics2.BodySize;
            double body3 = metrics3.BodySize;

            if (direction == PatternDirection.Bullish)
            {
                // Directions
                if (!metrics1.IsBearish || !metrics2.IsBullish || !metrics3.IsBearish) return null;

                // Closes of c1 and c3 nearly equal (within tolerance)
                bool closesMatch = Math.Abs(prices[c3].Close - prices[c1].Close) <= MaxCloseDifference;
                if (!closesMatch) return null;

                // Second candle exceeds first candle s range
                if (prices[c2].High < prices[c1].High || prices[c2].Low > prices[c1].Low) return null;

                // Significant bodies
                if (body1 < MinBodySize || body2 < MinBodySize || body3 < MinBodySize) return null;

                // Downtrend check using LookbackMeanTrend (3 candles)
                if (metrics3.GetLookbackMeanTrend(3) > -TrendThreshold) return null;
            }
            else // Bearish
            {
                // Directions
                if (!metrics1.IsBullish || !metrics2.IsBearish || !metrics3.IsBullish) return null;

                // Closes of c1 and c3 nearly equal (within tolerance)
                bool closesMatch = Math.Abs(prices[c3].Close - prices[c1].Close) <= MaxCloseDifference;
                if (!closesMatch) return null;

                // Second candle exceeds first candle s range
                if (prices[c2].High < prices[c1].High || prices[c2].Low > prices[c1].Low) return null;

                // Significant bodies
                if (body1 < MinBodySize || body2 < MinBodySize || body3 < MinBodySize) return null;

                // Uptrend check using LookbackMeanTrend (3 candles)
                if (metrics3.GetLookbackMeanTrend(3) < TrendThreshold) return null;
            }

            var candles = new List<int> { c1, c2, c3 };
            return new StickSandwichPattern(candles, direction);
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
                throw new InvalidOperationException("StickSandwichPattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on body sizes, close matching, range exceeding, trend
            double bodyScore = (metrics1.BodySize + metrics2.BodySize + metrics3.BodySize) / (3 * MinBodySize);
            bodyScore = Math.Min(bodyScore, 1);

            double closeMatchScore = 1 - (Math.Abs(prices3.Close - prices1.Close) / MaxCloseDifference);
            closeMatchScore = Math.Clamp(closeMatchScore, 0, 1);

            double rangeExceedScore = 0;
            if (prices2.High >= prices1.High && prices2.Low <= prices1.Low)
            {
                rangeExceedScore = 1.0;
            }
            else
            {
                // Partial exceeding
                double highExceed = Math.Max(0, prices2.High - prices1.High);
                double lowExceed = Math.Max(0, prices1.Low - prices2.Low);
                double totalRange = prices1.High - prices1.Low;
                rangeExceedScore = (highExceed + lowExceed) / (totalRange * 2);
                rangeExceedScore = Math.Min(rangeExceedScore, 1);
            }

            double trendStrength = Math.Abs(metrics3.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wBody = 0.25, wCloseMatch = 0.25, wRangeExceed = 0.25, wTrend = 0.15, wVolume = 0.1;
            double powerScore = (wBody * bodyScore + wCloseMatch * closeMatchScore + wRangeExceed * rangeExceedScore +
                                 wTrend * trendStrength + wVolume * volumeScore) /
                                (wBody + wCloseMatch + wRangeExceed + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(metrics1.BodySize - MinBodySize) / MinBodySize;
            double closeDeviation = Math.Abs(prices3.Close - prices1.Close) / MaxCloseDifference;
            double trendDeviation = Math.Abs(metrics3.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (bodyDeviation + closeDeviation + trendDeviation) / 3;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








