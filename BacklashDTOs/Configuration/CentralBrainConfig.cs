namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration settings for the CentralBrain component.
    /// </summary>
    public class CentralBrainConfig
    {
        /// <summary>
        /// Gets or sets the interval for error checking timer.
        /// </summary>
        public TimeSpan ErrorCheckInterval { get; set; } = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// Gets or sets the interval for startup retry timer when internet is down.
        /// </summary>
        public TimeSpan StartupRetryInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the initial delay before starting snapshot timer.
        /// </summary>
        public TimeSpan SnapshotInitialDelay { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the time of day to start overnight tasks (e.g., 3:00 AM).
        /// </summary>
        public TimeSpan OvernightStart { get; set; } = new TimeSpan(3, 0, 0);

        /// <summary>
        /// Gets or sets the delay after overnight start time to execute tasks.
        /// </summary>
        public TimeSpan OvernightTaskDelay { get; set; } = TimeSpan.FromMinutes(15);
    }
}