using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashBotData.Configuration;
using BacklashBotData.Data;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashDTOs.Data;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BacklashBotTasks
{
    /// <summary>
    /// Test fixture class for executing schedule update tasks.
    /// This class provides testing capabilities for fetching current exchange schedule
    /// information from the Kalshi API and updating all relevant database tables.
    /// </summary>
    [TestFixture]
    public class ScheduleUpdateTasks
    {
        private IKalshiAPIService? _kalshiApiService;
        private IBacklashBotContext _dbContext;
        private ServiceProvider? _serviceProvider;
        private IConfigurationRoot _config;

        /// <summary>
        /// Initializes the test fixture by setting up dependency injection services,
        /// configuring application settings, and preparing the database context.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var configBuilder = ConfigurationHelper.CreateConfigurationBuilder(basePath, Array.Empty<string>());
            var tempConfig = configBuilder.Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(tempConfig);

            // Register options using the same pattern as Program.cs
            services.AddOptions<KalshiConfig>()
                .Bind(tempConfig.GetSection(KalshiConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<KalshiAPIServiceConfig>()
                .Bind(tempConfig.GetSection(KalshiAPIServiceConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<BacklashBotDataConfig>()
                .Bind(tempConfig.GetSection(BacklashBotDataConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Get secrets config for interpolation
            var secretsConfig = tempConfig.GetSection(BacklashCommon.Configuration.SecretsConfig.SectionName).Get<BacklashCommon.Configuration.SecretsConfig>()!;

            // Interpolate placeholders in KalshiConfig (same as Program.cs)
            services.PostConfigure<KalshiConfig>(config =>
            {
                // Interpolate KeyId and KeyFile
                var interpolatedKeyId = BacklashCommon.Configuration.ConfigurationHelper.InterpolateConfigurationValue(config.KeyId, tempConfig);
                var interpolatedKeyFile = BacklashCommon.Configuration.ConfigurationHelper.InterpolateConfigurationValue(config.KeyFile, tempConfig);

                // Resolve the key file path to the secrets directory
                var resolvedKeyFile = BacklashCommon.Configuration.ConfigurationHelper.ResolveSecretsFilePath(interpolatedKeyFile, secretsConfig, basePath);

                // Update the config with interpolated values
                config.KeyId = interpolatedKeyId;
                config.KeyFile = resolvedKeyFile;
            });

            var tempProvider = services.BuildServiceProvider();
            var kalshiOptions = tempProvider.GetRequiredService<IOptions<KalshiConfig>>();
            var apiConfigOptions = tempProvider.GetRequiredService<IOptions<KalshiAPIServiceConfig>>();
            var dataConfigOptions = tempProvider.GetRequiredService<IOptions<BacklashBotDataConfig>>();

            // Build the final configuration with interpolated values
            _config = configBuilder.Build();

            var connectionString = ConfigurationHelper.BuildConnectionString(_config);
            Assert.That(connectionString, Is.Not.Null.And.Not.Empty, "DefaultConnection string is missing in appsettings.json");

            var dataConfig = dataConfigOptions.Value;

            // Register connection string as singleton for KalshiAPIService
            services.AddSingleton(connectionString);

            var apiLoggerMock = new Mock<ILogger<IKalshiAPIService>>();
            var statusTrackerMock = new Mock<IScopeManagerService>();
            var scopeManagerMock = new Mock<IStatusTrackerService>();
            var performanceMonitorMock = new Mock<IPerformanceMonitor>();

            // Create database context
            var dbContextLoggerMock = new Mock<ILogger<BacklashBotContext>>();
            services.AddScoped<IBacklashBotContext>(provider => new BacklashBotContext(connectionString, dbContextLoggerMock.Object, dataConfig, performanceMonitorMock.Object));

            // Register Kalshi API service dependencies
            services.AddScoped(p => apiLoggerMock.Object);
            services.AddScoped(p => statusTrackerMock.Object);
            services.AddScoped(p => scopeManagerMock.Object);
            services.AddSingleton<IOptions<KalshiConfig>>(kalshiOptions);
            services.AddSingleton<IOptions<KalshiAPIServiceConfig>>(apiConfigOptions);
            services.AddSingleton<IPerformanceMonitor>(performanceMonitorMock.Object);

            // Register Kalshi API service
            services.AddScoped<IKalshiAPIService, KalshiAPIService>();

            _serviceProvider = services.BuildServiceProvider();

            // Don't create KalshiAPIService in Setup - create it on demand in tests that need it
            _dbContext = _serviceProvider.GetRequiredService<IBacklashBotContext>();
        }

        /// <summary>
        /// Gets the Kalshi API service on demand for tests that require it.
        /// </summary>
        private IKalshiAPIService GetKalshiAPIService()
        {
            if (_kalshiApiService == null)
            {
                _kalshiApiService = _serviceProvider!.GetRequiredService<IKalshiAPIService>();
            }
            return _kalshiApiService;
        }

        /// <summary>
        /// Test method that fetches the current exchange schedule from the Kalshi API
        /// and updates all relevant database tables including ExchangeSchedule,
        /// MaintenanceWindows, StandardHours, StandardHoursSessions, and CurrentSchedule.
        /// </summary>
        [Test]
        [Explicit]
        public async Task UpdateExchangeSchedule()
        {
            TestContext.Out.WriteLine("Starting exchange schedule update from Kalshi API...");

            try
            {
                // Fetch exchange schedule from API
                TestContext.Out.WriteLine("Fetching exchange schedule from Kalshi API...");
                var (processedCount, errorCount) = await GetKalshiAPIService().FetchExchangeScheduleAsync();

                if (errorCount > 0)
                {
                    TestContext.Out.WriteLine($"Exchange schedule fetch completed with {processedCount} processed and {errorCount} errors.");
                }
                else
                {
                    TestContext.Out.WriteLine($"Exchange schedule fetch completed successfully: {processedCount} items processed.");
                }

                // Fetch announcements (maintenance info)
                TestContext.Out.WriteLine("Fetching announcements from Kalshi API...");
                var (announcementProcessedCount, announcementErrorCount) = await GetKalshiAPIService().FetchAnnouncementsAsync();

                if (announcementErrorCount > 0)
                {
                    TestContext.Out.WriteLine($"Announcements fetch completed with {announcementProcessedCount} processed and {announcementErrorCount} errors.");
                }
                else
                {
                    TestContext.Out.WriteLine($"Announcements fetch completed successfully: {announcementProcessedCount} items processed.");
                }

                // Update CurrentSchedule table with flattened schedule data
                TestContext.Out.WriteLine("Updating CurrentSchedule table with flattened schedule data...");
                await ((BacklashBotContext)_dbContext).PopulateCurrentScheduleFromStandardHours();

                TestContext.Out.WriteLine("Exchange schedule update completed successfully.");
                TestContext.Out.WriteLine("Updated tables: ExchangeSchedule, MaintenanceWindows, StandardHours, StandardHoursSessions, Announcements, CurrentSchedule");

            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Exchange schedule update failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    TestContext.Out.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Test method that only updates the CurrentSchedule table from existing StandardHours data
        /// without fetching new data from the API. Useful for rebuilding the flattened schedule view.
        /// </summary>
        [Test]
        [Explicit]
        public async Task RefreshCurrentScheduleFromExistingData()
        {
            TestContext.Out.WriteLine("Refreshing CurrentSchedule table from existing StandardHours data...");

            try
            {
                await ((BacklashBotContext)_dbContext).PopulateCurrentScheduleFromStandardHours();
                TestContext.Out.WriteLine("CurrentSchedule refresh completed successfully.");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"CurrentSchedule refresh failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    TestContext.Out.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Cleans up test resources by disposing of the service provider.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}