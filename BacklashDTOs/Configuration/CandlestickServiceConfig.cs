namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for CandlestickService settings.
    /// </summary>
    public class CandlestickServiceConfig
    {
        /// <summary>
        /// Gets or sets the data retention period in days for candlestick data.
        /// Data older than this will be subject to cleanup.
        /// </summary>
        /// <value>Default is 365 days (1 year).</value>
        public int CandlestickDataRetentionDays { get; set; } = 365;

        /// <summary>
        /// Gets or sets the maximum number of candlesticks to keep per market per interval.
        /// </summary>
        /// <value>Default is 10000 candlesticks.</value>
        public int MaxCandlesticksPerMarket { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the maximum number of parallel tasks for candlestick processing.
        /// </summary>
        /// <value>Default is 4 parallel tasks.</value>
        public int MaxParallelCandlestickTasks { get; set; } = 4;

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for CandlestickService operations.
        /// </summary>
        /// <value>Default is true.</value>
        public bool EnablePerformanceMetrics { get; set; } = true;
    }
}