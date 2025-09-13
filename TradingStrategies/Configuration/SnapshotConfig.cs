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
        public int SnapshotToleranceSeconds { get; set; }

        /// <summary>
        /// Version number of the snapshot JSON schema used for data serialization and deserialization.
        /// Ensures compatibility between snapshot data structures and processing logic across different versions.
        /// Incremented when schema changes require migration logic or backward compatibility handling.
        /// Used by TradingSnapshotService for schema validation and snapshot upgrading during loading.
        /// </summary>
        public int SnapshotSchemaVersion { get; set; }

        /// <summary>
        /// The maximum time gap in minutes between snapshots that allows them to be considered
        /// part of the same continuous period without requiring price stability checks.
        /// </summary>
        public double SmallGapMinutes { get; set; } = 10.0;

        /// <summary>
        /// The maximum time gap in hours for active market periods. Gaps exceeding this threshold
        /// will cause a period break regardless of price stability.
        /// </summary>
        public double MaxActiveGapHours { get; set; } = 1.0;

        /// <summary>
        /// The price change threshold in points for determining significant price movements.
        /// Used to break snapshot groups when price changes exceed this threshold.
        /// </summary>
        public int PriceChangeThreshold { get; set; } = 3;
    }
}
