namespace BacklashDTOs.Configuration
{
    public class ExecutionConfig
    {
        public int MarketUpdateTimeout { get; set; }
        public bool LaunchDataDashboard { get; set; }
        public string? BrainInstance { get; set; }
        public bool RunOvernightActivities { get; set; }
        public int MaxMarketsPerSubscriptionAction { get; set; }
        public required string HardDataStorageLocation { get; set; }
        public int QueuesTargetCount { get; set; }
        public double QueuesTargetPercentage { get; set; }
        public int NotificationQueueLimit { get; set; } = 50;
        public int OrderbookQueueLimit { get; set; } = 50;
        public int EventQueueLimit { get; set; } = 50;
        public int TickerQueueLimit { get; set; } = 50;

        /// <summary>
        /// Gets or sets the percentage threshold for queue high count alerts.
        /// When the EventQueue utilization exceeds this percentage, a performance alert is logged.
        /// </summary>
        /// <value>Default is 80.0%.</value>
        public double QueueHighCountAlertThreshold { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the percentage threshold for refresh usage alerts.
        /// When the market refresh cycle usage exceeds this percentage, a performance alert is logged.
        /// </summary>
        /// <value>Default is 90.0%.</value>
        public double RefreshUsageAlertThreshold { get; set; } = 90.0;

        /// <summary>
        /// Gets or sets the absolute count threshold for queue alerts.
        /// When any queue's average count exceeds this value, a performance alert is logged.
        /// </summary>
        /// <value>Default is 100 items.</value>
        public int QueueCountAlertThreshold { get; set; } = 100;
    }
}