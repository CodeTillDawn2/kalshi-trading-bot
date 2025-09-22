using BacklashCommon.Configuration;
using Microsoft.Extensions.Logging;

namespace KalshiBotLogging
{
    /// <summary>
    /// Provides logging to a database via a queue.
    /// </summary>
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly LogLevel _minLevel;
        private readonly LoggingConfig _loggingConfig;
        private readonly string _instanceName;
        private readonly string? _sessionIdentifier;
        private readonly string _defaultEnvironment;

        /// <summary>
        /// Initializes a new instance of the DatabaseLoggerProvider class.
        /// </summary>
        /// <param name="loggingQueue">The queue for logging messages.</param>
        /// <param name="minLevel">The minimum log level.</param>
        /// <param name="loggingConfig">The logging configuration.</param>
        /// <param name="instanceName">The instance name for logging.</param>
        /// <param name="sessionIdentifier">The session identifier for logging.</param>
        /// <param name="defaultEnvironment">The default environment name.</param>
        public DatabaseLoggerProvider(
            DatabaseLoggingQueue loggingQueue,
            LoggingConfig loggingConfig,
            string instanceName,
            LogLevel minLevel = LogLevel.Warning,
            string? sessionIdentifier = null,
            string defaultEnvironment = "KalshiBot"
            )
        {
            _loggingQueue = loggingQueue;
            _minLevel = minLevel;
            _loggingConfig = loggingConfig;
            _instanceName = instanceName;
            _sessionIdentifier = sessionIdentifier;
            _defaultEnvironment = defaultEnvironment;
        }

        /// <summary>
        /// Creates a new ILogger instance.
        /// </summary>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>A new DatabaseLogger instance.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new DatabaseLogger(categoryName, _loggingQueue, _minLevel, _loggingConfig, _instanceName, _sessionIdentifier, _defaultEnvironment);
        }

        /// <summary>
        /// Disposes the logger provider.
        /// </summary>
        public void Dispose() { }
    }
}
