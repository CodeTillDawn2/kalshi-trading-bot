using System;

namespace BacklashBot.Services
{
    public class OrderBookServiceConfig
    {
        public int SemaphoreTimeoutMs { get; set; } = 30000;
        public int QueueLimit { get; set; } = 1000;
        public int EventQueueSemaphoreTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// Enables or disables performance metrics collection for the OrderBookService.
        /// When disabled, metric collection is skipped to improve performance.
        /// Default value is true to maintain existing behavior.
        /// </summary>
        public bool OrderBookService_EnableMetrics { get; set; } = true;
    }
}