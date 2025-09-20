using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using KalshiBotLogging;
using BacklashDTOs.Configuration;

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

        /// <summary>
        /// Sets up the test environment by creating mocked dependencies.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _loggingQueueMock = new Mock<DatabaseLoggingQueue>(null, false);
            _loggingConfig = new LoggingConfig
            {
                LogLevel = new LogLevelSettings
                {
                    Default = "Warning",
                    Microsoft = "Warning",
                    SqlDatabaseLogLevel = "Warning",
                    ConsoleLogLevel = "Warning"
                },
                Environment = "Test",
                StoreWebSocketEvents = false
            };
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
                    LogLevel.Warning, // minLevel
                    _loggingConfig,
                    null, // IBrainStatusService
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
                LogLevel.Warning,
                _loggingConfig,
                null,
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
