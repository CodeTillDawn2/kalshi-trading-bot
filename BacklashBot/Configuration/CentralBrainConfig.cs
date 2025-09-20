using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for CentralBrain-specific settings.
    /// </summary>
    public class CentralBrainConfig
    {
        /// <summary>
        /// The configuration section name for CentralBrainConfig.
        /// </summary>
        public const string SectionName = "Central:CentralBrain";

        /// <summary>
        /// Gets or sets the error check interval as a TimeSpan.
        /// </summary>
        [Required(ErrorMessage = "The 'ErrorCheckInterval' is missing in the configuration.")]
        public TimeSpan ErrorCheckInterval { get; set; }

        /// <summary>
        /// Gets or sets the startup retry interval as a TimeSpan.
        /// </summary>
        [Required(ErrorMessage = "The 'StartupRetryInterval' is missing in the configuration.")]
        public TimeSpan StartupRetryInterval { get; set; }

        /// <summary>
        /// Gets or sets the snapshot initial delay as a TimeSpan.
        /// </summary>
        [Required(ErrorMessage = "The 'SnapshotInitialDelay' is missing in the configuration.")]
        public TimeSpan SnapshotInitialDelay { get; set; }

        /// <summary>
        /// Gets or sets the overnight start time as a TimeSpan.
        /// </summary>
        [Required(ErrorMessage = "The 'OvernightStart' is missing in the configuration.")]
        public TimeSpan OvernightStart { get; set; }

        /// <summary>
        /// Gets or sets the overnight task delay as a TimeSpan.
        /// </summary>
        [Required(ErrorMessage = "The 'OvernightTaskDelay' is missing in the configuration.")]
        public TimeSpan OvernightTaskDelay { get; set; }

        /// <summary>
        /// Gets or sets whether to launch the data dashboard.
        /// </summary>
        [Required(ErrorMessage = "The 'LaunchDataDashboard' is missing in the configuration.")]
        public bool LaunchDataDashboard { get; set; }

        /// <summary>
        /// Gets or sets whether to run overnight activities.
        /// </summary>
        [Required(ErrorMessage = "The 'RunOvernightActivities' is missing in the configuration.")]
        public bool RunOvernightActivities { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of markets per subscription action.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxMarketsPerSubscriptionAction' is missing in the configuration.")]
        public int MaxMarketsPerSubscriptionAction { get; set; }

        /// <summary>
        /// Gets or sets the hard data storage location.
        /// </summary>
        [Required(ErrorMessage = "The 'HardDataStorageLocation' is missing in the configuration.")]
        public string HardDataStorageLocation { get; set; } = null!;
    }
}
