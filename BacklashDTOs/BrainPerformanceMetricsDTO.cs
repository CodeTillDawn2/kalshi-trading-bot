namespace BacklashDTOs
{
    /// <summary>
    /// Represents performance metrics.
    /// </summary>
    public class BrainPerformanceMetricsDTO
    {

        /// <summary>
        /// Initializes a new instance of the PerformanceMetrics class.
        /// </summary>
        public BrainPerformanceMetricsDTO()
        {
            EventQueueAvg = 0;
            TickerQueueAvg = 0;
            NotificationQueueAvg = 0;
            OrderbookQueueAvg = 0;
            CurrentUsage = 0;
            CurrentCount = 0;
        }

        /// <summary>
        /// Gets or sets the event queue average.
        /// </summary>
        public double EventQueueAvg { get; set; }
        /// <summary>
        /// Gets or sets the ticker queue average.
        /// </summary>
        public double TickerQueueAvg { get; set; }
        /// <summary>
        /// Gets or sets the notification queue average.
        /// </summary>
        public double NotificationQueueAvg { get; set; }
        /// <summary>
        /// Gets or sets the orderbook queue average.
        /// </summary>
        public double OrderbookQueueAvg { get; set; }
        /// <summary>
        /// Gets or sets the current usage.
        /// </summary>
        public double CurrentUsage { get; set; }
        /// <summary>
        /// Gets or sets the current count.
        /// </summary>
        public int CurrentCount { get; set; }

    }
}
