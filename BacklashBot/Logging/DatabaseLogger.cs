using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashDTOs.Data;

namespace BacklashBot.Logging
{
    public class DatabaseLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly LoggingConfig _loggingConfig;
        private readonly ExecutionConfig _executionConfig;
        private readonly LogLevel _minLevel;
        private readonly IBrainStatusService _brainStatus;

        public DatabaseLogger(
            string categoryName,
            DatabaseLoggingQueue loggingQueue,
            LoggingConfig loggingConfig,
            ExecutionConfig executionConfig,
            LogLevel minLevel,
            IBrainStatusService brainStatus)
        {
            _categoryName = categoryName;
            _loggingQueue = loggingQueue;
            _loggingConfig = loggingConfig;
            _executionConfig = executionConfig;
            _minLevel = minLevel;
            _brainStatus = brainStatus;
        }

        private class NullScope : IDisposable
        {
            public void Dispose() { }
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var logEntry = new LogEntryDTO
            {
                Timestamp = DateTime.Now,
                Level = logLevel.ToString(),
                Message = $"{message} ({_categoryName})",
                Exception = exception?.ToString() ?? "",
                Environment = _loggingConfig.Environment,
                BrainInstance = _executionConfig.BrainInstance ?? "",
                SessionIdentifier = _brainStatus.SessionIdentifier ?? "",
                Source = _categoryName
            };

            // Console logging
            string formattedLogLevel = $"[{logEntry.Level}]".PadRight(12);
            Console.WriteLine($"{formattedLogLevel} {logEntry.Timestamp}: {message} {(logEntry.Exception != "" && logEntry.Exception != null ? "Exception: " + logEntry.Exception : "")} ({logEntry.Source})");

            // Database logging
            LogLevel minSqlLogLevel = LogLevel.Information;
            try
            {
                minSqlLogLevel = Enum.Parse<LogLevel>(_loggingConfig.SqlDatabaseLogLevel, true);
            }
            catch (ArgumentException ex)
            {
                Log(LogLevel.Warning, eventId, $"Invalid log level in config: {ex.Message}", ex, (s, e) => s.ToString());
            }

            if (logLevel >= minSqlLogLevel)
            {
                _loggingQueue.EnqueueDBLogs(logEntry);
            }

            _loggingQueue.EnqueueErrorHandlerLogs(logEntry.Message, logEntry.Source, logLevel, exception);
        }
    }
}
