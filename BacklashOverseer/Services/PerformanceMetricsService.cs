using BacklashInterfaces.PerformanceMetrics;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BacklashOverseer.Services
{
    /// <summary>
    /// Centralized service for collecting and managing performance metrics across the KalshiBot Overseer system.
    /// This service aggregates metrics from various components including WebSocket operations, API calls,
    /// SignalR communications, overnight tasks, and snapshot processing.
    /// </summary>
    public class PerformanceMetricsService : BasePerformanceMonitor
    {
        private readonly ILogger<PerformanceMetricsService> _logger;

        // Configurable metrics data structure for GUI consumption
        private Dictionary<string, object> _configurableMetrics;

        // WebSocket metrics
        private long _webSocketEventCount;
        private DateTime _lastWebSocketEventTime;

        // API metrics
        private long _totalApiFetchTime;
        private int _apiFetchCount;
        private DateTime _lastApiFetchTime;

        // SignalR metrics
        private long _totalMessagesProcessed;
        private long _totalHandshakeRequests;
        private long _totalCheckInRequests;
        private List<long> _handshakeLatencies = new();
        private List<long> _checkInLatencies = new();
        private List<long> _messageLatencies = new();
        private DateTime _lastMetricsReset;

        // Overnight task metrics
        private int _totalOvernightTasks;
        private int _successfulOvernightTasks;
        private TimeSpan _totalOvernightDuration;
        private Dictionary<string, TimeSpan> _overnightTaskTimings = new();

        // Snapshot aggregation metrics
        private long _totalSnapshotAggregationTime;
        private int _snapshotAggregationCount;
        private List<long> _snapshotAggregationTimes = new();

        // System health metrics
        private int _marketRefreshFailureCount;
        private DateTime? _lastMarketRefreshFailure;

        // Database metrics
        private Dictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> _databaseMetrics = new();

        // BrainPersistence metrics
        private Dictionary<string, (long AverageMs, long P50Ms, long P95Ms, long P99Ms)> _brainPersistenceOperationStats = new();
        private IReadOnlyDictionary<string, int> _brainPersistenceTrimmingCounts = new Dictionary<string, int>();
        private (long TotalWaitTimeMs, int ContentionCount) _brainPersistenceLockMetrics;
        private Dictionary<string, long> _brainPersistenceMemoryUsage = new();
        private (int AvailableWorkerThreads, int AvailableCompletionPortThreads, int MaxWorkerThreads, int MaxCompletionPortThreads) _brainPersistenceThreadPoolInfo;

        // WebSocket metrics
        private readonly ConcurrentDictionary<string, long> _webSocketProcessingTimeTicks = new();
        private readonly ConcurrentDictionary<string, int> _webSocketProcessingCount = new();
        private readonly ConcurrentDictionary<string, long> _webSocketBufferUsageBytes = new();
        private readonly ConcurrentDictionary<string, TimeSpan> _webSocketOperationTimes = new();
        private readonly ConcurrentDictionary<string, int> _webSocketSemaphoreWaitCount = new();

        // Cache metrics
        private long _cacheHits;
        private long _cacheMisses;

        // SubscriptionManager performance metrics
        private IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)>? _subscriptionManagerOperationMetrics;
        private IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)>? _subscriptionManagerLockMetrics;

        // MessageProcessor performance metrics
        private long _messageProcessorTotalMessagesProcessed;
        private long _messageProcessorTotalProcessingTimeMs;
        private double _messageProcessorAverageProcessingTimeMs;
        private double _messageProcessorMessagesPerSecond;
        private int _messageProcessorOrderBookQueueDepth;
        private int _messageProcessorDuplicateMessageCount;
        private int _messageProcessorDuplicatesInWindow;
        private DateTime _messageProcessorLastDuplicateWarningTime;
        private IReadOnlyDictionary<string, long>? _messageProcessorMessageTypeCounts;

        /// <summary>
        /// Gets the current database performance metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> DatabaseMetrics => _databaseMetrics;

        /// <summary>
        /// Gets the current database performance metrics.
        /// </summary>
        /// <returns>Dictionary containing operation names and their performance statistics.</returns>
        public IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> GetPerformanceMetrics()
        {
            return _databaseMetrics;
        }

        /// <summary>
        /// Resets all performance metrics.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            _databaseMetrics.Clear();
        }

        // Locks for thread safety
        private readonly object _webSocketLock = new();
        private readonly object _apiLock = new();
        private readonly object _signalRLock = new();
        private readonly object _overnightLock = new();
        private readonly object _snapshotLock = new();
        private readonly object _healthLock = new();
        private readonly object _cacheLock = new();

        /// <summary>
        /// Initializes a new instance of the PerformanceMetricsService.
        /// </summary>
        /// <param name="logger">The logger instance for recording metrics operations.</param>
        public PerformanceMetricsService(ILogger<PerformanceMetricsService> logger)
            : base(logger)
        {
            _logger = logger;
            _lastMetricsReset = DateTime.UtcNow;
            _configurableMetrics = new Dictionary<string, object>();
            InitializeConfigurableMetrics();
            _logger.LogInformation("PerformanceMetricsService initialized");
        }

        /// <summary>
        /// Initializes the configurable metrics data structure with default values.
        /// </summary>
        private void InitializeConfigurableMetrics()
        {
            _configurableMetrics = new Dictionary<string, object>
            {
                // Only include whether performance metrics are enabled for this class
                ["EnablePerformanceMetrics"] = true
            };
        }

     

   
        #region SignalR Metrics



        /// <summary>
        /// Gets the current SignalR metrics.
        /// </summary>
        public (long MessagesProcessed, long HandshakeRequests, long CheckInRequests, DateTime LastReset,
                double AvgHandshakeLatencyMs, double AvgCheckInLatencyMs, double AvgMessageLatencyMs) GetSignalRMetrics()
        {
            lock (_signalRLock)
            {
                return (_totalMessagesProcessed, _totalHandshakeRequests, _totalCheckInRequests, _lastMetricsReset,
                        _handshakeLatencies.Count > 0 ? _handshakeLatencies.Average() : 0,
                        _checkInLatencies.Count > 0 ? _checkInLatencies.Average() : 0,
                        _messageLatencies.Count > 0 ? _messageLatencies.Average() : 0);
            }
        }

        /// <summary>
        /// Resets the SignalR metrics counters.
        /// </summary>
        public void ResetSignalRMetrics()
        {
            lock (_signalRLock)
            {
                _totalMessagesProcessed = 0;
                _totalHandshakeRequests = 0;
                _totalCheckInRequests = 0;
                _handshakeLatencies.Clear();
                _checkInLatencies.Clear();
                _messageLatencies.Clear();
                _lastMetricsReset = DateTime.UtcNow;
            }
        }

        #endregion

        #region Overnight Task Metrics

     


        /// <summary>
        /// Gets the current overnight task metrics.
        /// </summary>
        public (int TotalTasks, int SuccessfulTasks, double SuccessRate, TimeSpan TotalDuration, Dictionary<string, TimeSpan> TaskTimings) GetOvernightMetrics()
        {
            lock (_overnightLock)
            {
                return (_totalOvernightTasks, _successfulOvernightTasks,
                       _totalOvernightTasks > 0 ? (double)_successfulOvernightTasks / _totalOvernightTasks : 0,
                       _totalOvernightDuration, new Dictionary<string, TimeSpan>(_overnightTaskTimings));
            }
        }

        #endregion

     

        /// <summary>
        /// Gets the current snapshot aggregation metrics.
        /// </summary>
        public (int Count, double AverageMs, long MinMs, long MaxMs, long TotalMs) GetSnapshotAggregationMetrics()
        {
            lock (_snapshotLock)
            {
                if (_snapshotAggregationTimes.Count == 0)
                    return (0, 0, 0, 0, 0);

                return (
                    _snapshotAggregationCount,
                    _snapshotAggregationTimes.Average(),
                    _snapshotAggregationTimes.Min(),
                    _snapshotAggregationTimes.Max(),
                    _totalSnapshotAggregationTime
                );
            }
        }

        /// <summary>
        /// Gets all recorded snapshot aggregation times.
        /// </summary>
        public long[] GetSnapshotAggregationTimes()
        {
            lock (_snapshotLock)
            {
                return _snapshotAggregationTimes.ToArray();
            }
        }

        /// <summary>
        /// Clears the snapshot aggregation metrics.
        /// </summary>
        public void ClearSnapshotAggregationMetrics()
        {
            lock (_snapshotLock)
            {
                _snapshotAggregationTimes.Clear();
                _totalSnapshotAggregationTime = 0;
                _snapshotAggregationCount = 0;
            }
        }





        #region Metrics Status

        /// <summary>
        /// Gets a comprehensive status of all metrics being captured.
        /// </summary>
        public Dictionary<string, object> GetMetricsStatus()
        {
            var status = new Dictionary<string, object>();

            // WebSocket metrics
            lock (_webSocketLock)
            {
                status["WebSocketEventCount"] = _webSocketEventCount;
                status["LastWebSocketEventTime"] = _lastWebSocketEventTime;
            }

            // API metrics
            lock (_apiLock)
            {
                status["TotalApiFetchTime"] = _totalApiFetchTime;
                status["ApiFetchCount"] = _apiFetchCount;
                status["LastApiFetchTime"] = _lastApiFetchTime;
            }

            // SignalR metrics
            lock (_signalRLock)
            {
                status["TotalMessagesProcessed"] = _totalMessagesProcessed;
                status["TotalHandshakeRequests"] = _totalHandshakeRequests;
                status["TotalCheckInRequests"] = _totalCheckInRequests;
            }

            // Overnight task metrics
            lock (_overnightLock)
            {
                status["TotalOvernightTasks"] = _totalOvernightTasks;
                status["SuccessfulOvernightTasks"] = _successfulOvernightTasks;
            }

            // Snapshot aggregation metrics
            lock (_snapshotLock)
            {
                status["SnapshotAggregationCount"] = _snapshotAggregationCount;
                status["TotalSnapshotAggregationTime"] = _totalSnapshotAggregationTime;
                status["SnapshotAggregationTimesCount"] = _snapshotAggregationTimes.Count;
            }

            // System health metrics
            lock (_healthLock)
            {
                status["MarketRefreshFailureCount"] = _marketRefreshFailureCount;
                status["LastMarketRefreshFailure"] = _lastMarketRefreshFailure;
            }

            // BrainPersistence metrics
            lock (_snapshotLock)
            {
                status["BrainPersistenceOperationStatsCount"] = _brainPersistenceOperationStats.Count;
                status["BrainPersistenceTrimmingCountsCount"] = _brainPersistenceTrimmingCounts.Count;
                status["BrainPersistenceLockTotalWaitTimeMs"] = _brainPersistenceLockMetrics.TotalWaitTimeMs;
                status["BrainPersistenceLockContentionCount"] = _brainPersistenceLockMetrics.ContentionCount;
                status["BrainPersistenceMemoryUsageCount"] = _brainPersistenceMemoryUsage.Count;
                status["BrainPersistenceThreadPoolAvailableWorkers"] = _brainPersistenceThreadPoolInfo.AvailableWorkerThreads;
                status["BrainPersistenceThreadPoolAvailableCompletions"] = _brainPersistenceThreadPoolInfo.AvailableCompletionPortThreads;
                status["BrainPersistenceThreadPoolMaxWorkers"] = _brainPersistenceThreadPoolInfo.MaxWorkerThreads;
                status["BrainPersistenceThreadPoolMaxCompletions"] = _brainPersistenceThreadPoolInfo.MaxCompletionPortThreads;
            }

            // Cache metrics
            lock (_cacheLock)
            {
                status["CacheHits"] = _cacheHits;
                status["CacheMisses"] = _cacheMisses;
                long total = _cacheHits + _cacheMisses;
                status["CacheHitRate"] = total > 0 ? (_cacheHits / (double)total) * 100 : 0;
            }

            status["LastMetricsReset"] = _lastMetricsReset;
            status["ServiceUptime"] = DateTime.UtcNow - _lastMetricsReset;

            return status;
        }


        /// <summary>
        /// Logs the current metrics status for debugging purposes.
        /// </summary>
        public void LogMetricsStatus()
        {
            var status = GetMetricsStatus();
            _logger.LogInformation("Performance Metrics Status:");
            foreach (var kvp in status)
            {
                _logger.LogInformation("  {Key}: {Value}", kvp.Key, kvp.Value);
            }
        }

        #endregion


        /// <summary>
        /// Gets the average processing times for WebSocket messages.
        /// </summary>
        public ConcurrentDictionary<string, double> GetAverageProcessingTimesMs()
        {
            return new ConcurrentDictionary<string, double>(
                _webSocketProcessingTimeTicks.ToDictionary(
                    kv => kv.Key,
                    kv => _webSocketProcessingCount.TryGetValue(kv.Key, out var count) && count > 0
                        ? TimeSpan.FromTicks(kv.Value / count).TotalMilliseconds
                        : 0.0
                )
            );
        }

        /// <summary>
        /// Gets the total buffer usage for WebSocket messages.
        /// </summary>
        public ConcurrentDictionary<string, long> GetBufferUsageBytes()
        {
            return new ConcurrentDictionary<string, long>(_webSocketBufferUsageBytes);
        }

        /// <summary>
        /// Gets the average times for WebSocket operations.
        /// </summary>
        public ConcurrentDictionary<string, double> GetAsyncOperationTimesMs()
        {
            return new ConcurrentDictionary<string, double>(
                _webSocketOperationTimes.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.TotalMilliseconds
                )
            );
        }

        /// <summary>
        /// Gets the semaphore wait counts for WebSocket operations.
        /// </summary>
        public ConcurrentDictionary<string, int> GetSemaphoreWaitCounts()
        {
            return new ConcurrentDictionary<string, int>(_webSocketSemaphoreWaitCount);
        }

        /// <summary>
        /// Resets all WebSocket performance metrics.
        /// </summary>
        public void ResetWebSocketMetrics()
        {
            _webSocketProcessingTimeTicks.Clear();
            _webSocketProcessingCount.Clear();
            _webSocketBufferUsageBytes.Clear();
            _webSocketOperationTimes.Clear();
            _webSocketSemaphoreWaitCount.Clear();
            _logger.LogInformation("WebSocket performance metrics reset");
        }




    }
}
