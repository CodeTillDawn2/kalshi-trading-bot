using BacklashDTOs;
using static BacklashPatterns.TrendCalcs;

namespace BacklashPatterns
{
    /// <summary>
    /// Provides utility methods for calculating comprehensive metrics for individual candles in candlestick pattern analysis.
    /// This class serves as the core computation engine for the pattern recognition system, aggregating data from
    /// multiple lookback periods and trend calculations to provide rich context for pattern detection algorithms.
    /// It integrates with the TrendCalcs static class to compute various technical indicators and market metrics.
    /// </summary>
    public static class PatternUtils
    {
        /// <summary>
        /// Computes comprehensive metrics for a specific candle at the given index, utilizing caching for performance.
        /// This method aggregates basic candle properties (body size, wicks, range) with advanced trend analysis
        /// including mean trends, consistency measures, volume ratios, and directional ratios across multiple
        /// lookback periods. The metrics are cached to avoid redundant calculations during pattern scanning.
        /// </summary>
        /// <param name="metricsCache">Reference to the cache dictionary for storing computed metrics by index.</param>
        /// <param name="index">The index of the candle in the prices array to analyze.</param>
        /// <param name="prices">Array of candle data containing OHLC and volume information.</param>
        /// <param name="trendLookback">Number of candles to look back for trend calculations.</param>
        /// <param name="loadLookbackMetrics">Whether to compute expensive lookback-based metrics or skip them for performance.</param>
        /// <returns>A CandleMetrics struct containing all computed metrics for the specified candle.</returns>
        public static CandleMetrics GetCandleMetrics(
            ref Dictionary<int, CandleMetrics> metricsCache,
            int index,
            CandleMids[] prices,
            int trendLookback,
            bool loadLookbackMetrics)
        {
            if (!metricsCache.TryGetValue(index, out CandleMetrics metrics))
            {
                var candle = prices[index];
                double[] meanTrend = new double[5];
                double[] lookbackAvgRange = new double[5];
                double[] trendConsistency = new double[5];
                double[] AvgVolumeVsLookback = new double[5];
                double[] bullishRatio = new double[5];
                double[] bearishRatio = new double[5];
                if (loadLookbackMetrics)
                {
                    if (index >= 2) trendConsistency[0] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 1);
                    if (index >= 3) trendConsistency[1] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 2);
                    if (index >= 4) trendConsistency[2] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 3);
                    if (index >= 5) trendConsistency[3] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 4);
                    if (index >= 6) trendConsistency[4] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 5);
                    if (index >= 2) lookbackAvgRange[0] = CalculateLookbackAvgRange(index, trendLookback, prices, 1);
                    if (index >= 3) lookbackAvgRange[1] = CalculateLookbackAvgRange(index, trendLookback, prices, 2);
                    if (index >= 4) lookbackAvgRange[2] = CalculateLookbackAvgRange(index, trendLookback, prices, 3);
                    if (index >= 5) lookbackAvgRange[3] = CalculateLookbackAvgRange(index, trendLookback, prices, 4);
                    if (index >= 6) lookbackAvgRange[4] = CalculateLookbackAvgRange(index, trendLookback, prices, 5);
                    if (index >= 2) meanTrend[0] = CalculateLookbackMeanTrend(prices, index, trendLookback, 1);
                    if (index >= 3) meanTrend[1] = CalculateLookbackMeanTrend(prices, index, trendLookback, 2);
                    if (index >= 4) meanTrend[2] = CalculateLookbackMeanTrend(prices, index, trendLookback, 3);
                    if (index >= 5) meanTrend[3] = CalculateLookbackMeanTrend(prices, index, trendLookback, 4);
                    if (index >= 6) meanTrend[4] = CalculateLookbackMeanTrend(prices, index, trendLookback, 5);
                    if (index >= 2) AvgVolumeVsLookback[0] = CalculateAverageVolume(prices, index, trendLookback, 1);
                    if (index >= 3) AvgVolumeVsLookback[1] = CalculateAverageVolume(prices, index, trendLookback, 2);
                    if (index >= 4) AvgVolumeVsLookback[2] = CalculateAverageVolume(prices, index, trendLookback, 3);
                    if (index >= 5) AvgVolumeVsLookback[3] = CalculateAverageVolume(prices, index, trendLookback, 4);
                    if (index >= 6) AvgVolumeVsLookback[4] = CalculateAverageVolume(prices, index, trendLookback, 5);
                    if (index >= 2) bullishRatio[0] = CalculateTrendDirectionRatio(index, trendLookback, prices, 1, true);
                    if (index >= 3) bullishRatio[1] = CalculateTrendDirectionRatio(index, trendLookback, prices, 2, true);
                    if (index >= 4) bullishRatio[2] = CalculateTrendDirectionRatio(index, trendLookback, prices, 3, true);
                    if (index >= 5) bullishRatio[3] = CalculateTrendDirectionRatio(index, trendLookback, prices, 4, true);
                    if (index >= 6) bullishRatio[4] = CalculateTrendDirectionRatio(index, trendLookback, prices, 5, true);
                    if (index >= 2) bearishRatio[0] = CalculateTrendDirectionRatio(index, trendLookback, prices, 1, false);
                    if (index >= 3) bearishRatio[1] = CalculateTrendDirectionRatio(index, trendLookback, prices, 2, false);
                    if (index >= 4) bearishRatio[2] = CalculateTrendDirectionRatio(index, trendLookback, prices, 3, false);
                    if (index >= 5) bearishRatio[3] = CalculateTrendDirectionRatio(index, trendLookback, prices, 4, false);
                    if (index >= 6) bearishRatio[4] = CalculateTrendDirectionRatio(index, trendLookback, prices, 5, false);
                }

                // Calculate intervalType based on timestamp difference
                int intervalType = 0; // Default value
                if (index > 0) // Ensure there�s a previous candle to compare
                {
                    double timeDiff = (prices[index].Timestamp - prices[index - 1].Timestamp).TotalSeconds;
                    // Assuming Timestamp is in milliseconds (adjust if it's in seconds or another unit)
                    if (timeDiff == 60 * 1000) // 1 minute in milliseconds
                        intervalType = 1;
                    else if (timeDiff == 60 * 60 * 1000) // 1 hour in milliseconds
                        intervalType = 2;
                    else if (timeDiff == 24 * 60 * 60 * 1000) // 1 day in milliseconds
                        intervalType = 3;
                }

                metrics = new CandleMetrics
                {
                    BodySize = Math.Abs(candle.Close - candle.Open),
                    UpperWick = candle.High - Math.Max(candle.Open, candle.Close),
                    LowerWick = Math.Min(candle.Open, candle.Close) - candle.Low,
                    TotalRange = candle.High - candle.Low,
                    BodyToRangeRatio = candle.High != candle.Low ? Math.Abs(candle.Close - candle.Open) / (candle.High - candle.Low) : 0,
                    MidPoint = (candle.Open + candle.Close) / 2.0,
                    HasNoUpperWick = candle.High == Math.Max(candle.Open, candle.Close),
                    HasNoLowerWick = candle.Low == Math.Min(candle.Open, candle.Close),
                    BodyMidPoint = (candle.Open + candle.Close) / 2.0,
                    LookbackMeanTrend = meanTrend,
                    LookbackAvgRange = lookbackAvgRange,
                    LookbackTrendConsistency = trendConsistency,
                    AvgVolumeVsLookback = AvgVolumeVsLookback,
                    BullishRatio = bullishRatio,
                    BearishRatio = bearishRatio,
                    IntervalType = intervalType,
                    IsBullish = candle.Close > candle.Open,
                    IsBearish = candle.Open > candle.Close,
                };
                metricsCache[index] = metrics;
            }

            return metrics;
        }


        /// <summary>
        /// Determines whether a candle represents a significant price movement that warrants pattern analysis.
        /// This method filters out minor price fluctuations to focus computational resources on meaningful market events.
        /// Significance is determined by either substantial price change, high volatility, or notable body size.
        /// </summary>
        /// <param name="current">The current candle to evaluate for significance.</param>
        /// <param name="previous">The previous candle for comparison (null for the first candle, which is always considered significant).</param>
        /// <returns>True if the candle shows significant movement, false otherwise.</returns>
        public static bool IsPatternSignificant(CandleMids current, CandleMids? previous)
        {
            if (previous == null) return true; // Always process the first candle

            double currentMidpoint = current.Close;
            double previousMidpoint = previous.Close;

            return Math.Abs(currentMidpoint - previousMidpoint) >= 0.5 || // Moderate price movement
               current.High - current.Low >= 1.0 || // Significant volatility
               Math.Abs(currentMidpoint - current.Open) >= 0.5; // Notable body size
        }



        /// <summary>
        /// Parses a timestamp string in ISO 8601 format and converts it to a DateTime object with UTC kind.
        /// This method is used for processing timestamp data from external sources, ensuring consistent
        /// UTC representation for time-based calculations and comparisons in the trading system.
        /// </summary>
        /// <param name="timestamp">The timestamp string in "yyyy-MM-dd'T'HH:mm:ss'Z'" format.</param>
        /// <returns>A DateTime object representing the parsed timestamp in UTC.</returns>
        public static DateTime GetBreakpointTimestamp(string timestamp)
        {
            // Parse the exact ISO 8601 format and enforce UTC
            return DateTime.ParseExact(timestamp, "yyyy-MM-dd'T'HH:mm:ss'Z'", null, System.Globalization.DateTimeStyles.RoundtripKind);
        }


    }
}







