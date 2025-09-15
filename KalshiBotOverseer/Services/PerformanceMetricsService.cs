using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using KalshiBotData.Data.Interfaces;
using BacklashInterfaces.PerformanceMetrics;

namespace KalshiBotOverseer.Services
{
    /// <summary>
    /// Centralized service for collecting and managing performance metrics across the KalshiBot Overseer system.
    /// This service aggregates metrics from various components including WebSocket operations, API calls,
    /// SignalR communications, overnight tasks, and snapshot processing.
    /// </summary>
    public class PerformanceMetricsService : IKalshiBotContextPerformanceMetrics, ISubscriptionManagerPerformanceMetrics
    {
        private readonly ILogger<PerformanceMetricsService> _logger;

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

        // SubscriptionManager performance metrics
        private IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)>? _subscriptionManagerOperationMetrics;
        private IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)>? _subscriptionManagerLockMetrics;

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

        /// <summary>
        /// Initializes a new instance of the PerformanceMetricsService.
        /// </summary>
        /// <param name="logger">The logger instance for recording metrics operations.</param>
        public PerformanceMetricsService(ILogger<PerformanceMetricsService> logger)
        {
            _logger = logger;
            _lastMetricsReset = DateTime.UtcNow;
            _logger.LogInformation("PerformanceMetricsService initialized");
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

    }
}