using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using BacklashDTOs.Configuration;
using BacklashBot.Management.Interfaces;

namespace KalshiBotLogging
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly LogLevel _minLevel;
        private readonly LoggingConfig? _loggingConfig;
        private readonly ExecutionConfig? _executionConfig;
        private readonly IBrainStatusService? _brainStatus;
        private readonly string _defaultEnvironment;
        private readonly string _defaultInstance;

        public DatabaseLoggerProvider(
            DatabaseLoggingQueue loggingQueue,
            LogLevel minLevel = LogLevel.Warning,
            LoggingConfig? loggingConfig = null,
            ExecutionConfig? executionConfig = null,
            IBrainStatusService? brainStatus = null,
            string defaultEnvironment = "KalshiBot",
            string defaultInstance = "DefaultInstance"
            )
        {
            _loggingQueue = loggingQueue;
            _minLevel = minLevel;
            _loggingConfig = loggingConfig;
            _executionConfig = executionConfig;
            _brainStatus = brainStatus;
            _defaultEnvironment = defaultEnvironment;
            _defaultInstance = defaultInstance;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DatabaseLogger(categoryName, _loggingQueue, _minLevel, _loggingConfig, _executionConfig, _brainStatus, _defaultEnvironment, _defaultInstance);
        }

        public void Dispose() { }
    }
}