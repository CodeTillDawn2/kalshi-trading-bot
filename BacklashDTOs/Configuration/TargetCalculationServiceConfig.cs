using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for TargetCalculationService-specific settings.
    /// </summary>
    public class TargetCalculationServiceConfig
    {
        /// <summary>
        /// Gets or sets the limit for the notification queue.
        /// </summary>
        public required int NotificationQueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the limit for the orderbook queue.
        /// </summary>
        public required int OrderbookQueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the limit for the event queue.
        /// </summary>
        public required int EventQueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the limit for the ticker queue.
        /// </summary>
        public required int TickerQueueLimit { get; set; }
    }
}
