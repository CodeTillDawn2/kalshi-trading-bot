using BacklashDTOs;
using TradingStrategies.Trading.Helpers;
using TradingStrategies.Configuration;
using static BacklashInterfaces.Enums.StrategyEnums;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Service responsible for determining and caching market types for trading snapshots.
    /// This class acts as a facade over the MarketTypeHelper, providing caching functionality
    /// to avoid redundant market type calculations for the same market snapshot.
    /// Includes performance metrics collection, configurable cache expiration, and async operations.
    /// </summary>
    /// <remarks>
    /// The MarketTypeService is used in the trading simulation pipeline to classify market conditions
    /// based on various indicators (price movement, liquidity, activity, etc.). It leverages a helper
    /// class to perform the actual classification logic and maintains an in-memory cache to optimize
    /// performance during simulation runs where the same snapshot may be processed multiple times.
    ///
    /// Key responsibilities:
    /// - Assign market types to MarketSnapshot instances (sync and async)
    /// - Cache market type results with configurable expiration to reduce computational overhead
    /// - Collect performance metrics (cache hit rates, classification timing)
    /// - Convert string representations back to MarketType enums
    ///
    /// This service is instantiated once per simulation engine and reused throughout the simulation process.
    /// Cache expiration can be configured via TradingConfig:MarketTypeCacheExpirationMinutes in appsettings.json.
    /// </remarks>
    public class MarketTypeService
    {
        /// <summary>
        /// Helper instance that performs the actual market type classification logic.
        /// </summary>
        /// <remarks>
        /// The MarketTypeHelper contains the rule-based system for determining market types
        /// based on various market conditions extracted from snapshots.
        /// </remarks>
        private readonly MarketTypeHelper _marketTypeHelper;

        /// <summary>
        /// Cache storing market type results keyed by market ticker and timestamp.
        /// </summary>
        /// <remarks>
        /// This dictionary prevents redundant calculations when the same market snapshot
        /// is processed multiple times during simulation. The key combines ticker and timestamp
        /// to uniquely identify a market state.
        /// </remarks>
        private readonly ConcurrentDictionary<string, (MarketType Type, DateTime CachedAt)> _marketTypeCache;

        /// <summary>
        /// Configuration for cache expiration.
        /// </summary>
        private readonly TimeSpan _cacheExpiration;

        /// <summary>
        /// Flag to enable or disable performance metrics collection.
        /// </summary>
        private readonly bool _enablePerformanceMetrics;

        /// <summary>
        /// Performance metrics: cache hits.
        /// </summary>
        private long _cacheHits;

        /// <summary>
        /// Performance metrics: cache misses.
        /// </summary>
        private long _cacheMisses;

        /// <summary>
        /// Performance metrics: total time spent on classification.
        /// </summary>
        private TimeSpan _totalClassificationTime;

        /// <summary>
        /// Performance metrics: number of classifications performed.
        /// </summary>
        private int _classificationCount;

        /// <summary>
        /// Initializes a new instance of the MarketTypeService class.
        /// </summary>
        /// <param name="config">The market type service configuration containing settings for cache expiration and performance metrics.</param>
        /// <remarks>
        /// Creates the MarketTypeHelper instance and initializes the cache with configurable expiration.
        /// The cache expiration can be configured via MarketTypeServiceConfig:CacheExpirationMinutes in appsettings.json.
        /// Performance metrics collection can be enabled/disabled via MarketTypeServiceConfig:EnablePerformanceMetrics.
        /// This constructor sets up the service for immediate use in market type classification.
        /// </remarks>
        public MarketTypeService(IOptions<MarketTypeServiceConfig> config)
        {
            _marketTypeHelper = new MarketTypeHelper();
            _marketTypeCache = new ConcurrentDictionary<string, (MarketType Type, DateTime CachedAt)>();
            _cacheExpiration = TimeSpan.FromMinutes(config.Value.CacheExpirationMinutes);
            _enablePerformanceMetrics = config.Value.EnablePerformanceMetrics;
        }

        /// <summary>
        /// Assigns the appropriate market type to the provided market snapshot.
        /// </summary>
        /// <param name="snapshot">The market snapshot to classify and update with market type information.</param>
        /// <remarks>
        /// This method first checks the cache for an existing classification result. If not found or expired,
        /// it delegates to the MarketTypeHelper to determine the market type based on the snapshot's
        /// trading conditions. The result is then cached and assigned to the snapshot's MarketType property.
        /// Performance metrics are collected including cache hit/miss rates and classification timing.
        ///
        /// If an exception occurs during classification (e.g., due to missing or invalid data),
        /// the market type is set to "Undefined" as a fallback to ensure simulation continuity.
        ///
        /// The method modifies the input snapshot in-place, setting its MarketType property to the
        /// string representation of the determined MarketType enum value.
        /// </remarks>
        public void AssignMarketTypeToSnapshot(MarketSnapshot snapshot)
        {
            try
            {
                if (string.IsNullOrEmpty(snapshot.MarketTicker))
                {
                    snapshot.MarketType = MarketType.Undefined.ToString();
                    return;
                }

                var key = $"{snapshot.MarketTicker}_{snapshot.Timestamp.Ticks}";
                if (_marketTypeCache.TryGetValue(key, out var cached))
                {
                    if (DateTime.Now - cached.CachedAt > _cacheExpiration)
                    {
                        _marketTypeCache.TryRemove(key, out _);
                    }
                    else
                    {
                        if (_enablePerformanceMetrics) _cacheHits++;
                        snapshot.MarketType = cached.Type.ToString();
                        return;
                    }
                }

                if (_enablePerformanceMetrics) _cacheMisses++;
                var stopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;
                var type = _marketTypeHelper.GetMarketType(snapshot);
                if (_enablePerformanceMetrics)
                {
                    stopwatch.Stop();
                    _totalClassificationTime += stopwatch.Elapsed;
                    _classificationCount++;
                }
                _marketTypeCache[key] = (type, DateTime.Now);
                snapshot.MarketType = type.ToString();
            }
            catch
            {
                snapshot.MarketType = MarketType.Undefined.ToString();
            }
        }

        /// <summary>
        /// Asynchronously assigns the appropriate market type to the provided market snapshot.
        /// </summary>
        /// <param name="snapshot">The market snapshot to classify and update with market type information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This async version of the method allows for better performance in high-throughput scenarios
        /// by not blocking the calling thread during market type classification. It performs the same
        /// caching, expiration, and metrics collection as the synchronous version.
        /// </remarks>
        public async Task AssignMarketTypeToSnapshotAsync(MarketSnapshot snapshot)
        {
            try
            {
                if (string.IsNullOrEmpty(snapshot.MarketTicker))
                {
                    snapshot.MarketType = MarketType.Undefined.ToString();
                    return;
                }

                var key = $"{snapshot.MarketTicker}_{snapshot.Timestamp.Ticks}";
                if (_marketTypeCache.TryGetValue(key, out var cached))
                {
                    if (DateTime.Now - cached.CachedAt > _cacheExpiration)
                    {
                        _marketTypeCache.TryRemove(key, out _);
                    }
                    else
                    {
                        if (_enablePerformanceMetrics) _cacheHits++;
                        snapshot.MarketType = cached.Type.ToString();
                        return;
                    }
                }

                if (_enablePerformanceMetrics) _cacheMisses++;
                var stopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;
                var type = await Task.Run(() => _marketTypeHelper.GetMarketType(snapshot));
                if (_enablePerformanceMetrics)
                {
                    stopwatch.Stop();
                    _totalClassificationTime += stopwatch.Elapsed;
                    _classificationCount++;
                }
                _marketTypeCache[key] = (type, DateTime.Now);
                snapshot.MarketType = type.ToString();
            }
            catch
            {
                snapshot.MarketType = MarketType.Undefined.ToString();
            }
        }

        /// <summary>
        /// Converts a string representation of a market type to its corresponding MarketType enum value.
        /// </summary>
        /// <param name="marketType">The string representation of the market type to convert.</param>
        /// <returns>The MarketType enum value corresponding to the input string, or MarketType.Undefined if parsing fails.</returns>
        /// <remarks>
        /// This method performs case-insensitive parsing of the market type string using Enum.TryParse.
        /// It serves as a utility for converting market type strings (e.g., from serialized data or user input)
        /// back to strongly-typed enum values for use in conditional logic and strategy selection.
        ///
        /// If the string cannot be parsed to a valid MarketType value, MarketType.Undefined is returned
        /// as a safe default to prevent exceptions in calling code.
        ///
        /// Common use cases include:
        /// - Deserializing market type data from persistent storage
        /// - Processing market type strings from external APIs
        /// - Converting user-provided market type filters
        /// </remarks>
        public MarketType ConvertStringToMarketType(string marketType)
        {
            if (!Enum.TryParse<MarketType>(marketType, true, out var currentMarketConditions))
            {
                currentMarketConditions = MarketType.Undefined;
            }
            return currentMarketConditions;
        }

        /// <summary>
        /// Gets the cache hit and miss statistics.
        /// </summary>
        /// <returns>A tuple containing the number of cache hits and misses.</returns>
        public (long Hits, long Misses) GetCacheStatistics() => (_cacheHits, _cacheMisses);

        /// <summary>
        /// Gets the average time spent on market type classification.
        /// </summary>
        /// <returns>The average classification time, or TimeSpan.Zero if no classifications have been performed.</returns>
        public TimeSpan GetAverageClassificationTime() => _classificationCount > 0 ? _totalClassificationTime / _classificationCount : TimeSpan.Zero;

        /// <summary>
        /// Gets the total number of market type classifications performed.
        /// </summary>
        /// <returns>The classification count.</returns>
        public int GetClassificationCount() => _classificationCount;

        /// <summary>
        /// Posts the current performance metrics to the specified performance monitor.
        /// </summary>
        /// <param name="performanceMonitor">The performance monitor to post metrics to. If null, no action is taken.</param>
        public void PostMetrics(PerformanceMonitor performanceMonitor)
        {
            if (performanceMonitor != null)
            {
                var metrics = new Dictionary<string, object>
                {
                    ["CacheHits"] = _cacheHits,
                    ["CacheMisses"] = _cacheMisses,
                    ["AverageClassificationTimeMs"] = GetAverageClassificationTime().TotalMilliseconds,
                    ["ClassificationCount"] = _classificationCount
                };
                performanceMonitor.RecordSimulationMetrics("MarketTypeService", metrics, _enablePerformanceMetrics);
            }
        }
    }
}
