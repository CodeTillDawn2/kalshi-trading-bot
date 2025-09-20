using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
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
        /// Gets or sets the log level settings.
        /// </summary>
        [Required(ErrorMessage = "The 'LogLevel' is missing in the configuration.")]
        public LogLevelSettings LogLevel { get; set; } = null!;

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
    }

    /// <summary>
    /// Configuration class for log level settings.
    /// </summary>
    public class LogLevelSettings
    {
        /// <summary>
        /// Gets or sets the default log level.
        /// </summary>
        [Required(ErrorMessage = "The 'Default' is missing in the configuration.")]
        public string Default { get; set; } = null!;

        /// <summary>
        /// Gets or sets the log level for Microsoft components.
        /// </summary>
        [Required(ErrorMessage = "The 'Microsoft' is missing in the configuration.")]
        public string Microsoft { get; set; } = null!;

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
