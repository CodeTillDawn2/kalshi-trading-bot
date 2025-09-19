using System.Text.Json.Serialization;

namespace BacklashOverseer.Config
{
    /// <summary>
    /// Configuration options for the Overseer system.
    /// </summary>
    public class OverseerConfig
    {
        /// <summary>
        /// Gets or sets the interval in minutes for periodic API data fetching.
        /// Default is 10 minutes.
        /// </summary>
        [JsonRequired]
        public int ApiFetchIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the interval in minutes for periodic system info logging.
        /// Default is 1 minute.
        /// </summary>
        [JsonRequired]
        public int SystemInfoLogIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the batch size for SignalR broadcast operations.
        /// Default is 10 to prevent payload size issues.
        /// </summary>
        [JsonRequired]
        public int SignalRBatchSize { get; set; }

        /// <summary>
        /// Gets or sets the batch size for brain persistence logging operations.
        /// Default is 50 for better performance with large brain sets.
        /// </summary>
        [JsonRequired]
        public int BrainBatchSize { get; set; }

        /// <summary>
        /// Gets or sets whether to enable Overseer performance metrics collection.
        /// When enabled, WebSocket events and API fetch operations are recorded.
        /// Default is true.
        /// </summary>
        [JsonRequired]
        public bool EnableOverseerPerformanceMetrics { get; set; }
    }
}