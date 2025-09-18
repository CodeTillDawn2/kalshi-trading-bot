using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Rising Falling Three Methods candlestick pattern.
    /// </summary>
    public class RisingFallingThreeMethodsPattern : PatternDefinition
    {
        /// <summary>
        /// Minimum body size for the first and fifth candles, ensuring they are significant.
        /// Loosest: 1.0 (small but notable body); Strictest: 2.0 (larger, prominent candle).
        /// </summary>
        public static double MinBodySizeMajor { get; } = 1.5;

        /// <summary>
        /// Maximum body size for the middle three candles, keeping them small relative to the major candles.
        /// Loosest: 2.0 (allows slightly larger consolidation); Strictest: 1.0 (very small bodies).
        /// </summary>
        public static double MaxBodySizeMinor { get; } = 1.5;

        /// <summary>
        /// Maximum proportion of the fifth candle s range that can be wicks, ensuring a strong body.
        /// Loosest: 0.6 (more wick allowed); Strictest: 0.3 (minimal wicks).
        /// </summary>
        public static double WickRangeRatio { get; } = 0.5;

        /// <summary>
        /// Minimum trend strength threshold to confirm the prior trend direction.
        /// Loosest: 0.2 (weak trend); Strictest: 0.5 (strong trend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.3;

        /// <summary>
        /// Buffer factor as a percentage of the first candle s range for middle candle containment.
        /// Loosest: 0.15 (wider range); Strictest: 0.05 (tight containment).
        /// </summary>
        public static double RangeBufferFactor { get; } = 0.1;

        /// <summary>
        /// Minimum buffer size for range containment when the first candle s range is small.
        /// Loosest: 0.3 (smaller buffer); Strictest: 1.0 (larger buffer).
        /// </summary>
        public static double MinRangeBuffer { get; } = 0.5;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "RisingFallingThreeMethods";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + "_" + Direction.ToString();
        /// <summary>
        /// Gets the direction of the pattern.
        /// </summary>
        public override PatternDirection Direction { get; }
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => Direction == PatternDirection.Bullish
            ? "A bullish continuation pattern in an uptrend with a long bullish candle followed by three smaller candles that stay within its range, and a fifth bullish candle that breaks higher."
            : "A bearish continuation pattern in a downtrend with a long bearish candle followed by three smaller candles that stay within its range, and a fifth bearish candle that breaks lower.";
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
        /// Initializes a new instance of the RisingFallingThreeMethodsPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="direction">The direction of the pattern.</param>
        public RisingFallingThreeMethodsPattern(List<int> candles, PatternDirection direction) : base(candles)
        {
            Direction = direction;
        }

        /// <summary>
        /// Identifies Rising/Falling Three Methods, a five-candle continuation pattern.
        /// Requirements (source: Investopedia, TradingView):
        /// - Rising: Occurs in a downtrend; first candle is bullish with a large body, followed by 
        ///   three smaller bearish candles within the first candle s range, and a final bullish candle 
        ///   closing above the first.
        /// - Falling: Occurs in an uptrend; first candle is bearish with a large body, followed by 
        ///   three smaller bullish candles within the first candle s range, and a final bearish candle 
        ///   closing below the first.
        /// Indicates: Continuation of the prior trend (bullish for Rising, bearish for Falling) after a brief consolidation.
        /// </summary>
        public static async Task<RisingFallingThreeMethodsPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            // Early exit if there aren't enough prior candles
            if (index < 4) return null;

            int startIndex = index - 4;
            var candles = new List<int> { startIndex, startIndex + 1, startIndex + 2, startIndex + 3, startIndex + 4 };
            var asks = prices.Skip(startIndex).Take(5).ToArray();

            // Lazy load metrics for all 5 candles
            CandleMetrics metrics1 = await GetCandleMetricsAsync(metricsCache, startIndex, prices, trendLookback, false);
            CandleMetrics metrics2 = await GetCandleMetricsAsync(metricsCache, startIndex + 1, prices, trendLookback, false);
            CandleMetrics metrics3 = await GetCandleMetricsAsync(metricsCache, startIndex + 2, prices, trendLookback, false);
            CandleMetrics metrics4 = await GetCandleMetricsAsync(metricsCache, startIndex + 3, prices, trendLookback, false);
            CandleMetrics metrics5 = await GetCandleMetricsAsync(metricsCache, startIndex + 4, prices, trendLookback, true);

            // Check if first candle has a significant body
            if (metrics1.BodySize < MinBodySizeMajor) return null;

            // Calculate range buffer as in original
            double rangeBuffer = Math.Max(MinRangeBuffer, RangeBufferFactor * metrics1.TotalRange);

            if (metrics1.IsBullish) // Rising
            {
                // Require a downtrend
                if (metrics5.GetLookbackMeanTrend(5) >= -TrendThreshold) return null;

                // Fifth candle: Must be bullish, significant body, close higher
                if (!metrics5.IsBullish ||
                    metrics5.BodySize < MinBodySizeMajor ||
                    asks[4].Close <= asks[0].Close) return null;

                // Fifth candle: Must have small wicks
                if (metrics5.UpperWick > WickRangeRatio * metrics5.TotalRange ||
                    metrics5.LowerWick > WickRangeRatio * metrics5.TotalRange ||
                    metrics5.TotalRange <= 0) return null;

                // Middle candles: Small bodies
                if (metrics2.BodySize > MaxBodySizeMinor ||
                    metrics3.BodySize > MaxBodySizeMinor ||
                    metrics4.BodySize > MaxBodySizeMinor) return null;

                // Count bearish middle candles and those within range
                int bearishCount = (metrics2.IsBearish ? 1 : 0) + (metrics3.IsBearish ? 1 : 0) + (metrics4.IsBearish ? 1 : 0);
                int inRangeCount = (asks[1].High <= asks[0].High + rangeBuffer && asks[1].Low >= asks[0].Low - rangeBuffer ? 1 : 0) +
                                   (asks[2].High <= asks[0].High + rangeBuffer && asks[2].Low >= asks[0].Low - rangeBuffer ? 1 : 0) +
                                   (asks[3].High <= asks[0].High + rangeBuffer && asks[3].Low >= asks[0].Low - rangeBuffer ? 1 : 0);

                if (bearishCount < 2 || inRangeCount < 2) return null;

                // Return the pattern instance if all conditions are met
                return new RisingFallingThreeMethodsPattern(candles, PatternDirection.Bullish);
            }
            else if (metrics1.IsBearish) // Falling
            {
                // Require an uptrend
                if (metrics5.GetLookbackMeanTrend(5) <= TrendThreshold) return null;

                // Fifth candle: Must be bearish, significant body, close lower
                if (!metrics5.IsBearish ||
                    metrics5.BodySize < MinBodySizeMajor ||
                    asks[4].Close >= asks[0].Close) return null;

                // Fifth candle: Must have small wicks
                if (metrics5.UpperWick > WickRangeRatio * metrics5.TotalRange ||
                    metrics5.LowerWick > WickRangeRatio * metrics5.TotalRange ||
                    metrics5.TotalRange <= 0) return null;

                // Middle candles: Small bodies
                if (metrics2.BodySize > MaxBodySizeMinor ||
                    metrics3.BodySize > MaxBodySizeMinor ||
                    metrics4.BodySize > MaxBodySizeMinor) return null;

                // Count bullish middle candles and those within range
                int bullishCount = (metrics2.IsBullish ? 1 : 0) + (metrics3.IsBullish ? 1 : 0) + (metrics4.IsBullish ? 1 : 0);
                int inRangeCount = (asks[1].High <= asks[0].High + rangeBuffer && asks[1].Low >= asks[0].Low - rangeBuffer ? 1 : 0) +
                                   (asks[2].High <= asks[0].High + rangeBuffer && asks[2].Low >= asks[0].Low - rangeBuffer ? 1 : 0) +
                                   (asks[3].High <= asks[0].High + rangeBuffer && asks[3].Low >= asks[0].Low - rangeBuffer ? 1 : 0);

                if (bullishCount < 2 || inRangeCount < 2) return null;

                // Return the pattern instance if all conditions are met
                return new RisingFallingThreeMethodsPattern(candles, PatternDirection.Bearish);
            }
            else
            {
                return null; // Neither rising nor falling
            }
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
                throw new InvalidOperationException("RisingFallingThreeMethodsPattern must have exactly 5 candles.");

            int startIndex = Candles[0];
            var metrics = new CandleMetrics[5];
            var asks = new CandleMids[5];

            for (int i = 0; i < 5; i++)
            {
                metrics[i] = metricsCache[Candles[i]];
                asks[i] = prices[Candles[i]];
            }

            // Power Score: Based on body sizes, containment, wick control, continuation, trend
            double firstBodyScore = metrics[0].BodySize / MinBodySizeMajor;
            firstBodyScore = Math.Min(firstBodyScore, 1);

            double middleBodyScore = 1 - ((metrics[1].BodySize + metrics[2].BodySize + metrics[3].BodySize) / (3 * MaxBodySizeMinor));
            middleBodyScore = Math.Clamp(middleBodyScore, 0, 1);

            double fifthBodyScore = metrics[4].BodySize / MinBodySizeMajor;
            fifthBodyScore = Math.Min(fifthBodyScore, 1);

            double wickScore = 1 - ((metrics[4].UpperWick + metrics[4].LowerWick) / (2 * WickRangeRatio * metrics[4].TotalRange));
            wickScore = Math.Clamp(wickScore, 0, 1);

            double rangeBuffer = Math.Max(MinRangeBuffer, RangeBufferFactor * metrics[0].TotalRange);
            int inRangeCount = 0;
            for (int i = 1; i <= 3; i++)
            {
                if (asks[i].High <= asks[0].High + rangeBuffer && asks[i].Low >= asks[0].Low - rangeBuffer)
                    inRangeCount++;
            }
            double containmentScore = inRangeCount / 3.0;

            double continuationScore = 0;
            if (metrics[0].IsBullish && metrics[4].IsBullish)
            {
                continuationScore = (asks[4].Close - asks[0].Close) / MinBodySizeMajor;
                continuationScore = Math.Min(continuationScore, 1);
            }
            else if (metrics[0].IsBearish && metrics[4].IsBearish)
            {
                continuationScore = (asks[0].Close - asks[4].Close) / MinBodySizeMajor;
                continuationScore = Math.Min(continuationScore, 1);
            }

            double trendStrength = Math.Abs(metrics[4].GetLookbackMeanTrend(5) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wFirst = 0.15, wMiddle = 0.15, wFifth = 0.15, wWick = 0.15, wContainment = 0.15, wContinuation = 0.15, wTrend = 0.05, wVolume = 0.05;
            double powerScore = (wFirst * firstBodyScore + wMiddle * middleBodyScore + wFifth * fifthBodyScore +
                                 wWick * wickScore + wContainment * containmentScore + wContinuation * continuationScore +
                                 wTrend * trendStrength + wVolume * volumeScore) /
                                (wFirst + wMiddle + wFifth + wWick + wContainment + wContinuation + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double firstDeviation = Math.Abs(metrics[0].BodySize - MinBodySizeMajor) / MinBodySizeMajor;
            double middleDeviation = Math.Abs((metrics[1].BodySize + metrics[2].BodySize + metrics[3].BodySize) / 3 - MaxBodySizeMinor) / MaxBodySizeMinor;
            double fifthDeviation = Math.Abs(metrics[4].BodySize - MinBodySizeMajor) / MinBodySizeMajor;
            double wickDeviation = (metrics[4].UpperWick + metrics[4].LowerWick) / (2 * WickRangeRatio * metrics[4].TotalRange);
            double trendDeviation = Math.Abs(metrics[4].GetLookbackMeanTrend(5) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - (firstDeviation + middleDeviation + fifthDeviation + wickDeviation + trendDeviation) / 5;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








