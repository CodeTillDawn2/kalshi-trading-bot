using System;
using System.Text.Json.Serialization;

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
        public required TimeSpan ErrorCheckInterval { get; set; }

        /// <summary>
        /// Gets or sets the startup retry interval as a TimeSpan.
        /// </summary>
        public required TimeSpan StartupRetryInterval { get; set; }

        /// <summary>
        /// Gets or sets the snapshot initial delay as a TimeSpan.
        /// </summary>
        public required TimeSpan SnapshotInitialDelay { get; set; }

        /// <summary>
        /// Gets or sets the overnight start time as a TimeSpan.
        /// </summary>
        public required TimeSpan OvernightStart { get; set; }

        /// <summary>
        /// Gets or sets the overnight task delay as a TimeSpan.
        /// </summary>
        public required TimeSpan OvernightTaskDelay { get; set; }

        /// <summary>
        /// Gets or sets whether to launch the data dashboard.
        /// </summary>
        public required bool LaunchDataDashboard { get; set; }

        /// <summary>
        /// Gets or sets whether to run overnight activities.
        /// </summary>
        public required bool RunOvernightActivities { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of markets per subscription action.
        /// </summary>
        public required int MaxMarketsPerSubscriptionAction { get; set; }

        /// <summary>
        /// Gets or sets the hard data storage location.
        /// </summary>
        public required string HardDataStorageLocation { get; set; }
    }
}
