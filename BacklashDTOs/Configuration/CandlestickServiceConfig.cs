using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for CandlestickService settings.
    /// </summary>
    public class CandlestickServiceConfig
    {
        /// <summary>
        /// Gets or sets the maximum number of parallel tasks for candlestick processing.
        /// </summary>
        /// <value>Default is 4 parallel tasks.</value>
        [JsonRequired]
        public int MaxParallelCandlestickTasks { get; set; }

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for CandlestickService operations.
        /// </summary>
        /// <value>Default is true.</value>
        [JsonRequired]
        public bool EnablePerformanceMetrics { get; set; }

        /// <summary>
        /// Gets or sets the mandatory overlap period in days for minute-interval candlesticks.
        /// This ensures sufficient historical data is available for reliable minute-level analysis.
        /// </summary>
        [JsonRequired]
        public int CandlestickMandatoryOverlapDaysMinute { get; set; }

        /// <summary>
        /// Gets or sets the mandatory overlap period in days for hour-interval candlesticks.
        /// This ensures sufficient historical data is available for reliable hour-level analysis.
        /// </summary>
        [JsonRequired]
        public int CandlestickMandatoryOverlapDaysHour { get; set; }

        /// <summary>
        /// Gets or sets the mandatory overlap period in days for day-interval candlesticks.
        /// This ensures sufficient historical data is available for reliable day-level analysis.
        /// </summary>
        [JsonRequired]
        public int CandlestickMandatoryOverlapDaysDay { get; set; }

        /// <summary>
        /// Gets or sets the hard data storage location path for storing candlestick data.
        /// </summary>
        [JsonRequired]
        public string HardDataStorageLocation { get; set; } = string.Empty;
    }
}