using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for general execution settings.
    /// </summary>
    public class GeneralExecutionConfig
    {
        /// <summary>
        /// The configuration section name for GeneralExecutionConfig.
        /// </summary>
        public const string SectionName = "Central:GeneralExecution";

        /// <summary>
        /// Gets or sets the brain instance identifier.
        /// </summary>
        [Required(ErrorMessage = "The 'BrainInstance' is missing in the configuration.")]
        public string BrainInstance { get; set; } = null!;

        /// <summary>
        /// Gets or sets the target count for queues.
        /// </summary>
        [Required(ErrorMessage = "The 'QueuesTargetCount' is missing in the configuration.")]
        public int QueuesTargetCount { get; set; }


        /// <summary>
        /// Interval in minutes for refreshing market data and recalculating trading metrics.
        /// This controls how often the system updates cached market information and technical indicators.
        /// Longer intervals reduce API load but may delay response to market changes.
        /// Typical values: 1-15 minutes depending on data freshness requirements and API rate limits.
        /// Used by MarketRefreshService and MarketData for periodic data synchronization.
        /// </summary>
        [Required(ErrorMessage = "The 'RefreshIntervalMinutes' is missing in the configuration.")]
        public int RefreshIntervalMinutes { get; set; }

    }
}
