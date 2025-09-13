using System;

namespace BacklashBot.Services
{
    public class OrderBookServiceConfig
    {
        public int SemaphoreTimeoutMs { get; set; } = 30000;
        public int QueueLimit { get; set; } = 1000;
        public int EventQueueSemaphoreTimeoutMs { get; set; } = 1000;
    }
}