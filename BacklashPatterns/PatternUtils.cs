using BacklashDTOs;
using static BacklashPatterns.TrendCalcs;
using System.Threading.Tasks;
using System.Diagnostics;

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
        private static int _totalCalculations = 0;
        private static long _totalCalculationTimeMs = 0;
        private static int _cacheHits = 0;
        private static int _cacheMisses = 0;
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
            var config = GetConfig();
            var stopwatch = Stopwatch.StartNew();

            if (!metricsCache.TryGetValue(index, out CandleMetrics metrics))
            {
                var candle = prices[index];
                int periodCount = config.LookbackPeriodCount;
                double[] meanTrend = new double[periodCount];
                double[] lookbackAvgRange = new double[periodCount];
                double[] trendConsistency = new double[periodCount];
                double[] AvgVolumeVsLookback = new double[periodCount];
                double[] bullishRatio = new double[periodCount];
                double[] bearishRatio = new double[periodCount];
                if (loadLookbackMetrics)
                {
                    for (int i = 0; i < periodCount; i++)
                    {
                        int patternSize = config.LookbackPeriods[i];
                        if (index >= patternSize + 1)
                        {
                            trendConsistency[i] = CalculateTrendConsistencyRatio(index, trendLookback, prices, patternSize);
                            lookbackAvgRange[i] = CalculateAverageRangeOverLookbackPeriod(index, trendLookback, prices, patternSize);
                            meanTrend[i] = CalculateAverageTrendOverLookbackPeriod(prices, index, trendLookback, patternSize);
                            AvgVolumeVsLookback[i] = CalculateVolumeRatioToHistoricalAverage(prices, index, trendLookback, patternSize);
                            bullishRatio[i] = CalculateTrendDirectionRatio(index, trendLookback, prices, patternSize, true);
                            bearishRatio[i] = CalculateTrendDirectionRatio(index, trendLookback, prices, patternSize, false);
                        }
                    }
                }

                // Calculate intervalType based on timestamp difference
                int intervalType = 0; // Default value
                if (index > 0) // Ensure there s a previous candle to compare
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
                    CalculationTimeMs = stopwatch.ElapsedMilliseconds
                };
                metricsCache[index] = metrics;
                Interlocked.Increment(ref _cacheMisses);
            }
            else
            {
                Interlocked.Increment(ref _cacheHits);
            }

            Interlocked.Increment(ref _totalCalculations);
            Interlocked.Add(ref _totalCalculationTimeMs, (long)stopwatch.ElapsedMilliseconds);

            return metrics;
        }

        /// <summary>
        /// Computes comprehensive metrics for a specific candle at the given index, utilizing caching for performance.
        /// This method aggregates basic candle properties (body size, wicks, range) with advanced trend analysis
        /// including mean trends, consistency measures, volume ratios, and directional ratios across multiple
        /// lookback periods. The metrics are cached to avoid redundant calculations during pattern scanning.
        /// Uses async calculations for better performance in high-volume scenarios.
        /// </summary>
        /// <param name="metricsCache">Reference to the cache dictionary for storing computed metrics by index.</param>
        /// <param name="index">The index of the candle in the prices array to analyze.</param>
        /// <param name="prices">Array of candle data containing OHLC and volume information.</param>
        /// <param name="trendLookback">Number of candles to look back for trend calculations.</param>
        /// <param name="loadLookbackMetrics">Whether to compute expensive lookback-based metrics or skip them for performance.</param>
        /// <returns>A CandleMetrics struct containing all computed metrics for the specified candle.</returns>
        public static async Task<CandleMetrics> GetCandleMetricsAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            int index,
            CandleMids[] prices,
            int trendLookback,
            bool loadLookbackMetrics)
        {
            var config = GetConfig();
            var stopwatch = Stopwatch.StartNew();

            if (!metricsCache.TryGetValue(index, out CandleMetrics metrics))
            {
                var candle = prices[index];
                int periodCount = config.LookbackPeriodCount;
                double[] meanTrend = new double[periodCount];
                double[] lookbackAvgRange = new double[periodCount];
                double[] trendConsistency = new double[periodCount];
                double[] AvgVolumeVsLookback = new double[periodCount];
                double[] bullishRatio = new double[periodCount];
                double[] bearishRatio = new double[periodCount];
                if (loadLookbackMetrics)
                {
                    for (int i = 0; i < periodCount; i++)
                    {
                        int patternSize = config.LookbackPeriods[i];
                        if (index >= patternSize + 1)
                        {
                            trendConsistency[i] = await CalculateTrendConsistencyRatioAsync(index, trendLookback, prices, patternSize);
                            lookbackAvgRange[i] = await CalculateAverageRangeOverLookbackPeriodAsync(index, trendLookback, prices, patternSize);
                            meanTrend[i] = await CalculateAverageTrendOverLookbackPeriodAsync(prices, index, trendLookback, patternSize);
                            AvgVolumeVsLookback[i] = await CalculateVolumeRatioToHistoricalAverageAsync(prices, index, trendLookback, patternSize);
                            bullishRatio[i] = await CalculateTrendDirectionRatioAsync(index, trendLookback, prices, patternSize, true);
                            bearishRatio[i] = await CalculateTrendDirectionRatioAsync(index, trendLookback, prices, patternSize, false);
                        }
                    }
                }

                // Calculate intervalType based on timestamp difference
                int intervalType = 0; // Default value
                if (index > 0) // Ensure there s a previous candle to compare
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
                    CalculationTimeMs = stopwatch.ElapsedMilliseconds
                };
                metricsCache[index] = metrics;
                Interlocked.Increment(ref _cacheMisses);
            }
            else
            {
                Interlocked.Increment(ref _cacheHits);
            }

            Interlocked.Increment(ref _totalCalculations);
            Interlocked.Add(ref _totalCalculationTimeMs, (long)stopwatch.ElapsedMilliseconds);

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
        public static bool? IsPatternSignificant(CandleMids current, CandleMids? previous)
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

        /// <summary>
        /// Gets current performance metrics for the PatternUtils class.
        /// Returns cache hit/miss statistics and calculation timing information.
        /// </summary>
        /// <returns>A tuple containing total calculations, cache hits, cache misses, and average calculation time in milliseconds.</returns>
        public static (int TotalCalculations, int CacheHits, int CacheMisses, double AverageCalculationTimeMs) GetPerformanceMetrics()
        {
            var total = _totalCalculations;
            var hits = _cacheHits;
            var misses = _cacheMisses;
            var avgTime = total > 0 ? (double)_totalCalculationTimeMs / total : 0.0;
            return (total, hits, misses, avgTime);
        }

        /// <summary>
        /// Gets the cache hit rate as a percentage.
        /// </summary>
        /// <returns>The cache hit rate (0.0 to 100.0).</returns>
        public static double GetCacheHitRate()
        {
            var (total, hits, _, _) = GetPerformanceMetrics();
            return total > 0 ? (double)hits / total * 100.0 : 0.0;
        }


    }
}








