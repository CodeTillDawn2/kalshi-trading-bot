using BacklashDTOs;
using BacklashPatterns;
using TradingStrategies.Extensions;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Configuration options for pattern detection parameters.
    /// </summary>
    public class PatternDetectionConfig
    {
        /// <summary>
        /// The lookback window in periods for trend context and pattern validation.
        /// </summary>
        public int LookbackWindow { get; set; }

        /// <summary>
        /// The types of patterns to detect. If empty, all patterns are detected.
        /// </summary>
        public List<string> PatternTypes { get; set; } = new List<string>();

        /// <summary>
        /// Minimum price change threshold for significance check.
        /// </summary>
        public double SignificancePriceThreshold { get; set; }

        /// <summary>
        /// Minimum volume increase multiplier for context check.
        /// </summary>
        public double VolumeIncreaseMultiplier { get; set; }

        /// <summary>
        /// Initial capacity for patterns array per candle.
        /// </summary>
        public int InitialPatternCapacity { get; set; }

        /// <summary>
        /// Whether to enable parallel processing for pattern detection.
        /// </summary>
        public bool EnableParallelProcessing { get; set; }

        /// <summary>
        /// Maximum degree of parallelism for pattern checks.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }
    }

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
        private readonly PatternDetectionConfig _config;

        /// <summary>
        /// Initializes a new instance of the PatternDetectionService with configuration from appsettings.json.
        /// </summary>
        /// <param name="configuration">The configuration instance for reading settings from appsettings.json.</param>
        public PatternDetectionService(IConfiguration configuration)
        {
            _config = new PatternDetectionConfig();

            // Bind PatternDetectionServiceConfig section (service-specific settings)
            var serviceConfig = configuration.GetSection("PatternDetectionServiceConfig");
            serviceConfig.Bind(_config);

            // Bind PatternDetectionConfig section (PatternSearch-specific settings)
            var patternConfig = configuration.GetSection("PatternDetectionConfig");
            patternConfig.Bind(_config);
        }

        /// <summary>
        /// Initializes a new instance of the PatternDetectionService with the specified configuration.
        /// This constructor is for backward compatibility and testing purposes.
        /// </summary>
        /// <param name="config">The configuration for pattern detection parameters.</param>
        public PatternDetectionService(PatternDetectionConfig config)
        {
            _config = config ?? new PatternDetectionConfig();
        }

        /// <summary>
        /// Detects candlestick patterns from the provided market snapshot.
        /// Analyzes recent candlestick data to identify various technical analysis patterns
        /// that can inform trading strategy decisions.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing recent candlestick data and market information.</param>
        /// <returns>A list of detected pattern definitions representing significant technical formations.</returns>
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
        public List<BacklashPatterns.PatternDefinitions.PatternDefinition> DetectPatterns(MarketSnapshot snapshot)
        {
            // Validate input data availability
            if (snapshot.RecentCandlesticks == null || snapshot.RecentCandlesticks.Count == 0)
            {
                return new List<BacklashPatterns.PatternDefinitions.PatternDefinition>();
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

                // Create metrics collector for detailed performance tracking
                var metrics = new BacklashPatterns.PatternDetectionMetrics();

                // Execute pattern detection asynchronously with custom config and metrics
                var patterns = PatternSearch.DetectPatternsAsync(mids, _config.LookbackWindow, patternConfig, metrics).GetAwaiter().GetResult();

                // Filter patterns based on configured pattern types if specified
                var filteredPatterns = FilterPatternsByTypes(patterns, _config.PatternTypes);

                // Get metrics summary for logging
                var metricsSummary = metrics.GetSummary();

                // Return the most recent set of detected patterns if any were found
                if (filteredPatterns.Keys.Count > 0)
                {
                    stopwatch.Stop();
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

                    return filteredPatterns[filteredPatterns.Keys.Last()];
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Log pattern detection errors while maintaining system stability
                Console.WriteLine($"Error detecting patterns: {ex.Message} (took {stopwatch.ElapsedMilliseconds} ms)");
            }

            // Return empty list as fallback for any processing failures
            return new List<BacklashPatterns.PatternDefinitions.PatternDefinition>();
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
        /// <param name="allowedTypes">The list of allowed pattern types. If empty, all patterns are allowed.</param>
        /// <returns>Filtered patterns dictionary.</returns>
        private Dictionary<int, List<BacklashPatterns.PatternDefinitions.PatternDefinition>> FilterPatternsByTypes(
            Dictionary<int, List<BacklashPatterns.PatternDefinitions.PatternDefinition>> patterns,
            List<string> allowedTypes)
        {
            if (allowedTypes == null || allowedTypes.Count == 0)
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

        /// <returns>A task representing the asynchronous operation, with a list of detected pattern definitions.</returns>
        /// <remarks>
        /// This async version is suitable for high-throughput scenarios to avoid blocking the calling thread.
        /// Uses the enhanced configuration and metrics for optimal performance.
        /// </remarks>
        public async Task<List<BacklashPatterns.PatternDefinitions.PatternDefinition>> DetectPatternsAsync(MarketSnapshot snapshot)
        {
            // Validate input data availability
            if (snapshot.RecentCandlesticks == null || snapshot.RecentCandlesticks.Count == 0)
            {
                return new List<BacklashPatterns.PatternDefinitions.PatternDefinition>();
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
                var patterns = await PatternSearch.DetectPatternsAsync(mids, _config.LookbackWindow, patternConfig, metrics);

                // Filter patterns based on configured pattern types if specified
                var filteredPatterns = FilterPatternsByTypes(patterns, _config.PatternTypes);

                // Get metrics summary for logging
                var metricsSummary = metrics.GetSummary();

                Console.WriteLine($"Async pattern detection completed in {metricsSummary.TotalDetectionTimeMs} ms for {snapshot.MarketTicker}");
                Console.WriteLine($"Detailed metrics: Candles processed: {metricsSummary.TotalCandlesProcessed}, Patterns found: {metricsSummary.TotalPatternsFound}");

                // Return the most recent set of detected patterns if any were found
                if (filteredPatterns.Keys.Count > 0)
                {
                    return filteredPatterns[filteredPatterns.Keys.Last()];
                }
            }
            catch (Exception ex)
            {
                // Log pattern detection errors while maintaining system stability
                Console.WriteLine($"Error detecting patterns asynchronously: {ex.Message}");
            }

            // Return empty list as fallback for any processing failures
            return new List<BacklashPatterns.PatternDefinitions.PatternDefinition>();
        }
    }
}