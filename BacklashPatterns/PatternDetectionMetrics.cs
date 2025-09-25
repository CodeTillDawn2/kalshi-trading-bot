using System.Diagnostics;

namespace BacklashPatterns
{
    /// <summary>
    /// Service for collecting performance metrics during pattern detection.
    /// </summary>
    public class PatternDetectionMetrics
    {
        private readonly Stopwatch _totalStopwatch = new Stopwatch();
        private readonly Dictionary<string, long> _patternCheckTimes = new Dictionary<string, long>();
        private readonly Dictionary<string, int> _patternCounts = new Dictionary<string, int>();
        private int _totalCandlesProcessed;
        private int _totalPatternsFound;

        /// <summary>
        /// Starts overall detection timing.
        /// </summary>
        public void StartDetection() => _totalStopwatch.Start();

        /// <summary>
        /// Stops overall detection timing.
        /// </summary>
        public void StopDetection() => _totalStopwatch.Stop();

        /// <summary>
        /// Records time taken for a specific pattern check.
        /// </summary>
        public void RecordPatternCheckTime(string patternName, long ticks)
        {
            if (!_patternCheckTimes.ContainsKey(patternName))
                _patternCheckTimes[patternName] = 0;
            _patternCheckTimes[patternName] += ticks;
        }

        /// <summary>
        /// Records a pattern detection.
        /// </summary>
        public void RecordPatternFound(string patternName)
        {
            if (!_patternCounts.ContainsKey(patternName))
                _patternCounts[patternName] = 0;
            _patternCounts[patternName]++;
            _totalPatternsFound++;
        }

        /// <summary>
        /// Increments the candle processing count.
        /// </summary>
        public void IncrementCandlesProcessed() => _totalCandlesProcessed++;

        /// <summary>
        /// Gets the performance metrics summary.
        /// </summary>
        public PatternDetectionMetricsSummary GetSummary()
        {
            return new PatternDetectionMetricsSummary
            {
                TotalDetectionTimeMs = _totalStopwatch.ElapsedMilliseconds,
                TotalCandlesProcessed = _totalCandlesProcessed,
                TotalPatternsFound = _totalPatternsFound,
                PatternCheckTimes = new Dictionary<string, long>(_patternCheckTimes),
                PatternCounts = new Dictionary<string, int>(_patternCounts)
            };
        }
    }
}