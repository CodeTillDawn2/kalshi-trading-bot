using KalshiBotData.Data.Interfaces;
using BacklashDTOs.Data;
using Microsoft.Extensions.Logging;

namespace KalshiBotOverseer.Logging
{
    public class DatabaseLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly LogLevel _minLevel;

        public DatabaseLogger(
            string categoryName,
            DatabaseLoggingQueue loggingQueue,
            LogLevel minLevel)
        {
            _categoryName = categoryName;
            _loggingQueue = loggingQueue;
            _minLevel = minLevel;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            var logEntry = new LogEntryDTO
            {
                Timestamp = DateTime.Now,
                Level = logLevel.ToString(),
                Message = $"{message} ({_categoryName})",
                Exception = exception?.ToString(),
                Environment = "Overseer", // Simplified
                BrainInstance = "OverseerInstance", // Simplified
                SessionIdentifier = "OVR", // Simplified
                Source = _categoryName
            };

            // Console logging
            string formattedLogLevel = $"[{logEntry.Level}]".PadRight(12);
            Console.WriteLine($"{formattedLogLevel} {logEntry.Timestamp}: {message} {(logEntry.Exception != "" && logEntry.Exception != null ? "Exception: " + logEntry.Exception : "")} ({logEntry.Source})");

            // Database logging
            LogLevel minSqlLogLevel = LogLevel.Information;
            if (logLevel >= minSqlLogLevel)
            {
                _loggingQueue.EnqueueDBLogs(logEntry);
            }

            _loggingQueue.EnqueueErrorHandlerLogs(logEntry.Message, logEntry.Source, logLevel, exception);
        }
    }
}