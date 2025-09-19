using System.Text.Json.Serialization;

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
        [JsonRequired]
        public string BrainInstance { get; set; }

        /// <summary>
        /// Gets or sets the target count for queues.
        /// </summary>
        [JsonRequired]
        public int QueuesTargetCount { get; set; }

        /// <summary>
        /// Gets or sets the retry delay in milliseconds for operations that require retries.
        /// </summary>
        [JsonRequired]
        public int RetryDelayMs { get; set; }

        /// <summary>
        /// Gets or sets the authentication token validity duration in hours.
        /// </summary>
        [JsonRequired]
        public int AuthTokenValidityHours { get; set; }

        /// <summary>
        /// Gets or sets the hard data storage location.
        /// </summary>
        [JsonRequired]
        public string HardDataStorageLocation { get; set; }

        /// <summary>
        /// Frequency in seconds at which trading decisions are evaluated and executed.
        /// This controls how often the trading strategy analyzes market conditions and makes buy/sell/hold decisions.
        /// Lower values provide more responsive trading but increase computational load and potential for overtrading.
        /// Typical values: 30-300 seconds depending on strategy requirements and market volatility.
        /// Used by TradingStrategy to determine snapshot intervals and decision timing.
        /// </summary>
        [JsonRequired]
        public int DecisionFrequencySeconds { get; set; }

        /// <summary>
        /// Interval in minutes for refreshing market data and recalculating trading metrics.
        /// This controls how often the system updates cached market information and technical indicators.
        /// Longer intervals reduce API load but may delay response to market changes.
        /// Typical values: 1-15 minutes depending on data freshness requirements and API rate limits.
        /// Used by MarketRefreshService and MarketData for periodic data synchronization.
        /// </summary>
        [JsonRequired]
        public int RefreshIntervalMinutes { get; set; }

        /// <summary>
        /// Version number of the snapshot JSON schema used for data serialization and deserialization.
        /// Ensures compatibility between snapshot data structures and processing logic across different versions.
        /// Incremented when schema changes require migration logic or backward compatibility handling.
        /// Used by TradingSnapshotService for schema validation and snapshot upgrading during loading.
        /// </summary>
        [JsonRequired]
        public int SnapshotSchemaVersion { get; set; }
    }
}