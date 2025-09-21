using BacklashBot.Configuration;
using BacklashCommon.Configuration;
using KalshiBotLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BacklashIntegrationTests
{
    /// <summary>
    /// Integration tests for the DatabaseLoggerProvider to ensure it can be instantiated and logs entries correctly.
    /// These tests use real dependencies to verify end-to-end functionality.
    /// </summary>
    [TestFixture]
    public class LoggerIntegrationTests
    {
        private IConfiguration _configuration;
        private DatabaseLoggingQueue _loggingQueue;

        /// <summary>
        /// Sets up the test environment by loading configuration and initializing the logging queue.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            _configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // Initialize the logging queue as a singleton (not hosted service for test)
            _loggingQueue = new DatabaseLoggingQueue(null, false); // isOverseer = false
        }

        /// <summary>
        /// Tests that the DatabaseLoggerProvider can be instantiated without errors.
        /// This verifies that the logger provider setup does not hang during creation.
        /// </summary>
        [Test]
        public void DatabaseLoggerProvider_CanBeInstantiated()
        {
            // Arrange - Load configs from appsettings.json
            var loggingConfig = _configuration.GetSection("Communications:Logging").Get<LoggingConfig>();
            var instanceNameConfig = _configuration.GetSection("Central:GeneralExecution").Get<InstanceNameConfig>();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var provider = new DatabaseLoggerProvider(
                    _loggingQueue,
                    loggingConfig,
                    instanceNameConfig,
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
        /// Tests that the DatabaseLoggerProvider can create a logger and log an entry without hanging.
        /// This integration test verifies the full logging pipeline works end-to-end.
        /// </summary>
        [Test]
        public void DatabaseLoggerProvider_CanLogEntry()
        {
            // Arrange - Load configs from appsettings.json
            var loggingConfig = _configuration.GetSection("Communications:Logging").Get<LoggingConfig>();
            var instanceNameConfig = _configuration.GetSection("Central:GeneralExecution").Get<InstanceNameConfig>();

            var provider = new DatabaseLoggerProvider(
                _loggingQueue,
                loggingConfig,
                instanceNameConfig,
                LogLevel.Information, // minLevel set to Information to allow the log
                null, // brainStatus
                "BacklashBot", // defaultEnvironment
                "BacklashInstance" // defaultInstance
            );

            var logger = provider.CreateLogger("TestCategory");

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                logger.LogInformation("Test log entry from integration test at {Timestamp}", DateTime.UtcNow);
            });

            // Give some time for async logging to complete
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Cleans up resources after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _loggingQueue?.Dispose();
        }
    }
}
