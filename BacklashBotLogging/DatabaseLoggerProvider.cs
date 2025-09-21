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
        private readonly InstanceNameConfig _instanceNameConfig;
        private readonly object? _brainStatus; // Simplified to avoid circular dependency
        private readonly string _defaultEnvironment;
        private readonly string _defaultInstance;

        /// <summary>
        /// Initializes a new instance of the DatabaseLoggerProvider class.
        /// </summary>
        /// <param name="loggingQueue">The queue for logging messages.</param>
        /// <param name="minLevel">The minimum log level.</param>
        /// <param name="loggingConfig">The logging configuration.</param>
        /// <param name="instanceNameConfig">The execution configuration.</param>
        /// <param name="brainStatus">The brain status service (not used in simplified version).</param>
        /// <param name="defaultEnvironment">The default environment name.</param>
        /// <param name="defaultInstance">The default instance name.</param>
        public DatabaseLoggerProvider(
            DatabaseLoggingQueue loggingQueue,
            LoggingConfig loggingConfig,
            InstanceNameConfig instanceNameConfig,
            LogLevel minLevel = LogLevel.Warning,
            object? brainStatus = null,
            string defaultEnvironment = "KalshiBot",
            string defaultInstance = "DefaultInstance"
            )
        {
            _loggingQueue = loggingQueue;
            _minLevel = minLevel;
            _loggingConfig = loggingConfig;
            _instanceNameConfig = instanceNameConfig;
            _brainStatus = brainStatus;
            _defaultEnvironment = defaultEnvironment;
            _defaultInstance = defaultInstance;
        }

        /// <summary>
        /// Creates a new ILogger instance.
        /// </summary>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>A new DatabaseLogger instance.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new DatabaseLogger(categoryName, _loggingQueue, _minLevel, _loggingConfig, _instanceNameConfig, _brainStatus, _defaultEnvironment, _defaultInstance);
        }

        /// <summary>
        /// Disposes the logger provider.
        /// </summary>
        public void Dispose() { }
    }
}
