using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration class containing snapshot-related parameters used throughout the trading bot system.
    /// These settings control snapshot timing tolerances, schema versioning, and data persistence behavior
    /// for market data snapshots used in trading strategy evaluation and backtesting.
    /// </summary>
    public class SnapshotConfig
    {
        /// <summary>
        /// Number of seconds allowed as tolerance for snapshot timing irregularities.
        /// Used to determine acceptable deviations from expected snapshot intervals before triggering warnings or discarding snapshots.
        /// Higher values provide more flexibility for irregular market data arrival but may mask timing issues.
        /// Typically set to match expected decision frequency intervals (e.g., 30-60 seconds).
        /// </summary>
        [Range(1, 3600, ErrorMessage = "SnapshotToleranceSeconds must be between 1 and 3600 seconds.")]
        public int SnapshotToleranceSeconds { get; set; }

        /// <summary>
        /// Version number of the snapshot JSON schema used for data serialization and deserialization.
        /// Ensures compatibility between snapshot data structures and processing logic across different versions.
        /// Incremented when schema changes require migration logic or backward compatibility handling.
        /// Used by TradingSnapshotService for schema validation and snapshot upgrading during loading.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "SnapshotSchemaVersion must be at least 1.")]
        public int SnapshotSchemaVersion { get; set; }

        /// <summary>
        /// The maximum time gap in minutes between snapshots that allows them to be considered
        /// part of the same continuous period without requiring price stability checks.
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "SmallGapMinutes must be greater than 0.")]
        public double SmallGapMinutes { get; set; } = 10.0;

        /// <summary>
        /// The maximum time gap in hours for active market periods. Gaps exceeding this threshold
        /// will cause a period break regardless of price stability.
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "MaxActiveGapHours must be greater than 0.")]
        public double MaxActiveGapHours { get; set; } = 1.0;

        /// <summary>
        /// The price change threshold in points for determining significant price movements.
        /// Used to break snapshot groups when price changes exceed this threshold.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "PriceChangeThreshold must be at least 1.")]
        public int PriceChangeThreshold { get; set; } = 3;

        /// <summary>
        /// Directory path for storing snapshot files. If not specified, defaults to a hardcoded path.
        /// Should be a valid absolute or relative path accessible for file operations.
        /// </summary>
        public string? StorageDirectory { get; set; }

        /// <summary>
        /// Maximum degree of parallelism for snapshot loading operations.
        /// Limits the number of concurrent threads to prevent resource exhaustion during high-volume operations.
        /// Set to -1 for unlimited parallelism, or a positive integer to limit concurrent processing.
        /// </summary>
        [Range(-1, int.MaxValue, ErrorMessage = "MaxParallelism must be -1 (unlimited) or a positive integer.")]
        public int MaxParallelism { get; set; } = Environment.ProcessorCount;
    }
}
