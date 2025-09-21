using BacklashCommon.Performance;
using BacklashOverseer.Services;

namespace BacklashOverseer.Performance
{
    /// <summary>
    /// Extension methods to convert complex PerformanceMetricsService objects into simple PerformanceMetric instances
    /// for uniform GUI handling across all performance monitors.
    /// </summary>
    public static class PerformanceMetricsServiceExtensions
    {
        /// <summary>
        /// Converts PerformanceMetricsService core metrics to a collection of simple PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToCorePerformanceMetrics(
            this PerformanceMetricsService service,
            string category = "Performance Metrics Service")
        {
            var metrics = new List<PerformanceMetric>();

            // WebSocket metrics
            var webSocketEventCount = service.GetMetricsStatus().GetValueOrDefault("WebSocketEventCount", 0L);
            if (webSocketEventCount is long wsCount)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "WebSocket_EventCount",
                    Name = "WebSocket Events",
                    Description = "Total WebSocket events processed",
                    Value = wsCount,
                    Unit = "events",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            // API metrics
            var totalApiFetchTime = service.GetMetricsStatus().GetValueOrDefault("TotalApiFetchTime", 0L);
            var apiFetchCount = service.GetMetricsStatus().GetValueOrDefault("ApiFetchCount", 0);

            if (totalApiFetchTime is long apiTime && apiFetchCount is int apiCount && apiCount > 0)
            {
                var avgApiTime = (double)apiTime / apiCount;

                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "API_AvgFetchTime",
                    Name = "Avg API Fetch Time",
                    Description = "Average API fetch time",
                    Value = avgApiTime,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 1000,
                    CriticalThreshold = 5000
                });

                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "API_FetchCount",
                    Name = "API Fetch Count",
                    Description = "Total API fetch operations",
                    Value = apiCount,
                    Unit = "ops",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            // SignalR metrics
            var signalRMetrics = service.GetSignalRMetrics();
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "SignalR_MessagesProcessed",
                Name = "SignalR Messages",
                Description = "Total SignalR messages processed",
                Value = signalRMetrics.MessagesProcessed,
                Unit = "messages",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "SignalR_HandshakeRequests",
                Name = "SignalR Handshakes",
                Description = "Total SignalR handshake requests",
                Value = signalRMetrics.HandshakeRequests,
                Unit = "requests",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            if (signalRMetrics.AvgHandshakeLatencyMs > 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "SignalR_AvgHandshakeLatency",
                    Name = "Avg Handshake Latency",
                    Description = "Average SignalR handshake latency",
                    Value = signalRMetrics.AvgHandshakeLatencyMs,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 500,
                    CriticalThreshold = 2000
                });
            }

            if (signalRMetrics.AvgMessageLatencyMs > 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "SignalR_AvgMessageLatency",
                    Name = "Avg Message Latency",
                    Description = "Average SignalR message latency",
                    Value = signalRMetrics.AvgMessageLatencyMs,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100,
                    CriticalThreshold = 500
                });
            }

            // Overnight task metrics
            var overnightMetrics = service.GetOvernightMetrics();
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Overnight_TotalTasks",
                Name = "Overnight Tasks",
                Description = "Total overnight tasks executed",
                Value = overnightMetrics.TotalTasks,
                Unit = "tasks",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            if (overnightMetrics.SuccessRate >= 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Overnight_SuccessRate",
                    Name = "Overnight Success Rate",
                    Description = "Overnight task success rate",
                    Value = overnightMetrics.SuccessRate * 100,
                    Unit = "%",
                    VisualType = VisualType.ProgressBar,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 80,
                    CriticalThreshold = 95
                });
            }

            if (overnightMetrics.TotalDuration.TotalMilliseconds > 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Overnight_TotalDuration",
                    Name = "Overnight Duration",
                    Description = "Total time spent on overnight tasks",
                    Value = overnightMetrics.TotalDuration.TotalMilliseconds,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 300000, // 5 minutes
                    CriticalThreshold = 600000  // 10 minutes
                });
            }

            // Snapshot aggregation metrics
            var snapshotMetrics = service.GetSnapshotAggregationMetrics();
            if (snapshotMetrics.Count > 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Snapshot_AggregationCount",
                    Name = "Snapshot Aggregations",
                    Description = "Total snapshot aggregation operations",
                    Value = snapshotMetrics.Count,
                    Unit = "ops",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });

                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Snapshot_AvgAggregationTime",
                    Name = "Avg Snapshot Time",
                    Description = "Average snapshot aggregation time",
                    Value = snapshotMetrics.AverageMs,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 1000,
                    CriticalThreshold = 5000
                });

                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Snapshot_MaxAggregationTime",
                    Name = "Max Snapshot Time",
                    Description = "Maximum snapshot aggregation time",
                    Value = snapshotMetrics.MaxMs,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 2000,
                    CriticalThreshold = 10000
                });
            }

            // System health metrics
            var healthMetrics = service.GetHealthMetrics();
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Health_MarketRefreshFailures",
                Name = "Market Refresh Failures",
                Description = "Number of market refresh failures",
                Value = healthMetrics.MarketRefreshFailureCount,
                Unit = "failures",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            // Cache metrics
            var cacheMetrics = service.GetCacheMetrics();
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Cache_Hits",
                Name = "Cache Hits",
                Description = "Total cache hits",
                Value = cacheMetrics.CacheHits,
                Unit = "hits",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Cache_Misses",
                Name = "Cache Misses",
                Description = "Total cache misses",
                Value = cacheMetrics.CacheMisses,
                Unit = "misses",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Cache_HitRate",
                Name = "Cache Hit Rate",
                Description = "Cache hit rate percentage",
                Value = cacheMetrics.HitRate,
                Unit = "%",
                VisualType = VisualType.ProgressBar,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 50,
                CriticalThreshold = 80
            });

            // Service uptime
            var uptime = service.GetMetricsStatus().GetValueOrDefault("ServiceUptime", TimeSpan.Zero);
            if (uptime is TimeSpan uptimeSpan)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Service_Uptime",
                    Name = "Service Uptime",
                    Description = "Service uptime since last reset",
                    Value = uptimeSpan.TotalMinutes,
                    Unit = "minutes",
                    VisualType = VisualType.NumericDisplay,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            return metrics;
        }

        /// <summary>
        /// Converts database performance metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToDatabasePerformanceMetrics(
            this PerformanceMetricsService service,
            string category = "Database Performance")
        {
            var metrics = new List<PerformanceMetric>();
            var dbMetrics = service.GetDatabaseMetrics();

            foreach (var kvp in dbMetrics)
            {
                var operationName = kvp.Key;
                var (successCount, failureCount, totalTime, avgTime) = kvp.Value;

                // Success rate
                var totalOperations = successCount + failureCount;
                if (totalOperations > 0)
                {
                    var successRate = (double)successCount / totalOperations * 100;
                    metrics.Add(new GeneralPerformanceMetric
                    {
                        Id = $"DB_{operationName}_SuccessRate",
                        Name = $"{operationName} Success Rate",
                        Description = $"Success rate for {operationName} operations",
                        Value = successRate,
                        Unit = "%",
                        VisualType = VisualType.ProgressBar,
                        Category = category,
                        Timestamp = DateTime.UtcNow,
                        MinThreshold = 0,
                        WarningThreshold = 95,
                        CriticalThreshold = 99
                    });
                }

                // Average execution time
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"DB_{operationName}_AvgTime",
                    Name = $"{operationName} Avg Time",
                    Description = $"Average execution time for {operationName}",
                    Value = avgTime,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100,
                    CriticalThreshold = 1000
                });

                // Total operations
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"DB_{operationName}_TotalOps",
                    Name = $"{operationName} Total Ops",
                    Description = $"Total operations for {operationName}",
                    Value = totalOperations,
                    Unit = "ops",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            return metrics;
        }

        /// <summary>
        /// Converts BrainPersistence metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToBrainPersistencePerformanceMetrics(
            this PerformanceMetricsService service,
            string category = "Brain Persistence")
        {
            var metrics = new List<PerformanceMetric>();

            // Operation statistics
            var operationStats = service.GetBrainPersistenceOperationStats();
            foreach (var kvp in operationStats)
            {
                var operationName = kvp.Key;
                var (avgMs, p50Ms, p95Ms, p99Ms) = kvp.Value;

                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"Brain_{operationName}_AvgTime",
                    Name = $"{operationName} Avg Time",
                    Description = $"Average time for {operationName}",
                    Value = avgMs,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100,
                    CriticalThreshold = 500
                });

                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"Brain_{operationName}_P95Time",
                    Name = $"{operationName} P95 Time",
                    Description = $"95th percentile time for {operationName}",
                    Value = p95Ms,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 200,
                    CriticalThreshold = 1000
                });
            }

            // Trimming counts
            var trimmingCounts = service.GetBrainPersistenceTrimmingCounts();
            foreach (var kvp in trimmingCounts)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"Brain_Trimming_{kvp.Key}",
                    Name = $"{kvp.Key} Trimmings",
                    Description = $"Number of trimmings for {kvp.Key}",
                    Value = kvp.Value,
                    Unit = "trims",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Lock metrics
            var lockMetrics = service.GetBrainPersistenceLockMetrics();
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Brain_Lock_WaitTime",
                Name = "Lock Wait Time",
                Description = "Total time spent waiting for locks",
                Value = lockMetrics.TotalWaitTimeMs,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 1000,
                CriticalThreshold = 5000
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Brain_Lock_Contention",
                Name = "Lock Contention",
                Description = "Number of lock contentions",
                Value = lockMetrics.ContentionCount,
                Unit = "contentions",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            // Memory usage
            var memoryUsage = service.GetBrainPersistenceMemoryUsage();
            foreach (var kvp in memoryUsage)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"Brain_Memory_{kvp.Key}",
                    Name = $"{kvp.Key} Memory",
                    Description = $"Memory usage for {kvp.Key}",
                    Value = kvp.Value / (1024.0 * 1024.0), // Convert to MB
                    Unit = "MB",
                    VisualType = VisualType.ProgressBar,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100,
                    CriticalThreshold = 500
                });
            }

            // Thread pool info
            var threadPoolInfo = service.GetBrainPersistenceThreadPoolInfo();
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Brain_ThreadPool_WorkerThreads",
                Name = "Worker Threads",
                Description = "Available worker threads in thread pool",
                Value = threadPoolInfo.AvailableWorkerThreads,
                Unit = "threads",
                VisualType = VisualType.NumericDisplay,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Brain_ThreadPool_CompletionThreads",
                Name = "Completion Threads",
                Description = "Available completion port threads in thread pool",
                Value = threadPoolInfo.AvailableCompletionPortThreads,
                Unit = "threads",
                VisualType = VisualType.NumericDisplay,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            return metrics;
        }

        /// <summary>
        /// Converts WebSocket performance metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToWebSocketPerformanceMetrics(
            this PerformanceMetricsService service,
            string category = "WebSocket Performance")
        {
            var metrics = new List<PerformanceMetric>();

            // WebSocket processing times
            var avgProcessingTimes = service.GetAverageProcessingTimesMs();
            foreach (var kvp in avgProcessingTimes)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"WS_{kvp.Key}_AvgTime",
                    Name = $"{kvp.Key} Avg Time",
                    Description = $"Average processing time for {kvp.Key}",
                    Value = kvp.Value,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100,
                    CriticalThreshold = 500
                });
            }

            // WebSocket buffer usage
            var bufferUsage = service.GetBufferUsageBytes();
            foreach (var kvp in bufferUsage)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"WS_{kvp.Key}_Buffer",
                    Name = $"{kvp.Key} Buffer",
                    Description = $"Buffer usage for {kvp.Key}",
                    Value = kvp.Value / (1024.0 * 1024.0), // Convert to MB
                    Unit = "MB",
                    VisualType = VisualType.ProgressBar,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 50,
                    CriticalThreshold = 100
                });
            }

            // WebSocket operation times
            var operationTimes = service.GetAsyncOperationTimesMs();
            foreach (var kvp in operationTimes)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"WS_Operation_{kvp.Key}",
                    Name = $"{kvp.Key} Operation",
                    Description = $"Operation time for {kvp.Key}",
                    Value = kvp.Value,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 200,
                    CriticalThreshold = 1000
                });
            }

            // WebSocket semaphore waits
            var semaphoreWaits = service.GetSemaphoreWaitCounts();
            foreach (var kvp in semaphoreWaits)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"WS_Semaphore_{kvp.Key}",
                    Name = $"{kvp.Key} Semaphore Waits",
                    Description = $"Semaphore wait count for {kvp.Key}",
                    Value = kvp.Value,
                    Unit = "waits",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            return metrics;
        }

        /// <summary>
        /// Converts SubscriptionManager metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToSubscriptionPerformanceMetrics(
            this PerformanceMetricsService service,
            string category = "Subscription Manager")
        {
            var metrics = new List<PerformanceMetric>();

            var operationMetrics = service.GetOperationMetrics();
            foreach (var kvp in operationMetrics)
            {
                var operationName = kvp.Key;
                var (avgTicks, totalOps, successfulOps) = kvp.Value;

                // Success rate
                if (totalOps > 0)
                {
                    var successRate = (double)successfulOps / totalOps * 100;
                    metrics.Add(new GeneralPerformanceMetric
                    {
                        Id = $"SubMgr_{operationName}_SuccessRate",
                        Name = $"{operationName} Success Rate",
                        Description = $"Success rate for {operationName}",
                        Value = successRate,
                        Unit = "%",
                        VisualType = VisualType.ProgressBar,
                        Category = category,
                        Timestamp = DateTime.UtcNow,
                        MinThreshold = 0,
                        WarningThreshold = 95,
                        CriticalThreshold = 99
                    });
                }

                // Average time (convert ticks to milliseconds)
                var avgTimeMs = avgTicks / (double)TimeSpan.TicksPerMillisecond;
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"SubMgr_{operationName}_AvgTime",
                    Name = $"{operationName} Avg Time",
                    Description = $"Average execution time for {operationName}",
                    Value = avgTimeMs,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100,
                    CriticalThreshold = 500
                });

                // Total operations
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"SubMgr_{operationName}_TotalOps",
                    Name = $"{operationName} Total Ops",
                    Description = $"Total operations for {operationName}",
                    Value = totalOps,
                    Unit = "ops",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            var lockMetrics = service.GetLockContentionMetrics();
            foreach (var kvp in lockMetrics)
            {
                var lockName = kvp.Key;
                var (acquisitionCount, avgWaitTicks, contentionCount) = kvp.Value;

                // Average wait time
                var avgWaitMs = avgWaitTicks / (double)TimeSpan.TicksPerMillisecond;
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"SubMgr_Lock_{lockName}_AvgWait",
                    Name = $"{lockName} Avg Wait",
                    Description = $"Average wait time for {lockName} lock",
                    Value = avgWaitMs,
                    Unit = "ms",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 50,
                    CriticalThreshold = 200
                });

                // Contention count
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"SubMgr_Lock_{lockName}_Contention",
                    Name = $"{lockName} Contention",
                    Description = $"Contention count for {lockName} lock",
                    Value = contentionCount,
                    Unit = "count",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            return metrics;
        }

        /// <summary>
        /// Converts MessageProcessor metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToMessageProcessorPerformanceMetrics(
            this PerformanceMetricsService service,
            string category = "Message Processor")
        {
            var metrics = new List<PerformanceMetric>();

            var processingMetrics = service.GetMessageProcessingMetrics();
            var (totalMessages, totalTimeMs, avgTimeMs, messagesPerSecond, queueDepth) = processingMetrics;

            // Processing metrics
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "MsgProc_TotalMessages",
                Name = "Total Messages",
                Description = "Total messages processed",
                Value = totalMessages,
                Unit = "messages",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "MsgProc_AvgProcessingTime",
                Name = "Avg Processing Time",
                Description = "Average time to process a message",
                Value = avgTimeMs,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 100,
                CriticalThreshold = 500
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "MsgProc_MessagesPerSecond",
                Name = "Messages/Second",
                Description = "Current message processing rate",
                Value = messagesPerSecond,
                Unit = "msg/sec",
                VisualType = VisualType.SpeedDial,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 100,
                CriticalThreshold = 500
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "MsgProc_QueueDepth",
                Name = "Queue Depth",
                Description = "Current order book queue depth",
                Value = queueDepth,
                Unit = "items",
                VisualType = VisualType.ProgressBar,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 1000,
                CriticalThreshold = 5000
            });

            // Duplicate message metrics
            var duplicateMetrics = service.GetDuplicateMessageMetrics();
            var (duplicateCount, duplicatesInWindow, lastWarningTime) = duplicateMetrics;

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "MsgProc_DuplicateCount",
                Name = "Duplicate Messages",
                Description = "Total duplicate messages detected",
                Value = duplicateCount,
                Unit = "duplicates",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "MsgProc_DuplicatesInWindow",
                Name = "Duplicates in Window",
                Description = "Duplicate messages in current time window",
                Value = duplicatesInWindow,
                Unit = "duplicates",
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            // Message type distribution
            var messageTypeMetrics = service.GetMessageTypeMetrics();
            foreach (var kvp in messageTypeMetrics)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"MsgProc_Type_{kvp.Key}",
                    Name = $"{kvp.Key} Messages",
                    Description = $"Count of {kvp.Key} messages processed",
                    Value = kvp.Value,
                    Unit = "messages",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            return metrics;
        }

        /// <summary>
        /// Gets all performance metrics from PerformanceMetricsService as a unified collection
        /// </summary>
        public static IEnumerable<PerformanceMetric> GetAllPerformanceMetrics(this PerformanceMetricsService service)
        {
            var allMetrics = new List<PerformanceMetric>();

            // Core service metrics
            allMetrics.AddRange(service.ToCorePerformanceMetrics());

            // Database metrics
            allMetrics.AddRange(service.ToDatabasePerformanceMetrics());

            // BrainPersistence metrics
            allMetrics.AddRange(service.ToBrainPersistencePerformanceMetrics());

            // WebSocket metrics
            allMetrics.AddRange(service.ToWebSocketPerformanceMetrics());

            // Subscription manager metrics
            allMetrics.AddRange(service.ToSubscriptionPerformanceMetrics());

            // Message processor metrics
            allMetrics.AddRange(service.ToMessageProcessorPerformanceMetrics());

            return allMetrics;
        }
    }
}
