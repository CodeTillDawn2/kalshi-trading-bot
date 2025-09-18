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
        public int ApiFetchIntervalMinutes { get; set; } = 10;

        /// <summary>
        /// Gets or sets the interval in minutes for periodic system info logging.
        /// Default is 1 minute.
        /// </summary>
        public int SystemInfoLogIntervalMinutes { get; set; } = 1;

        /// <summary>
        /// Gets or sets the batch size for SignalR broadcast operations.
        /// Default is 10 to prevent payload size issues.
        /// </summary>
        public int SignalRBatchSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the batch size for brain persistence logging operations.
        /// Default is 50 for better performance with large brain sets.
        /// </summary>
        public int BrainBatchSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets whether to enable Overseer performance metrics collection.
        /// When enabled, WebSocket events and API fetch operations are recorded.
        /// Default is true.
        /// </summary>
        public bool EnableOverseerPerformanceMetrics { get; set; } = true;
    }
}