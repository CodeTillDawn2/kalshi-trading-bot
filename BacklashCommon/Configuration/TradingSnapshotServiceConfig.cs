using System.ComponentModel.DataAnnotations;

namespace BacklashCommon.Configuration
{
    /// <summary>
    /// Configuration class for TradingSnapshotService settings.
    /// Contains parameters specific to snapshot storage, timing tolerances, and parallel processing.
    /// </summary>
    public class TradingSnapshotServiceConfig
    {
        public const string SectionName = "WatchedMarkets:TradingSnapshotService";

        /// <summary>
        /// Number of seconds allowed as tolerance for snapshot timing irregularities.
        /// Used to determine acceptable deviations from expected snapshot intervals before triggering warnings or discarding snapshots.
        /// Higher values provide more flexibility for irregular market data arrival but may mask timing issues.
        /// Typically set to match expected decision frequency intervals (e.g., 30-60 seconds).
        /// </summary>
        [Required(ErrorMessage = "The 'SnapshotToleranceSeconds' is missing in the configuration.")]
        public int SnapshotToleranceSeconds { get; set; }

        /// <summary>
        /// Directory path for storing snapshot files. If not specified, defaults to a hardcoded path.
        /// Should be a valid absolute or relative path accessible for file operations.
        /// </summary>
        [Required(ErrorMessage = "The 'StorageDirectory' is missing in the configuration.")]
        public string StorageDirectory { get; set; } = null!;

        /// <summary>
        /// Maximum degree of parallelism for snapshot loading operations.
        /// Limits the number of concurrent threads to prevent resource exhaustion during high-volume operations.
        /// Set to -1 for unlimited parallelism, or a positive integer to limit concurrent processing.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxParallelism' is missing in the configuration.")]
        public int MaxParallelism { get; set; }

        /// <summary>
        /// Version number of the snapshot JSON schema used for data serialization and deserialization.
        /// Ensures compatibility between snapshot data structures and processing logic across different versions.
        /// Incremented when schema changes require migration logic or backward compatibility handling.
        /// Used by TradingSnapshotService for schema validation and snapshot upgrading during loading.
        /// </summary>
        [Required(ErrorMessage = "The 'SnapshotSchemaVersion' is missing in the configuration.")]
        public int SnapshotSchemaVersion { get; set; }

        /// <summary>
        /// Frequency in seconds at which trading decisions are evaluated and executed.
        /// This controls how often the trading strategy analyzes market conditions and makes buy/sell/hold decisions.
        /// Lower values provide more responsive trading but increase computational load and potential for overtrading.
        /// Typical values: 30-300 seconds depending on strategy requirements and market volatility.
        /// Used by TradingStrategy to determine snapshot intervals and decision timing.
        /// </summary>
        [Required(ErrorMessage = "The 'DecisionFrequencySeconds' is missing in the configuration.")]
        public int DecisionFrequencySeconds { get; set; }

        /// <summary>
        /// Flag to enable or disable performance metrics collection for snapshot operations.
        /// When enabled, tracks timing and resource usage for snapshot loading and processing operations.
        /// Useful for monitoring system performance but may add slight overhead.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
