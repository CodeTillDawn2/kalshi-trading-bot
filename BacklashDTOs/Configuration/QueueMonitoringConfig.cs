namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for queue monitoring and alert settings.
    /// </summary>
    public class QueueMonitoringConfig
    {
        /// <summary>
        /// Gets or sets the percentage threshold for queue high count alerts.
        /// When the EventQueue utilization exceeds this percentage, a performance alert is logged.
        /// </summary>
        public double QueueHighCountAlertThreshold { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the percentage threshold for refresh usage alerts.
        /// When the market refresh cycle usage exceeds this percentage, a performance alert is logged.
        /// </summary>
        public double RefreshUsageAlertThreshold { get; set; } = 90.0;

        /// <summary>
        /// Gets or sets the absolute count threshold for queue alerts.
        /// When any queue's average count exceeds this value, a performance alert is logged.
        /// </summary>
        public int QueueCountAlertThreshold { get; set; } = 100;
    }
}