using System.Text.Json.Serialization;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for TradingSnapshotService settings.
    /// Contains parameters specific to snapshot storage, timing tolerances, and parallel processing.
    /// </summary>
    public class TradingSnapshotServiceConfig
    {
        /// <summary>
        /// Number of seconds allowed as tolerance for snapshot timing irregularities.
        /// Used to determine acceptable deviations from expected snapshot intervals before triggering warnings or discarding snapshots.
        /// Higher values provide more flexibility for irregular market data arrival but may mask timing issues.
        /// Typically set to match expected decision frequency intervals (e.g., 30-60 seconds).
        /// </summary>
        public required int SnapshotToleranceSeconds { get; set; }

        /// <summary>
        /// Directory path for storing snapshot files. If not specified, defaults to a hardcoded path.
        /// Should be a valid absolute or relative path accessible for file operations.
        /// </summary>
        public required string StorageDirectory { get; set; }

        /// <summary>
        /// Maximum degree of parallelism for snapshot loading operations.
        /// Limits the number of concurrent threads to prevent resource exhaustion during high-volume operations.
        /// Set to -1 for unlimited parallelism, or a positive integer to limit concurrent processing.
        /// </summary>
        public required int MaxParallelism { get; set; }

        /// <summary>
        /// Flag to enable or disable performance metrics collection for snapshot operations.
        /// When enabled, tracks timing and resource usage for snapshot loading and processing operations.
        /// Useful for monitoring system performance but may add slight overhead.
        /// </summary>
        public required bool EnablePerformanceMetrics { get; set; }
    }
}
