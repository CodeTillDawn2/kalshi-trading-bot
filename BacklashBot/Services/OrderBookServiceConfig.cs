using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Services
{
    /// <summary>
    /// Configuration class for the OrderBookService, containing settings for semaphore timeouts, queue limits, and performance metrics.
    /// </summary>
    public class OrderBookServiceConfig
    {
        /// <summary>
        /// The configuration section name for OrderBookService settings.
        /// </summary>
        public const string SectionName = "WatchedMarkets:OrderBookService";

        /// <summary>
        /// The timeout in milliseconds for semaphore operations in the OrderBookService.
        /// </summary>
        [Required(ErrorMessage = "The 'SemaphoreTimeoutMs' is missing in the configuration.")]
        public int SemaphoreTimeoutMs { get; set; }

        /// <summary>
        /// The maximum number of items allowed in the order book queue.
        /// </summary>
        [Required(ErrorMessage = "The 'QueueLimit' is missing in the configuration.")]
        public int QueueLimit { get; set; }

        /// <summary>
        /// The timeout in milliseconds for semaphore operations on the event queue.
        /// </summary>
        [Required(ErrorMessage = "The 'EventQueueSemaphoreTimeoutMs' is missing in the configuration.")]
        public int EventQueueSemaphoreTimeoutMs { get; set; }

        /// <summary>
        /// Enables or disables performance metrics collection for the OrderBookService.
        /// When disabled, metric collection is skipped to improve performance.
        /// Default value is true to maintain existing behavior.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
