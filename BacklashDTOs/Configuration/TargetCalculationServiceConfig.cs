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
        [JsonRequired]
        public int NotificationQueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the limit for the orderbook queue.
        /// </summary>
        [JsonRequired]
        public int OrderbookQueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the limit for the event queue.
        /// </summary>
        [JsonRequired]
        public int EventQueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the limit for the ticker queue.
        /// </summary>
        [JsonRequired]
        public int TickerQueueLimit { get; set; }
    }
}