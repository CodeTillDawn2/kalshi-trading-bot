using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;

namespace KalshiBotOverseer.Logging
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly LogLevel _minLevel;
        private readonly LoggingConfig? _loggingConfig;
        private readonly ExecutionConfig? _executionConfig;
        private readonly IBrainStatusService? _brainStatus;

        public DatabaseLoggerProvider(
            DatabaseLoggingQueue loggingQueue,
            LogLevel minLevel = LogLevel.Warning,
            LoggingConfig? loggingConfig = null,
            ExecutionConfig? executionConfig = null,
            IBrainStatusService? brainStatus = null
            )
        {
            _loggingQueue = loggingQueue;
            _minLevel = minLevel;
            _loggingConfig = loggingConfig;
            _executionConfig = executionConfig;
            _brainStatus = brainStatus;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DatabaseLogger(categoryName, _loggingQueue, _minLevel, _loggingConfig, _executionConfig, _brainStatus);
        }

        public void Dispose() { }
    }
}