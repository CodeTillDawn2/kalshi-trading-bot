using System.ComponentModel.DataAnnotations;

namespace BacklashCommon.Configuration
{
    /// <summary>
    /// Configuration class for logging settings.
    /// </summary>
    public class LoggingConfig
    {
        /// <summary>
        /// The configuration section name for LoggingConfig.
        /// </summary>
        public const string SectionName = "Communications:Logging";

        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        [Required(ErrorMessage = "The 'Environment' is missing in the configuration.")]
        public string Environment { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether to store WebSocket events.
        /// </summary>
        [Required(ErrorMessage = "The 'StoreWebSocketEvents' is missing in the configuration.")]
        public bool StoreWebSocketEvents { get; set; }

        /// <summary>
        /// Gets or sets the log level for SQL database logging.
        /// </summary>
        [Required(ErrorMessage = "The 'SqlDatabaseLogLevel' is missing in the configuration.")]
        public string SqlDatabaseLogLevel { get; set; } = null!;

        /// <summary>
        /// Gets or sets the log level for console logging.
        /// </summary>
        [Required(ErrorMessage = "The 'ConsoleLogLevel' is missing in the configuration.")]
        public string ConsoleLogLevel { get; set; } = null!;
    }

}
