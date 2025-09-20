using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for logging settings.
    /// </summary>
    public class LoggingConfig
    {
        /// <summary>
        /// Gets or sets the log level settings.
        /// </summary>
        public required LogLevelSettings LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        public required string Environment { get; set; }

        /// <summary>
        /// Gets or sets whether to store WebSocket events.
        /// </summary>
        public required bool StoreWebSocketEvents { get; set; }
    }

    /// <summary>
    /// Configuration class for log level settings.
    /// </summary>
    public class LogLevelSettings
    {
        /// <summary>
        /// Gets or sets the default log level.
        /// </summary>
        public required string Default { get; set; }

        /// <summary>
        /// Gets or sets the log level for Microsoft components.
        /// </summary>
        public required string Microsoft { get; set; }

        /// <summary>
        /// Gets or sets the log level for SQL database logging.
        /// </summary>
        public required string SqlDatabaseLogLevel { get; set; }

        /// <summary>
        /// Gets or sets the log level for console logging.
        /// </summary>
        public required string ConsoleLogLevel { get; set; }
    }
}
