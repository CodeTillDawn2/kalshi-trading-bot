using System.Collections.Concurrent;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Generic performance monitor for tracking various metrics
    /// in the trading simulator. This class collects comprehensive performance data
    /// including execution times, memory usage, trade counts, and detailed metrics.
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, List<PerformanceRecord>> _performanceRecords;
        private readonly ConcurrentDictionary<string, Dictionary<string, object>> _simulationMetrics;

        /// <summary>
        /// Initializes a new instance of the PerformanceMonitor.
        /// </summary>
        public PerformanceMonitor()
        {
            _performanceRecords = new ConcurrentDictionary<string, List<PerformanceRecord>>();
            _simulationMetrics = new ConcurrentDictionary<string, Dictionary<string, object>>();
        }

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
        /// </remarks>
        public void RecordPerformanceMetrics(
            string methodName,
            long totalExecutionTimeMs,
            int totalItemsProcessed,
            int totalItemsFound,
            Dictionary<string, long>? itemCheckTimes = null)
        {
            var record = new PerformanceRecord(
                Timestamp: DateTime.UtcNow,
                TotalExecutionTimeMs: totalExecutionTimeMs,
                TotalItemsProcessed: totalItemsProcessed,
                TotalItemsFound: totalItemsFound,
                ItemCheckTimes: itemCheckTimes ?? new Dictionary<string, long>()
            );

            _performanceRecords.AddOrUpdate(
                methodName,
                _ => new List<PerformanceRecord> { record },
                (_, list) => { list.Add(record); return list; });
        }

        /// <summary>
        /// Records simulation performance metrics from StrategySimulation.
        /// </summary>
        /// <param name="simulationName">The name of the simulation.</param>
        /// <param name="metrics">The detailed metrics dictionary from StrategySimulation.GetDetailedPerformanceMetrics().</param>
        public void RecordSimulationMetrics(string simulationName, Dictionary<string, object> metrics)
        {
            _simulationMetrics[simulationName] = metrics;
        }

        /// <summary>
        /// Records the execution time for a specific method or operation (for backward compatibility).
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        /// <remarks>
        /// This method is provided for backward compatibility with the IPerformanceMonitor interface.
        /// For comprehensive metrics, use RecordPerformanceMetrics.
        /// </remarks>
        public void RecordExecutionTime(string methodName, long milliseconds)
        {
            RecordPerformanceMetrics(methodName, milliseconds, 0, 0, null);
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
    }

    /// <summary>
    /// Represents a single performance record.
    /// </summary>
    public record PerformanceRecord(
        DateTime Timestamp,
        long TotalExecutionTimeMs,
        int TotalItemsProcessed,
        int TotalItemsFound,
        Dictionary<string, long> ItemCheckTimes
    );

    /// <summary>
    /// Comprehensive performance statistics for operations.
    /// </summary>
    public record PerformanceStats(
        int RecordCount = 0,
        double AverageExecutionTimeMs = 0.0,
        long MinExecutionTimeMs = 0,
        long MaxExecutionTimeMs = 0,
        double AverageItemsProcessed = 0.0,
        long TotalItemsProcessed = 0,
        double AverageItemsFound = 0.0,
        long TotalItemsFound = 0,
        Dictionary<string, (int Count, double AverageMs, long MinMs, long MaxMs)> ItemCheckStats = null
    )
    {
        public PerformanceStats() : this(0, 0.0, 0, 0, 0.0, 0, 0.0, 0, new Dictionary<string, (int, double, long, long)>()) { }
    }
}