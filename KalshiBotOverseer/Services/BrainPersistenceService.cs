using KalshiBotOverseer.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using System.Timers;
using System.Text.Json;

namespace KalshiBotOverseer.Services
{
    /// <summary>
    /// Configuration options for the BrainPersistenceService.
    /// </summary>
    public class BrainPersistenceServiceConfig
    {
        /// <summary>
        /// Gets or sets the maximum number of entries to keep in metric history lists.
        /// Default is 50.
        /// </summary>
        public int MaxHistoryEntries { get; set; } = 50;

        /// <summary>
        /// Gets or sets the mapping of metric names to their string identifiers.
        /// </summary>
        public Dictionary<string, string> MetricNames { get; set; } = new()
        {
            ["CpuUsage"] = "CpuUsage",
            ["EventQueue"] = "EventQueue",
            ["TickerQueue"] = "TickerQueue",
            ["NotificationQueue"] = "NotificationQueue",
            ["OrderbookQueue"] = "OrderbookQueue",
            ["MarketCount"] = "MarketCount",
            ["Error"] = "Error"
        };

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable data persistence to database.
        /// </summary>
        public bool EnablePersistence { get; set; } = false;

        /// <summary>
        /// Gets or sets the interval in minutes for saving data to persistence store.
        /// </summary>
        public int PersistenceSaveIntervalMinutes { get; set; } = 5;
    }

    /// <summary>
    /// Service responsible for managing brain instance states with configurable persistence options.
    /// This service provides thread-safe operations for storing, retrieving, and updating brain
    /// configuration, market watch lists, and performance metrics history. It supports both
    /// in-memory storage and optional database persistence for recovery after application restarts.
    /// The service includes configurable history retention, performance metrics collection,
    /// batch operations, and comprehensive health monitoring.
    /// </summary>
    public class BrainPersistenceService
    {
        private readonly ConcurrentDictionary<string, BrainPersistence> _brains = new();
        private readonly BrainPersistenceServiceConfig _config;
        private readonly ILogger<BrainPersistenceService>? _logger;
        private readonly IKalshiBotContext? _context;
        private readonly Stopwatch _serviceStopwatch = new();
        private long _totalOperations;
        private readonly object _metricsLock = new();
        private readonly System.Timers.Timer _persistenceTimer;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the BrainPersistenceService with configurable options.
        /// When persistence is enabled, automatically starts a background timer for periodic saves.
        /// </summary>
        /// <param name="config">Configuration options including history limits, metric names, and persistence settings.</param>
        /// <param name="context">Optional database context for persistence operations. Required when EnablePersistence is true.</param>
        /// <param name="logger">Optional logger for service operations and performance metrics.</param>
        public BrainPersistenceService(
            IOptions<BrainPersistenceServiceConfig> config,
            IKalshiBotContext? context = null,
            ILogger<BrainPersistenceService>? logger = null)
        {
            _config = config?.Value ?? new BrainPersistenceServiceConfig();
            _context = context;
            _logger = logger;
            _serviceStopwatch.Start();

            if (_config.EnablePersistence && _context != null)
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

                // Persist to database if enabled
                if (_config.EnablePersistence && _context != null)
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
        public IEnumerable<BrainPersistence> GetAllBrains()
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
        /// Records performance metrics for service operations.
        /// </summary>
        /// <param name="operationName">The name of the operation being measured.</param>
        /// <param name="elapsedMilliseconds">The time taken for the operation in milliseconds.</param>
        private void RecordOperationMetrics(string operationName, long elapsedMilliseconds)
        {
            if (!_config.EnablePerformanceMetrics)
                return;

            lock (_metricsLock)
            {
                _totalOperations++;
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
            if (_isInitialized || !_config.EnablePersistence || _context == null)
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
            if (!_config.EnablePersistence || _context == null)
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
        }

        /// <summary>
        /// Persists a specific brain to the database.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance to persist.</param>
        private async Task PersistBrainAsync(string brainInstanceName)
        {
            if (!_config.EnablePersistence || _context == null || !_brains.TryGetValue(brainInstanceName, out var brain))
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
        /// Disposes of the service and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _persistenceTimer?.Stop();
            _persistenceTimer?.Dispose();

            // Final persistence save
            if (_config.EnablePersistence && _context != null)
            {
                Task.Run(() => PersistAllBrainsAsync()).Wait();
            }
        }

        /// <summary>
        /// Retrieves the appropriate metric history list for a given brain and metric name.
        /// This method maps configurable metric names to their corresponding history collections.
        /// Supports custom metric name mappings through configuration for flexibility.
        /// </summary>
        /// <param name="brain">The BrainPersistence object containing the history lists.</param>
        /// <param name="metricName">The name of the metric to retrieve history for (supports configured aliases).</param>
        /// <returns>The list of MetricHistory entries for the specified metric.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown metric name is provided.</exception>
        private List<Models.MetricHistory> GetMetricHistoryList(BrainPersistence brain, string metricName)
        {
            if (_config.MetricNames.TryGetValue(metricName, out var configuredName))
            {
                metricName = configuredName;
            }

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