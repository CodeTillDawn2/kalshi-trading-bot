using BacklashInterfaces.PerformanceMetrics;
using System.Collections.Concurrent;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Generic performance monitor for tracking various metrics
    /// in the trading simulator. This class collects comprehensive performance data
    /// including execution times, memory usage, trade counts, and detailed metrics.
    /// It supports both general operation metrics and specialized simulation performance metrics
    /// from StrategySimulation, with typed access via StrategySimulationPerformanceMetrics.
    /// All metrics collection can be enabled/disabled via the EnablePerformanceMetrics property.
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, List<PerformanceRecord>> _performanceRecords;
        private readonly ConcurrentDictionary<string, Dictionary<string, object>> _simulationMetrics;

        // Configurable metrics data structure for GUI consumption
        private Dictionary<string, object> _configurableMetrics;

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled.
        /// When disabled, metric recording methods will return early without storing data.
        /// Default is true. Can be configured from appsettings.json Simulation:Simulation_EnablePerformanceMetrics.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the PerformanceMonitor.
        /// </summary>
        public PerformanceMonitor()
        {
            _performanceRecords = new ConcurrentDictionary<string, List<PerformanceRecord>>();
            _simulationMetrics = new ConcurrentDictionary<string, Dictionary<string, object>>();
            _configurableMetrics = new Dictionary<string, object>();
            InitializeConfigurableMetrics();

            // Send initial status message to indicate enablement state
            if (!EnablePerformanceMetrics)
            {
                SendEnablementStatus();
            }
        }

        /// <summary>
        /// Initializes the configurable metrics data structure with default values.
        /// </summary>
        private void InitializeConfigurableMetrics()
        {
            _configurableMetrics = new Dictionary<string, object>
            {
                // Only include whether performance metrics are enabled for this class
                ["EnablePerformanceMetrics"] = EnablePerformanceMetrics
            };
        }

        #region General Performance Methods

        /// <summary>
        /// Records comprehensive performance metrics.
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="totalExecutionTimeMs">Total time spent on execution.</param>
        /// <param name="totalItemsProcessed">Number of items processed.</param>
        /// <param name="totalItemsFound">Number of items found.</param>
        /// <param name="itemCheckTimes">Dictionary of item names to their processing times.</param>
        /// <remarks>
        /// All performance metrics are stored in a thread-safe concurrent dictionary with timestamps.
        /// This data can be used for detailed performance analysis and optimization.
        /// Metrics collection is gated by EnablePerformanceMetrics flag.
        /// </remarks>
        public void RecordPerformanceMetrics(
            string methodName,
            long totalExecutionTimeMs,
            int totalItemsProcessed,
            int totalItemsFound,
            Dictionary<string, long>? itemCheckTimes = null)
        {
            if (!EnablePerformanceMetrics)
            {
                return;
            }

            var record = new PerformanceRecord(
                Timestamp: DateTime.UtcNow,
                TotalExecutionTimeMs: totalExecutionTimeMs,
                TotalItemsProcessed: totalItemsProcessed,
                TotalItemsFound: totalItemsFound,
                ItemCheckTimes: itemCheckTimes ?? new Dictionary<string, long>(),
                MetricsEnabled: null
            );

            _performanceRecords.AddOrUpdate(
                methodName,
                _ => new List<PerformanceRecord> { record },
                (_, list) => { list.Add(record); return list; });
        }

        /// <summary>
        /// Sends the current enablement status to indicate whether this class is enabled for performance monitoring.
        /// </summary>
        private void SendEnablementStatus()
        {
            // Update the configurable metrics with current enablement status
            _configurableMetrics["EnablePerformanceMetrics"] = EnablePerformanceMetrics;
        }

        #region Strategy Simulation Methods

        /// <summary>
        /// Records strategy simulation performance metrics from StrategySimulation.
        /// </summary>
        /// <param name="simulationName">The name of the simulation.</param>
        /// <param name="metrics">The detailed metrics dictionary from StrategySimulation.GetDetailedPerformanceMetrics().</param>
        /// <remarks>
        /// Metrics collection is gated by EnablePerformanceMetrics flag.
        /// This overload assumes metrics are enabled for the calling class.
        /// </remarks>
        public void RecordSimulationMetrics(string simulationName, Dictionary<string, object> metrics)
        {
            RecordSimulationMetrics(simulationName, metrics, true);
        }

        /// <summary>
        /// Records strategy simulation performance metrics from StrategySimulation.
        /// </summary>
        /// <param name="simulationName">The name of the simulation.</param>
        /// <param name="metrics">The detailed metrics dictionary from StrategySimulation.GetDetailedPerformanceMetrics().</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        /// <remarks>
        /// Metrics collection is gated by EnablePerformanceMetrics flag.
        /// The metricsEnabled parameter indicates the enablement status of the calling class.
        /// </remarks>
        public void RecordSimulationMetrics(string simulationName, Dictionary<string, object> metrics, bool metricsEnabled)
        {
            if (!EnablePerformanceMetrics)
            {
                return;
            }

            // Add enablement status to the metrics
            var enhancedMetrics = new Dictionary<string, object>(metrics)
            {
                ["MetricsEnabled"] = metricsEnabled
            };

            _simulationMetrics[simulationName] = enhancedMetrics;
        }

        /// <summary>
        /// Records the execution time for a specific method or operation (for backward compatibility).
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        /// <remarks>
        /// This method is provided for backward compatibility with the IPerformanceMonitor interface.
        /// For comprehensive metrics, use RecordPerformanceMetrics.
        /// Metrics collection is gated by EnablePerformanceMetrics flag.
        /// </remarks>
        public void RecordExecutionTime(string methodName, long milliseconds)
        {
            if (!EnablePerformanceMetrics)
            {
                return;
            }
            RecordPerformanceMetrics(methodName, milliseconds, 0, 0, null);
        }

        /// <summary>
        /// Records the execution time for a specific method or operation with enablement status.
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        /// <remarks>
        /// This overloaded method includes enablement status tracking.
        /// Metrics collection is gated by EnablePerformanceMetrics flag.
        /// The metricsEnabled parameter indicates the enablement status of the calling class.
        /// </remarks>
        public void RecordExecutionTime(string methodName, long milliseconds, bool metricsEnabled)
        {
            if (!EnablePerformanceMetrics)
            {
                return;
            }

            var record = new PerformanceRecord(
                Timestamp: DateTime.UtcNow,
                TotalExecutionTimeMs: milliseconds,
                TotalItemsProcessed: 0,
                TotalItemsFound: 0,
                ItemCheckTimes: new Dictionary<string, long>(),
                MetricsEnabled: metricsEnabled
            );

            _performanceRecords.AddOrUpdate(
                methodName,
                _ => new List<PerformanceRecord> { record },
                (_, list) => { list.Add(record); return list; });
        }

        /// <summary>
        /// Gets all recorded performance records for a specific method.
        /// </summary>
        /// <param name="methodName">The name of the method to get records for.</param>
        /// <returns>A list of performance records.</returns>
        public IReadOnlyList<PerformanceRecord> GetPerformanceRecords(string methodName)
        {
            return _performanceRecords.TryGetValue(methodName, out var records) ? records : new List<PerformanceRecord>();
        }

        /// <summary>
        /// Gets simulation metrics for a specific simulation.
        /// </summary>
        /// <param name="simulationName">The name of the simulation.</param>
        /// <returns>The metrics dictionary.</returns>
        public Dictionary<string, object> GetSimulationMetrics(string simulationName)
        {
            return _simulationMetrics.TryGetValue(simulationName, out var metrics) ? metrics : new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets typed strategy simulation performance metrics for a specific simulation.
        /// </summary>
        /// <param name="simulationName">The name of the simulation.</param>
        /// <returns>The typed performance metrics record.</returns>
        public StrategySimulationPerformanceMetrics GetTypedStrategySimulationMetrics(string simulationName)
        {
            var metrics = GetSimulationMetrics(simulationName);
            return ConvertToStrategySimulationPerformanceMetrics(metrics);
        }

        /// <summary>
        /// Converts a raw metrics dictionary to typed StrategySimulationPerformanceMetrics.
        /// </summary>
        /// <param name="metrics">The raw metrics dictionary from StrategySimulation.</param>
        /// <returns>The typed performance metrics.</returns>
        private StrategySimulationPerformanceMetrics ConvertToStrategySimulationPerformanceMetrics(Dictionary<string, object> metrics)
        {
            if (metrics.Count == 0)
            {
                return new StrategySimulationPerformanceMetrics(
                    TimeSpan.Zero, 0.0, 0, 0, 0.0, 0.0, 0, 0, 0, 0, 0.0, 0, 0.0, 0.0, 0, 0.0, 0.0, 0
                );
            }

            return new StrategySimulationPerformanceMetrics(
                TotalExecutionTime: (TimeSpan)metrics.GetValueOrDefault("TotalExecutionTime", TimeSpan.Zero),
                AverageExecutionTimeMs: (double)metrics.GetValueOrDefault("AverageExecutionTimeMs", 0.0),
                PeakMemoryUsage: (long)metrics.GetValueOrDefault("PeakMemoryUsage", 0L),
                TotalSnapshotsProcessed: (int)metrics.GetValueOrDefault("TotalSnapshotsProcessed", 0),
                PerformanceThresholdMs: (double)metrics.GetValueOrDefault("PerformanceThresholdMs", 0.0),
                MemoryThresholdMB: (double)metrics.GetValueOrDefault("MemoryThresholdMB", 0.0),
                SlowOperationsCount: (int)metrics.GetValueOrDefault("SlowOperationsCount", 0),
                HighMemoryOperationsCount: (int)metrics.GetValueOrDefault("HighMemoryOperationsCount", 0),
                RestingOrdersCount: (int)metrics.GetValueOrDefault("RestingOrdersCount", 0),
                CurrentPosition: (int)metrics.GetValueOrDefault("CurrentPosition", 0),
                CurrentCash: (double)metrics.GetValueOrDefault("CurrentCash", 0.0),
                TotalTradesExecuted: (int)metrics.GetValueOrDefault("TotalTradesExecuted", 0),
                AverageDecisionTimeMs: (double)metrics.GetValueOrDefault("AverageDecisionTimeMs", 0.0),
                AverageApplyTimeMs: (double)metrics.GetValueOrDefault("AverageApplyTimeMs", 0.0),
                SlowDecisionsCount: (int)metrics.GetValueOrDefault("SlowDecisionsCount", 0),
                DecisionThresholdMs: (double)metrics.GetValueOrDefault("DecisionThresholdMs", 0.0),
                BandWidthRatioThreshold: (double)metrics.GetValueOrDefault("BandWidthRatioThreshold", 0.0),
                TradeRateLimitPerSnapshot: (int)metrics.GetValueOrDefault("TradeRateLimitPerSnapshot", 0)
            );
        }

        #endregion

        /// <summary>
        /// Gets comprehensive performance statistics for a specific method.
        /// </summary>
        /// <param name="methodName">The name of the method to get statistics for.</param>
        /// <returns>A comprehensive performance statistics object.</returns>
        public PerformanceStats GetPerformanceStats(string methodName)
        {
            var records = GetPerformanceRecords(methodName);
            if (!records.Any())
            {
                return new PerformanceStats();
            }

            var totalExecutionTimes = records.Select(r => r.TotalExecutionTimeMs).ToList();
            var totalItems = records.Select(r => r.TotalItemsProcessed).ToList();
            var totalFound = records.Select(r => r.TotalItemsFound).ToList();

            // Aggregate item check times across all records
            var aggregatedItemTimes = new Dictionary<string, List<long>>();
            foreach (var record in records)
            {
                foreach (var kvp in record.ItemCheckTimes)
                {
                    if (!aggregatedItemTimes.ContainsKey(kvp.Key))
                    {
                        aggregatedItemTimes[kvp.Key] = new List<long>();
                    }
                    aggregatedItemTimes[kvp.Key].Add(kvp.Value);
                }
            }

            var itemStats = aggregatedItemTimes.ToDictionary(
                kvp => kvp.Key,
                kvp => (
                    Count: kvp.Value.Count,
                    AverageMs: kvp.Value.Average(),
                    MinMs: kvp.Value.Min(),
                    MaxMs: kvp.Value.Max()
                )
            );

            return new PerformanceStats(
                RecordCount: records.Count,
                AverageExecutionTimeMs: totalExecutionTimes.Average(),
                MinExecutionTimeMs: totalExecutionTimes.Min(),
                MaxExecutionTimeMs: totalExecutionTimes.Max(),
                AverageItemsProcessed: totalItems.Average(),
                TotalItemsProcessed: totalItems.Sum(),
                AverageItemsFound: totalFound.Average(),
                TotalItemsFound: totalFound.Sum(),
                ItemCheckStats: itemStats
            );
        }

        /// <summary>
        /// Gets all method names that have recorded performance data.
        /// </summary>
        /// <returns>A collection of method names.</returns>
        public IEnumerable<string> GetMonitoredMethods()
        {
            return _performanceRecords.Keys;
        }

        /// <summary>
        /// Gets all simulation names that have recorded metrics.
        /// </summary>
        /// <returns>A collection of simulation names.</returns>
        public IEnumerable<string> GetMonitoredSimulations()
        {
            return _simulationMetrics.Keys;
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
        /// Updates the configurable metrics data structure with current values.
        /// </summary>
        private void UpdateConfigurableMetrics()
        {
            _configurableMetrics["EnablePerformanceMetrics"] = EnablePerformanceMetrics;
        }

        /// <summary>
        /// Clears all recorded performance data.
        /// </summary>
        public void Clear()
        {
            _performanceRecords.Clear();
            _simulationMetrics.Clear();
        }

        /// <summary>
        /// Gets a summary of all performance metrics.
        /// </summary>
        /// <returns>A dictionary mapping method names to their comprehensive performance statistics.</returns>
        public Dictionary<string, PerformanceStats> GetAllPerformanceStats()
        {
            return _performanceRecords.Keys.ToDictionary(
                method => method,
                method => GetPerformanceStats(method)
            );
        }


        #region PatternUtils Methods

        /// <summary>
        /// Records PatternUtils-specific performance metrics.
        /// </summary>
        /// <param name="totalCalculations">Total number of calculations performed.</param>
        /// <param name="totalCalculationTimeMs">Total time spent on calculations in milliseconds.</param>
        /// <param name="cacheHits">Number of cache hits.</param>
        /// <param name="cacheMisses">Number of cache misses.</param>
        /// <param name="throughput">Calculations per second (optional).</param>
        /// <param name="cpuTimeMs">CPU time used in milliseconds (optional).</param>
        /// <param name="memoryUsage">Memory usage in bytes (optional).</param>
        /// <param name="configurationStatus">Current configuration status dictionary (optional).</param>
        /// <remarks>
        /// This method is specifically designed to integrate with PatternUtils performance metrics.
        /// All parameters are optional to support different levels of metric collection.
        /// Metrics collection is gated by EnablePerformanceMetrics flag.
        /// </remarks>
        public void RecordPatternUtilsMetrics(
            int totalCalculations,
            long totalCalculationTimeMs,
            int cacheHits,
            int cacheMisses,
            double? throughput = null,
            double? cpuTimeMs = null,
            long? memoryUsage = null,
            Dictionary<string, bool>? configurationStatus = null)
        {
            if (!EnablePerformanceMetrics)
            {
                return;
            }

            var metrics = new Dictionary<string, object>
            {
                ["TotalCalculations"] = totalCalculations,
                ["TotalCalculationTimeMs"] = totalCalculationTimeMs,
                ["CacheHits"] = cacheHits,
                ["CacheMisses"] = cacheMisses,
                ["CacheHitRate"] = cacheHits + cacheMisses > 0 ? (double)cacheHits / (cacheHits + cacheMisses) * 100.0 : 0.0,
                ["AverageCalculationTimeMs"] = totalCalculations > 0 ? (double)totalCalculationTimeMs / totalCalculations : 0.0
            };

            if (throughput.HasValue) metrics["Throughput"] = throughput.Value;
            if (cpuTimeMs.HasValue) metrics["CpuTimeMs"] = cpuTimeMs.Value;
            if (memoryUsage.HasValue) metrics["MemoryUsage"] = memoryUsage.Value;
            if (configurationStatus != null) metrics["ConfigurationStatus"] = configurationStatus;

            _simulationMetrics["PatternUtils"] = metrics;
        }

        /// <summary>
        /// Records PatternUtils scalability test results.
        /// </summary>
        /// <param name="scalabilityResults">Dictionary mapping data sizes to throughput measurements.</param>
        /// <remarks>
        /// Metrics collection is gated by EnablePerformanceMetrics flag.
        /// </remarks>
        public void RecordPatternUtilsScalability(Dictionary<int, double> scalabilityResults)
        {
            if (!EnablePerformanceMetrics)
            {
                return;
            }
            _simulationMetrics["PatternUtils.Scalability"] = new Dictionary<string, object>
            {
                ["ScalabilityResults"] = scalabilityResults,
                ["Timestamp"] = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Records PatternUtils CPU profiling results.
        /// </summary>
        /// <param name="cpuTimeMs">CPU time used in milliseconds.</param>
        /// <param name="throughput">Calculations per second achieved.</param>
        /// <param name="dataSize">Size of data processed.</param>
        /// <remarks>
        /// Metrics collection is gated by EnablePerformanceMetrics flag.
        /// </remarks>
        public void RecordPatternUtilsCpuProfile(double cpuTimeMs, double throughput, int dataSize)
        {
            if (!EnablePerformanceMetrics)
            {
                return;
            }
            _simulationMetrics["PatternUtils.CpuProfile"] = new Dictionary<string, object>
            {
                ["CpuTimeMs"] = cpuTimeMs,
                ["Throughput"] = throughput,
                ["DataSize"] = dataSize,
                ["Timestamp"] = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets PatternUtils performance metrics.
        /// </summary>
        /// <returns>The PatternUtils metrics dictionary.</returns>
        public Dictionary<string, object> GetPatternUtilsMetrics()
        {
            return GetSimulationMetrics("PatternUtils");
        }

        /// <summary>
        /// Gets PatternUtils scalability results.
        /// </summary>
        /// <returns>The scalability results dictionary.</returns>
        public Dictionary<string, object> GetPatternUtilsScalability()
        {
            return GetSimulationMetrics("PatternUtils.Scalability");
        }

        /// <summary>
        /// Gets PatternUtils CPU profiling results.
        /// </summary>
        /// <returns>The CPU profiling results dictionary.</returns>
        public Dictionary<string, object> GetPatternUtilsCpuProfile()
        {
            return GetSimulationMetrics("PatternUtils.CpuProfile");
        }

        /// <summary>
        /// Gets typed PatternUtils performance metrics.
        /// </summary>
        /// <returns>The typed PatternUtils metrics record.</returns>
        public PatternUtilsPerformanceMetrics GetTypedPatternUtilsMetrics()
        {
            var metrics = GetPatternUtilsMetrics();
            return ConvertToPatternUtilsPerformanceMetrics(metrics);
        }

        /// <summary>
        /// Converts raw PatternUtils metrics to typed record.
        /// </summary>
        /// <param name="metrics">The raw metrics dictionary.</param>
        /// <returns>The typed PatternUtils performance metrics.</returns>
        private PatternUtilsPerformanceMetrics ConvertToPatternUtilsPerformanceMetrics(Dictionary<string, object> metrics)
        {
            if (metrics.Count == 0)
            {
                return new PatternUtilsPerformanceMetrics(
                    0, 0, 0, 0, 0.0, 0.0, null, null, null, null
                );
            }

            return new PatternUtilsPerformanceMetrics(
                TotalCalculations: (int)metrics.GetValueOrDefault("TotalCalculations", 0),
                TotalCalculationTimeMs: (long)metrics.GetValueOrDefault("TotalCalculationTimeMs", 0L),
                CacheHits: (int)metrics.GetValueOrDefault("CacheHits", 0),
                CacheMisses: (int)metrics.GetValueOrDefault("CacheMisses", 0),
                CacheHitRate: (double)metrics.GetValueOrDefault("CacheHitRate", 0.0),
                AverageCalculationTimeMs: (double)metrics.GetValueOrDefault("AverageCalculationTimeMs", 0.0),
                Throughput: metrics.TryGetValue("Throughput", out var t) ? (double?)t : null,
                CpuTimeMs: metrics.TryGetValue("CpuTimeMs", out var c) ? (double?)c : null,
                MemoryUsage: metrics.TryGetValue("MemoryUsage", out var m) ? (long?)m : null,
                ConfigurationStatus: metrics.TryGetValue("ConfigurationStatus", out var config) ? (Dictionary<string, bool>)config : null
            );
        }

        /// <summary>
        /// Gets all recorded execution times for a specific method (for backward compatibility).
        /// </summary>
        /// <param name="methodName">The name of the method to get times for.</param>
        /// <returns>A list of execution time records with timestamps.</returns>
        public IReadOnlyList<(DateTime Timestamp, long Milliseconds)> GetExecutionTimes(string methodName)
        {
            var records = GetPerformanceRecords(methodName);
            return records.Select(r => (r.Timestamp, r.TotalExecutionTimeMs)).ToList();
        }

        #endregion

        /// <summary>
        /// Records delayed API call metrics for rate limiting analysis.
        /// </summary>
        /// <param name="componentName">The name of the component recording the metrics.</param>
        /// <param name="totalDelayedCalls">Total number of delayed API calls in the period.</param>
        /// <param name="averageWaitTimeMs">Average wait time in milliseconds.</param>
        /// <param name="maxWaitTimeMs">Maximum wait time in milliseconds.</param>
        /// <param name="currentQueueDepth">Current number of items in the delay queue.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled.</param>
        public void RecordDelayedApiCallMetrics(string componentName, long totalDelayedCalls, double averageWaitTimeMs, long maxWaitTimeMs, int currentQueueDepth, bool metricsEnabled)
        {
            if (!EnablePerformanceMetrics)
            {
                return;
            }

            var metrics = new Dictionary<string, object>
            {
                ["TotalDelayedCalls"] = totalDelayedCalls,
                ["AverageWaitTimeMs"] = averageWaitTimeMs,
                ["MaxWaitTimeMs"] = maxWaitTimeMs,
                ["CurrentQueueDepth"] = currentQueueDepth,
                ["MetricsEnabled"] = metricsEnabled,
                ["Timestamp"] = DateTime.UtcNow
            };

            _simulationMetrics[$"{componentName}.DelayedApiCalls"] = metrics;
        }

    }

    namespace TradingStrategies.Performance
    {
        /// <summary>
        /// Extension methods to convert complex performance objects into simple PerformanceMetric instances
        /// for uniform GUI handling across all performance monitors.
        /// </summary>
        public static class PerformanceMetricExtensions
        {
            /// <summary>
            /// Converts StrategySimulationPerformanceMetrics to a collection of simple PerformanceMetrics
            /// </summary>
            public static IEnumerable<BacklashCommon.Performance.PerformanceMetric> ToPerformanceMetrics(
                this StrategySimulationPerformanceMetrics simulationMetrics,
                string simulationName)
            {
                var metrics = new List<BacklashCommon.Performance.PerformanceMetric>();

                // Execution Time - Speed Dial
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{simulationName}_ExecutionTime",
                    Name = "Execution Time",
                    Description = "Total time spent executing the simulation",
                    Value = simulationMetrics.TotalExecutionTime.TotalMilliseconds,
                    Unit = "ms",
                    VisualType = BacklashCommon.Performance.VisualType.SpeedDial,
                    Category = "Strategy Simulation",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 5000, // 5 seconds
                    CriticalThreshold = 30000  // 30 seconds
                });

                // Average Execution Time - Speed Dial
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{simulationName}_AvgExecutionTime",
                    Name = "Avg Execution Time",
                    Description = "Average execution time per operation",
                    Value = simulationMetrics.AverageExecutionTimeMs,
                    Unit = "ms",
                    VisualType = BacklashCommon.Performance.VisualType.SpeedDial,
                    Category = "Strategy Simulation",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100, // 100ms
                    CriticalThreshold = 1000 // 1 second
                });

                // Peak Memory Usage - Progress Bar
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{simulationName}_PeakMemory",
                    Name = "Peak Memory",
                    Description = "Maximum memory usage during simulation",
                    Value = simulationMetrics.PeakMemoryUsage / (1024.0 * 1024.0), // Convert to MB
                    Unit = "MB",
                    VisualType = BacklashCommon.Performance.VisualType.ProgressBar,
                    Category = "Strategy Simulation",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 500, // 500MB
                    CriticalThreshold = 1000 // 1GB
                });

                // Memory Threshold Ratio - Traffic Light
                if (simulationMetrics.MemoryThresholdMB > 0)
                {
                    var memoryRatio = (simulationMetrics.PeakMemoryUsage / (1024.0 * 1024.0)) / simulationMetrics.MemoryThresholdMB * 100;
                    metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                    {
                        Id = $"{simulationName}_MemoryThreshold",
                        Name = "Memory Usage %",
                        Description = "Memory usage as percentage of threshold",
                        Value = memoryRatio,
                        Unit = "%",
                        VisualType = BacklashCommon.Performance.VisualType.TrafficLight,
                        Category = "Strategy Simulation",
                        Timestamp = DateTime.UtcNow,
                        MinThreshold = 0,
                        WarningThreshold = 75,
                        CriticalThreshold = 90
                    });
                }

                // Total Trades Executed - Counter
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{simulationName}_TotalTrades",
                    Name = "Total Trades",
                    Description = "Number of trades executed during simulation",
                    Value = simulationMetrics.TotalTradesExecuted,
                    Unit = "trades",
                    VisualType = BacklashCommon.Performance.VisualType.Counter,
                    Category = "Strategy Simulation",
                    Timestamp = DateTime.UtcNow
                });

                // Current Position - Numeric Display
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{simulationName}_CurrentPosition",
                    Name = "Current Position",
                    Description = "Current trading position",
                    Value = simulationMetrics.CurrentPosition,
                    Unit = "units",
                    VisualType = BacklashCommon.Performance.VisualType.NumericDisplay,
                    Category = "Strategy Simulation",
                    Timestamp = DateTime.UtcNow
                });

                // Current Cash - Numeric Display
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{simulationName}_CurrentCash",
                    Name = "Current Cash",
                    Description = "Current cash balance",
                    Value = simulationMetrics.CurrentCash,
                    Unit = "USD",
                    VisualType = BacklashCommon.Performance.VisualType.NumericDisplay,
                    Category = "Strategy Simulation",
                    Timestamp = DateTime.UtcNow
                });

                return metrics;
            }

            /// <summary>
            /// Converts PatternUtilsPerformanceMetrics to a collection of simple PerformanceMetrics
            /// </summary>
            public static IEnumerable<BacklashCommon.Performance.PerformanceMetric> ToPerformanceMetrics(
                this global::TradingStrategies.Trading.Overseer.PatternUtilsPerformanceMetrics patternMetrics,
                string patternName)
            {
                var metrics = new List<BacklashCommon.Performance.PerformanceMetric>();

                // Total Calculations - Counter
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{patternName}_TotalCalculations",
                    Name = "Total Calculations",
                    Description = "Number of pattern calculations performed",
                    Value = patternMetrics.TotalCalculations,
                    Unit = "calculations",
                    VisualType = BacklashCommon.Performance.VisualType.Counter,
                    Category = "Pattern Utils",
                    Timestamp = DateTime.UtcNow
                });

                // Cache Hit Rate - Pie Chart
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{patternName}_CacheHitRate",
                    Name = "Cache Hit Rate",
                    Description = "Percentage of cache hits vs misses",
                    Value = patternMetrics.CacheHitRate,
                    SecondaryValue = 100 - patternMetrics.CacheHitRate, // Miss rate
                    Unit = "%",
                    VisualType = BacklashCommon.Performance.VisualType.PieChart,
                    Category = "Pattern Utils",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 50,
                    CriticalThreshold = 80
                });

                // Average Calculation Time - Speed Dial
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{patternName}_AvgCalcTime",
                    Name = "Avg Calc Time",
                    Description = "Average time per calculation",
                    Value = patternMetrics.AverageCalculationTimeMs,
                    Unit = "ms",
                    VisualType = BacklashCommon.Performance.VisualType.SpeedDial,
                    Category = "Pattern Utils",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 10, // 10ms
                    CriticalThreshold = 100 // 100ms
                });

                // Throughput - Speed Dial
                if (patternMetrics.Throughput.HasValue)
                {
                    metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                    {
                        Id = $"{patternName}_Throughput",
                        Name = "Throughput",
                        Description = "Calculations per second",
                        Value = patternMetrics.Throughput.Value,
                        Unit = "calc/sec",
                        VisualType = BacklashCommon.Performance.VisualType.SpeedDial,
                        Category = "Pattern Utils",
                        Timestamp = DateTime.UtcNow,
                        MinThreshold = 0,
                        WarningThreshold = 100,
                        CriticalThreshold = 1000
                    });
                }

                // Memory Usage - Progress Bar
                if (patternMetrics.MemoryUsage.HasValue)
                {
                    metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                    {
                        Id = $"{patternName}_MemoryUsage",
                        Name = "Memory Usage",
                        Description = "Current memory usage",
                        Value = patternMetrics.MemoryUsage.Value / (1024.0 * 1024.0), // Convert to MB
                        Unit = "MB",
                        VisualType = BacklashCommon.Performance.VisualType.ProgressBar,
                        Category = "Pattern Utils",
                        Timestamp = DateTime.UtcNow,
                        MinThreshold = 0,
                        WarningThreshold = 100, // 100MB
                        CriticalThreshold = 500 // 500MB
                    });
                }

                return metrics;
            }

            /// <summary>
            /// Converts PerformanceStats to a collection of simple PerformanceMetrics
            /// </summary>
            public static IEnumerable<BacklashCommon.Performance.PerformanceMetric> ToPerformanceMetrics(
                this global::TradingStrategies.Trading.Overseer.PerformanceStats stats,
                string methodName)
            {
                var metrics = new List<BacklashCommon.Performance.PerformanceMetric>();

                // Record Count - Badge
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{methodName}_RecordCount",
                    Name = "Record Count",
                    Description = "Number of performance records collected",
                    Value = stats.RecordCount,
                    Unit = "records",
                    VisualType = BacklashCommon.Performance.VisualType.Badge,
                    Category = "Performance Stats",
                    Timestamp = DateTime.UtcNow
                });

                // Average Execution Time - Speed Dial
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{methodName}_AvgExecutionTime",
                    Name = "Avg Execution Time",
                    Description = "Average execution time across all records",
                    Value = stats.AverageExecutionTimeMs,
                    Unit = "ms",
                    VisualType = BacklashCommon.Performance.VisualType.SpeedDial,
                    Category = "Performance Stats",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100,
                    CriticalThreshold = 1000
                });

                // Items Processed - Counter
                metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                {
                    Id = $"{methodName}_TotalItemsProcessed",
                    Name = "Items Processed",
                    Description = "Total number of items processed",
                    Value = stats.TotalItemsProcessed,
                    Unit = "items",
                    VisualType = BacklashCommon.Performance.VisualType.Counter,
                    Category = "Performance Stats",
                    Timestamp = DateTime.UtcNow
                });

                // Success Rate - Progress Bar
                if (stats.TotalItemsFound > 0 && stats.TotalItemsProcessed > 0)
                {
                    var successRate = (double)stats.TotalItemsFound / stats.TotalItemsProcessed * 100;
                    metrics.Add(new BacklashCommon.Performance.GeneralPerformanceMetric
                    {
                        Id = $"{methodName}_SuccessRate",
                        Name = "Success Rate",
                        Description = "Percentage of items successfully found",
                        Value = successRate,
                        Unit = "%",
                        VisualType = BacklashCommon.Performance.VisualType.ProgressBar,
                        Category = "Performance Stats",
                        Timestamp = DateTime.UtcNow,
                        MinThreshold = 0,
                        WarningThreshold = 50,
                        CriticalThreshold = 80
                    });
                }

                return metrics;
            }
        }
    }
}
#endregion
