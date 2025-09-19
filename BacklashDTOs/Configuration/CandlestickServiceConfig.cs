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
    }
}