using Microsoft.Extensions.Options;
using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;

namespace BacklashBot.Logging
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly LoggingConfig _loggingConfig;
        private readonly ExecutionConfig _executionConfig;
        private readonly IBrainStatusService _brainStatuService;
        private readonly LogLevel _minLevel;

        public DatabaseLoggerProvider(
            DatabaseLoggingQueue loggingQueue,
            IOptions<LoggingConfig> loggingConfig,
            IOptions<ExecutionConfig> executionConfig,
            IBrainStatusService brainStatus,
            LogLevel minLevel = LogLevel.Warning
            )
        {
            _loggingQueue = loggingQueue;
            _loggingConfig = loggingConfig.Value;
            _executionConfig = executionConfig.Value;
            _brainStatuService = brainStatus;
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DatabaseLogger(categoryName, _loggingQueue, _loggingConfig, _executionConfig, _minLevel, _brainStatuService);
        }

        public void Dispose() { }
    }
}
