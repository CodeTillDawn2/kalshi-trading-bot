using BacklashDTOs;
using BacklashInterfaces.PerformanceMetrics;
using BacklashPatterns;
using System.Diagnostics;
using TradingStrategies.Configuration;
using TradingStrategies.Extensions;

/// <summary>
/// Represents the comprehensive result of pattern detection including patterns and performance metrics.
/// </summary>
public record PatternDetectionResult(
    List<BacklashPatterns.PatternDefinitions.PatternDefinition> Patterns,
    long? ExecutionTimeMs,
    int? TotalCandlesProcessed = null,
    int? TotalPatternsFound = null,
    Dictionary<string, long>? PatternCheckTimes = null
);

namespace TradingStrategies.Trading.Overseer
{

    /// <summary>
    /// Service responsible for detecting candlestick patterns from market snapshots in the Kalshi trading bot system.
    /// This service acts as a bridge between market data and pattern recognition algorithms, enabling trading
    /// strategies to incorporate technical analysis based on historical price movements.
    ///
    /// The service processes market snapshots containing recent candlestick data, converts them to a format
    /// suitable for pattern analysis, and applies comprehensive pattern detection algorithms to identify
    /// various technical analysis formations including single-candle, multi-candle, and complex patterns.
    ///
    /// Key responsibilities:
    /// - Convert market snapshot candlestick data to pattern analysis format
    /// - Execute pattern detection algorithms across multiple timeframes and pattern types
    /// - Filter and return the most relevant patterns for trading decision making
    /// - Handle edge cases and provide graceful fallbacks for data processing errors
    /// - Collect performance metrics for timing analysis in high-frequency scenarios
    ///
    /// This service is used by the SimulationEngine during backtesting and strategy evaluation to enrich
    /// market analysis with technical pattern recognition capabilities.
    /// </summary>
    /// <remarks>
    /// The pattern detection process involves several steps:
    /// 1. Validation of input data (candlestick availability)
    /// 2. Conversion of PseudoCandlesticks to CandleMids format for analysis
    /// 3. Application of pattern recognition algorithms with trend analysis
    /// 4. Pattern filtering to remove redundancies and prioritize significant formations
    /// 5. Return of filtered patterns for strategy consumption
    ///
    /// Error handling ensures that pattern detection failures don't interrupt the overall trading simulation,
    /// with appropriate logging for debugging and monitoring purposes.
    ///
    /// Performance metrics are collected to monitor detection timing, especially in high-frequency analysis.
    /// Async versions are provided for better performance in high-throughput scenarios.
    /// </remarks>
    public class PatternDetectionService
    {
        private readonly PatternDetectionServiceConfig _config;
        private readonly BacklashInterfaces.PerformanceMetrics.IPerformanceMonitor? _performanceMonitor;


        /// <summary>
        /// Initializes a new instance of the PatternDetectionService with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration for pattern detection parameters.</param>
        /// <param name="performanceMonitor">Optional performance monitor for recording metrics.</param>
        public PatternDetectionService(PatternDetectionServiceConfig config, IPerformanceMonitor? performanceMonitor = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _performanceMonitor = performanceMonitor;
        }

        /// <summary>
        /// Gets or sets whether pattern image generation is enabled.
        /// This setting controls whether visualization images are generated for detected patterns.
        /// </summary>
        public bool EnablePatternImageGeneration { get; set; } = true;

        /// <summary>
        /// Detects candlestick patterns from the provided market snapshot.
        /// Analyzes recent candlestick data to identify various technical analysis patterns
        /// that can inform trading strategy decisions.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing recent candlestick data and market information.</param>
        /// <returns>A comprehensive result containing detected patterns and detailed performance metrics.</returns>
        /// <remarks>
        /// The detection process involves:
        /// - Converting PseudoCandlesticks to CandleMids format for pattern analysis
        /// - Applying comprehensive pattern recognition algorithms across multiple pattern types
        /// - Using the configured lookback window for trend context and pattern validation
        /// - Filtering patterns to return only the most recent and relevant formations
        ///
        /// If no candlestick data is available or pattern detection fails, an empty list is returned
        /// to ensure graceful degradation in the trading simulation pipeline.
        ///
        /// The method handles various edge cases:
        /// - Null or empty candlestick collections
        /// - Pattern detection algorithm failures
        /// - Data conversion errors
        ///
        /// All exceptions are caught and logged to prevent interruption of the simulation process,
        /// with appropriate fallback behavior ensuring system stability.
        ///
        /// Performance metrics are collected and logged for monitoring detection timing.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when snapshot is null (handled internally).</exception>
        /// <exception cref="Exception">Any pattern detection errors are caught and logged internally.</exception>
        public PatternDetectionResult DetectPatterns(MarketSnapshot snapshot)
        {
            // Validate input data availability
            if (snapshot.RecentCandlesticks == null || snapshot.RecentCandlesticks.Count == 0)
            {
                return new PatternDetectionResult(new List<BacklashPatterns.PatternDefinitions.PatternDefinition>(), null);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Convert PseudoCandlesticks to CandleMids format for pattern analysis
                var mids = snapshot.RecentCandlesticks.ToCandleMids(snapshot.MarketTicker ?? string.Empty);

                // Create PatternSearch config from service config
                var patternConfig = new BacklashPatterns.PatternDetectionConfig
                {
                    SignificancePriceThreshold = _config.SignificancePriceThreshold,
                    VolumeIncreaseMultiplier = _config.VolumeIncreaseMultiplier,
                    InitialPatternCapacity = _config.InitialPatternCapacity,
                    EnableParallelProcessing = _config.EnableParallelProcessing,
                    MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism
                };

                // Create metrics collector for detailed performance tracking (conditionally)
                var metrics = _config.EnablePatternDetectionMetrics ? new BacklashPatterns.PatternDetectionMetrics() : null;

                // Execute pattern detection with or without visualization based on setting
                Dictionary<int, List<BacklashPatterns.PatternDefinitions.PatternDefinition>> patterns;
                if (EnablePatternImageGeneration)
                {
                    var visualizationPatterns = PatternSearch.DetectPatternsWithVisualizationAsync(mids, _config.LookbackWindow, patternConfig, metrics, _performanceMonitor, true, 10).GetAwaiter().GetResult();
                    // Convert back to PatternDefinition for compatibility
                    patterns = visualizationPatterns.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Select(v => v.Pattern).ToList()
                    );
                }
                else
                {
                    patterns = PatternSearch.DetectPatternsAsync(mids, _config.LookbackWindow, patternConfig, metrics, _performanceMonitor).GetAwaiter().GetResult();
                }

                // Filter patterns based on configured pattern types if specified
                var filteredPatterns = FilterPatternsByTypes(patterns, _config.PatternTypes);

                // Return the most recent set of detected patterns if any were found
                if (filteredPatterns.Keys.Count > 0)
                {
                    stopwatch.Stop();

                    // Create result with comprehensive metrics
                    PatternDetectionResult result;
                    if (_config.EnablePatternDetectionMetrics && metrics != null)
                    {
                        var metricsSummary = metrics.GetSummary();
                        Console.WriteLine($"Pattern detection completed in {stopwatch.ElapsedMilliseconds} ms for {snapshot.MarketTicker}");
                        Console.WriteLine($"Detailed metrics: Total time {metricsSummary.TotalDetectionTimeMs} ms, Candles processed: {metricsSummary.TotalCandlesProcessed}, Patterns found: {metricsSummary.TotalPatternsFound}");

                        // Log pattern check times if any
                        if (metricsSummary.PatternCheckTimes.Any())
                        {
                            Console.WriteLine("Pattern check times (ms):");
                            foreach (var kvp in metricsSummary.PatternCheckTimes.OrderByDescending(x => x.Value))
                            {
                                Console.WriteLine($"  {kvp.Key}: {TimeSpan.FromTicks(kvp.Value).TotalMilliseconds:F2} ms");
                            }
                        }

                        result = new PatternDetectionResult(
                            Patterns: filteredPatterns[filteredPatterns.Keys.Last()],
                            ExecutionTimeMs: stopwatch.ElapsedMilliseconds,
                            TotalCandlesProcessed: metricsSummary.TotalCandlesProcessed,
                            TotalPatternsFound: metricsSummary.TotalPatternsFound,
                            PatternCheckTimes: metricsSummary.PatternCheckTimes.ToDictionary(
                                kvp => kvp.Key,
                                kvp => (long)TimeSpan.FromTicks(kvp.Value).TotalMilliseconds
                            )
                        );
                    }
                    else
                    {
                        result = new PatternDetectionResult(
                            Patterns: filteredPatterns[filteredPatterns.Keys.Last()],
                            ExecutionTimeMs: null,
                            TotalCandlesProcessed: null,
                            TotalPatternsFound: null,
                            PatternCheckTimes: null
                        );
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Log pattern detection errors while maintaining system stability (only if metrics enabled)
                if (_config.EnablePatternDetectionMetrics)
                {
                    Console.WriteLine($"Error detecting patterns: {ex.Message} (took {stopwatch.ElapsedMilliseconds} ms)");
                }
            }

            // Return empty list as fallback for any processing failures
            return new PatternDetectionResult(
                new List<BacklashPatterns.PatternDefinitions.PatternDefinition>(),
                _config.EnablePatternDetectionMetrics ? stopwatch.ElapsedMilliseconds : null
            );
        }

        /// <summary>
        /// Asynchronously detects candlestick patterns from the provided market snapshot.
        /// Analyzes recent candlestick data to identify various technical analysis patterns
        /// that can inform trading strategy decisions.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing recent candlestick data and market information.</param>
        /// <summary>
        /// Filters patterns based on the configured pattern types.
        /// </summary>
        /// <param name="patterns">The raw detected patterns dictionary.</param>
        /// <param name="allowedTypes">The array of allowed pattern types. If contains "All", all patterns are allowed.</param>
        /// <returns>Filtered patterns dictionary.</returns>
        private Dictionary<int, List<BacklashPatterns.PatternDefinitions.PatternDefinition>> FilterPatternsByTypes(
            Dictionary<int, List<BacklashPatterns.PatternDefinitions.PatternDefinition>> patterns,
            string[] allowedTypes)
        {
            if (allowedTypes == null || allowedTypes.Length == 0 || allowedTypes.Contains("All"))
            {
                return patterns;
            }

            var filtered = new Dictionary<int, List<BacklashPatterns.PatternDefinitions.PatternDefinition>>();

            foreach (var kvp in patterns)
            {
                var filteredList = kvp.Value.Where(p => allowedTypes.Contains(p.Name)).ToList();
                if (filteredList.Any())
                {
                    filtered[kvp.Key] = filteredList;
                }
            }

            return filtered;
        }

        /// <returns>A task representing the asynchronous operation with comprehensive pattern detection results.</returns>
        /// <remarks>
        /// This async version is suitable for high-throughput scenarios to avoid blocking the calling thread.
        /// Uses the enhanced configuration and metrics for optimal performance.
        /// </remarks>
        public async Task<PatternDetectionResult> DetectPatternsAsync(MarketSnapshot snapshot)
        {
            // Validate input data availability
            if (snapshot.RecentCandlesticks == null || snapshot.RecentCandlesticks.Count == 0)
            {
                return new PatternDetectionResult(new List<BacklashPatterns.PatternDefinitions.PatternDefinition>(), null);
            }

            try
            {
                // Convert PseudoCandlesticks to CandleMids format for pattern analysis
                var mids = snapshot.RecentCandlesticks.ToCandleMids(snapshot.MarketTicker ?? string.Empty);

                // Create PatternSearch config from service config
                var patternConfig = new BacklashPatterns.PatternDetectionConfig
                {
                    SignificancePriceThreshold = _config.SignificancePriceThreshold,
                    VolumeIncreaseMultiplier = _config.VolumeIncreaseMultiplier,
                    InitialPatternCapacity = _config.InitialPatternCapacity,
                    EnableParallelProcessing = _config.EnableParallelProcessing,
                    MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism
                };

                // Create metrics collector for detailed performance tracking (conditionally)
                var metrics = _config.EnablePatternDetectionMetrics ? new BacklashPatterns.PatternDetectionMetrics() : null;

                // Execute pattern detection asynchronously with custom config and metrics
                Dictionary<int, List<BacklashPatterns.PatternDefinitions.PatternDefinition>> patterns;
                if (EnablePatternImageGeneration)
                {
                    var visualizationPatterns = await PatternSearch.DetectPatternsWithVisualizationAsync(mids, _config.LookbackWindow, patternConfig, metrics, _performanceMonitor, true, 10);
                    // Convert back to PatternDefinition for compatibility
                    patterns = visualizationPatterns.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Select(v => v.Pattern).ToList()
                    );
                }
                else
                {
                    patterns = await PatternSearch.DetectPatternsAsync(mids, _config.LookbackWindow, patternConfig, metrics, _performanceMonitor);
                }

                // Filter patterns based on configured pattern types if specified
                var filteredPatterns = FilterPatternsByTypes(patterns, _config.PatternTypes);

                // Create result with comprehensive metrics
                if (filteredPatterns.Keys.Count > 0)
                {
                    PatternDetectionResult result;
                    if (_config.EnablePatternDetectionMetrics && metrics != null)
                    {
                        var metricsSummary = metrics.GetSummary();
                        Console.WriteLine($"Async pattern detection completed in {metricsSummary.TotalDetectionTimeMs} ms for {snapshot.MarketTicker}");
                        Console.WriteLine($"Detailed metrics: Candles processed: {metricsSummary.TotalCandlesProcessed}, Patterns found: {metricsSummary.TotalPatternsFound}");

                        result = new PatternDetectionResult(
                            Patterns: filteredPatterns[filteredPatterns.Keys.Last()],
                            ExecutionTimeMs: metricsSummary.TotalDetectionTimeMs,
                            TotalCandlesProcessed: metricsSummary.TotalCandlesProcessed,
                            TotalPatternsFound: metricsSummary.TotalPatternsFound,
                            PatternCheckTimes: metricsSummary.PatternCheckTimes.ToDictionary(
                                kvp => kvp.Key,
                                kvp => (long)TimeSpan.FromTicks(kvp.Value).TotalMilliseconds
                            )
                        );
                    }
                    else
                    {
                        result = new PatternDetectionResult(
                            Patterns: filteredPatterns[filteredPatterns.Keys.Last()],
                            ExecutionTimeMs: null,
                            TotalCandlesProcessed: null,
                            TotalPatternsFound: null,
                            PatternCheckTimes: null
                        );
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                // Log pattern detection errors while maintaining system stability (only if metrics enabled)
                if (_config.EnablePatternDetectionMetrics)
                {
                    Console.WriteLine($"Error detecting patterns asynchronously: {ex.Message}");
                }
            }

            // Return empty list as fallback for any processing failures
            return new PatternDetectionResult(new List<BacklashPatterns.PatternDefinitions.PatternDefinition>(), null);
        }
    }
}
