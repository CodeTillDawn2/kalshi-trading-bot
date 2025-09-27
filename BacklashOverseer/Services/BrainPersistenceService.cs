using BacklashBotData.Data.Interfaces;
using BacklashInterfaces.PerformanceMetrics;
using BacklashOverseer.Config;
using BacklashOverseer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Timers;

namespace BacklashOverseer.Services
{

    /// <summary>
    /// Service responsible for managing brain instance states with database persistence.
    /// This service provides thread-safe operations for storing, retrieving, and updating brain
    /// configuration, market watch lists, and performance metrics history. It supports
    /// in-memory storage and database persistence for recovery after application restarts.
    /// The service includes configurable history retention, performance metrics collection,
    /// batch operations, and comprehensive health monitoring.
    /// </summary>
    public class BrainPersistenceService
    {
        private readonly ConcurrentDictionary<string, BrainPersistence> _brains = new();
        private readonly BrainPersistenceServiceConfig _config;
        private readonly ILogger<BrainPersistenceService>? _logger;
        private readonly IBacklashBotContext? _context;
        private readonly IPerformanceMonitor? _performanceMonitor;
        private readonly Stopwatch _serviceStopwatch = new();
        private long _totalOperations;
        private readonly Dictionary<string, List<long>> _operationTimings = new();
        private readonly Dictionary<string, int> _trimmingCounts = new();
        private long _totalLockWaitTime;
        private int _lockContentionCount;
        private readonly object _metricsLock = new();
        private readonly System.Timers.Timer? _persistenceTimer;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the BrainPersistenceService.
        /// Automatically starts a background timer for periodic saves if context is provided.
        /// </summary>
        /// <param name="config">Configuration options including history limits and metric settings.</param>
        /// <param name="context">Database context for persistence operations.</param>
        /// <param name="logger">Optional logger for service operations and performance metrics.</param>
        /// <param name="performanceMonitor">Optional performance monitor for transmitting metrics.</param>
        public BrainPersistenceService(
            IOptions<BrainPersistenceServiceConfig> config,
            IBacklashBotContext? context = null,
            ILogger<BrainPersistenceService>? logger = null,
            IPerformanceMonitor? performanceMonitor = null)
        {
            _config = config.Value;
            _context = context;
            _logger = logger;
            _performanceMonitor = performanceMonitor;
            _serviceStopwatch.Start();

            if (_context != null)
            {
                _persistenceTimer = new System.Timers.Timer(_config.PersistenceSaveIntervalMinutes * 60 * 1000);
                _persistenceTimer.Elapsed += OnPersistenceTimerElapsed;
                _persistenceTimer.Start();
            }
        }

        /// <summary>
        /// Retrieves or creates a brain instance by name. If the brain doesn't exist,
        /// a new instance is created with the specified name and added to the collection.
        /// </summary>
        /// <param name="brainInstanceName">The unique name identifier for the brain instance.</param>
        /// <returns>The BrainPersistence object for the specified brain instance.</returns>
        /// <exception cref="ArgumentException">Thrown when brainInstanceName is null or empty.</exception>
        public BrainPersistence GetBrain(string brainInstanceName)
        {
            if (string.IsNullOrWhiteSpace(brainInstanceName))
                throw new ArgumentException("Brain instance name cannot be null or empty.", nameof(brainInstanceName));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = _brains.GetOrAdd(brainInstanceName, name => new BrainPersistence { BrainInstanceName = name });
                RecordOperationMetrics("GetBrain", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting brain instance {BrainInstanceName}", brainInstanceName);
                throw;
            }
        }

        /// <summary>
        /// Saves or updates a brain instance in the persistence store asynchronously.
        /// This method replaces any existing brain instance with the same name and
        /// optionally persists to database if persistence is enabled.
        /// </summary>
        /// <param name="brain">The BrainPersistence object to save.</param>
        /// <exception cref="ArgumentNullException">Thrown when brain is null.</exception>
        /// <returns>A task representing the asynchronous save operation.</returns>
        public async Task SaveBrainAsync(BrainPersistence brain)
        {
            if (brain == null)
                throw new ArgumentNullException(nameof(brain));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                _brains[brain.BrainInstanceName] = brain;

                // Persist to database
                if (_context != null)
                {
                    await PersistBrainAsync(brain.BrainInstanceName);
                }

                RecordOperationMetrics("SaveBrain", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving brain instance {BrainInstanceName}", brain.BrainInstanceName);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the target market tickers for a specific brain instance.
        /// Target tickers represent the markets the brain should be monitoring.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance.</param>
        /// <returns>An enumerable collection of target market ticker symbols.</returns>
        /// <exception cref="ArgumentException">Thrown when brainInstanceName is invalid.</exception>
        public IEnumerable<string> GetTargetMarketTickers(string brainInstanceName)
        {
            if (string.IsNullOrWhiteSpace(brainInstanceName))
                throw new ArgumentException("Brain instance name cannot be null or empty.", nameof(brainInstanceName));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var brain = GetBrain(brainInstanceName);
                var result = brain.TargetMarketTickers.AsEnumerable();
                RecordOperationMetrics("GetTargetMarketTickers", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting target market tickers for brain {BrainInstanceName}", brainInstanceName);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all brain instances currently stored in the persistence service.
        /// This provides access to the complete set of brain configurations and states.
        /// </summary>
        /// <returns>An enumerable collection of all BrainPersistence objects.</returns>
        public virtual IEnumerable<BrainPersistence> GetAllBrains()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = _brains.Values.AsEnumerable();
                RecordOperationMetrics("GetAllBrains", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all brain instances");
                throw;
            }
        }

        /// <summary>
        /// Updates the current market tickers for a brain instance asynchronously.
        /// This reflects the markets the brain is actively watching at runtime.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance to update.</param>
        /// <param name="tickers">The collection of market ticker symbols currently being watched.</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        /// <returns>A task representing the asynchronous update operation.</returns>
        public async Task UpdateCurrentMarketTickersAsync(string brainInstanceName, IEnumerable<string> tickers)
        {
            if (string.IsNullOrWhiteSpace(brainInstanceName))
                throw new ArgumentException("Brain instance name cannot be null or empty.", nameof(brainInstanceName));
            if (tickers == null)
                throw new ArgumentNullException(nameof(tickers));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var brain = GetBrain(brainInstanceName);
                brain.CurrentMarketTickers = new List<string>(tickers);
                await SaveBrainAsync(brain);
                RecordOperationMetrics("UpdateCurrentMarketTickers", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating current market tickers for brain {BrainInstanceName}", brainInstanceName);
                throw;
            }
        }

        /// <summary>
        /// Updates the historical metrics for a specific brain instance asynchronously.
        /// Metrics are stored in rolling lists with a configurable maximum number of entries to prevent memory issues.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance.</param>
        /// <param name="metricName">The name of the metric to update (e.g., "CpuUsage", "EventQueue").</param>
        /// <param name="value">The metric value to record.</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        /// <returns>A task representing the asynchronous metric update operation.</returns>
        public async Task UpdateMetricHistoryAsync(string brainInstanceName, string metricName, double value)
        {
            if (string.IsNullOrWhiteSpace(brainInstanceName))
                throw new ArgumentException("Brain instance name cannot be null or empty.", nameof(brainInstanceName));
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty.", nameof(metricName));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var brain = GetBrain(brainInstanceName);
                var history = GetMetricHistoryList(brain, metricName);
                history.Add(new Models.MetricHistory { Timestamp = DateTime.UtcNow, Value = value });

                // Keep only last configured entries to prevent memory issues
                if (history.Count > _config.MaxHistoryEntries)
                {
                    history.RemoveRange(0, history.Count - _config.MaxHistoryEntries);
                    if (_config.EnablePerformanceMetrics)
                    {
                        lock (_metricsLock)
                        {
                            if (!_trimmingCounts.ContainsKey(metricName))
                                _trimmingCounts[metricName] = 0;
                            _trimmingCounts[metricName]++;
                        }
                    }
                }

                await SaveBrainAsync(brain);
                RecordOperationMetrics("UpdateMetricHistory", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating metric history for brain {BrainInstanceName}, metric {MetricName}",
                    brainInstanceName, metricName);
                throw;
            }
        }

        /// <summary>
        /// Updates comprehensive performance metrics for a brain instance asynchronously.
        /// This method stores the performance metrics object as-is for monitoring purposes.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance.</param>
        /// <param name="performanceMetrics">The comprehensive performance metrics data.</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        /// <returns>A task representing the asynchronous performance metrics update operation.</returns>
        public async Task UpdatePerformanceMetricsAsync(string brainInstanceName, object performanceMetrics)
        {
            if (string.IsNullOrWhiteSpace(brainInstanceName))
                throw new ArgumentException("Brain instance name cannot be null or empty.", nameof(brainInstanceName));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var brain = GetBrain(brainInstanceName);

                // Simply store the performance metrics object as-is
                brain.LatestPerformanceMetrics = performanceMetrics;

                await SaveBrainAsync(brain);
                RecordOperationMetrics("UpdatePerformanceMetrics", stopwatch.ElapsedMilliseconds);

                _logger?.LogInformation("Updated comprehensive performance metrics for brain {BrainName}", brainInstanceName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating performance metrics for brain {BrainInstanceName}", brainInstanceName);
                throw;
            }
        }

        /// <summary>
        /// Records performance metrics for service operations.
        /// </summary>
        /// <param name="operationName">The name of the operation being measured.</param>
        /// <param name="elapsedMilliseconds">The time taken for the operation in milliseconds.</param>
        private void RecordOperationMetrics(string operationName, long elapsedMilliseconds)
        {
            if (!_config.EnablePerformanceMetrics)
                return;

            var lockStart = Stopwatch.GetTimestamp();
            lock (_metricsLock)
            {
                var lockWaitTicks = Stopwatch.GetTimestamp() - lockStart;
                _totalLockWaitTime += (long)(lockWaitTicks * 1000.0 / Stopwatch.Frequency);
                _lockContentionCount++;
                _totalOperations++;
                if (!_operationTimings.ContainsKey(operationName))
                    _operationTimings[operationName] = new List<long>();
                _operationTimings[operationName].Add(elapsedMilliseconds);
                // Keep only last 1000 entries to prevent unbounded growth
                if (_operationTimings[operationName].Count > 1000)
                    _operationTimings[operationName].RemoveAt(0);
                _logger?.LogDebug("Operation {OperationName} completed in {ElapsedMs}ms", operationName, elapsedMilliseconds);
            }
        }

        /// <summary>
        /// Performs batch update operations for multiple metrics asynchronously to reduce save operations.
        /// This method updates multiple metrics in a single operation, improving performance by minimizing
        /// database persistence calls.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance.</param>
        /// <param name="metrics">Dictionary of metric names and their values to update.</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        /// <returns>A task representing the asynchronous batch update operation.</returns>
        public async Task UpdateMetricHistoriesBatchAsync(string brainInstanceName, Dictionary<string, double> metrics)
        {
            if (string.IsNullOrWhiteSpace(brainInstanceName))
                throw new ArgumentException("Brain instance name cannot be null or empty.", nameof(brainInstanceName));
            if (metrics == null || metrics.Count == 0)
                throw new ArgumentException("Metrics dictionary cannot be null or empty.", nameof(metrics));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var brain = GetBrain(brainInstanceName);
                bool hasChanges = false;

                foreach (var (metricName, value) in metrics)
                {
                    if (string.IsNullOrWhiteSpace(metricName))
                        throw new ArgumentException("Metric name cannot be null or empty.", nameof(metricName));

                    var history = GetMetricHistoryList(brain, metricName);
                    history.Add(new Models.MetricHistory { Timestamp = DateTime.UtcNow, Value = value });

                    // Keep only last configured entries to prevent memory issues
                    if (history.Count > _config.MaxHistoryEntries)
                    {
                        history.RemoveRange(0, history.Count - _config.MaxHistoryEntries);
                        if (_config.EnablePerformanceMetrics)
                        {
                            lock (_metricsLock)
                            {
                                if (!_trimmingCounts.ContainsKey(metricName))
                                    _trimmingCounts[metricName] = 0;
                                _trimmingCounts[metricName]++;
                            }
                        }
                    }
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    await SaveBrainAsync(brain);
                }

                RecordOperationMetrics("UpdateMetricHistoriesBatch", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating metric histories batch for brain {BrainInstanceName}",
                    brainInstanceName);
                throw;
            }
        }

        /// <summary>
        /// Performs comprehensive health checks to monitor service status and resource usage.
        /// Returns detailed statistics including memory usage, brain instance count, total history entries,
        /// and service uptime for monitoring and diagnostics.
        /// </summary>
        /// <returns>A tuple containing health metrics: (TotalMemoryBytes, BrainCount, TotalHistoryEntries, ServiceUptime).</returns>
        public (long TotalMemoryBytes, int BrainCount, long TotalHistoryEntries, TimeSpan ServiceUptime) GetHealthStatus()
        {
            var totalHistoryEntries = 0L;
            foreach (var brain in _brains.Values)
            {
                totalHistoryEntries += brain.CpuUsageHistory.Count +
                                      brain.EventQueueHistory.Count +
                                      brain.TickerQueueHistory.Count +
                                      brain.NotificationQueueHistory.Count +
                                      brain.OrderbookQueueHistory.Count +
                                      brain.MarketCountHistory.Count +
                                      brain.ErrorHistory.Count +
                                      brain.RefreshCycleSecondsHistory.Count +
                                      brain.RefreshCycleIntervalHistory.Count +
                                      brain.RefreshMarketCountHistory.Count +
                                      brain.RefreshUsagePercentageHistory.Count +
                                      brain.PerformanceSampleDateHistory.Count;
            }

            return (
                GC.GetTotalMemory(false),
                _brains.Count,
                totalHistoryEntries,
                _serviceStopwatch.Elapsed
            );
        }

        /// <summary>
        /// Initializes the service by loading persisted brain data from the database.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized || _context == null)
                return;

            try
            {
                var brainNames = await _context.GetAllBrainPersistenceNames();
                foreach (var brainName in brainNames)
                {
                    var persistenceData = await _context.LoadBrainPersistence(brainName);
                    if (!string.IsNullOrEmpty(persistenceData))
                    {
                        try
                        {
                            var brain = JsonSerializer.Deserialize<BrainPersistence>(persistenceData);
                            if (brain != null)
                            {
                                _brains[brainName] = brain;
                                _logger?.LogInformation("Loaded persisted brain data for {BrainName}", brainName);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Failed to deserialize persisted brain data for {BrainName}", brainName);
                        }
                    }
                }
                _isInitialized = true;
                _logger?.LogInformation("BrainPersistenceService initialized with {BrainCount} persisted brains", _brains.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize BrainPersistenceService from persistence store");
            }
        }

        /// <summary>
        /// Persists all brain data to the database.
        /// </summary>
        public async Task PersistAllBrainsAsync()
        {
            if (_context == null)
                return;

            var stopwatch = Stopwatch.StartNew();
            try
            {
                foreach (var (brainName, brain) in _brains)
                {
                    var persistenceData = JsonSerializer.Serialize(brain);
                    await _context.SaveBrainPersistence(brainName, persistenceData);
                }
                RecordOperationMetrics("PersistAllBrains", stopwatch.ElapsedMilliseconds);
                _logger?.LogDebug("Persisted {BrainCount} brains to database", _brains.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist brains to database");
            }
        }

        /// <summary>
        /// Handles the persistence timer elapsed event.
        /// </summary>
        private async void OnPersistenceTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            await PersistAllBrainsAsync();
            TransmitMetrics();
        }

        /// <summary>
        /// Persists a specific brain to the database.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance to persist.</param>
        private async Task PersistBrainAsync(string brainInstanceName)
        {
            if (_context == null || !_brains.TryGetValue(brainInstanceName, out var brain))
                return;

            try
            {
                var persistenceData = JsonSerializer.Serialize(brain);
                await _context.SaveBrainPersistence(brainInstanceName, persistenceData);
                _logger?.LogDebug("Persisted brain {BrainName} to database", brainInstanceName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist brain {BrainName} to database", brainInstanceName);
            }
        }

        /// <summary>
        /// Gets the total number of operations performed by this service.
        /// </summary>
        public long TotalOperations => _totalOperations;

        /// <summary>
        /// Gets the service uptime as a TimeSpan.
        /// </summary>
        public TimeSpan ServiceUptime => _serviceStopwatch.Elapsed;

        /// <summary>
        /// Gets the current number of brain instances being managed.
        /// </summary>
        public int BrainCount => _brains.Count;

        /// <summary>
        /// Gets the total number of metric history entries across all brain instances.
        /// </summary>
        public long TotalHistoryEntries
        {
            get
            {
                long total = 0;
                foreach (var brain in _brains.Values)
                {
                    total += brain.CpuUsageHistory.Count +
                             brain.EventQueueHistory.Count +
                             brain.TickerQueueHistory.Count +
                             brain.NotificationQueueHistory.Count +
                             brain.OrderbookQueueHistory.Count +
                             brain.MarketCountHistory.Count +
                             brain.ErrorHistory.Count +
                             brain.RefreshCycleSecondsHistory.Count +
                             brain.RefreshCycleIntervalHistory.Count +
                             brain.RefreshMarketCountHistory.Count +
                             brain.RefreshUsagePercentageHistory.Count +
                             brain.PerformanceSampleDateHistory.Count;
                }
                return total;
            }
        }

        /// <summary>
        /// Gets operation performance statistics including averages and percentiles.
        /// </summary>
        public IReadOnlyDictionary<string, (long AverageMs, long P50Ms, long P95Ms, long P99Ms)> GetOperationStats()
        {
            if (!_config.EnablePerformanceMetrics)
                return new Dictionary<string, (long, long, long, long)>();

            var result = new Dictionary<string, (long, long, long, long)>();
            lock (_metricsLock)
            {
                foreach (var kvp in _operationTimings)
                {
                    var times = kvp.Value.OrderBy(x => x).ToList();
                    if (times.Count == 0) continue;
                    var avg = (long)times.Average();
                    var p50 = times[times.Count / 2];
                    var p95Index = (int)(times.Count * 0.95);
                    var p95 = times[Math.Min(p95Index, times.Count - 1)];
                    var p99Index = (int)(times.Count * 0.99);
                    var p99 = times[Math.Min(p99Index, times.Count - 1)];
                    result[kvp.Key] = (avg, p50, p95, p99);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the count of history trimmings per metric type.
        /// </summary>
        public IReadOnlyDictionary<string, int> TrimmingCounts => _config.EnablePerformanceMetrics ? _trimmingCounts : new Dictionary<string, int>();

        /// <summary>
        /// Gets lock contention metrics.
        /// </summary>
        public (long TotalWaitTimeMs, int ContentionCount) LockMetrics
        {
            get
            {
                if (!_config.EnablePerformanceMetrics)
                    return (0, 0);

                lock (_metricsLock)
                {
                    return (_totalLockWaitTime, _lockContentionCount);
                }
            }
        }

        /// <summary>
        /// Estimates memory usage for a specific brain instance.
        /// </summary>
        public long GetMemoryUsageForBrain(string brainInstanceName)
        {
            if (!_config.EnablePerformanceMetrics)
                return 0;

            if (!_brains.TryGetValue(brainInstanceName, out var brain))
                return 0;
            // Rough estimate based on JSON serialization size
            try
            {
                var json = JsonSerializer.Serialize(brain);
                return json.Length * 2; // Approximate bytes (UTF-16 to bytes)
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets current thread pool information.
        /// </summary>
        public (int AvailableWorkerThreads, int AvailableCompletionPortThreads, int MaxWorkerThreads, int MaxCompletionPortThreads) GetThreadPoolInfo()
        {
            ThreadPool.GetAvailableThreads(out int worker, out int completion);
            ThreadPool.GetMaxThreads(out int maxWorker, out int maxCompletion);
            return (worker, completion, maxWorker, maxCompletion);
        }

        /// <summary>
        /// Transmits current performance metrics to the PerformanceMetricsService.
        /// </summary>
        private void TransmitMetrics()
        {
            if (_performanceMonitor == null || !_config.EnablePerformanceMetrics)
                return;

            // Record brain count
            _performanceMonitor.RecordCounterMetric(
                className: "BrainPersistenceService",
                id: "BrainCount",
                name: "Brain Count",
                description: "Number of brain instances managed",
                value: _brains.Count,
                unit: "count",
                category: "Persistence",
                metricsEnabled: true);

            // Record total operations
            _performanceMonitor.RecordCounterMetric(
                className: "BrainPersistenceService",
                id: "TotalOperations",
                name: "Total Operations",
                description: "Total number of operations performed",
                value: _totalOperations,
                unit: "count",
                category: "Persistence",
                metricsEnabled: true);

            // Record total history entries
            _performanceMonitor.RecordCounterMetric(
                className: "BrainPersistenceService",
                id: "TotalHistoryEntries",
                name: "Total History Entries",
                description: "Total number of metric history entries",
                value: TotalHistoryEntries,
                unit: "count",
                category: "Persistence",
                metricsEnabled: true);
        }

        /// <summary>
        /// Disposes of the service and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _persistenceTimer?.Stop();
            _persistenceTimer?.Dispose();

            // Final persistence save
            if (_context != null)
            {
                Task.Run(() => PersistAllBrainsAsync()).Wait();
            }
        }

        /// <summary>
        /// Retrieves the appropriate metric history list for a given brain and metric name.
        /// This method maps metric names directly to their corresponding history collections.
        /// </summary>
        /// <param name="brain">The BrainPersistence object containing the history lists.</param>
        /// <param name="metricName">The name of the metric to retrieve history for.</param>
        /// <returns>The list of MetricHistory entries for the specified metric.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown metric name is provided.</exception>
        private List<Models.MetricHistory> GetMetricHistoryList(BrainPersistence brain, string metricName)
        {
            return metricName switch
            {
                "CpuUsage" => brain.CpuUsageHistory,
                "EventQueue" => brain.EventQueueHistory,
                "TickerQueue" => brain.TickerQueueHistory,
                "NotificationQueue" => brain.NotificationQueueHistory,
                "OrderbookQueue" => brain.OrderbookQueueHistory,
                "MarketCount" => brain.MarketCountHistory,
                "Error" => brain.ErrorHistory,
                _ => throw new ArgumentException($"Unknown metric: {metricName}")
            };
        }
    }
}
