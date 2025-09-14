using System.Collections.Concurrent;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Performance monitor specifically for tracking pattern detection metrics
    /// in the trading simulator. This class collects comprehensive performance data
    /// including execution times, pattern counts, and detailed metrics.
    /// </summary>
    public class PatternPerformanceMonitor : IPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, List<PatternDetectionRecord>> _performanceRecords;

        /// <summary>
        /// Initializes a new instance of the PatternPerformanceMonitor.
        /// </summary>
        public PatternPerformanceMonitor()
        {
            _performanceRecords = new ConcurrentDictionary<string, List<PatternDetectionRecord>>();
        }

        /// <summary>
        /// Records comprehensive pattern detection performance metrics.
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="totalDetectionTimeMs">Total time spent on pattern detection.</param>
        /// <param name="totalCandlesProcessed">Number of candles processed.</param>
        /// <param name="totalPatternsFound">Number of patterns found.</param>
        /// <param name="patternCheckTimes">Dictionary of pattern names to their processing times.</param>
        /// <remarks>
        /// All performance metrics are stored in a thread-safe concurrent dictionary with timestamps.
        /// This data can be used for detailed performance analysis and optimization.
        /// </remarks>
        public void RecordPatternDetectionMetrics(
            string methodName,
            long totalDetectionTimeMs,
            int totalCandlesProcessed,
            int totalPatternsFound,
            Dictionary<string, long>? patternCheckTimes = null)
        {
            var record = new PatternDetectionRecord(
                Timestamp: DateTime.UtcNow,
                TotalDetectionTimeMs: totalDetectionTimeMs,
                TotalCandlesProcessed: totalCandlesProcessed,
                TotalPatternsFound: totalPatternsFound,
                PatternCheckTimes: patternCheckTimes ?? new Dictionary<string, long>()
            );

            _performanceRecords.AddOrUpdate(
                methodName,
                _ => new List<PatternDetectionRecord> { record },
                (_, list) => { list.Add(record); return list; });
        }

        /// <summary>
        /// Records the execution time for a specific method or operation (for backward compatibility).
        /// </summary>
        /// <param name="methodName">The name of the method or operation.</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        /// <remarks>
        /// This method is provided for backward compatibility with the IPerformanceMonitor interface.
        /// For pattern detection, use RecordPatternDetectionMetrics for comprehensive metrics.
        /// </remarks>
        public void RecordExecutionTime(string methodName, long milliseconds)
        {
            RecordPatternDetectionMetrics(methodName, milliseconds, 0, 0, null);
        }

        /// <summary>
        /// Gets all recorded performance records for a specific method.
        /// </summary>
        /// <param name="methodName">The name of the method to get records for.</param>
        /// <returns>A list of pattern detection performance records.</returns>
        public IReadOnlyList<PatternDetectionRecord> GetPerformanceRecords(string methodName)
        {
            return _performanceRecords.TryGetValue(methodName, out var records) ? records : new List<PatternDetectionRecord>();
        }

        /// <summary>
        /// Gets comprehensive performance statistics for a specific method.
        /// </summary>
        /// <param name="methodName">The name of the method to get statistics for.</param>
        /// <returns>A comprehensive performance statistics object.</returns>
        public PatternDetectionStats GetPatternDetectionStats(string methodName)
        {
            var records = GetPerformanceRecords(methodName);
            if (!records.Any())
            {
                return new PatternDetectionStats();
            }

            var totalDetectionTimes = records.Select(r => r.TotalDetectionTimeMs).ToList();
            var totalCandles = records.Select(r => r.TotalCandlesProcessed).ToList();
            var totalPatterns = records.Select(r => r.TotalPatternsFound).ToList();

            // Aggregate pattern check times across all records
            var aggregatedPatternTimes = new Dictionary<string, List<long>>();
            foreach (var record in records)
            {
                foreach (var kvp in record.PatternCheckTimes)
                {
                    if (!aggregatedPatternTimes.ContainsKey(kvp.Key))
                    {
                        aggregatedPatternTimes[kvp.Key] = new List<long>();
                    }
                    aggregatedPatternTimes[kvp.Key].Add(kvp.Value);
                }
            }

            var patternStats = aggregatedPatternTimes.ToDictionary(
                kvp => kvp.Key,
                kvp => (
                    Count: kvp.Value.Count,
                    AverageMs: kvp.Value.Average(),
                    MinMs: kvp.Value.Min(),
                    MaxMs: kvp.Value.Max()
                )
            );

            return new PatternDetectionStats(
                RecordCount: records.Count,
                AverageDetectionTimeMs: totalDetectionTimes.Average(),
                MinDetectionTimeMs: totalDetectionTimes.Min(),
                MaxDetectionTimeMs: totalDetectionTimes.Max(),
                AverageCandlesProcessed: totalCandles.Average(),
                TotalCandlesProcessed: totalCandles.Sum(),
                AveragePatternsFound: totalPatterns.Average(),
                TotalPatternsFound: totalPatterns.Sum(),
                PatternCheckStats: patternStats
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
        /// Clears all recorded performance data.
        /// </summary>
        public void Clear()
        {
            _performanceRecords.Clear();
        }

        /// <summary>
        /// Gets a summary of all performance metrics.
        /// </summary>
        /// <returns>A dictionary mapping method names to their comprehensive performance statistics.</returns>
        public Dictionary<string, PatternDetectionStats> GetAllPatternDetectionStats()
        {
            return _performanceRecords.Keys.ToDictionary(
                method => method,
                method => GetPatternDetectionStats(method)
            );
        }

        /// <summary>
        /// Gets performance statistics for a specific method (for backward compatibility).
        /// </summary>
        /// <param name="methodName">The name of the method to get statistics for.</param>
        /// <returns>A tuple containing count, average, min, and max execution times.</returns>
        public (int Count, double AverageMs, long MinMs, long MaxMs) GetPerformanceStats(string methodName)
        {
            var stats = GetPatternDetectionStats(methodName);
            return (
                Count: stats.RecordCount,
                AverageMs: stats.AverageDetectionTimeMs,
                MinMs: stats.MinDetectionTimeMs,
                MaxMs: stats.MaxDetectionTimeMs
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
            return records.Select(r => (r.Timestamp, r.TotalDetectionTimeMs)).ToList();
        }
    }

    /// <summary>
    /// Represents a single pattern detection performance record.
    /// </summary>
    public record PatternDetectionRecord(
        DateTime Timestamp,
        long TotalDetectionTimeMs,
        int TotalCandlesProcessed,
        int TotalPatternsFound,
        Dictionary<string, long> PatternCheckTimes
    );

    /// <summary>
    /// Comprehensive performance statistics for pattern detection operations.
    /// </summary>
    public record PatternDetectionStats(
        int RecordCount = 0,
        double AverageDetectionTimeMs = 0.0,
        long MinDetectionTimeMs = 0,
        long MaxDetectionTimeMs = 0,
        double AverageCandlesProcessed = 0.0,
        long TotalCandlesProcessed = 0,
        double AveragePatternsFound = 0.0,
        long TotalPatternsFound = 0,
        Dictionary<string, (int Count, double AverageMs, long MinMs, long MaxMs)> PatternCheckStats = null
    )
    {
        public PatternDetectionStats() : this(0, 0.0, 0, 0, 0.0, 0, 0.0, 0, new Dictionary<string, (int, double, long, long)>()) { }
    }
}