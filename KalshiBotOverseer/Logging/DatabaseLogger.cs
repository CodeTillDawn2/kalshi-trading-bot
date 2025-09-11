using KalshiBotData.Data.Interfaces;
using BacklashDTOs.Data;
using Microsoft.Extensions.Logging;
using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;

namespace KalshiBotOverseer.Logging
{
    /// <summary>
    /// Custom logger implementation that integrates with the KalshiBotOverseer logging infrastructure.
    /// This logger formats log messages, outputs them to the console for immediate visibility,
    /// enqueues them for asynchronous database storage via the DatabaseLoggingQueue,
    /// and forwards warnings and errors to the error handler for further processing.
    /// Supports optional configuration for dynamic values.
    /// </summary>
    public class DatabaseLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly LogLevel _minLevel;
        private readonly LoggingConfig? _loggingConfig;
        private readonly ExecutionConfig? _executionConfig;
        private readonly IBrainStatusService? _brainStatus;

        /// <summary>
        /// Initializes a new instance of the DatabaseLogger class with the specified parameters.
        /// </summary>
        /// <param name="categoryName">The category name for this logger instance, typically the fully qualified name of the logging class.</param>
        /// <param name="loggingQueue">The queue responsible for handling database logging operations asynchronously.</param>
        /// <param name="minLevel">The minimum log level that this logger will process; logs below this level are ignored.</param>
        /// <param name="loggingConfig">Optional logging configuration for dynamic environment settings.</param>
        /// <param name="executionConfig">Optional execution configuration for brain instance settings.</param>
        /// <param name="brainStatus">Optional brain status service for session identifier.</param>
        public DatabaseLogger(
            string categoryName,
            DatabaseLoggingQueue loggingQueue,
            LogLevel minLevel,
            LoggingConfig? loggingConfig = null,
            ExecutionConfig? executionConfig = null,
            IBrainStatusService? brainStatus = null)
        {
            _categoryName = categoryName;
            _loggingQueue = loggingQueue;
            _minLevel = minLevel;
            _loggingConfig = loggingConfig;
            _executionConfig = executionConfig;
            _brainStatus = brainStatus;
        }

        /// <summary>
        /// Begins a logical operation scope. This implementation returns null as scoped logging is not supported.
        /// </summary>
        /// <typeparam name="TState">The type of the state object associated with the scope.</typeparam>
        /// <param name="state">The state object for the scope.</param>
        /// <returns>A disposable object that ends the scope, or null if not supported.</returns>
        public IDisposable BeginScope<TState>(TState state) => null;

        /// <summary>
        /// Determines whether logging is enabled for the specified log level.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns>True if logging is enabled for the level; otherwise, false.</returns>
        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        /// <summary>
        /// Logs a message with the specified log level, event ID, state, and optional exception.
        /// The message is formatted, logged to console, enqueued for database storage if appropriate,
        /// and forwarded to the error handler.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">The severity level of the log message.</param>
        /// <param name="eventId">The event ID associated with the log message.</param>
        /// <param name="state">The state object containing additional information.</param>
        /// <param name="exception">The exception associated with the log message, if any.</param>
        /// <param name="formatter">A function to format the state and exception into a string message.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);

            // Get dynamic values or use defaults
            string environment = _loggingConfig?.Environment ?? "Overseer";
            string brainInstance = _executionConfig?.BrainInstance ?? "OverseerInstance";
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

            // Console logging for immediate visibility
            string formattedLogLevel = $"[{logEntry.Level}]".PadRight(12);
            Console.WriteLine($"{formattedLogLevel} {logEntry.Timestamp}: {message} {(logEntry.Exception != "" && logEntry.Exception != null ? "Exception: " + logEntry.Exception : "")} ({logEntry.Source})");

            // Database logging for persistent storage
            LogLevel minSqlLogLevel = _loggingConfig != null ? Enum.Parse<LogLevel>(_loggingConfig.SqlDatabaseLogLevel, true) : LogLevel.Information;
            if (logLevel >= minSqlLogLevel)
            {
                _loggingQueue.EnqueueDBLogs(logEntry);
            }

            // Forward to error handler for warnings and errors
            _loggingQueue.EnqueueErrorHandlerLogs(logEntry.Message, logEntry.Source, logLevel, exception);
        }

        private string GetSessionIdentifier()
        {
            if (_brainStatus == null) return "OVR";
            try
            {
                return _brainStatus.SessionIdentifier ?? "OVR";
            }
            catch (InvalidOperationException)
            {
                return "OVR";
            }
        }
    }
}