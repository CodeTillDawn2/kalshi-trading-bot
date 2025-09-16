using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using BacklashInterfaces.PerformanceMetrics;
using System.Collections.Concurrent;

namespace BacklashOverseer.Services
{
    /// <summary>
    /// Centralized service for collecting and managing performance metrics across the KalshiBot Overseer system.
    /// This service aggregates metrics from various components including WebSocket operations, API calls,
    /// SignalR communications, overnight tasks, and snapshot processing.
    /// </summary>
    public class PerformanceMetricsService : IKalshiBotContextPerformanceMetrics, IWebSocketPerformanceMetrics, ISqlDataServicePerformanceMetrics, ISubscriptionManagerPerformanceMetrics, IMessageProcessorPerformanceMetrics, IPerformanceMonitor
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

        #region WebSocket Metrics

        /// <summary>
        /// Records a WebSocket event occurrence.
        /// </summary>
        public void RecordWebSocketEvent()
        {
            lock (_webSocketLock)
            {
                _webSocketEventCount++;
                _lastWebSocketEventTime = DateTime.UtcNow;
            }
        }


        #endregion

        #region API Metrics

        /// <summary>
        /// Records an API fetch operation with its duration.
        /// </summary>
        /// <param name="duration">The duration of the API fetch operation.</param>
        public void RecordApiFetch(TimeSpan duration)
        {
            lock (_apiLock)
            {
                _totalApiFetchTime += (long)duration.TotalMilliseconds;
                _apiFetchCount++;
                _lastApiFetchTime = DateTime.UtcNow;
            }
        }


        #endregion

        #region SignalR Metrics

        /// <summary>
        /// Records a SignalR message processing event.
        /// </summary>
        public void RecordSignalRMessage()
        {
            lock (_signalRLock)
            {
                _totalMessagesProcessed++;
            }
        }

        /// <summary>
        /// Records a SignalR handshake request.
        /// </summary>
        public void RecordSignalRHandshake()
        {
            lock (_signalRLock)
            {
                _totalHandshakeRequests++;
            }
        }

        /// <summary>
        /// Records a SignalR check-in request.
        /// </summary>
        public void RecordSignalRCheckIn()
        {
            lock (_signalRLock)
            {
                _totalCheckInRequests++;
            }
        }

        /// <summary>
        /// Records the latency of a SignalR handshake operation.
        /// </summary>
        /// <param name="latency">The latency duration.</param>
        public void RecordSignalRHandshakeLatency(TimeSpan latency)
        {
            lock (_signalRLock)
            {
                _handshakeLatencies.Add((long)latency.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Records the latency of a SignalR check-in operation.
        /// </summary>
        /// <param name="latency">The latency duration.</param>
        public void RecordSignalRCheckInLatency(TimeSpan latency)
        {
            lock (_signalRLock)
            {
                _checkInLatencies.Add((long)latency.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Records the latency of a general SignalR message processing operation.
        /// </summary>
        /// <param name="latency">The latency duration.</param>
        public void RecordSignalRMessageLatency(TimeSpan latency)
        {
            lock (_signalRLock)
            {
                _messageLatencies.Add((long)latency.TotalMilliseconds);
            }
        }

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
        /// Records an overnight task execution.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        /// <param name="duration">The duration of the task execution.</param>
        /// <param name="success">Whether the task was successful.</param>
        public void RecordOvernightTask(string taskName, TimeSpan duration, bool success)
        {
            lock (_overnightLock)
            {
                _totalOvernightTasks++;
                if (success) _successfulOvernightTasks++;
                _totalOvernightDuration += duration;
                _overnightTaskTimings[taskName] = duration;
            }
        }


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

        #region Snapshot Aggregation Metrics

        /// <summary>
        /// Records a snapshot aggregation operation with its duration.
        /// </summary>
        /// <param name="duration">The duration of the snapshot aggregation operation.</param>
        public void RecordSnapshotAggregation(TimeSpan duration)
        {
            lock (_snapshotLock)
            {
                _totalSnapshotAggregationTime += (long)duration.TotalMilliseconds;
                _snapshotAggregationCount++;
                _snapshotAggregationTimes.Add((long)duration.TotalMilliseconds);
                _logger.LogDebug("Snapshot aggregation recorded: Duration={Duration}ms, TotalCount={Count}, TotalTime={TotalTime}ms",
                    duration.TotalMilliseconds, _snapshotAggregationCount, _totalSnapshotAggregationTime);
            }
        }

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

        #endregion

        #region System Health Metrics

        /// <summary>
        /// Records a market refresh failure.
        /// </summary>
        public void RecordMarketRefreshFailure()
        {
            lock (_healthLock)
            {
                _marketRefreshFailureCount++;
                _lastMarketRefreshFailure = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Resets the market refresh failure count.
        /// </summary>
        public void ResetMarketRefreshFailures()
        {
            lock (_healthLock)
            {
                _marketRefreshFailureCount = 0;
                _lastMarketRefreshFailure = null;
            }
        }

        /// <summary>
        /// Gets the current system health metrics.
        /// </summary>
        public (int MarketRefreshFailureCount, DateTime? LastMarketRefreshFailure) GetHealthMetrics()
        {
            lock (_healthLock)
            {
                return (_marketRefreshFailureCount, _lastMarketRefreshFailure);
            }
        }

        #endregion

        #region Cache Metrics

        /// <summary>
        /// Records a cache hit event.
        /// </summary>
        public void RecordCacheHit()
        {
            lock (_cacheLock)
            {
                _cacheHits++;
            }
        }

        /// <summary>
        /// Records a cache miss event.
        /// </summary>
        public void RecordCacheMiss()
        {
            lock (_cacheLock)
            {
                _cacheMisses++;
            }
        }

        /// <summary>
        /// Posts cache performance metrics from a component.
        /// </summary>
        /// <param name="cacheHits">Number of cache hits to add.</param>
        /// <param name="cacheMisses">Number of cache misses to add.</param>
        public void PostCacheMetrics(long cacheHits, long cacheMisses)
        {
            lock (_cacheLock)
            {
                _cacheHits += cacheHits;
                _cacheMisses += cacheMisses;
                _logger.LogDebug("Cache metrics posted: Hits={Hits}, Misses={Misses}", cacheHits, cacheMisses);
            }
        }

        /// <summary>
        /// Gets the current cache metrics.
        /// </summary>
        public (long CacheHits, long CacheMisses, double HitRate) GetCacheMetrics()
        {
            lock (_cacheLock)
            {
                long total = _cacheHits + _cacheMisses;
                double hitRate = total > 0 ? (_cacheHits / (double)total) * 100 : 0;
                return (_cacheHits, _cacheMisses, hitRate);
            }
        }

        /// <summary>
        /// Resets the cache metrics.
        /// </summary>
        public void ResetCacheMetrics()
        {
            lock (_cacheLock)
            {
                _cacheHits = 0;
                _cacheMisses = 0;
            }
        }

        #endregion

        #region Database Metrics

        /// <summary>
        /// Records database performance metrics.
        /// </summary>
        /// <param name="metrics">Dictionary containing database operation metrics.</param>
        public void RecordDatabaseMetrics(Dictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> metrics)
        {
            lock (_snapshotLock) // reusing snapshot lock for database metrics
            {
                _databaseMetrics = new Dictionary<string, (int, int, TimeSpan, double)>(metrics);
                _logger.LogDebug("Database metrics recorded: {Count} operations", metrics.Count);
            }
        }

        /// <summary>
        /// Gets the current database performance metrics.
        /// </summary>
        public Dictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> GetDatabaseMetrics()
        {
            lock (_snapshotLock)
            {
                return new Dictionary<string, (int, int, TimeSpan, double)>(_databaseMetrics);
            }
        }

        #endregion

        #region BrainPersistence Metrics

        /// <summary>
        /// Records BrainPersistence service performance metrics.
        /// </summary>
        public void RecordBrainPersistenceMetrics(
            IReadOnlyDictionary<string, (long AverageMs, long P50Ms, long P95Ms, long P99Ms)> operationStats,
            IReadOnlyDictionary<string, int> trimmingCounts,
            (long TotalWaitTimeMs, int ContentionCount) lockMetrics,
            Dictionary<string, long> memoryUsage,
            (int AvailableWorkerThreads, int AvailableCompletionPortThreads, int MaxWorkerThreads, int MaxCompletionPortThreads) threadPoolInfo)
        {
            lock (_snapshotLock)
            {
                _brainPersistenceOperationStats = new Dictionary<string, (long, long, long, long)>(operationStats);
                _brainPersistenceTrimmingCounts = new Dictionary<string, int>(trimmingCounts);
                _brainPersistenceLockMetrics = lockMetrics;
                _brainPersistenceMemoryUsage = new Dictionary<string, long>(memoryUsage);
                _brainPersistenceThreadPoolInfo = threadPoolInfo;
                _logger.LogDebug("BrainPersistence metrics recorded: {OperationCount} operations, {TrimmingCount} trimmings",
                    operationStats.Count, trimmingCounts.Count);
            }
        }

        /// <summary>
        /// Gets the current BrainPersistence operation statistics.
        /// </summary>
        public IReadOnlyDictionary<string, (long AverageMs, long P50Ms, long P95Ms, long P99Ms)> GetBrainPersistenceOperationStats()
        {
            lock (_snapshotLock)
            {
                return new Dictionary<string, (long, long, long, long)>(_brainPersistenceOperationStats);
            }
        }

        /// <summary>
        /// Gets the current BrainPersistence trimming counts.
        /// </summary>
        public IReadOnlyDictionary<string, int> GetBrainPersistenceTrimmingCounts()
        {
            lock (_snapshotLock)
            {
                return new Dictionary<string, int>(_brainPersistenceTrimmingCounts);
            }
        }

        /// <summary>
        /// Gets the current BrainPersistence lock metrics.
        /// </summary>
        public (long TotalWaitTimeMs, int ContentionCount) GetBrainPersistenceLockMetrics()
        {
            lock (_snapshotLock)
            {
                return _brainPersistenceLockMetrics;
            }
        }

        /// <summary>
        /// Gets the current BrainPersistence memory usage.
        /// </summary>
        public IReadOnlyDictionary<string, long> GetBrainPersistenceMemoryUsage()
        {
            lock (_snapshotLock)
            {
                return new Dictionary<string, long>(_brainPersistenceMemoryUsage);
            }
        }

        /// <summary>
        /// Gets the current BrainPersistence thread pool information.
        /// </summary>
        public (int AvailableWorkerThreads, int AvailableCompletionPortThreads, int MaxWorkerThreads, int MaxCompletionPortThreads) GetBrainPersistenceThreadPoolInfo()
        {
            lock (_snapshotLock)
            {
                return _brainPersistenceThreadPoolInfo;
            }
        }

        #endregion

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
        /// Records overnight activities performance metrics from the common OvernightActivitiesHelper.
        /// </summary>
        /// <param name="metrics">The performance metrics from overnight activities.</param>
        /// <remarks>
        /// This method receives comprehensive performance data from the OvernightActivitiesHelper
        /// and integrates it with the overseer performance monitoring system.
        /// </remarks>
        public void RecordOvernightActivitiesMetrics(INightActivitiesPerformanceMetrics metrics)
        {
            var (totalTime, marketsProcessed, apiCalls, errors, peakMemory, startTime, endTime, taskDurations) = metrics.GetOvernightPerformanceMetrics();

            // Record as an overnight task
            RecordOvernightTask("OvernightActivities", TimeSpan.FromMilliseconds(totalTime), errors == 0);

            // Record API calls
            for (int i = 0; i < apiCalls; i++)
            {
                RecordApiFetch(TimeSpan.Zero); // We don't have individual API call times, so record as 0
            }

            // Log comprehensive overnight performance summary
            _logger.LogInformation("OVERNIGHT PERFORMANCE: Total={TotalTime}ms, Markets={Markets}, API Calls={ApiCalls}, Errors={Errors}, Peak Memory={PeakMemory}MB",
                totalTime, marketsProcessed, apiCalls, errors, peakMemory);

            // Log individual task performances
            foreach (var task in taskDurations)
            {
                _logger.LogInformation("OVERNIGHT TASK: {TaskName}={Duration}ms", task.Key, task.Value);
            }

            // Check for overnight performance alerts
            if (totalTime > 300000) // 5 minutes
            {
                _logger.LogWarning("PERFORMANCE ALERT: Overnight activities took {TotalTime}ms (>5 minutes)", totalTime);
            }

            if (errors > 10)
            {
                _logger.LogWarning("PERFORMANCE ALERT: Overnight activities had {ErrorCount} errors", errors);
            }

            if (peakMemory > 1000) // 1GB
            {
                _logger.LogWarning("PERFORMANCE ALERT: Overnight activities used {PeakMemory}MB peak memory (>1GB)", peakMemory);
            }
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

        #region IWebSocketPerformanceMetrics Implementation

        /// <summary>
        /// Records WebSocket message processing performance with enablement status.
        /// </summary>
        public void RecordWebSocketMessageProcessing(string messageType, long processingTimeTicks, int messageCount, long bufferSizeBytes, bool metricsEnabled)
        {
            if (!metricsEnabled) return;

            _webSocketProcessingTimeTicks.AddOrUpdate(messageType, 0, (k, v) => v + processingTimeTicks);
            _webSocketProcessingCount.AddOrUpdate(messageType, 0, (k, v) => v + messageCount);
            _webSocketBufferUsageBytes.AddOrUpdate(messageType, 0, (k, v) => v + bufferSizeBytes);
            _logger.LogDebug("WebSocket message processing recorded: Type={Type}, TimeTicks={TimeTicks}, Count={Count}, BufferBytes={BufferBytes}, MetricsEnabled={MetricsEnabled}",
                messageType, processingTimeTicks, messageCount, bufferSizeBytes, metricsEnabled);
        }

        /// <summary>
        /// Records WebSocket connection performance.
        /// </summary>
        public void RecordWebSocketOperation(string operation, TimeSpan duration)
        {
            _webSocketOperationTimes[operation] = duration;
            _logger.LogDebug("WebSocket operation recorded: {Operation}={Duration}ms", operation, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Records semaphore wait counts for WebSocket operations.
        /// </summary>
        public void RecordSemaphoreWait(string operation, int waitCount)
        {
            _webSocketSemaphoreWaitCount.AddOrUpdate(operation, 0, (k, v) => v + waitCount);
            _logger.LogDebug("Semaphore wait recorded: {Operation}={WaitCount}", operation, waitCount);
        }

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

        #endregion

        #region ISqlDataServicePerformanceMetrics Implementation

        /// <summary>
        /// Receives throughput metrics from SqlDataService.
        /// </summary>
        /// <param name="operationsPerSecond">Current operations per second rate.</param>
        /// <param name="totalProcessed">Total operations processed successfully.</param>
        /// <param name="totalFailed">Total operations that failed.</param>
        public void ReceiveThroughputMetrics(double operationsPerSecond, long totalProcessed, long totalFailed)
        {
            _logger.LogDebug("SqlDataService Throughput: {OpsPerSec:F2} ops/sec, Processed: {Processed}, Failed: {Failed}",
                operationsPerSecond, totalProcessed, totalFailed);

            // Store metrics in status for monitoring
            lock (_snapshotLock)
            {
                // Could extend to store historical data or expose via API
            }
        }

        /// <summary>
        /// Receives latency metrics from SqlDataService.
        /// </summary>
        /// <param name="averageLatencyMs">Average latency in milliseconds for processed operations.</param>
        /// <param name="sampleCount">Number of latency samples collected.</param>
        public void ReceiveLatencyMetrics(double averageLatencyMs, long sampleCount)
        {
            _logger.LogDebug("SqlDataService Latency: {AvgLatency:F2}ms over {SampleCount} samples", averageLatencyMs, sampleCount);

            // Check for performance alerts
            if (averageLatencyMs > 1000) // 1 second
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService average latency {AvgLatency:F2}ms exceeds 1 second", averageLatencyMs);
            }
        }

        /// <summary>
        /// Receives resource utilization metrics from SqlDataService.
        /// </summary>
        /// <param name="cpuUsagePercent">Current CPU usage percentage.</param>
        /// <param name="memoryUsageMB">Current memory usage in MB.</param>
        public void ReceiveResourceMetrics(double cpuUsagePercent, double memoryUsageMB)
        {
            _logger.LogDebug("SqlDataService Resources: CPU {CpuUsage:F2}%, Memory {MemoryUsage:F2}MB", cpuUsagePercent, memoryUsageMB);

            // Check for resource alerts
            if (cpuUsagePercent > 80)
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService CPU usage {CpuUsage:F2}% exceeds 80%", cpuUsagePercent);
            }

            if (memoryUsageMB > 1000) // 1GB
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService memory usage {MemoryUsage:F2}MB exceeds 1GB", memoryUsageMB);
            }
        }

        /// <summary>
        /// Receives queue depth metrics from SqlDataService.
        /// </summary>
        /// <param name="orderBookQueueDepth">Current depth of order book queue.</param>
        /// <param name="tradeQueueDepth">Current depth of trade queue.</param>
        /// <param name="fillQueueDepth">Current depth of fill queue.</param>
        /// <param name="eventLifecycleQueueDepth">Current depth of event lifecycle queue.</param>
        /// <param name="marketLifecycleQueueDepth">Current depth of market lifecycle queue.</param>
        /// <param name="totalQueuedOperations">Total operations across all queues.</param>
        public void ReceiveQueueMetrics(int orderBookQueueDepth, int tradeQueueDepth, int fillQueueDepth,
                                       int eventLifecycleQueueDepth, int marketLifecycleQueueDepth, int totalQueuedOperations)
        {
            _logger.LogDebug("SqlDataService Queues: OrderBook={OrderBook}, Trade={Trade}, Fill={Fill}, Event={Event}, Market={Market}, Total={Total}",
                orderBookQueueDepth, tradeQueueDepth, fillQueueDepth, eventLifecycleQueueDepth, marketLifecycleQueueDepth, totalQueuedOperations);

            // Check for queue alerts
            if (totalQueuedOperations > 5000)
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService total queued operations {Total} exceeds 5000", totalQueuedOperations);
            }
        }

        /// <summary>
        /// Receives success rate metrics from SqlDataService.
        /// </summary>
        /// <param name="successRatePercent">Success rate as a percentage (0-100).</param>
        public void ReceiveSuccessRateMetrics(double successRatePercent)
        {
            _logger.LogDebug("SqlDataService Success Rate: {SuccessRate:F2}%", successRatePercent);

            // Check for success rate alerts
            if (successRatePercent < 95.0)
            {
                _logger.LogWarning("PERFORMANCE ALERT: SqlDataService success rate {SuccessRate:F2}% is below 95%", successRatePercent);
            }
        }

        #endregion

        #region ISubscriptionManagerPerformanceMetrics Implementation

        /// <summary>
        /// Posts operation performance metrics from SubscriptionManager.
        /// </summary>
        /// <param name="metrics">Dictionary containing operation names and their performance statistics.</param>
        public void PostOperationMetrics(IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)> metrics)
        {
            _subscriptionManagerOperationMetrics = metrics;
            _logger.LogDebug("SubscriptionManager operation metrics posted: {Count} operations", metrics?.Count ?? 0);
        }

        /// <summary>
        /// Posts lock contention metrics from SubscriptionManager.
        /// </summary>
        /// <param name="metrics">Dictionary containing lock names and their contention statistics.</param>
        public void PostLockContentionMetrics(IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)> metrics)
        {
            _subscriptionManagerLockMetrics = metrics;
            _logger.LogDebug("SubscriptionManager lock contention metrics posted: {Count} locks", metrics?.Count ?? 0);
        }

        /// <summary>
        /// Gets the current operation performance metrics.
        /// </summary>
        /// <returns>Dictionary containing operation names and their performance statistics.</returns>
        public IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)> GetOperationMetrics()
        {
            return _subscriptionManagerOperationMetrics ?? new Dictionary<string, (long, long, long)>();
        }

        /// <summary>
        /// Gets the current lock contention metrics.
        /// </summary>
        /// <returns>Dictionary containing lock names and their contention statistics.</returns>
        public IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)> GetLockContentionMetrics()
        {
            return _subscriptionManagerLockMetrics ?? new Dictionary<string, (long, long, long)>();
        }

        /// <summary>
        /// Resets all SubscriptionManager performance metrics.
        /// </summary>
        public void ResetSubscriptionManagerMetrics()
        {
            _subscriptionManagerOperationMetrics = null;
            _subscriptionManagerLockMetrics = null;
            _logger.LogInformation("SubscriptionManager performance metrics reset");
        }

        #endregion

        #region IMessageProcessorPerformanceMetrics Implementation

        /// <summary>
        /// Posts message processing performance metrics from MessageProcessor.
        /// </summary>
        /// <param name="totalMessagesProcessed">Total number of messages processed since last reset.</param>
        /// <param name="totalProcessingTimeMs">Total processing time in milliseconds since last reset.</param>
        /// <param name="averageProcessingTimeMs">Average processing time per message in milliseconds.</param>
        /// <param name="messagesPerSecond">Current messages per second rate.</param>
        /// <param name="orderBookQueueDepth">Current depth of the order book update queue.</param>
        public void PostMessageProcessingMetrics(long totalMessagesProcessed, long totalProcessingTimeMs,
            double averageProcessingTimeMs, double messagesPerSecond, int orderBookQueueDepth)
        {
            lock (_snapshotLock) // reusing snapshot lock for MessageProcessor metrics
            {
                _messageProcessorTotalMessagesProcessed = totalMessagesProcessed;
                _messageProcessorTotalProcessingTimeMs = totalProcessingTimeMs;
                _messageProcessorAverageProcessingTimeMs = averageProcessingTimeMs;
                _messageProcessorMessagesPerSecond = messagesPerSecond;
                _messageProcessorOrderBookQueueDepth = orderBookQueueDepth;
                _logger.LogDebug("MessageProcessor metrics posted: {TotalMessages} messages, {AvgTime:F2}ms avg, {MsgsPerSec:F2} msg/sec, QueueDepth={QueueDepth}",
                    totalMessagesProcessed, averageProcessingTimeMs, messagesPerSecond, orderBookQueueDepth);
            }
        }

        /// <summary>
        /// Posts duplicate message detection metrics from MessageProcessor.
        /// </summary>
        /// <param name="duplicateMessageCount">Total number of duplicate messages detected.</param>
        /// <param name="duplicatesInWindow">Number of duplicates detected in the current time window.</param>
        /// <param name="lastDuplicateWarningTime">Timestamp of the last duplicate message warning.</param>
        public void PostDuplicateMessageMetrics(int duplicateMessageCount, int duplicatesInWindow, DateTime lastDuplicateWarningTime)
        {
            lock (_snapshotLock)
            {
                _messageProcessorDuplicateMessageCount = duplicateMessageCount;
                _messageProcessorDuplicatesInWindow = duplicatesInWindow;
                _messageProcessorLastDuplicateWarningTime = lastDuplicateWarningTime;
                _logger.LogDebug("MessageProcessor duplicate metrics posted: {DuplicateCount} total, {DuplicatesInWindow} in window",
                    duplicateMessageCount, duplicatesInWindow);
            }
        }

        /// <summary>
        /// Posts message type distribution metrics from MessageProcessor.
        /// </summary>
        /// <param name="messageTypeCounts">Dictionary containing counts for each message type processed.</param>
        public void PostMessageTypeMetrics(IReadOnlyDictionary<string, long> messageTypeCounts)
        {
            lock (_snapshotLock)
            {
                _messageProcessorMessageTypeCounts = messageTypeCounts;
                _logger.LogDebug("MessageProcessor message type metrics posted: {Count} types", messageTypeCounts?.Count ?? 0);
            }
        }

        /// <summary>
        /// Gets the current message processing performance metrics.
        /// </summary>
        /// <returns>Tuple containing current performance metrics.</returns>
        public (long TotalMessagesProcessed, long TotalProcessingTimeMs, double AverageProcessingTimeMs,
            double MessagesPerSecond, int OrderBookQueueDepth) GetMessageProcessingMetrics()
        {
            lock (_snapshotLock)
            {
                return (_messageProcessorTotalMessagesProcessed, _messageProcessorTotalProcessingTimeMs,
                    _messageProcessorAverageProcessingTimeMs, _messageProcessorMessagesPerSecond, _messageProcessorOrderBookQueueDepth);
            }
        }

        /// <summary>
        /// Gets the current duplicate message metrics.
        /// </summary>
        /// <returns>Tuple containing duplicate message statistics.</returns>
        public (int DuplicateMessageCount, int DuplicatesInWindow, DateTime LastDuplicateWarningTime) GetDuplicateMessageMetrics()
        {
            lock (_snapshotLock)
            {
                return (_messageProcessorDuplicateMessageCount, _messageProcessorDuplicatesInWindow, _messageProcessorLastDuplicateWarningTime);
            }
        }

        /// <summary>
        /// Gets the current message type distribution metrics.
        /// </summary>
        /// <returns>Dictionary containing message type counts.</returns>
        public IReadOnlyDictionary<string, long> GetMessageTypeMetrics()
        {
            lock (_snapshotLock)
            {
                return _messageProcessorMessageTypeCounts ?? new Dictionary<string, long>();
            }
        }

        /// <summary>
        /// Resets all MessageProcessor performance metrics.
        /// </summary>
        public void ResetMessageProcessorMetrics()
        {
            lock (_snapshotLock)
            {
                _messageProcessorTotalMessagesProcessed = 0;
                _messageProcessorTotalProcessingTimeMs = 0;
                _messageProcessorAverageProcessingTimeMs = 0;
                _messageProcessorMessagesPerSecond = 0;
                _messageProcessorOrderBookQueueDepth = 0;
                _messageProcessorDuplicateMessageCount = 0;
                _messageProcessorDuplicatesInWindow = 0;
                _messageProcessorLastDuplicateWarningTime = DateTime.MinValue;
                _messageProcessorMessageTypeCounts = null;
                _logger.LogInformation("MessageProcessor performance metrics reset");
            }
        }

        #endregion

        #region IPerformanceMonitor Implementation

        /// <summary>
        /// Records the execution time for a specific method or operation.
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        public void RecordExecutionTime(string methodName, long milliseconds)
        {
            // Store in API metrics for consistency
            lock (_apiLock)
            {
                _totalApiFetchTime += milliseconds;
                _apiFetchCount++;
                _lastApiFetchTime = DateTime.UtcNow;
            }

            _logger.LogDebug("Execution time recorded: {MethodName}={Milliseconds}ms", methodName, milliseconds);
        }

        /// <summary>
        /// Records the execution time for a specific method or operation with enablement status.
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        public void RecordExecutionTime(string methodName, long milliseconds, bool metricsEnabled)
        {
            // Store in API metrics for consistency
            lock (_apiLock)
            {
                _totalApiFetchTime += milliseconds;
                _apiFetchCount++;
                _lastApiFetchTime = DateTime.UtcNow;
            }

            _logger.LogDebug("Execution time recorded: {MethodName}={Milliseconds}ms, MetricsEnabled={MetricsEnabled}", methodName, milliseconds, metricsEnabled);
        }

        /// <summary>
        /// Records simulation performance metrics from StrategySimulation.
        /// </summary>
        /// <param name="simulationName">The name of the simulation.</param>
        /// <param name="metrics">The detailed metrics dictionary from StrategySimulation.GetDetailedPerformanceMetrics().</param>
        public void RecordSimulationMetrics(string simulationName, Dictionary<string, object> metrics)
        {
            _logger.LogInformation("Simulation metrics recorded for {SimulationName}: {MetricsCount} metrics", simulationName, metrics.Count);

            // Log key metrics for monitoring
            foreach (var kvp in metrics)
            {
                _logger.LogDebug("Simulation metric: {Key}={Value}", kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Records simulation performance metrics from StrategySimulation with enablement status.
        /// </summary>
        /// <param name="simulationName">The name of the simulation.</param>
        /// <param name="metrics">The detailed metrics dictionary from StrategySimulation.GetDetailedPerformanceMetrics().</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        public void RecordSimulationMetrics(string simulationName, Dictionary<string, object> metrics, bool metricsEnabled)
        {
            _logger.LogInformation("Simulation metrics recorded for {SimulationName}: {MetricsCount} metrics, MetricsEnabled={MetricsEnabled}",
                simulationName, metrics.Count, metricsEnabled);

            // Log key metrics for monitoring
            foreach (var kvp in metrics)
            {
                _logger.LogDebug("Simulation metric: {Key}={Value}", kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Records comprehensive performance metrics.
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="totalExecutionTimeMs">Total time spent on execution.</param>
        /// <param name="totalItemsProcessed">Number of items processed.</param>
        /// <param name="totalItemsFound">Number of items found.</param>
        /// <param name="itemCheckTimes">Dictionary of item names to their processing times.</param>
        public void RecordPerformanceMetrics(
            string methodName,
            long totalExecutionTimeMs,
            int totalItemsProcessed,
            int totalItemsFound,
            Dictionary<string, long>? itemCheckTimes = null)
        {
            _logger.LogInformation("Performance metrics recorded for {MethodName}: TotalTime={TotalTime}ms, ItemsProcessed={Processed}, ItemsFound={Found}",
                methodName, totalExecutionTimeMs, totalItemsProcessed, totalItemsFound);

            // Record as API fetch for consistency
            RecordExecutionTime(methodName, totalExecutionTimeMs, true);

            // Log individual item times if provided
            if (itemCheckTimes != null && itemCheckTimes.Count > 0)
            {
                foreach (var kvp in itemCheckTimes)
                {
                    _logger.LogDebug("Item processing time: {ItemName}={Time}ms", kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Gets all configurable performance metrics for GUI consumption.
        /// </summary>
        /// <returns>Dictionary containing all configurable metrics.</returns>
        public IReadOnlyDictionary<string, object> GetConfigurableMetrics()
        {
            return _configurableMetrics;
        }

        /// <summary>
        /// Sends an empty result when performance metrics are disabled.
        /// </summary>
        private void SendEmptyResult()
        {
            // Initialize with false when metrics are disabled
            _configurableMetrics["EnablePerformanceMetrics"] = false;
        }

        #endregion

    }
}