using BacklashBot.Configuration;
using BacklashCommon.Configuration;
using KalshiBotLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BacklashIntegrationTests
{
    /// <summary>
    /// Integration tests for the DatabaseLoggerProvider to ensure it can be instantiated and logs entries correctly.
    /// These tests use real dependencies to verify end-to-end functionality.
    /// </summary>
    [TestFixture]
    public class LoggerIntegrationTests
    {
        private IServiceProvider _serviceProvider;
        private DatabaseLoggingQueue _loggingQueue;

        /// <summary>
        /// Sets up the test environment by loading configuration and initializing the logging queue.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<LoggingConfig>(configuration.GetSection(LoggingConfig.SectionName));
            services.Configure<InstanceNameConfig>(configuration.GetSection(InstanceNameConfig.SectionName));
            _serviceProvider = services.BuildServiceProvider();

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
            // Arrange - Load configs from DI
            var loggingConfig = _serviceProvider.GetRequiredService<IOptions<LoggingConfig>>().Value;
            var instanceNameConfig = _serviceProvider.GetRequiredService<IOptions<InstanceNameConfig>>().Value;

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var provider = new DatabaseLoggerProvider(
                    _loggingQueue,
                    loggingConfig,
                    instanceNameConfig.Name,
                    LogLevel.Warning, // minLevel
                    null, // brainStatus
                    "BacklashBot" // defaultEnvironment
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
            // Arrange - Load configs from DI
            var loggingConfig = _serviceProvider.GetRequiredService<IOptions<LoggingConfig>>().Value;
            var instanceNameConfig = _serviceProvider.GetRequiredService<IOptions<InstanceNameConfig>>().Value;

            var provider = new DatabaseLoggerProvider(
                _loggingQueue,
                loggingConfig,
                instanceNameConfig.Name,
                LogLevel.Information, // minLevel set to Information to allow the log
                null, // brainStatus
                "BacklashBot" // defaultEnvironment
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
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}
