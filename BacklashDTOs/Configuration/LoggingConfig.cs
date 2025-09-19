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
        [JsonRequired]
        public LogLevelSettings LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        [JsonRequired]
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets whether to store WebSocket events.
        /// </summary>
        [JsonRequired]
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
        [JsonRequired]
        public string Default { get; set; }

        /// <summary>
        /// Gets or sets the log level for Microsoft components.
        /// </summary>
        [JsonRequired]
        public string Microsoft { get; set; }

        /// <summary>
        /// Gets or sets the log level for SQL database logging.
        /// </summary>
        [JsonRequired]
        public string SqlDatabaseLogLevel { get; set; }

        /// <summary>
        /// Gets or sets the log level for console logging.
        /// </summary>
        [JsonRequired]
        public string ConsoleLogLevel { get; set; }
    }
}