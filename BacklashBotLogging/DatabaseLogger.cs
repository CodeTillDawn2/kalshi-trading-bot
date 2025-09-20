using BacklashBotData.Data.Interfaces;
using BacklashDTOs.Data;
using Microsoft.Extensions.Logging;
using BacklashDTOs.Configuration;
// Removed reference to BacklashBot to avoid circular dependency

namespace KalshiBotLogging
{
    /// <summary>
    /// Custom logger implementation that integrates with the KalshiBot logging infrastructure.
    /// This logger provides comprehensive logging functionality by:
    /// - Formatting log messages and outputting them to the console for immediate developer visibility (configurable verbosity)
    /// - Enqueuing log entries for asynchronous database storage via the DatabaseLoggingQueue with batching support
    /// - Forwarding warnings and errors to the error handler for centralized error management
    /// - Supporting dynamic configuration for environment-specific settings including min log levels
    /// - Maintaining structured logging with consistent metadata across all log entries
    /// - Providing metrics and monitoring for queue depth and processing performance
    /// </summary>
    public class DatabaseLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly LogLevel _minLevel;
        private readonly LogLevel _minConsoleLogLevel;
        private readonly LogLevel _minSqlLogLevel;
        private readonly LoggingConfig? _loggingConfig;
        private readonly GeneralExecutionConfig? _executionConfig;
        private readonly object? _brainStatus; // Simplified to avoid circular dependency
        private readonly string _defaultEnvironment;
        private readonly string _defaultInstance;

        /// <summary>
        /// Initializes a new instance of the DatabaseLogger class with the specified parameters.
        /// </summary>
        /// <param name="categoryName">The category name for this logger instance, typically the fully qualified name of the logging class.</param>
        /// <param name="loggingQueue">The queue responsible for handling database logging operations asynchronously.</param>
        /// <param name="minLevel">The minimum log level that this logger will process; logs below this level are ignored.</param>
        /// <param name="loggingConfig">Optional logging configuration for dynamic environment settings including min log levels.</param>
        /// <param name="executionConfig">Optional execution configuration for brain instance settings.</param>
        /// <param name="brainStatus">Optional brain status service for session identifier retrieval (not used in simplified version).</param>
        /// <param name="defaultEnvironment">Default environment name if not specified in config.</param>
        /// <param name="defaultInstance">Default instance name if not specified in config.</param>
        public DatabaseLogger(
            string categoryName,
            DatabaseLoggingQueue loggingQueue,
            LogLevel minLevel,
            LoggingConfig? loggingConfig = null,
            GeneralExecutionConfig? executionConfig = null,
            object? brainStatus = null,
            string defaultEnvironment = "KalshiBot",
            string defaultInstance = "DefaultInstance")
        {
            _categoryName = categoryName;
            _loggingQueue = loggingQueue;
            _minLevel = minLevel;
            _loggingConfig = loggingConfig;
            _executionConfig = executionConfig;
            _brainStatus = brainStatus;
            _defaultEnvironment = defaultEnvironment;
            _defaultInstance = defaultInstance;

            // Parse configurable log levels with defaults
            if (_loggingConfig?.LogLevel?.ConsoleLogLevel != null)
            {
                try
                {
                    _minConsoleLogLevel = Enum.Parse<LogLevel>(_loggingConfig.LogLevel.ConsoleLogLevel, true);
                }
                catch (ArgumentException)
                {
                    _minConsoleLogLevel = LogLevel.Information; // Default fallback
                }
            }
            else
            {
                _minConsoleLogLevel = LogLevel.Information; // Default when not configured
            }

            if (_loggingConfig?.LogLevel?.SqlDatabaseLogLevel != null)
            {
                try
                {
                    _minSqlLogLevel = Enum.Parse<LogLevel>(_loggingConfig.LogLevel.SqlDatabaseLogLevel, true);
                }
                catch (ArgumentException)
                {
                    _minSqlLogLevel = LogLevel.Warning; // Default fallback
                }
            }
            else
            {
                _minSqlLogLevel = LogLevel.Warning; // Default when not configured
            }
        }

        /// <summary>
        /// Begins a logical operation scope. This implementation returns null as scoped logging is not supported.
        /// </summary>
        /// <typeparam name="TState">The type of the state object associated with the scope.</typeparam>
        /// <param name="state">The state object for the scope.</param>
        /// <returns>A disposable object that ends the scope, or null if not supported.</returns>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>
        /// Determines whether logging is enabled for the specified log level.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns>True if logging is enabled for the level; otherwise, false.</returns>
        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        /// <summary>
        /// Logs a message with the specified log level, event ID, state, and optional exception.
        /// The message is formatted, logged to console if above min console level, enqueued asynchronously for database storage if appropriate,
        /// and forwarded to the error handler for centralized error management.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">The severity level of the log message.</param>
        /// <param name="eventId">The event ID associated with the log message.</param>
        /// <param name="state">The state object containing additional information.</param>
        /// <param name="exception">The exception associated with the log message, if any.</param>
        /// <param name="formatter">A function to format the state and exception into a string message.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            // Get dynamic values or use defaults
            string environment = _loggingConfig?.Environment ?? _defaultEnvironment;
            string brainInstance = _executionConfig?.BrainInstance ?? _defaultInstance;
            string sessionIdentifier = GetSessionIdentifier();

            var logEntry = new LogEntryDTO
            {
                Timestamp = DateTime.Now,
                Level = logLevel.ToString(),
                Message = $"{message} ({_categoryName})",
                Exception = exception?.ToString() ?? "",
                Environment = environment,
                BrainInstance = brainInstance,
                SessionIdentifier = sessionIdentifier,
                Source = _categoryName
            };

            // Console logging for immediate developer visibility - only if above configured min level
            if (logLevel >= _minConsoleLogLevel)
            {
                string formattedLogLevel = $"[{logEntry.Level}]".PadRight(12);
                string consoleMessage = $"{formattedLogLevel} {logEntry.Timestamp:yyyy-MM-dd HH:mm:ss}: {message}";
                if (!string.IsNullOrEmpty(logEntry.Exception))
                {
                    consoleMessage += $" | Exception: {logEntry.Exception}";
                }
                // Removed source information from console output as requested
                Console.WriteLine(consoleMessage);
            }

            // Database logging for persistent storage - only for configured minimum level
            if (logLevel >= _minSqlLogLevel)
            {
                // Async enqueue for high-throughput scenarios
                _ = Task.Run(async () => await _loggingQueue.EnqueueDBLogsAsync(logEntry));
            }

            // Forward to error handler for warnings and errors
            _loggingQueue.EnqueueErrorHandlerLogs(logEntry.Message, logEntry.Source, logLevel, exception);
        }

        /// <summary>
        /// Retrieves the current session identifier.
        /// Returns "DEF" as a fallback since brain status service is not available in logging project.
        /// </summary>
        /// <returns>The session identifier string, always "DEF" in this simplified version.</returns>
        private string GetSessionIdentifier()
        {
            // Simplified to avoid circular dependency - brain status service not available in logging project
            return "DEF";
        }
    }
}