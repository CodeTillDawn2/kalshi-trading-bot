using BacklashCommon.Configuration;
using KalshiBotLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private IServiceProvider _serviceProvider;
        private Mock<DatabaseLoggingQueue> _loggingQueueMock;

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

            // Set up DI container
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<LoggingConfig>(configuration.GetSection(LoggingConfig.SectionName));
            services.Configure<InstanceNameConfig>(configuration.GetSection(InstanceNameConfig.SectionName));
            _serviceProvider = services.BuildServiceProvider();

            _loggingQueueMock = new Mock<DatabaseLoggingQueue>(null, false);
        }

        /// <summary>
        /// Tests that the DatabaseLoggerProvider can be instantiated with mocked dependencies.
        /// This verifies that the logger provider setup does not hang during creation.
        /// </summary>
        [Test]
        public void DatabaseLoggerProvider_CanBeInstantiated()
        {
            TestContext.WriteLine("Testing that the DatabaseLoggerProvider can be instantiated with mocked dependencies.");
            // Arrange - Load configs from DI
            var loggingConfig = _serviceProvider.GetRequiredService<IOptions<LoggingConfig>>().Value;
            var instanceNameConfig = _serviceProvider.GetRequiredService<IOptions<InstanceNameConfig>>().Value;

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var provider = new DatabaseLoggerProvider(
                    _loggingQueueMock.Object,
                    loggingConfig,
                    instanceNameConfig.Name,
                    LogLevel.Warning, // minLevel
                    null, // brainStatus
                    "BacklashBot" // defaultEnvironment
                );

                // Verify the provider is not null
                Assert.That(provider, Is.Not.Null);
            });
            TestContext.WriteLine("Result: DatabaseLoggerProvider instantiated successfully.");
        }

        /// <summary>
        /// Tests that the DatabaseLoggerProvider can create a logger instance.
        /// This verifies that the logger creation process works correctly.
        /// </summary>
        [Test]
        public void DatabaseLoggerProvider_CanCreateLogger()
        {
            TestContext.WriteLine("Testing that the DatabaseLoggerProvider can create a logger instance.");
            // Arrange - Load configs from DI
            var loggingConfig = _serviceProvider.GetRequiredService<IOptions<LoggingConfig>>().Value;
            var instanceNameConfig = _serviceProvider.GetRequiredService<IOptions<InstanceNameConfig>>().Value;

            var provider = new DatabaseLoggerProvider(
                _loggingQueueMock.Object,
                loggingConfig,
                instanceNameConfig.Name,
                LogLevel.Warning,
                null, // brainStatus
                "BacklashBot"
            );

            // Act
            var logger = provider.CreateLogger("TestCategory");

            // Assert
            Assert.That(logger, Is.Not.Null);
            Assert.That(logger, Is.InstanceOf<ILogger>());
            TestContext.WriteLine("Result: Logger instance created successfully.");
        }

        /// <summary>
        /// Cleans up resources after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}
