using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration settings for the CentralErrorHandler component that manages error processing,
    /// catastrophic failure detection, and internet connectivity monitoring.
    /// </summary>
    /// <remarks>
    /// This configuration allows customization of error handling behavior including:
    /// - Time window for error frequency monitoring
    /// - Threshold for triggering catastrophic failure detection
    /// - Parameters for internet connectivity check retry logic
    /// </remarks>
    public class CentralErrorHandlerConfig
    {
        /// <summary>
        /// The configuration section name for ErrorHandlerConfig.
        /// </summary>
        public const string SectionName = "Central:ErrorHandler";

        /// <summary>
        /// Gets or sets the time window in minutes for monitoring error frequency.
        /// Errors occurring within this window are counted toward the catastrophic threshold.
        /// </summary>
        /// <value>Default is 5 minutes.</value>
        [Required(ErrorMessage = "The 'ErrorWindowMinutes' is missing in the configuration.")]
        public int ErrorWindowMinutes { get; set; }

        /// <summary>
        /// Gets or sets the threshold number of non-catastrophic errors within the monitoring window
        /// that triggers catastrophic failure detection and system restart.
        /// </summary>
        /// <value>Default is 10 errors.</value>
        [Required(ErrorMessage = "The 'ErrorThreshold' is missing in the configuration.")]
        public int ErrorThreshold { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of attempts for internet connectivity checks
        /// before declaring the connection as failed.
        /// </summary>
        /// <value>Default is 100 attempts.</value>
        [Required(ErrorMessage = "The 'InternetCheckMaxAttempts' is missing in the configuration.")]
        public int InternetCheckMaxAttempts { get; set; }

        /// <summary>
        /// Gets or sets the initial delay in milliseconds between internet connectivity check attempts.
        /// This delay doubles with each retry attempt until reaching the maximum delay.
        /// </summary>
        /// <value>Default is 1000 milliseconds (1 second).</value>
        [Required(ErrorMessage = "The 'InternetCheckInitialDelayMs' is missing in the configuration.")]
        public int InternetCheckInitialDelayMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum delay in milliseconds between internet connectivity check attempts.
        /// The delay will not exceed this value even after exponential backoff.
        /// </summary>
        /// <value>Default is 60000 milliseconds (60 seconds).</value>
        [Required(ErrorMessage = "The 'InternetCheckMaxDelayMs' is missing in the configuration.")]
        public int InternetCheckMaxDelayMs { get; set; }
    }
}
