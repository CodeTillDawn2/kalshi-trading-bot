namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for general execution settings.
    /// </summary>
    public class GeneralExecutionConfig
    {
        /// <summary>
        /// Gets or sets the brain instance identifier.
        /// </summary>
        public string? BrainInstance { get; set; }

        /// <summary>
        /// Gets or sets the target count for queues.
        /// </summary>
        public int QueuesTargetCount { get; set; }

        /// <summary>
        /// Gets or sets the retry delay in milliseconds for operations that require retries.
        /// </summary>
        public int RetryDelayMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the authentication token validity duration in hours.
        /// </summary>
        public int AuthTokenValidityHours { get; set; } = 24;

        /// <summary>
        /// Gets or sets the hard data storage location.
        /// </summary>
        public required string HardDataStorageLocation { get; set; }
    }
}