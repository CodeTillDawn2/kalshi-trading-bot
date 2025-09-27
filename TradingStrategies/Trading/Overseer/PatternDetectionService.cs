using BacklashDTOs;
using BacklashInterfaces.PerformanceMetrics;
using BacklashPatterns;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using TradingStrategies.Configuration;
using TradingStrategies.Extensions;

/// <summary>
/// Represents the comprehensive result of pattern detection including patterns and performance metrics.
/// </summary>
public record PatternDetectionResult(
    List<BacklashPatterns.PatternDefinitions.PatternDefinition> Patterns,
    long? ExecutionTimeMs,
    int? TotalCandlesProcessed,
    int? TotalPatternsFound,
    Dictionary<string, long>? PatternCheckTimes
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
        private readonly IPerformanceMonitor _performanceMonitor;

        // Cache for pattern detection results to avoid repeated computation
        private readonly ConcurrentDictionary<string, PatternDetectionResult> _patternCache = new();


        /// <summary>
        /// Initializes a new instance of the PatternDetectionService with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration for pattern detection parameters.</param>
        /// <param name="performanceMonitor">Optional performance monitor for recording metrics.</param>
        public PatternDetectionService(IOptions<PatternDetectionServiceConfig> config, IPerformanceMonitor performanceMonitor)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
            _performanceMonitor = performanceMonitor;
        }

        /// <summary>
        /// Gets or sets whether pattern image generation is enabled.
        /// This setting controls whether visualization images are generated for detected patterns.
        /// </summary>
        public bool EnablePatternImageGeneration { get; set; } = true;

        /// <summary>
        /// Clears the pattern detection cache.
        /// </summary>
        public void ClearCache()
        {
            _patternCache.Clear();
        }

        /// <summary>
        /// Generates a cache key for the given market snapshot based on market ticker and candlestick data hash.
        /// </summary>
        /// <param name="snapshot">The market snapshot to generate a cache key for.</param>
        /// <returns>A string cache key.</returns>
        private string GenerateCacheKey(MarketSnapshot snapshot)
        {
            if (snapshot.RecentCandlesticks == null || snapshot.RecentCandlesticks.Count == 0)
            {
                return $"{snapshot.MarketTicker ?? "Unknown"}_empty";
            }

            // Create a simple hash of the candlestick data for cache key
            var hash = 0;
            foreach (var candle in snapshot.RecentCandlesticks)
            {
                hash = hash * 31 + candle.MidClose.GetHashCode();
                hash = hash * 31 + candle.MidHigh.GetHashCode();
                hash = hash * 31 + candle.MidLow.GetHashCode();
                hash = hash * 31 + candle.Volume.GetHashCode();
                hash = hash * 31 + candle.Timestamp.GetHashCode();
            }

            return $"{snapshot.MarketTicker ?? "Unknown"}_{hash}";
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
                return new PatternDetectionResult(new List<BacklashPatterns.PatternDefinitions.PatternDefinition>(), null, null, null, null);
            }

            // Check cache first
            var cacheKey = GenerateCacheKey(snapshot);
            if (_patternCache.TryGetValue(cacheKey, out var cachedResult))
            {
                return cachedResult;
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

                // Create metrics collector for detailed performance tracking
                var metrics = new BacklashPatterns.PatternDetectionMetrics();

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
                    if (_config.EnablePatternDetectionMetrics)
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
            return new PatternDetectionResult(new List<BacklashPatterns.PatternDefinitions.PatternDefinition>(), null, null, null, null);
        }
    }
}
