namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for logging settings.
    /// </summary>
    public class LoggingConfig
    {
        /// <summary>
        /// Gets or sets the log level for SQL database logging.
        /// </summary>
        public string SqlDatabaseLogLevel { get; set; } = "Information";

        /// <summary>
        /// Gets or sets the log level for console logging.
        /// </summary>
        public string ConsoleLogLevel { get; set; } = "Debug";

        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        public string Environment { get; set; } = "dev";

        /// <summary>
        /// Gets or sets whether to store WebSocket events.
        /// </summary>
        public bool StoreWebSocketEvents { get; set; } = true;
    }
}