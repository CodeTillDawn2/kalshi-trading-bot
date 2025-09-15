using BacklashDTOs;
using static BacklashPatterns.TrendCalcs;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;

namespace BacklashPatterns
{
    /// <summary>
    /// Provides utility methods for calculating comprehensive metrics for individual candles in candlestick pattern analysis.
    /// This class serves as the core computation engine for the pattern recognition system, aggregating data from
    /// multiple lookback periods and trend calculations to provide rich context for pattern detection algorithms.
    /// It integrates with the TrendCalcs static class to compute various technical indicators and market metrics.
    /// Performance metrics collection can be controlled via the EnablePerformanceMetrics property.
    /// </summary>
    public static class PatternUtils
    {
        /// <summary>
        /// Configuration keys for PatternUtils performance metrics.
        /// All keys are prefixed with the class name for clear identification.
        /// </summary>
        public static class PerformanceConfigKeys
        {
            /// <summary>
            /// Master switch for all PatternUtils performance metrics.
            /// When disabled, all performance measurements are skipped for maximum runtime performance.
            /// </summary>
            public const string Enabled = "PatternUtils.PerformanceMetrics.Enabled";
        }

        /// <summary>
        /// Master switch for all PatternUtils performance metrics.
        /// When false, all performance measurements are disabled for maximum runtime performance.
        /// </summary>
        public static bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Shared PerformanceMonitor instance for posting metrics.
        /// Can be set by external components to enable centralized metrics collection.
        /// </summary>
        public static object? PerformanceMonitor { get; set; }

        /// <summary>
        /// Applies performance configuration settings from an IConfiguration instance.
        /// This method reads the PatternUtils performance metrics configuration key and applies it.
        /// </summary>
        /// <param name="configuration">The configuration instance to read from.</param>
        public static void ConfigureFromSettings(IConfiguration configuration)
        {
            if (configuration == null) return;

            // Helper method to safely parse boolean configuration values
            bool GetBoolConfig(string key, bool defaultValue)
            {
                var value = configuration[key];
                return !string.IsNullOrEmpty(value) && bool.TryParse(value, out var result) ? result : defaultValue;
            }

            // Single master switch for all PatternUtils performance metrics
            EnablePerformanceMetrics = GetBoolConfig(PerformanceConfigKeys.Enabled, true);
        }

        /// <summary>
        /// Gets the current performance configuration status.
        /// </summary>
        /// <returns>A dictionary containing the configuration key and its current value.</returns>
        public static Dictionary<string, bool> GetPerformanceConfigurationStatus()
        {
            return new Dictionary<string, bool>
            {
                { PerformanceConfigKeys.Enabled, EnablePerformanceMetrics }
            };
        }

        /// <summary>
        /// Validates that the current configuration is consistent and provides recommendations.
        /// </summary>
        /// <returns>A tuple containing validation result and any recommendations.</returns>
        public static (bool IsValid, string[] Recommendations) ValidateConfiguration()
        {
            var recommendations = new List<string>();

            // With single flag configuration, validation is minimal
            // Just ensure the configuration is properly set
            if (!EnablePerformanceMetrics)
            {
                recommendations.Add("Performance metrics are disabled. Enable for monitoring and optimization insights.");
            }

            return (true, recommendations.ToArray());
        }

        private static int _totalCalculations = 0;
        private static long _totalCalculationTimeMs = 0;
        private static int _cacheHits = 0;
        private static int _cacheMisses = 0;

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
            var stopwatch = EnablePerformanceMetrics ? Stopwatch.StartNew() : null;

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
                    CalculationTimeMs = stopwatch?.ElapsedMilliseconds ?? 0
                };
                metricsCache[index] = metrics;
                if (EnablePerformanceMetrics) Interlocked.Increment(ref _cacheMisses);
            }
            else
            {
                if (EnablePerformanceMetrics) Interlocked.Increment(ref _cacheHits);
            }

            if (EnablePerformanceMetrics)
            {
                Interlocked.Increment(ref _totalCalculations);
                Interlocked.Add(ref _totalCalculationTimeMs, (long)(stopwatch?.ElapsedMilliseconds ?? 0));
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
        /// Note: Metrics are only collected when EnablePerformanceMetrics is true.
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

        /// <summary>
        /// Measures throughput by calculating metrics for multiple indices asynchronously and returns calculations per second.
        /// </summary>
        /// <param name="metricsCache">Reference to the cache dictionary for storing computed metrics.</param>
        /// <param name="indices">Array of candle indices to process.</param>
        /// <param name="prices">Array of candle data.</param>
        /// <param name="trendLookback">Number of candles to look back for trend calculations.</param>
        /// <param name="loadLookbackMetrics">Whether to compute expensive lookback-based metrics.</param>
        /// <returns>Calculations per second.</returns>
        public static async Task<double> MeasureThroughputAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            int[] indices,
            CandleMids[] prices,
            int trendLookback,
            bool loadLookbackMetrics)
        {
            if (!EnablePerformanceMetrics)
                return 0.0;

            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<CandleMetrics>>();
            foreach (var index in indices)
            {
                tasks.Add(GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, loadLookbackMetrics));
            }
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            double totalTimeSeconds = stopwatch.Elapsed.TotalSeconds;
            return totalTimeSeconds > 0 ? indices.Length / totalTimeSeconds : 0.0;
        }

        /// <summary>
        /// Tests scalability by measuring throughput with varying data sizes asynchronously.
        /// </summary>
        /// <param name="metricsCache">Reference to the cache dictionary for storing computed metrics.</param>
        /// <param name="basePrices">Base array of candle data to scale.</param>
        /// <param name="sizes">Array of data sizes to test (e.g., number of candles).</param>
        /// <param name="trendLookback">Number of candles to look back for trend calculations.</param>
        /// <param name="loadLookbackMetrics">Whether to compute expensive lookback-based metrics.</param>
        /// <returns>Dictionary mapping data size to throughput (calculations per second).</returns>
        public static async Task<Dictionary<int, double>> TestScalabilityAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] basePrices,
            int[] sizes,
            int trendLookback,
            bool loadLookbackMetrics)
        {
            if (!EnablePerformanceMetrics)
                return new Dictionary<int, double>();

            var results = new Dictionary<int, double>();
            foreach (var size in sizes)
            {
                // Create a subset of prices for the given size
                var prices = basePrices.Take(size).ToArray();
                var indices = Enumerable.Range(0, size).ToArray();
                double throughput = await MeasureThroughputAsync(metricsCache, indices, prices, trendLookback, loadLookbackMetrics);
                results[size] = throughput;
            }

            // Post scalability results to PerformanceMonitor if available
            PostScalabilityToMonitor(results);

            return results;
        }

        /// <summary>
        /// Profiles CPU usage for a batch of metric calculations asynchronously.
        /// </summary>
        /// <param name="metricsCache">Reference to the cache dictionary for storing computed metrics.</param>
        /// <param name="indices">Array of candle indices to process.</param>
        /// <param name="prices">Array of candle data.</param>
        /// <param name="trendLookback">Number of candles to look back for trend calculations.</param>
        /// <param name="loadLookbackMetrics">Whether to compute expensive lookback-based metrics.</param>
        /// <returns>Tuple of total CPU time (ms) and throughput (calculations per second).</returns>
        public static async Task<(double CpuTimeMs, double Throughput)> ProfileCpuUsageAsync(
            Dictionary<int, CandleMetrics> metricsCache,
            int[] indices,
            CandleMids[] prices,
            int trendLookback,
            bool loadLookbackMetrics)
        {
            if (!EnablePerformanceMetrics)
                return (0.0, 0.0);

            var process = Process.GetCurrentProcess();
            TimeSpan startCpuTime = process.TotalProcessorTime;
            var stopwatch = Stopwatch.StartNew();

            var tasks = new List<Task<CandleMetrics>>();
            foreach (var index in indices)
            {
                tasks.Add(GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, loadLookbackMetrics));
            }
            await Task.WhenAll(tasks);

            stopwatch.Stop();
            TimeSpan endCpuTime = process.TotalProcessorTime;
            double cpuTimeMs = (endCpuTime - startCpuTime).TotalMilliseconds;
            double totalTimeSeconds = stopwatch.Elapsed.TotalSeconds;
            double throughput = totalTimeSeconds > 0 ? indices.Length / totalTimeSeconds : 0.0;

            // Post CPU profiling results to PerformanceMonitor if available
            PostCpuProfileToMonitor(cpuTimeMs, throughput, indices.Length);

            return (cpuTimeMs, throughput);
        }

        /// <summary>
        /// Posts current performance metrics to the configured PerformanceMonitor.
        /// </summary>
        /// <remarks>
        /// This method collects all current performance metrics and posts them to the
        /// configured PerformanceMonitor instance if available and metrics are enabled.
        /// </remarks>
        public static void PostMetricsToMonitor()
        {
            if (!EnablePerformanceMetrics || PerformanceMonitor == null) return;

            try
            {
                var (totalCalculations, cacheHits, cacheMisses, avgTime) = GetPerformanceMetrics();
                var configStatus = GetPerformanceConfigurationStatus();

                // Try to call the PerformanceMonitor method dynamically
                var monitorType = PerformanceMonitor.GetType();
                var method = monitorType.GetMethod("RecordPatternUtilsMetrics");
                if (method != null)
                {
                    method.Invoke(PerformanceMonitor, new object[] {
                        totalCalculations,
                        (long)(avgTime * totalCalculations), // Convert back to total time
                        cacheHits,
                        cacheMisses,
                        null, // throughput
                        null, // cpuTimeMs
                        null, // memoryUsage
                        configStatus
                    });
                }
            }
            catch
            {
                // Silently ignore if posting fails
            }
        }

        /// <summary>
        /// Posts scalability test results to the configured PerformanceMonitor.
        /// </summary>
        /// <param name="scalabilityResults">Dictionary mapping data sizes to throughput measurements.</param>
        /// <remarks>
        /// This method posts scalability test results to the configured PerformanceMonitor
        /// if available and metrics are enabled.
        /// </remarks>
        public static void PostScalabilityToMonitor(Dictionary<int, double> scalabilityResults)
        {
            if (!EnablePerformanceMetrics || PerformanceMonitor == null || scalabilityResults == null) return;

            try
            {
                var monitorType = PerformanceMonitor.GetType();
                var method = monitorType.GetMethod("RecordPatternUtilsScalability");
                if (method != null)
                {
                    method.Invoke(PerformanceMonitor, new object[] { scalabilityResults });
                }
            }
            catch
            {
                // Silently ignore if posting fails
            }
        }

        /// <summary>
        /// Posts CPU profiling results to the configured PerformanceMonitor.
        /// </summary>
        /// <param name="cpuTimeMs">CPU time used in milliseconds.</param>
        /// <param name="throughput">Calculations per second achieved.</param>
        /// <param name="dataSize">Size of data processed.</param>
        /// <remarks>
        /// This method posts CPU profiling results to the configured PerformanceMonitor
        /// if available and metrics are enabled.
        /// </remarks>
        public static void PostCpuProfileToMonitor(double cpuTimeMs, double throughput, int dataSize)
        {
            if (!EnablePerformanceMetrics || PerformanceMonitor == null) return;

            try
            {
                var monitorType = PerformanceMonitor.GetType();
                var method = monitorType.GetMethod("RecordPatternUtilsCpuProfile");
                if (method != null)
                {
                    method.Invoke(PerformanceMonitor, new object[] { cpuTimeMs, throughput, dataSize });
                }
            }
            catch
            {
                // Silently ignore if posting fails
            }
        }

        /// <summary>
        /// Automatically posts metrics to monitor when performance metrics are enabled.
        /// This method should be called periodically or at key points in the application lifecycle.
        /// </summary>
        public static void AutoPostMetrics()
        {
            if (EnablePerformanceMetrics)
            {
                PostMetricsToMonitor();
            }
        }


    }
}








