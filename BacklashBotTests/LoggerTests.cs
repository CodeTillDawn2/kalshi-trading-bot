using KalshiBotLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BacklashBotTests
{
    /// <summary>
    /// Unit tests for the DatabaseLoggerProvider to ensure it can be instantiated without hanging.
    /// These tests use mocked dependencies to isolate the logger provider instantiation.
    /// </summary>
    [TestFixture]
    public class LoggerTests
    {
        private Mock<DatabaseLoggingQueue> _loggingQueueMock;
        private LoggingConfig _loggingConfig;
        private GeneralExecutionConfig _executionConfig;

        /// <summary>
        /// Sets up the test environment by loading configuration and creating mocked dependencies.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // Load configs from configuration
            _loggingConfig = configuration.GetSection("Communications:Logging").Get<LoggingConfig>();
            _executionConfig = configuration.GetSection("Central:GeneralExecution").Get<GeneralExecutionConfig>();

            _loggingQueueMock = new Mock<DatabaseLoggingQueue>(null, false);
        }

        /// <summary>
        /// Tests that the DatabaseLoggerProvider can be instantiated with mocked dependencies.
        /// This verifies that the logger provider setup does not hang during creation.
        /// </summary>
        [Test]
        public void DatabaseLoggerProvider_CanBeInstantiated()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var provider = new DatabaseLoggerProvider(
                    _loggingQueueMock.Object,
                    _loggingConfig,
                    _executionConfig,
                    LogLevel.Warning, // minLevel
                    null, // brainStatus
                    "BacklashBot", // defaultEnvironment
                    "BacklashInstance" // defaultInstance
                );

                // Verify the provider is not null
                Assert.That(provider, Is.Not.Null);
            });
        }

        /// <summary>
        /// Tests that the DatabaseLoggerProvider can create a logger instance.
        /// This verifies that the logger creation process works correctly.
        /// </summary>
        [Test]
        public void DatabaseLoggerProvider_CanCreateLogger()
        {
            // Arrange
            var provider = new DatabaseLoggerProvider(
                _loggingQueueMock.Object,
                _loggingConfig,
                _executionConfig,
                LogLevel.Warning,
                null, // brainStatus
                "BacklashBot",
                "BacklashInstance"
            );

            // Act
            var logger = provider.CreateLogger("TestCategory");

            // Assert
            Assert.That(logger, Is.Not.Null);
            Assert.That(logger, Is.InstanceOf<ILogger>());
        }
    }
}
