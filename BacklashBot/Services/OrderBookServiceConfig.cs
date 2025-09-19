using System;
using System.Text.Json.Serialization;

namespace BacklashBot.Services
{
    public class OrderBookServiceConfig
    {
        [JsonRequired]
        public int SemaphoreTimeoutMs { get; set; }
        [JsonRequired]
        public int QueueLimit { get; set; }
        [JsonRequired]
        public int EventQueueSemaphoreTimeoutMs { get; set; }

        /// <summary>
        /// Enables or disables performance metrics collection for the OrderBookService.
        /// When disabled, metric collection is skipped to improve performance.
        /// Default value is true to maintain existing behavior.
        /// </summary>
        [JsonRequired]
        public bool OrderBookService_EnableMetrics { get; set; }
    }
}