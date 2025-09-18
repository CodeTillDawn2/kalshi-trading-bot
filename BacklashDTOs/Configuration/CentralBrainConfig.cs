namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for CentralBrain-specific settings.
    /// </summary>
    public class CentralBrainConfig
    {
        /// <summary>
        /// Gets or sets the error check interval as a TimeSpan.
        /// </summary>
        public TimeSpan ErrorCheckInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the startup retry interval as a TimeSpan.
        /// </summary>
        public TimeSpan StartupRetryInterval { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets or sets the snapshot initial delay as a TimeSpan.
        /// </summary>
        public TimeSpan SnapshotInitialDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets the overnight start time as a TimeSpan.
        /// </summary>
        public TimeSpan OvernightStart { get; set; } = TimeSpan.FromHours(3);

        /// <summary>
        /// Gets or sets the overnight task delay as a TimeSpan.
        /// </summary>
        public TimeSpan OvernightTaskDelay { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets whether to launch the data dashboard.
        /// </summary>
        public bool LaunchDataDashboard { get; set; }

        /// <summary>
        /// Gets or sets whether to run overnight activities.
        /// </summary>
        public bool RunOvernightActivities { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of markets per subscription action.
        /// </summary>
        public int MaxMarketsPerSubscriptionAction { get; set; }

        /// <summary>
        /// Gets or sets the hard data storage location.
        /// </summary>
        public required string HardDataStorageLocation { get; set; }
    }
}