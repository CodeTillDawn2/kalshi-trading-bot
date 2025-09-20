using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashBot.Services
{
    public class OrderBookServiceConfig
    {
        public const string SectionName = "WatchedMarkets:OrderBookService";

        [Required(ErrorMessage = "The 'SemaphoreTimeoutMs' is missing in the configuration.")]
        public int SemaphoreTimeoutMs { get; set; }

        [Required(ErrorMessage = "The 'QueueLimit' is missing in the configuration.")]
        public int QueueLimit { get; set; }

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
