using System.ComponentModel.DataAnnotations;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for general execution settings.
    /// </summary>
    public class GeneralExecutionConfig
    {
        /// <summary>
        /// Gets or sets the brain instance identifier.
        /// </summary>
        public string? BrainInstance { get; set; }

        /// <summary>
        /// Gets or sets the target count for queues.
        /// </summary>
        public int QueuesTargetCount { get; set; }

        /// <summary>
        /// Gets or sets the retry delay in milliseconds for operations that require retries.
        /// </summary>
        public int RetryDelayMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the authentication token validity duration in hours.
        /// </summary>
        public int AuthTokenValidityHours { get; set; } = 24;

        /// <summary>
        /// Gets or sets the hard data storage location.
        /// </summary>
        public required string HardDataStorageLocation { get; set; }

        [Range(1, 3600)]
        /// <summary>
        /// Frequency in seconds at which trading decisions are evaluated and executed.
        /// This controls how often the trading strategy analyzes market conditions and makes buy/sell/hold decisions.
        /// Lower values provide more responsive trading but increase computational load and potential for overtrading.
        /// Typical values: 30-300 seconds depending on strategy requirements and market volatility.
        /// Used by TradingStrategy to determine snapshot intervals and decision timing.
        /// </summary>
        public int DecisionFrequencySeconds { get; set; }

        [Range(1, 1440)]
        /// <summary>
        /// Interval in minutes for refreshing market data and recalculating trading metrics.
        /// This controls how often the system updates cached market information and technical indicators.
        /// Longer intervals reduce API load but may delay response to market changes.
        /// Typical values: 1-15 minutes depending on data freshness requirements and API rate limits.
        /// Used by MarketRefreshService and MarketData for periodic data synchronization.
        /// </summary>
        public int RefreshIntervalMinutes { get; set; }

        [Range(1, int.MaxValue)]
        /// <summary>
        /// Version number of the snapshot JSON schema used for data serialization and deserialization.
        /// Ensures compatibility between snapshot data structures and processing logic across different versions.
        /// Incremented when schema changes require migration logic or backward compatibility handling.
        /// Used by TradingSnapshotService for schema validation and snapshot upgrading during loading.
        /// </summary>
        public int SnapshotSchemaVersion { get; set; }
    }
}