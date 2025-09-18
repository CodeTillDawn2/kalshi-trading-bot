using BacklashDTOs;
using System.Diagnostics;
using static BacklashPatterns.PatternUtils;
using static BacklashPatterns.TrendCalcs;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Belt Hold candlestick pattern, a single-candle reversal pattern.
    /// Bullish: Indicates a potential reversal from a downtrend to an uptrend.
    /// Bearish: Indicates a potential reversal from an uptrend to a downtrend.
    /// Requirements (Source: BabyPips, TradingView, Investopedia, with refinements):
    /// - Single candle with a large body dominating the range.
    /// - Minimal wick on the opening side (lower for bullish, upper for bearish).
    /// - Occurs after a clear, consistent prior trend (downtrend for bullish, uptrend for bearish).
    /// - Significant reversal relative to the prior trend and recent volatility.
    /// Optimized for: A 0-100 fixed-range market with loose thresholds to maximize detection.
    /// </summary>
    public class BeltHoldPattern : PatternDefinition
    {
        /// <summary>
        /// Number of candles required to form the pattern.
        /// Purpose: Defines the fixed size of the Belt Hold pattern (single candle).
        /// Default: 1
        /// </summary>
        public static int PatternSize { get; } = 1;

        /// <summary>
        /// Minimum ratio of body size to total range for the candle.
        /// Purpose: Ensures the candle has a dominant body relative to its range.
        /// Default: 0.4 (40%, very loose to include smaller bodies)
        /// Range: 0.4-1.0 (0.4 for minimal dominance, 1.0 for pure Marubozu-like patterns)
        /// </summary>
        public static double BodyRangeRatio { get; set; } = 0.4;

        /// <summary>
        /// Maximum ratio of the critical wick (lower for bullish, upper for bearish) to body size.
        /// Purpose: Ensures minimal wick on the opening side, scaled to the candle s body.
        /// Default: 0.6 (60% of body size, loose to allow noticeable wicks)
        /// Range: 0.0 0.6 (0.0 for no wick, 0.6 for moderate wick relative to body)
        /// </summary>
        public static double CriticalWickToBodyMax { get; set; } = 0.6;

        /// <summary>
        /// Minimum composite trend score to validate the prior trend.
        /// Purpose: Combines trend strength, consistency, and momentum to confirm a prior trend.
        /// Default: 0.4 (very loose, below half the maximum score)
        /// Range: 0.4 1.5 (0.4 for weak trends, 1.5 for pronounced, consistent trends)
        /// </summary>
        public static double MinTrendScore { get; set; } = 0.3;

        /// <summary>
        /// Minimum ratio of body size to cumulative trend change.
        /// Purpose: Ensures the reversal is significant relative to the prior trend s directional movement.
        /// Default: 0.2 (20%, loose to include subtle reversals)
        /// Range: 0.2 1.0 (0.2 for minimal significance, 1.0 for strong reversals)
        /// </summary>
        public static double MinReversalRatio { get; set; } = 0.2;

        /// <summary>
        /// Minimum body size relative to the Average True Range (ATR).
        /// Purpose: Ensures the candle is significant compared to recent volatility.
        /// Default: 0.3 (30% of ATR, loose to allow small moves)
        /// Range: 0.3 2.0 (0.3 for minimal significance, 2.0 for highly volatile moves)
        /// </summary>
        public static double MinBodyToATRRRatio { get; set; } = 0.3;

        /// <summary>
        /// Minimum position suitability score for the candle s close relative to the recent range.
        /// Purpose: Ensures bullish patterns close near the bottom and bearish near the top.
        /// Calculation: Bullish = 1 - (Close - Low) / Range; Bearish = (Close - Low) / Range.
        /// Default: 0.4 (loose, allows flexibility in position)
        /// Range: 0.0 1.0 (0.0 for no restriction, 1.0 for extreme position only)
        /// </summary>
        /// Lowered to include true positives
        public static double MinPositionSuitability { get; set; } = 0.2;

        /// <summary>
        /// Weight for trend strength in the composite trend score.
        /// Purpose: Adjusts the influence of normalized trend movement (net change / StdDev).
        /// Default: 0.4 (balanced contribution)
        /// Range: 0.0 1.0 (0.0 to ignore, 1.0 to dominate)
        /// </summary>
        public static double TrendStrengthWeight { get; set; } = 0.7;

        /// <summary>
        /// Weight for trend consistency in the composite trend score.
        /// Purpose: Adjusts the influence of directional consistency.
        /// Default: 0.4 (balanced contribution)
        /// Range: 0.0 1.0 (0.0 to ignore, 1.0 to dominate)
        /// </summary>
        public static double TrendConsistencyWeight { get; set; } = 0.3;

        /// <summary>
        /// Weight for trend momentum in the composite trend score.
        /// Purpose: Adjusts the influence of regression slope of midpoints.
        /// Default: 0.2 (lesser contribution)
        /// Range: 0.0 1.0 (0.0 to ignore, 1.0 to dominate)
        /// </summary>
        public static double TrendMomentumWeight { get; set; } = 0.0;

/// <summary>Gets or sets the BaseName.</summary>
/// <summary>Gets or sets the BaseName.</summary>
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
        public const string BaseName = "BeltHold";

        /// <summary>
        /// Gets the name of the pattern, appending "_Bullish" or "_Bearish" based on direction.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => Direction == PatternDirection.Bullish
            ? "A bullish reversal pattern with a single candle that opens near its low and closes near its high, with minimal upper wick. Signals potential reversal from downtrend to uptrend."
            : "A bearish reversal pattern with a single candle that opens near its high and closes near its low, with minimal lower wick. Signals potential reversal from uptrend to downtrend.";
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction { get; }

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
        /// Initializes a new instance of the BeltHoldPattern class.
        /// </summary>
        /// <param name="candles">List of candle indices forming the pattern (single candle).</param>
        /// <param name="direction">The direction of the pattern.</param>
        public BeltHoldPattern(List<int> candles, PatternDirection direction) : base(candles)
        {
            Direction = direction;
            Strength = 0.0;
            Certainty = 0.0;
            Uncertainty = 1.0;
        }

        /// <summary>
        /// Determines if a Belt Hold pattern exists at the specified index.
        /// Purpose: Evaluates candle shape, prior trend, reversal significance, and volatility context
        /// to identify potential Belt Hold patterns with loose thresholds for maximum detection.
        /// Optimized for: A 0 100 fixed-range market, using a unified position suitability metric.
        /// </summary>
        /// <param name="index">The current candle index to evaluate.</param>
        /// <param name="trendLookback">Number of candles to look back for trend analysis (e.g., 15 for minutes, 5 for hours/days).</param>
        /// <param name="prices">Array of candle data.</param>
        /// <param name="metricsCache">Cache of precomputed candle metrics.</param>
        /// <param name="direction">The direction of the pattern to check for.</param>
        /// <returns>A BeltHoldPattern instance if the pattern is detected, otherwise null.</returns>
        public static async Task<BeltHoldPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            PatternDirection direction)
        {
            if (index < (PatternSize - 1) + trendLookback) return null;
            CandleMids currentPrices = prices[index];

            CandleMetrics currentMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
            var candles = new List<int> { index };

            // Step 1: Shape and Direction
            if (currentMetrics.TotalRange <= 0) return null;
            double bodySize = currentMetrics.BodySize;
            if (bodySize / currentMetrics.TotalRange < BodyRangeRatio) return null;
            if (direction == PatternDirection.Bullish && !currentMetrics.IsBullish || direction == PatternDirection.Bearish && !currentMetrics.IsBearish) return null;
            double criticalWick = direction == PatternDirection.Bullish ? (currentPrices.Open - currentPrices.Low) : (currentPrices.High - currentPrices.Open);
            if (criticalWick / bodySize > CriticalWickToBodyMax) return null;

            // Step 2: Trend Validation
            int lookbackStart = Math.Max(0, index - trendLookback);
            int lookbackCount = index - lookbackStart;
            double[] changes = new double[lookbackCount];
            double[] midpoints = new double[lookbackCount];
            double directionalChangeSum = 0.0;
            double highestHigh = double.MinValue, lowestLow = double.MaxValue;
            int trendDirectionCount = 0;

            for (int i = lookbackStart; i < index; i++)
            {
                int k = i - lookbackStart;
                changes[k] = prices[i].Close - prices[i].Open;
                midpoints[k] = (prices[i].Open + prices[i].Close) / 2;
                bool isTrendDirection = direction == PatternDirection.Bullish ? changes[k] < 0 : changes[k] > 0;
                if (isTrendDirection)
                {
                    directionalChangeSum += Math.Abs(changes[k]);
                    trendDirectionCount++;
                }
                highestHigh = Math.Max(highestHigh, prices[i].High);
                lowestLow = Math.Min(lowestLow, prices[i].Low);
            }

            double meanChange = changes.Average();
            double variance = changes.Sum(c => Math.Pow(c - meanChange, 2)) / lookbackCount;
            double stdDev = Math.Sqrt(variance);
            double totalTrendChange = CalculatePriorTrendStrength(index, trendLookback, prices, PatternSize);
            double trendStrength = stdDev > 0 ? Math.Abs(totalTrendChange) / stdDev : 0.0;
            double trendConsistency = (double)trendDirectionCount / lookbackCount;

            double xMean = (lookbackCount - 1) / 2.0;
            double yMean = midpoints.Average();
            double numerator = 0.0, denominator = 0.0;
            for (int i = 0; i < lookbackCount; i++)
            {
                double xDiff = i - xMean;
                numerator += xDiff * (midpoints[i] - yMean);
                denominator += xDiff * xDiff;
            }
            double momentum = denominator > 0 ? (numerator / denominator) : 0.0; // Removed * 100
            double trendScore = (
                trendStrength * TrendStrengthWeight +
                trendConsistency * TrendConsistencyWeight +
                (isBullish ? -momentum : momentum) * TrendMomentumWeight
            );
            bool hasValidTrend = isBullish ? totalTrendChange < 0 : totalTrendChange > 0;
            if (!hasValidTrend || trendScore < MinTrendScore) return null;

            // Step 3: Reversal Significance
            double reversalRatio = directionalChangeSum > 0 ? bodySize / directionalChangeSum : 0.0;
            if (reversalRatio < MinReversalRatio) return null;

            double relativePosition = (currentPrices.Close - lowestLow) / (highestHigh - lowestLow);
            double positionSuitability = isBullish ? relativePosition : (1 - relativePosition);
            if (positionSuitability < MinPositionSuitability) return null;

            // Step 4: Volatility Check (ATR)
            double atr = 0.0;
            for (int i = lookbackStart; i < index; i++)
            {
                double tr = Math.Max(prices[i].High - prices[i].Low,
                    Math.Max(Math.Abs(prices[i].High - (i > 0 ? prices[i - 1].Close : prices[i].Open)),
                             Math.Abs((i > 0 ? prices[i - 1].Close : prices[i].Open) - prices[i].Low)));
                atr += tr;
            }
            atr /= lookbackCount;
            if (bodySize < MinBodyToATRRRatio * atr) return null;

            return new BeltHoldPattern(candles, isBullish);
        }

        /// <summary>
        /// Calculates the strength of the pattern using historical cache for comparison.
        /// </summary>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="avgVolume">The average volume (not used in BeltHold).</param>
        /// <param name="historicalCache">The historical pattern cache.</param>
        public void CalculateStrength(
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            double avgVolume,
            HistoricalPatternCache historicalCache)
        {
            if (Candles.Count != 1)
                throw new InvalidOperationException("BeltHoldPattern must have exactly 1 candle.");

            int index = Candles[0];
            var currentMetrics = metricsCache[index];
            var currentPrices = prices[index];

            // Power Score: Based on body dominance, wick minimization, trend, reversal, position, ATR
            double bodyDominance = currentMetrics.BodySize / currentMetrics.TotalRange;
            double criticalWick = Direction == PatternDirection.Bullish ? (currentPrices.Open - currentPrices.Low) : (currentPrices.High - currentPrices.Open);
            double wickScore = Math.Max(1 - (criticalWick / currentMetrics.BodySize), 0);

            // Trend score (simplified, as full calculation is complex)
            double trendScore = 0.5; // Placeholder, could compute from metrics if available

            // Reversal ratio (simplified)
            double reversalRatio = 0.5; // Placeholder

            // Position suitability
            double positionSuitability = 0.5; // Placeholder

            // ATR ratio
            double atrRatio = 0.5; // Placeholder

            double wBody = 0.3, wWick = 0.2, wTrend = 0.2, wReversal = 0.1, wPosition = 0.1, wATR = 0.1;
            double powerScore = (wBody * bodyDominance + wWick * wickScore + wTrend * trendScore +
                                 wReversal * reversalRatio + wPosition * positionSuitability + wATR * atrRatio) /
                                (wBody + wWick + wTrend + wReversal + wPosition + wATR);

            // Match Score: Deviation from thresholds
            double bodyDeviation = Math.Abs(bodyDominance - BodyRangeRatio) / BodyRangeRatio;
            double wickDeviation = Math.Abs((criticalWick / currentMetrics.BodySize) - CriticalWickToBodyMax) / CriticalWickToBodyMax;
            double matchScore = 1 - (bodyDeviation + wickDeviation) / 2;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}


