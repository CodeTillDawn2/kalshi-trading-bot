using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for CandlestickService settings.
    /// </summary>
    public class CandlestickServiceConfig
    {
        /// <summary>
        /// The configuration section name for CandlestickServiceConfig.
        /// </summary>
        public const string SectionName = "WatchedMarkets:CandlestickService";

        /// <summary>
        /// Gets or sets the maximum number of parallel tasks for candlestick processing.
        /// </summary>
        /// <value>Default is 4 parallel tasks.</value>
        [Required(ErrorMessage = "The 'MaxParallelCandlestickTasks' is missing in the configuration.")]
        public int MaxParallelCandlestickTasks { get; set; }

        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for CandlestickService operations.
        /// </summary>
        /// <value>Default is true.</value>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
        /// <summary>
        /// Gets or sets the mandatory overlap period in days for minute-interval candlesticks.
        /// This ensures sufficient historical data is available for reliable minute-level analysis.
        /// </summary>
        [Required(ErrorMessage = "The 'CandlestickMandatoryOverlapDaysMinute' is missing in the configuration.")]
        public int CandlestickMandatoryOverlapDaysMinute { get; set; }

        /// <summary>
        /// Gets or sets the mandatory overlap period in days for hour-interval candlesticks.
        /// This ensures sufficient historical data is available for reliable hour-level analysis.
        /// </summary>
        [Required(ErrorMessage = "The 'CandlestickMandatoryOverlapDaysHour' is missing in the configuration.")]
        public int CandlestickMandatoryOverlapDaysHour { get; set; }

        /// <summary>
        /// Gets or sets the mandatory overlap period in days for day-interval candlesticks.
        /// This ensures sufficient historical data is available for reliable day-level analysis.
        /// </summary>
        [Required(ErrorMessage = "The 'CandlestickMandatoryOverlapDaysDay' is missing in the configuration.")]
        public int CandlestickMandatoryOverlapDaysDay { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of parallel tasks for loading Parquet files.
        /// This controls concurrency when batch loading historical candlestick data to prevent I/O overload.
        /// </summary>
        /// <value>Default is 4 parallel tasks.</value>
        [Required(ErrorMessage = "The 'MaxParallelParquetTasks' is missing in the configuration.")]
        public int MaxParallelParquetTasks { get; set; }
    }
}
