using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KalshiBotOverseer.Services
{
    /// <summary>
    /// Centralized service for collecting and managing performance metrics across the KalshiBot Overseer system.
    /// This service aggregates metrics from various components including WebSocket operations, API calls,
    /// SignalR communications, overnight tasks, and snapshot processing.
    /// </summary>
    public class PerformanceMetricsService
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
        /// Gets the current SignalR metrics.
        /// </summary>
        public (long MessagesProcessed, long HandshakeRequests, long CheckInRequests, DateTime LastReset) GetSignalRMetrics()
        {
            lock (_signalRLock)
            {
                return (_totalMessagesProcessed, _totalHandshakeRequests, _totalCheckInRequests, _lastMetricsReset);
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

    }
}