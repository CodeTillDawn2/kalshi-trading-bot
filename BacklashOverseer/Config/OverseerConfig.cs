using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashOverseer.Config
{
    /// <summary>
    /// Configuration options for the Overseer system.
    /// </summary>
    public class OverseerConfig
    {
        /// <summary>
        /// The configuration section name for OverseerConfig.
        /// </summary>
        public const string SectionName = "Endpoints:Overseer";

        /// <summary>
        /// Gets or sets the interval in minutes for periodic API data fetching.
        /// Default is 10 minutes.
        /// </summary>
        [Required(ErrorMessage = "The 'ApiFetchIntervalMinutes' is missing in the configuration.")]
        public int ApiFetchIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the interval in minutes for periodic system info logging.
        /// Default is 1 minute.
        /// </summary>
        [Required(ErrorMessage = "The 'SystemInfoLogIntervalMinutes' is missing in the configuration.")]
        public int SystemInfoLogIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the batch size for SignalR broadcast operations.
        /// Default is 10 to prevent payload size issues.
        /// </summary>
        [Required(ErrorMessage = "The 'SignalRBatchSize' is missing in the configuration.")]
        public int SignalRBatchSize { get; set; }

        /// <summary>
        /// Gets or sets the batch size for brain persistence logging operations.
        /// Default is 50 for better performance with large brain sets.
        /// </summary>
        [Required(ErrorMessage = "The 'BrainBatchSize' is missing in the configuration.")]
        public int BrainBatchSize { get; set; }

        /// <summary>
        /// Gets or sets whether to enable Overseer performance metrics collection.
        /// When enabled, WebSocket events and API fetch operations are recorded.
        /// Default is true.
        /// </summary>
        [Required(ErrorMessage = "The 'EnableOverseerPerformanceMetrics' is missing in the configuration.")]
        public bool EnableOverseerPerformanceMetrics { get; set; }
    }
}
