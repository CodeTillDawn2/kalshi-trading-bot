using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for TargetCalculationService-specific settings.
    /// </summary>
    public class TargetCalculationServiceConfig
    {
        /// <summary>
        /// The configuration section name for TargetCalculationServiceConfig.
        /// </summary>
        public const string SectionName = "WatchedMarkets:TargetCalculationService";

        /// <summary>
        /// Gets or sets the limit for the notification queue.
        /// </summary>
        [Required(ErrorMessage = "The 'NotificationQueueLimit' is missing in the configuration.")]
        public int NotificationQueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the limit for the orderbook queue.
        /// </summary>
        [Required(ErrorMessage = "The 'OrderbookQueueLimit' is missing in the configuration.")]
        public int OrderbookQueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the limit for the event queue.
        /// </summary>
        [Required(ErrorMessage = "The 'EventQueueLimit' is missing in the configuration.")]
        public int EventQueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the limit for the ticker queue.
        /// </summary>
        [Required(ErrorMessage = "The 'TickerQueueLimit' is missing in the configuration.")]
        public int TickerQueueLimit { get; set; }
    }
}
