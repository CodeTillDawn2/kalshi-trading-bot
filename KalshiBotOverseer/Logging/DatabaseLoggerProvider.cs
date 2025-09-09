using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace KalshiBotOverseer.Logging
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly LogLevel _minLevel;

        public DatabaseLoggerProvider(
            DatabaseLoggingQueue loggingQueue,
            LogLevel minLevel = LogLevel.Warning
            )
        {
            _loggingQueue = loggingQueue;
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DatabaseLogger(categoryName, _loggingQueue, _minLevel);
        }

        public void Dispose() { }
    }
}