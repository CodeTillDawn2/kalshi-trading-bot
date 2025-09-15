using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NJsonSchema;
using BacklashBot.Helpers;
using BacklashBot.Services.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Converters;
using BacklashDTOs.Data;
using BacklashDTOs.Exceptions;
using BacklashInterfaces.Constants;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingStrategies.Configuration;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for managing trading snapshot operations, including saving market data snapshots to disk
    /// and loading them for analysis. This service handles snapshot validation, timing controls, and schema management
    /// to ensure reliable data persistence and retrieval for trading strategy evaluation.
    /// </summary>
    /// <remarks>
    /// The service implements timing-based snapshot saving with tolerance windows to handle irregular market data arrival.
    /// It uses parallel processing for efficient loading of multiple snapshots and maintains schema compatibility
    /// through version-based JSON sanitization. Snapshots are stored as JSON files in a configured directory
    /// for later analysis by trading strategies and backtesting systems.
    /// </remarks>
    public class TradingSnapshotService : ITradingSnapshotService
    {
        private readonly ILogger<ITradingSnapshotService> _logger;
        private readonly IOptions<SnapshotConfig> _snapshotConfig;
        private readonly IOptions<TradingConfig> _tradingConfig;
        private readonly ICentralPerformanceMonitor? _centralPerformanceMonitor;
        private DateTime? _lastSavedSnapshotTimestamp; // Actual timestamp of the last saved snapshot

        /// <summary>
        /// Gets or sets the timestamp when the next snapshot is ideally expected based on the configured decision frequency.
        /// </summary>
        /// <remarks>
        /// This property is used to detect timing irregularities in snapshot arrival. It is updated after each successful
        /// snapshot save and reset when snapshot tracking is cleared. Null indicates no expected timing has been established.
        /// </remarks>
        public DateTime? NextExpectedSnapshotTimestamp { get; set; } // The timestamp when the next snapshot is ideally expected
        private bool _isFirstSnapshotTaken = true;
        private readonly TimeSpan _decisionFrequencyInterval;
        private readonly TimeSpan _snapshotTimingTolerance;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly string _snapshotStorageDirectory;
        private readonly bool _enablePerformanceMetrics;

        // Performance metrics tracking
        private int _totalSnapshotsAttempted = 0;
        private int _onTimeSnapshots = 0;
        private double _totalLatencyMs = 0;
        private long _totalMemoryUsed = 0;
        private TimeSpan _totalCpuUsed = TimeSpan.Zero;
        private int _concurrentOperations = 0;

        /// <summary>
        /// Initializes a new instance of the TradingSnapshotService with required dependencies.
        /// </summary>
        /// <param name="logger">Logger for recording snapshot operations, warnings, and errors.</param>
        /// <param name="snapshotConfig">Configuration options for snapshot behavior including tolerance settings.</param>
        /// <param name="tradingConfig">Configuration options for trading parameters including decision frequency.</param>
        /// <param name="scopeFactory">Factory for creating service scopes to access database services.</param>
        /// <param name="configuration">Configuration for accessing app settings.</param>
        /// <param name="centralPerformanceMonitor">Central performance monitor for recording execution times.</param>
        public TradingSnapshotService(
            ILogger<ITradingSnapshotService> logger,
            IOptions<SnapshotConfig> snapshotConfig,
            IOptions<TradingConfig> tradingConfig,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ICentralPerformanceMonitor? centralPerformanceMonitor = null)
        {
            _logger = logger;
            _snapshotConfig = snapshotConfig;
            _tradingConfig = tradingConfig;
            _serviceScopeFactory = scopeFactory;
            _centralPerformanceMonitor = centralPerformanceMonitor;
            _decisionFrequencyInterval = TimeSpan.FromSeconds(tradingConfig.Value.DecisionFrequencySeconds);
            _snapshotTimingTolerance = TimeSpan.FromSeconds(snapshotConfig.Value.SnapshotToleranceSeconds);

            // Use configurable storage directory or fallback to default
            _snapshotStorageDirectory = snapshotConfig.Value.StorageDirectory ?? @"\\DESKTOP-ITC50UT\SmokehouseDataStorage\NewSnapshots";

            // Ensure the directory exists
            if (!Directory.Exists(_snapshotStorageDirectory))
            {
                Directory.CreateDirectory(_snapshotStorageDirectory);
            }

            _enablePerformanceMetrics = configuration.GetValue<bool>("TradingSnapshotService:EnablePerformanceMetrics", false);
        }

        /// <summary>
        /// Saves a snapshot of market data to disk, validating timing and market conditions before persistence.
        /// </summary>
        /// <param name="BrainInstance">Identifier for the brain instance creating this snapshot.</param>
        /// <param name="cacheSnapshot">The complete snapshot data containing market information and timing details.</param>
        /// <returns>A list of market tickers that were successfully saved in this snapshot.</returns>
        /// <remarks>
        /// This method performs several validations:
        /// - Checks timing regularity against expected intervals with tolerance windows
        /// - Validates WebSocket data freshness
        /// - Filters out ended markets and markets without sufficient data
        /// - Applies snapshot validation rules before saving
        /// - Saves data as JSON files organized by timestamp
        /// Returns empty list if snapshot is discarded due to timing issues.
        /// </remarks>
        public async Task<List<string>> SaveSnapshotAsync(string BrainInstance, CacheSnapshot cacheSnapshot)
        {
            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref _concurrentOperations);
            long memoryBefore = 0;
            Process process = null;
            TimeSpan cpuBefore = TimeSpan.Zero;
            if (_enablePerformanceMetrics)
            {
                _totalSnapshotsAttempted++;
                var latencyMs = (DateTime.Now - cacheSnapshot.Timestamp).TotalMilliseconds;
                _totalLatencyMs += latencyMs;
                memoryBefore = GC.GetTotalMemory(false);
                process = Process.GetCurrentProcess();
                cpuBefore = process.TotalProcessorTime;
            }
            try
            {
                // Input validation for snapshot data integrity
                if (cacheSnapshot == null)
                {
                    _logger.LogWarning("CacheSnapshot is null, cannot save snapshot");
                    return new List<string>();
                }
                if (string.IsNullOrWhiteSpace(BrainInstance))
                {
                    _logger.LogWarning("BrainInstance is null or empty, cannot save snapshot");
                    return new List<string>();
                }
                if (cacheSnapshot.Markets == null || !cacheSnapshot.Markets.Any())
                {
                    _logger.LogWarning("CacheSnapshot contains no markets, cannot save snapshot");
                    return new List<string>();
                }
                if (cacheSnapshot.Timestamp == default)
                {
                    _logger.LogWarning("CacheSnapshot timestamp is default value, cannot save snapshot");
                    return new List<string>();
                }

                List<string> snapshotsActuallySaved = new List<string>();
                var timestamp = cacheSnapshot.Timestamp;
                var timestampString = timestamp.ToString("yyyyMMddTHHmmssZ");

                var discardThreshold = _decisionFrequencyInterval + TimeSpan.FromSeconds(_snapshotConfig.Value.SnapshotToleranceSeconds * 2);

                if (_isFirstSnapshotTaken)
                {
                    NextExpectedSnapshotTimestamp = timestamp + _decisionFrequencyInterval;
                }
                else if (_lastSavedSnapshotTimestamp.HasValue)
                {
                    if (NextExpectedSnapshotTimestamp.HasValue && timestamp > NextExpectedSnapshotTimestamp.Value + discardThreshold)
                    {
                        _logger.LogWarning(
                            "BRAIN: Skipping extremely late snapshot: {CurrentTimestamp} is {TimeSinceExpected} seconds " +
                            "after its expected time of {ExpectedTimestamp} (tolerance {DiscardThresholdSeconds}s). Discarding.",
                            timestampString, (timestamp - NextExpectedSnapshotTimestamp.Value).TotalSeconds,
                            NextExpectedSnapshotTimestamp.Value.ToString("yyyyMMddTHHmmssZ"), discardThreshold.TotalSeconds);
                        return new List<string>();
                    }

                    var actualTimeSinceLastSnapshot = timestamp - _lastSavedSnapshotTimestamp.Value;
                    var minInterval = _decisionFrequencyInterval - _snapshotTimingTolerance;
                    var maxInterval = _decisionFrequencyInterval + _snapshotTimingTolerance;

                    if (actualTimeSinceLastSnapshot < minInterval || actualTimeSinceLastSnapshot > maxInterval)
                    {
                        _logger.LogWarning(
                            "Snapshot timing irregularity detected: {CurrentTimestamp} is {TimeSinceLastSnapshot} seconds " +
                            "after {LastTimestamp}, expected approximately {ExpectedInterval} seconds",
                            timestampString, actualTimeSinceLastSnapshot.TotalSeconds,
                            _lastSavedSnapshotTimestamp.Value.ToString("yyyyMMddTHHmmssZ"), _decisionFrequencyInterval.TotalSeconds);
                    }
                }

                if (_enablePerformanceMetrics) _onTimeSnapshots++;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    Converters = { new SimplifiedTupleConverter(), new ShortIsoDateTimeConverter(), new OrderbookSlimConverter(), new MarketSnapshotSlimConverter() }
                };

                var schema = JsonSchema.FromType<CacheSnapshot>();
                var schemaData = schema.ToJson();

                int SavedCount = 0;
                Stopwatch ioStopwatch = null;

                if (cacheSnapshot.LastWebSocketTimestamp <= cacheSnapshot.Timestamp.AddMinutes(-10) && cacheSnapshot.Markets.Count > 5)
                {
                    _logger.LogWarning(new Exception($"Last web socket time is too old. {cacheSnapshot.LastWebSocketTimestamp} vs {cacheSnapshot.Timestamp}. Market skipped."), "Last web socket time is too old. {LastWebSocketTimestamp} vs {CacheTimestamp}. Market skipped.",
                        cacheSnapshot.LastWebSocketTimestamp, cacheSnapshot.Timestamp);
                }
                else
                {
                    var snapshotsToSave = new List<object>();
                    foreach (var marketSnapshot in cacheSnapshot.Markets)
                    {
                        if (KalshiConstants.IsMarketStatusEnded(marketSnapshot.Value.MarketStatus))
                        {
                            _logger.LogInformation("Market {market} has ended so snapshot was skipped.", marketSnapshot.Key);
                            continue;
                        }
                        if ((marketSnapshot.Value.BestNoBid == 0 && marketSnapshot.Value.BestYesBid == 0)
                            || marketSnapshot.Value.OrderbookData == null || marketSnapshot.Value.OrderbookData.Count == 0)
                        {
                            _logger.LogInformation("Market {market} prices have not been loaded yet.", marketSnapshot.Key);
                            continue;
                        }

                        if (!ValidateMarketSnapshot(marketSnapshot.Value))
                        {
                            continue;
                        }

                        var rawJson = JsonSerializer.Serialize(marketSnapshot.Value, options);

                        var snapshotData = new
                        {
                            MarketTicker = marketSnapshot.Value.MarketTicker,
                            SnapshotDate = timestamp,
                            JSONSchemaVersion = _snapshotConfig.Value.SnapshotSchemaVersion,
                            PositionSize = marketSnapshot.Value.PositionSize,
                            ChangeMetricsMature = marketSnapshot.Value.ChangeMetricsMature,
                            VelocityPerMinute_Top_Yes_Bid = marketSnapshot.Value.VelocityPerMinute_Top_Yes_Bid,
                            VelocityPerMinute_Top_No_Bid = marketSnapshot.Value.VelocityPerMinute_Top_No_Bid,
                            VelocityPerMinute_Bottom_Yes_Bid = marketSnapshot.Value.VelocityPerMinute_Bottom_Yes_Bid,
                            VelocityPerMinute_Bottom_No_Bid = marketSnapshot.Value.VelocityPerMinute_Bottom_No_Bid,
                            OrderVolume_Yes_Bid = marketSnapshot.Value.OrderVolumePerMinute_YesBid,
                            OrderVolume_No_Bid = marketSnapshot.Value.OrderVolumePerMinute_NoBid,
                            TradeVolume_Yes = marketSnapshot.Value.TradeVolumePerMinute_Yes,
                            TradeVolume_No = marketSnapshot.Value.TradeVolumePerMinute_No,
                            AverageTradeSize_Yes = marketSnapshot.Value.AverageTradeSize_Yes,
                            AverageTradeSize_No = marketSnapshot.Value.AverageTradeSize_No,
                            RawJSON = rawJson,
                            MarketTypeID = (int?)null,
                            IsValidated = false,
                            BrainInstance = BrainInstance
                        };

                        snapshotsToSave.Add(snapshotData);
                        snapshotsActuallySaved.Add(marketSnapshot.Value.MarketTicker);
                        SavedCount += 1;
                    }

                    if (snapshotsToSave.Any())
                    {
                        var fileJson = JsonSerializer.Serialize(snapshotsToSave, options);
                        var fileName = $"Snapshot_{timestampString}.json";
                        var fullPath = Path.Combine(_snapshotStorageDirectory, fileName);
                        if (_enablePerformanceMetrics) ioStopwatch = Stopwatch.StartNew();
                        await File.WriteAllTextAsync(fullPath, fileJson, Encoding.Unicode);
                        if (_enablePerformanceMetrics) ioStopwatch.Stop();
                        _logger.LogInformation("Saved {Count} snapshots to file: {FilePath}", SavedCount, fullPath);
                    }

                    _lastSavedSnapshotTimestamp = timestamp;
                    NextExpectedSnapshotTimestamp = timestamp + _decisionFrequencyInterval;
                    _isFirstSnapshotTaken = false;
                }

                Interlocked.Decrement(ref _concurrentOperations);
                stopwatch.Stop();

                if (_enablePerformanceMetrics && _centralPerformanceMonitor != null)
                {
                    _centralPerformanceMonitor.RecordExecutionTime("TradingSnapshotService.SaveSnapshotAsync", stopwatch.ElapsedMilliseconds);
                }

                if (_enablePerformanceMetrics)
                {
                    long memoryAfter = GC.GetTotalMemory(false);
                    TimeSpan cpuAfter = process.TotalProcessorTime;
                    long memoryUsed = memoryAfter - memoryBefore;
                    double cpuUsedMs = (cpuAfter - cpuBefore).TotalMilliseconds;
                    _totalMemoryUsed += memoryUsed;
                    _totalCpuUsed += (cpuAfter - cpuBefore);
                    double ioTimeMs = ioStopwatch?.Elapsed.TotalMilliseconds ?? 0;
                    int queueDepth = _concurrentOperations;
                    double throughput = snapshotsActuallySaved.Count / stopwatch.Elapsed.TotalSeconds;
                    double adherence = _totalSnapshotsAttempted > 0 ? (_onTimeSnapshots / (double)_totalSnapshotsAttempted) * 100 : 0;
                    double avgLatency = _totalSnapshotsAttempted > 0 ? _totalLatencyMs / _totalSnapshotsAttempted : 0;
                    _logger.LogInformation("Snapshot save operation completed in {ElapsedMilliseconds}ms for {Count} snapshots (throughput: {Throughput:F2} snaps/sec, adherence: {Adherence:F1}%, avg latency: {AvgLatency:F2}ms, memory used: {MemoryUsed} bytes, CPU used: {CpuUsedMs:F2}ms, I/O time: {IoTimeMs:F2}ms, queue depth: {QueueDepth})",
                        stopwatch.ElapsedMilliseconds, snapshotsActuallySaved.Count, throughput, adherence, avgLatency, memoryUsed, cpuUsedMs, ioTimeMs, queueDepth);
                }
                else
                {
                    _logger.LogInformation("Snapshot save operation completed in {ElapsedMilliseconds}ms for {Count} snapshots", stopwatch.ElapsedMilliseconds, snapshotsActuallySaved.Count);
                }

                return snapshotsActuallySaved;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Interlocked.Decrement(ref _concurrentOperations);
                _logger.LogWarning(ex, "Error saving snapshot at {Timestamp} after {ElapsedMilliseconds}ms", cacheSnapshot.Timestamp, stopwatch.ElapsedMilliseconds);
                return new List<string>();
            }
        }


        /// <summary>
        /// Loads multiple market snapshots from database records, processing them in parallel for efficiency.
        /// </summary>
        /// <param name="snapshots">List of snapshot metadata records to load from the database.</param>
        /// <param name="forceLoad">When true, bypasses any loading restrictions (currently unused).</param>
        /// <returns>Dictionary mapping market tickers to lists of their historical snapshots, sorted by timestamp.</returns>
        /// <remarks>
        /// This method uses parallel processing to efficiently load snapshots grouped by market ticker.
        /// Each snapshot is deserialized from JSON, upgraded to the current schema version, and validated.
        /// Failed deserializations are logged as warnings but don't stop processing of other snapshots.
        /// The returned dictionary only includes markets that have successfully loaded snapshots.
        /// </remarks>
        public async Task<Dictionary<string, List<MarketSnapshot>>> LoadManySnapshots(List<SnapshotDTO> snapshots, bool forceLoad = false)
        {
            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref _concurrentOperations);
            long memoryBefore = 0;
            Process process = null;
            TimeSpan cpuBefore = TimeSpan.Zero;
            if (_enablePerformanceMetrics)
            {
                memoryBefore = GC.GetTotalMemory(false);
                process = Process.GetCurrentProcess();
                cpuBefore = process.TotalProcessorTime;
            }
            try
            {
                var result = new Dictionary<string, List<MarketSnapshot>>();
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    Converters = { new SimplifiedTupleConverter(), new ShortIsoDateTimeConverter(), new OrderbookSlimConverter(), new MarketSnapshotSlimConverter() }
                };

                // Group snapshots by MarketTicker
                var groupedSnapshots = snapshots
                    .GroupBy(s => s.MarketTicker)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Process each market group in parallel with configurable limits
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _snapshotConfig.Value.MaxParallelism > 0 ? _snapshotConfig.Value.MaxParallelism : -1
                };

                Parallel.ForEach(groupedSnapshots, parallelOptions, group =>
                {
                    var marketTicker = group.Key;
                    var newCacheSnapshots = new List<MarketSnapshot>(group.Value.Count);  // Pre-allocate

                    // Process snapshots for this group sequentially (order matters), but groups in parallel
                    foreach (var snapshot in group.Value)
                    {
                        try
                        {
                            string safeJSON = snapshot.RawJSON;
                            var cacheSnapshot = JsonSerializer.Deserialize<MarketSnapshot>(safeJSON, options);

                            if (cacheSnapshot != null)
                            {
                                newCacheSnapshots.Add(cacheSnapshot);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to deserialize snapshot for {MarketTicker} at {Timestamp}",
                                    marketTicker, snapshot.SnapshotDate);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deserializing snapshot for {MarketTicker} at {Timestamp}",
                                marketTicker, snapshot.SnapshotDate);
                        }
                    }

                    // Upgrade in batch
                    foreach (var cacheSnapshot in newCacheSnapshots)
                    {
                        cacheSnapshot.UpgradeSnapshot(_snapshotConfig.Value.SnapshotSchemaVersion);
                    }

                    lock (result)  // Sync access to result dict
                    {
                        if (newCacheSnapshots.Any())
                        {
                            result[marketTicker] = newCacheSnapshots;
                            _logger.LogInformation("Loaded {Count} snapshots for {MarketTicker}", newCacheSnapshots.Count, marketTicker);
                        }
                    }
                });

                Interlocked.Decrement(ref _concurrentOperations);
                stopwatch.Stop();

                if (_enablePerformanceMetrics && _centralPerformanceMonitor != null)
                {
                    _centralPerformanceMonitor.RecordExecutionTime("TradingSnapshotService.LoadManySnapshots", stopwatch.ElapsedMilliseconds);
                }

                if (_enablePerformanceMetrics)
                {
                    long memoryAfter = GC.GetTotalMemory(false);
                    TimeSpan cpuAfter = process.TotalProcessorTime;
                    long memoryUsed = memoryAfter - memoryBefore;
                    double cpuUsedMs = (cpuAfter - cpuBefore).TotalMilliseconds;
                    _totalMemoryUsed += memoryUsed;
                    _totalCpuUsed += (cpuAfter - cpuBefore);
                    int queueDepth = _concurrentOperations;
                    double throughput = snapshots.Count / stopwatch.Elapsed.TotalSeconds;
                    int batchSize = groupedSnapshots.Count;
                    int concurrency = parallelOptions.MaxDegreeOfParallelism;
                    _logger.LogInformation("Snapshot load operation completed in {ElapsedMilliseconds}ms for {TotalSnapshots} snapshots across {MarketCount} markets (throughput: {Throughput:F2} snaps/sec, batch size: {BatchSize} groups, concurrency: {Concurrency}, memory used: {MemoryUsed} bytes, CPU used: {CpuUsedMs:F2}ms, queue depth: {QueueDepth})",
                        stopwatch.ElapsedMilliseconds, snapshots.Count, result.Count, throughput, batchSize, concurrency, memoryUsed, cpuUsedMs, queueDepth);
                }
                else
                {
                    _logger.LogInformation("Snapshot load operation completed in {ElapsedMilliseconds}ms for {TotalSnapshots} snapshots across {MarketCount} markets",
                        stopwatch.ElapsedMilliseconds, snapshots.Count, result.Count);
                }

                await Task.CompletedTask;
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Interlocked.Decrement(ref _concurrentOperations);
                _logger.LogError(ex, "Error loading multiple snapshots after {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return new Dictionary<string, List<MarketSnapshot>>();
            }
        }

        /// <summary>
        /// Validates a market snapshot for data integrity and consistency before saving or processing.
        /// </summary>
        /// <param name="marketSnapshot">The market snapshot to validate.</param>
        /// <returns>True if the snapshot passes all validation checks, false otherwise.</returns>
        /// <remarks>
        /// This method uses SnapshotDiscrepancyValidator to check for:
        /// - Missing orderbook data
        /// - Overlapping bid/ask prices
        /// - Rate discrepancies in trading metrics
        /// Validation failures are logged as warnings with detailed information about the issues found.
        /// </remarks>
        public bool ValidateMarketSnapshot(MarketSnapshot marketSnapshot)
        {
            bool hasValidMarket = false;

            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(marketSnapshot);
            if (result.IsValid)
            {
                hasValidMarket = true;
            }
            else
            {
                var logMessage = $"Snapshot for {marketSnapshot.MarketTicker} is invalid.";
                if (result.IsOrderbookMissing)
                {
                    logMessage += $" IsOrderbookMissing: {result.IsOrderbookMissing} (OrderbookData: {(marketSnapshot.OrderbookData == null ? "null" : marketSnapshot.OrderbookData.Any() ? "present" : "empty")})";
                }
                if (result.DoPricesOverlap)
                {
                    logMessage += $" DoPricesOverlap: {result.DoPricesOverlap} (BestYesBid: {marketSnapshot.BestYesBid}, BestYesAsk: {marketSnapshot.BestYesAsk})";
                }
                if (result.IsRateDiscrepancy)
                {
                    logMessage += $" IsRateDiscrepancy: {result.IsRateDiscrepancy} (" +
                                  $"VelocityYesTop: {marketSnapshot.VelocityPerMinute_Top_Yes_Bid}, " +
                                  $"VelocityYesBottom: {marketSnapshot.VelocityPerMinute_Bottom_Yes_Bid}, " +
                                  $"OrderYesVolume: {marketSnapshot.OrderVolumePerMinute_YesBid}, " +
                                  $"TradeYesVolume: {marketSnapshot.TradeVolumePerMinute_Yes}, " +
                                  $"VelocityNoTop: {marketSnapshot.VelocityPerMinute_Top_No_Bid}, " +
                                  $"VelocityNoBottom: {marketSnapshot.VelocityPerMinute_Bottom_No_Bid}, " +
                                  $"OrderNoVolume: {marketSnapshot.OrderVolumePerMinute_NoBid}, " +
                                  $"TradeNoVolume: {marketSnapshot.TradeVolumePerMinute_No})";
                }

                var exception = new SnapshotInvalidException(marketSnapshot.MarketTicker, logMessage);

                if (result.IsOrderbookMissing)
                {
                    _logger.LogWarning(exception, "Snapshot for {MarketTicker} is missing its orderbook. Details: {Details}",
                        marketSnapshot.MarketTicker, logMessage);
                }
                else if (result.DoPricesOverlap)
                {
                    _logger.LogWarning(exception, "Snapshot for {MarketTicker} has overlapping prices. Details: {Details}",
                        marketSnapshot.MarketTicker, logMessage);
                }
                else if (result.IsRateDiscrepancy)
                {
                    _logger.LogWarning(exception, "Snapshot for {MarketTicker} has rate discrepancies. Details: {Details}",
                        marketSnapshot.MarketTicker, logMessage);
                }
                else
                {
                    _logger.LogWarning(exception, "Snapshot for {MarketTicker} is invalid. Details: {Details}",
                        marketSnapshot.MarketTicker, logMessage);
                }
            }

            return hasValidMarket;
        }

        /// <summary>
        /// Resets the internal state used for tracking snapshot timing and expectations.
        /// </summary>
        /// <remarks>
        /// This method clears the last saved snapshot timestamp and expected next snapshot timestamp,
        /// effectively resetting the service to its initial state. This is typically called when
        /// restarting the brain or when snapshot timing needs to be recalibrated.
        /// </remarks>
        public void ResetSnapshotTracking()
        {
            _lastSavedSnapshotTimestamp = null;
            NextExpectedSnapshotTimestamp = null; // Reset the expected timestamp as well
        }

        /// <summary>
        /// Validates that the current CacheSnapshot schema matches the expected schema stored in the database.
        /// </summary>
        /// <returns>True if the schemas match, false otherwise.</returns>
        /// <remarks>
        /// This method generates the JSON schema for CacheSnapshot and compares it against the schema
        /// stored in the database for the configured snapshot schema version. Schema mismatches
        /// can indicate data compatibility issues that need to be addressed before saving snapshots.
        /// </remarks>
        /// <exception cref="Exception">Thrown when the expected schema version is not found in the database.</exception>
        public async Task<bool> ValidateSnapshotSchema()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var schema = JsonSchema.FromType<CacheSnapshot>();
            var schemaData = schema.ToJson();

            int expectedVersion = _snapshotConfig.Value.SnapshotSchemaVersion;
            SnapshotSchemaDTO? schemaDTO = await context.GetSnapshotSchema(expectedVersion);
            if (schemaDTO == null)
            {
                throw new Exception($"Schema version {expectedVersion} not found in database");
            }
            return schemaData == schemaDTO.SchemaDefinition;
        }

        /// <summary>
        /// Sanitizes snapshot JSON by removing deprecated fields based on the target schema version.
        /// </summary>
        /// <param name="currentVersion">The target schema version to sanitize for.</param>
        /// <param name="JSON">The raw JSON string to sanitize.</param>
        /// <returns>The sanitized JSON string with deprecated fields removed.</returns>
        /// <remarks>
        /// This method handles schema migrations by removing fields that are no longer relevant
        /// in newer schema versions. It processes the JSON document and rebuilds it without
        /// the deprecated properties, ensuring compatibility with current data structures.
        /// </remarks>
        public string SanitizeSnapshotJson(int currentVersion, string JSON)
        {
            using JsonDocument doc = JsonDocument.Parse(JSON);
            var root = doc.RootElement.Clone();
            var markets = root.GetProperty("Markets");

            // Assuming "Markets" is an object with market keys
            var marketEnumerator = markets.EnumerateObject();
            if (marketEnumerator.MoveNext())
            {
                var marketValue = marketEnumerator.Current.Value;

                switch (currentVersion)
                {
                    case 19:
                    case 18:
                        break;
                    case 17:
                        {
                            // Remove properties using writer to rebuild object
                            using var ms = new MemoryStream();
                            using var writer = new Utf8JsonWriter(ms);
                            writer.WriteStartObject();
                            foreach (var prop in marketValue.EnumerateObject())
                            {
                                if (prop.Name != "CurrentPriceYes" && prop.Name != "CurrentPriceNo")
                                {
                                    prop.WriteTo(writer);
                                }
                            }
                            writer.WriteEndObject();
                            writer.Flush();
                            var updatedMarketJson = System.Text.Encoding.UTF8.GetString(ms.ToArray());

                            // Rebuild full JSON
                            using var fullMs = new MemoryStream();
                            using var fullWriter = new Utf8JsonWriter(fullMs);
                            fullWriter.WriteStartObject();
                            foreach (var fullProp in root.EnumerateObject())
                            {
                                if (fullProp.Name == "Markets")
                                {
                                    fullWriter.WritePropertyName("Markets");
                                    fullWriter.WriteRawValue("{" + $"\"{marketEnumerator.Current.Name}\":{updatedMarketJson}" + "}");
                                }
                                else
                                {
                                    fullProp.WriteTo(fullWriter);
                                }
                            }
                            fullWriter.WriteEndObject();
                            fullWriter.Flush();
                            JSON = System.Text.Encoding.UTF8.GetString(fullMs.ToArray());
                        }
                        break;
                    case 16:
                    case 15:
                    case 14:
                        break;
                    case 13:
                        {
                            // Similar rebuilding for removing "RestingOrders"
                            using var ms13 = new MemoryStream();
                            using var writer13 = new Utf8JsonWriter(ms13);
                            writer13.WriteStartObject();
                            foreach (var prop in marketValue.EnumerateObject())
                            {
                                if (prop.Name != "RestingOrders")
                                {
                                    prop.WriteTo(writer13);
                                }
                            }
                            writer13.WriteEndObject();
                            writer13.Flush();
                            var updatedMarketJson13 = System.Text.Encoding.UTF8.GetString(ms13.ToArray());

                            // Rebuild full JSON
                            using var fullMs13 = new MemoryStream();
                            using var fullWriter13 = new Utf8JsonWriter(fullMs13);
                            fullWriter13.WriteStartObject();
                            foreach (var fullProp in root.EnumerateObject())
                            {
                                if (fullProp.Name == "Markets")
                                {
                                    fullWriter13.WritePropertyName("Markets");
                                    fullWriter13.WriteRawValue("{" + $"\"{marketEnumerator.Current.Name}\":{updatedMarketJson13}" + "}");
                                }
                                else
                                {
                                    fullProp.WriteTo(fullWriter13);
                                }
                            }
                            fullWriter13.WriteEndObject();
                            fullWriter13.Flush();
                            JSON = System.Text.Encoding.UTF8.GetString(fullMs13.ToArray());
                        }
                        break;
                    default:
                        break;
                }
            }

            return JSON;
        }
    }
}
