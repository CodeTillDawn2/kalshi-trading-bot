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
        public int NotificationQueueLimit { get; set; } = 50;

        /// <summary>
        /// Gets or sets the limit for the orderbook queue.
        /// </summary>
        public int OrderbookQueueLimit { get; set; } = 50;

        /// <summary>
        /// Gets or sets the limit for the event queue.
        /// </summary>
        public int EventQueueLimit { get; set; } = 50;

        /// <summary>
        /// Gets or sets the limit for the ticker queue.
        /// </summary>
        public int TickerQueueLimit { get; set; } = 50;
    }
}