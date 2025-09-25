namespace BacklashPatterns
{
    /// <summary>
    /// Summary of pattern detection performance metrics.
    /// </summary>
    public class PatternDetectionMetricsSummary
    {
        /// <summary>
        /// Gets or sets the total detection time in milliseconds.
        /// </summary>
        public long TotalDetectionTimeMs { get; set; }
        /// <summary>
        /// Gets or sets the total number of candles processed.
        /// </summary>
        public int TotalCandlesProcessed { get; set; }
        /// <summary>
        /// Gets or sets the total number of patterns found.
        /// </summary>
        public int TotalPatternsFound { get; set; }
        /// <summary>
        /// Gets or sets the dictionary of pattern check times.
        /// </summary>
        public Dictionary<string, long> PatternCheckTimes { get; set; } = new Dictionary<string, long>();
        /// <summary>
        /// Gets or sets the dictionary of pattern counts.
        /// </summary>
        public Dictionary<string, int> PatternCounts { get; set; } = new Dictionary<string, int>();
    }
}