using System;
using System.Collections.Generic;
using System.Linq;
using BacklashBot.Management;
using BacklashCommon.Performance;

namespace BacklashBot.Performance
{
    /// <summary>
    /// Extension methods to convert complex CentralPerformanceMonitor objects into simple PerformanceMetric instances
    /// for uniform GUI handling across all performance monitors.
    /// </summary>
    public static class CentralPerformanceMetricExtensions
    {
        /// <summary>
        /// Converts CentralPerformanceMonitor API execution times to a collection of simple PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToCorePerformanceMetrics(
            this CentralPerformanceMonitor monitor,
            string category = "Central Performance Monitor")
        {
            var metrics = new List<PerformanceMetric>();

            // API Execution Times - Convert to Speed Dial metrics
            if (monitor.ApiExecutionTimes != null)
            {
                foreach (var kvp in monitor.ApiExecutionTimes)
                {
                    var methodName = kvp.Key;
                    var executions = kvp.Value;

                    if (executions.Any())
                    {
                        var avgTime = executions.Average(e => e.Milliseconds);
                        var maxTime = executions.Max(e => e.Milliseconds);
                        var totalExecutions = executions.Count;

                        // Average execution time
                        metrics.Add(new GeneralPerformanceMetric
                        {
                            Id = $"API_{methodName}_AvgTime",
                            Name = $"{methodName} Avg Time",
                            Description = $"Average execution time for {methodName}",
                            Value = avgTime,
                            Unit = "ms",
                            VisualType = VisualType.SpeedDial,
                            Category = category,
                            Timestamp = DateTime.UtcNow,
                            MinThreshold = 0,
                            WarningThreshold = 1000, // 1 second
                            CriticalThreshold = 5000  // 5 seconds
                        });

                        // Max execution time
                        metrics.Add(new GeneralPerformanceMetric
                        {
                            Id = $"API_{methodName}_MaxTime",
                            Name = $"{methodName} Max Time",
                            Description = $"Maximum execution time for {methodName}",
                            Value = maxTime,
                            Unit = "ms",
                            VisualType = VisualType.SpeedDial,
                            Category = category,
                            Timestamp = DateTime.UtcNow,
                            MinThreshold = 0,
                            WarningThreshold = 2000,
                            CriticalThreshold = 10000
                        });

                        // Execution count
                        metrics.Add(new GeneralPerformanceMetric
                        {
                            Id = $"API_{methodName}_Count",
                            Name = $"{methodName} Executions",
                            Description = $"Number of executions for {methodName}",
                            Value = totalExecutions,
                            Unit = "count",
                            VisualType = VisualType.Counter,
                            Category = category,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }

            // Queue Rolling Averages
            var queueAverages = monitor.GetQueueCountRollingAverages();
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Queue_Event_Avg",
                Name = "Event Queue Avg",
                Description = "Average EventQueue count over last 5 minutes",
                Value = queueAverages.EventQueueAvg,
                Unit = "count",
                VisualType = VisualType.ProgressBar,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 100,
                CriticalThreshold = 500
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Queue_Ticker_Avg",
                Name = "Ticker Queue Avg",
                Description = "Average TickerQueue count over last 5 minutes",
                Value = queueAverages.TickerQueueAvg,
                Unit = "count",
                VisualType = VisualType.ProgressBar,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 100,
                CriticalThreshold = 500
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Queue_Notification_Avg",
                Name = "Notification Queue Avg",
                Description = "Average NotificationQueue count over last 5 minutes",
                Value = queueAverages.NotificationQueueAvg,
                Unit = "count",
                VisualType = VisualType.ProgressBar,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 100,
                CriticalThreshold = 500
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Queue_OrderBook_Avg",
                Name = "OrderBook Queue Avg",
                Description = "Average OrderBookQueue count over last 5 minutes",
                Value = queueAverages.OrderBookQueueAvg,
                Unit = "count",
                VisualType = VisualType.ProgressBar,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 100,
                CriticalThreshold = 500
            });

            // Queue High Count Percentage
            var highCountPercentage = monitor.GetQueueHighCountPercentage();
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "Queue_HighCount_Percentage",
                Name = "Queue High Count %",
                Description = "Percentage of time EventQueue exceeds threshold",
                Value = highCountPercentage,
                Unit = "%",
                VisualType = VisualType.TrafficLight,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 50,
                CriticalThreshold = 80
            });

            // Market Refresh Metrics
            if (monitor.LastRefreshUsagePercentage > 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Refresh_Usage_Percentage",
                    Name = "Refresh Usage %",
                    Description = "Market refresh CPU usage percentage",
                    Value = monitor.LastRefreshUsagePercentage,
                    Unit = "%",
                    VisualType = VisualType.ProgressBar,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 70,
                    CriticalThreshold = 90
                });
            }

            if (monitor.LastRefreshCycleSeconds > 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Refresh_Cycle_Seconds",
                    Name = "Refresh Cycle Time",
                    Description = "Time taken for last market refresh cycle",
                    Value = monitor.LastRefreshCycleSeconds,
                    Unit = "s",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 30,
                    CriticalThreshold = 60
                });
            }

            if (monitor.LastRefreshMarketCount > 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Refresh_Market_Count",
                    Name = "Markets Refreshed",
                    Description = "Number of markets processed in last refresh",
                    Value = monitor.LastRefreshMarketCount,
                    Unit = "markets",
                    VisualType = VisualType.Counter,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Memory and CPU metrics
            if (monitor.LastRefreshMemoryUsage > 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Refresh_Memory_Usage",
                    Name = "Refresh Memory",
                    Description = "Memory usage during market refresh",
                    Value = monitor.LastRefreshMemoryUsage / (1024.0 * 1024.0), // Convert to MB
                    Unit = "MB",
                    VisualType = VisualType.ProgressBar,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 500,
                    CriticalThreshold = 1000
                });
            }

            if (monitor.LastRefreshThroughput > 0)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = "Refresh_Throughput",
                    Name = "Refresh Throughput",
                    Description = "Markets processed per second",
                    Value = monitor.LastRefreshThroughput,
                    Unit = "markets/sec",
                    VisualType = VisualType.SpeedDial,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 10,
                    CriticalThreshold = 50
                });
            }

            // WebSocket Connection Manager Status
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "WebSocket_Metrics_Recording",
                Name = "WebSocket Metrics",
                Description = "WebSocket performance metrics recording status",
                Value = monitor.WebSocketConnectionManagerMetricsRecording ? 1 : 0,
                Unit = "status",
                VisualType = VisualType.TrafficLight,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 0.5,
                CriticalThreshold = 0.5
            });

            // System Status
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "System_StartingUp",
                Name = "System Starting",
                Description = "System startup status",
                Value = monitor.IsStartingUp ? 1 : 0,
                Unit = "status",
                VisualType = VisualType.TrafficLight,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "System_ShuttingDown",
                Name = "System Shutting Down",
                Description = "System shutdown status",
                Value = monitor.IsShuttingDown ? 1 : 0,
                Unit = "status",
                VisualType = VisualType.TrafficLight,
                Category = category,
                Timestamp = DateTime.UtcNow
            });

            return metrics;
        }

        /// <summary>
        /// Converts database performance metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToPerformanceMetrics(
            this IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> databaseMetrics,
            string category = "Database Performance")
        {
            var metrics = new List<PerformanceMetric>();

            if (databaseMetrics != null)
            {
                foreach (var kvp in databaseMetrics)
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
            }

            return metrics;
        }

        /// <summary>
        /// Converts OverseerClientService metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToPerformanceMetrics(
            this IReadOnlyDictionary<string, object> overseerMetrics,
            string category = "Overseer Client Service")
        {
            var metrics = new List<PerformanceMetric>();

            if (overseerMetrics != null)
            {
                foreach (var kvp in overseerMetrics)
                {
                    var metricName = kvp.Key;
                    var value = kvp.Value;

                    // Convert based on value type
                    if (value is double doubleValue)
                    {
                        metrics.Add(new GeneralPerformanceMetric
                        {
                            Id = $"Overseer_{metricName}",
                            Name = metricName,
                            Description = $"Overseer metric: {metricName}",
                            Value = doubleValue,
                            Unit = "value",
                            VisualType = VisualType.NumericDisplay,
                            Category = category,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    else if (value is int intValue)
                    {
                        metrics.Add(new GeneralPerformanceMetric
                        {
                            Id = $"Overseer_{metricName}",
                            Name = metricName,
                            Description = $"Overseer metric: {metricName}",
                            Value = intValue,
                            Unit = "count",
                            VisualType = VisualType.Counter,
                            Category = category,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    else if (value is TimeSpan timeSpanValue)
                    {
                        metrics.Add(new GeneralPerformanceMetric
                        {
                            Id = $"Overseer_{metricName}",
                            Name = metricName,
                            Description = $"Overseer metric: {metricName}",
                            Value = timeSpanValue.TotalMilliseconds,
                            Unit = "ms",
                            VisualType = VisualType.SpeedDial,
                            Category = category,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }

            return metrics;
        }

        /// <summary>
        /// Converts WebSocket performance metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToWebSocketPerformanceMetrics(
            this CentralPerformanceMonitor monitor,
            string webSocketCategory = "WebSocket Performance")
        {
            var metrics = new List<PerformanceMetric>();

            // WebSocket processing times
            var avgProcessingTimes = monitor.GetAverageProcessingTimesMs();
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
                    Category = webSocketCategory,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100,
                    CriticalThreshold = 500
                });
            }

            // WebSocket buffer usage
            var bufferUsage = monitor.GetBufferUsageBytes();
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
                    Category = webSocketCategory,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 50,
                    CriticalThreshold = 100
                });
            }

            // WebSocket operation times
            var operationTimes = monitor.GetAsyncOperationTimesMs();
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
                    Category = webSocketCategory,
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 200,
                    CriticalThreshold = 1000
                });
            }

            // WebSocket semaphore waits
            var semaphoreWaits = monitor.GetSemaphoreWaitCounts();
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
                    Category = webSocketCategory,
                    Timestamp = DateTime.UtcNow
                });
            }

            return metrics;
        }

        /// <summary>
        /// Converts OrderBook service processing metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToOrderBookPerformanceMetrics(
            this CentralPerformanceMonitor monitor,
            string orderBookCategory = "OrderBook Service")
        {
            var metrics = new List<PerformanceMetric>();

            var processingMetrics = monitor.GetOrderBookServiceProcessingMetricsRollingAverages();

            // Event Queue
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "OrderBook_EventQueue_AvgTime",
                Name = "Event Queue Avg Time",
                Description = "Average processing time for EventQueue",
                Value = processingMetrics.EventQueueAvgTime,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                Category = orderBookCategory,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 50,
                CriticalThreshold = 200
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "OrderBook_EventQueue_TotalOps",
                Name = "Event Queue Total Ops",
                Description = "Total operations processed for EventQueue",
                Value = processingMetrics.EventQueueTotalOps,
                Unit = "ops",
                VisualType = VisualType.Counter,
                Category = orderBookCategory,
                Timestamp = DateTime.UtcNow
            });

            // Ticker Queue
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "OrderBook_TickerQueue_AvgTime",
                Name = "Ticker Queue Avg Time",
                Description = "Average processing time for TickerQueue",
                Value = processingMetrics.TickerQueueAvgTime,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                Category = orderBookCategory,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 50,
                CriticalThreshold = 200
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "OrderBook_TickerQueue_TotalOps",
                Name = "Ticker Queue Total Ops",
                Description = "Total operations processed for TickerQueue",
                Value = processingMetrics.TickerQueueTotalOps,
                Unit = "ops",
                VisualType = VisualType.Counter,
                Category = orderBookCategory,
                Timestamp = DateTime.UtcNow
            });

            // Notification Queue
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "OrderBook_NotificationQueue_AvgTime",
                Name = "Notification Queue Avg Time",
                Description = "Average processing time for NotificationQueue",
                Value = processingMetrics.NotificationQueueAvgTime,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                Category = orderBookCategory,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 50,
                CriticalThreshold = 200
            });

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "OrderBook_NotificationQueue_TotalOps",
                Name = "Notification Queue Total Ops",
                Description = "Total operations processed for NotificationQueue",
                Value = processingMetrics.NotificationQueueTotalOps,
                Unit = "ops",
                VisualType = VisualType.Counter,
                Category = orderBookCategory,
                Timestamp = DateTime.UtcNow
            });

            return metrics;
        }

        /// <summary>
        /// Converts SubscriptionManager metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToSubscriptionPerformanceMetrics(
            this CentralPerformanceMonitor monitor,
            string subscriptionCategory = "Subscription Manager")
        {
            var metrics = new List<PerformanceMetric>();

            var operationMetrics = monitor.GetOperationMetrics();
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
                        Category = subscriptionCategory,
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
                    Category = subscriptionCategory,
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
                    Category = subscriptionCategory,
                    Timestamp = DateTime.UtcNow
                });
            }

            var lockMetrics = monitor.GetLockContentionMetrics();
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
                    Category = subscriptionCategory,
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
                    Category = subscriptionCategory,
                    Timestamp = DateTime.UtcNow
                });
            }

            return metrics;
        }

        /// <summary>
        /// Converts MessageProcessor metrics to PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToMessageProcessorPerformanceMetrics(
            this CentralPerformanceMonitor monitor,
            string messageProcessorCategory = "Message Processor")
        {
            var metrics = new List<PerformanceMetric>();

            var processingMetrics = monitor.GetMessageProcessingMetrics();
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
                Category = messageProcessorCategory,
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
                Category = messageProcessorCategory,
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
                Category = messageProcessorCategory,
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
                Category = messageProcessorCategory,
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 1000,
                CriticalThreshold = 5000
            });

            // Duplicate message metrics
            var duplicateMetrics = monitor.GetDuplicateMessageMetrics();
            var (duplicateCount, duplicatesInWindow, lastWarningTime) = duplicateMetrics;

            metrics.Add(new GeneralPerformanceMetric
            {
                Id = "MsgProc_DuplicateCount",
                Name = "Duplicate Messages",
                Description = "Total duplicate messages detected",
                Value = duplicateCount,
                Unit = "duplicates",
                VisualType = VisualType.Counter,
                Category = messageProcessorCategory,
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
                Category = messageProcessorCategory,
                Timestamp = DateTime.UtcNow
            });

            // Message type distribution
            var messageTypeMetrics = monitor.GetMessageTypeMetrics();
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
                    Category = messageProcessorCategory,
                    Timestamp = DateTime.UtcNow
                });
            }

            return metrics;
        }

        /// <summary>
        /// Gets all performance metrics from CentralPerformanceMonitor as a unified collection
        /// </summary>
        public static IEnumerable<PerformanceMetric> GetAllPerformanceMetrics(this CentralPerformanceMonitor monitor)
        {
            var allMetrics = new List<PerformanceMetric>();

            // Core monitor metrics
            allMetrics.AddRange(monitor.ToCorePerformanceMetrics());

            // Database metrics
            var dbMetrics = monitor.GetPerformanceMetrics();
            allMetrics.AddRange(dbMetrics.ToPerformanceMetrics());

            // Overseer metrics
            if (monitor.OverseerClientServiceMetrics != null)
            {
                allMetrics.AddRange(monitor.OverseerClientServiceMetrics.ToPerformanceMetrics());
            }

            // WebSocket metrics
            allMetrics.AddRange(monitor.ToWebSocketPerformanceMetrics());

            // OrderBook service metrics
            allMetrics.AddRange(monitor.ToOrderBookPerformanceMetrics());

            // Subscription manager metrics
            allMetrics.AddRange(monitor.ToSubscriptionPerformanceMetrics());

            // Message processor metrics
            allMetrics.AddRange(monitor.ToMessageProcessorPerformanceMetrics());

            return allMetrics;
        }
    }
}