using BacklashBot.Configuration;
using BacklashBot.Helpers;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashBotData.Configuration;
using BacklashBotData.Data;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashCommon.Helpers;
using BacklashCommon.Services;
using BacklashDTOs;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotData.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using TradingSimulator.Strategies;
using TradingStrategies.ML.SpikePrediction;


namespace BacklashBotTasks
{
    /// <summary>
    /// Test fixture class for executing overnight activities and related maintenance tasks.
    /// This class provides testing capabilities for market data refresh, cleanup operations,
    /// and snapshot group generation workflows.
    /// </summary>
    [TestFixture]
    public class OvernightTasks
    {
        private TradingSnapshotService _snapshotService;
        private OvernightActivitiesHelper _overnightService;
        private IInterestScoreService _interestScoreService;
        private SnapshotPeriodHelper _snapshotPeriodHelper;
        private IOptions<DataStorageConfig> _dataStorageConfig;
        private Mock<ILogger<SqlDataService>> _sqlLoggerMock;
        private SnapshotGroupHelper _snapshotGroupHelper;
        private IBacklashBotContext _dbContext;
        private ServiceProvider? _serviceProvider;
        private IOptions<TradingSnapshotServiceConfig> _tradingSnapshotServiceOptions;
        private IServiceScopeFactory _scopeFactory;
        private SqlDataService _sqlDataService;
        private Mock<ILogger<SpikePredictionModel>> _spikeLoggerMock;
        private BacklashBotDataConfig _dataConfig;
        private IConfigurationRoot config;


        /// <summary>
        /// Initializes the test fixture by setting up dependency injection services,
        /// configuring application settings, and preparing mock objects for testing.
        /// This method creates a comprehensive service provider with all required dependencies
        /// for executing overnight activities and maintenance tasks.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var configBuilder = ConfigurationHelper.CreateConfigurationBuilder(basePath, Array.Empty<string>());
            this.config = configBuilder.Build();

            var snapshotLoggerMock = new Mock<ILogger<TradingSnapshotService>>();
            var apiLoggerMock = new Mock<ILogger<IKalshiAPIService>>(); // Align to IKalshiAPIService to match constructor
            var overnightLoggerMock = new Mock<ILogger<OvernightActivitiesHelper>>();
            var interestLoggerMock = new Mock<ILogger<InterestScoreService>>();
            var tradingLoggerMock = new Mock<ILogger<TradingStrategy<MarketSnapshot>>>();
            var marketAnalysisLoggerMock = new Mock<ILogger<SnapshotGroupHelper>>();
            _spikeLoggerMock = new Mock<ILogger<SpikePredictionModel>>();

            // Add mocks for missing dependencies
            var scopeManagerMock = new Mock<IScopeManagerService>();
            var statusTrackerMock = new Mock<IStatusTrackerService>();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(config);

            // Register options using the same pattern as Program.cs
            services.AddOptions<TradingSnapshotServiceConfig>()
                .Bind(config.GetSection(TradingSnapshotServiceConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<GeneralExecutionConfig>()
                .Bind(config.GetSection(GeneralExecutionConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<KalshiConfig>()
                .Bind(config.GetSection(KalshiConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<InterestScoreConfig>()
                .Bind(config.GetSection(InterestScoreConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<BacklashBotDataConfig>()
                .Bind(config.GetSection(BacklashBotDataConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<SnapshotPeriodHelperConfig>()
                .Bind(config.GetSection(SnapshotPeriodHelperConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<SnapshotGroupHelperConfig>()
                .Bind(config.GetSection(SnapshotGroupHelperConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<InstanceNameConfig>()
                .Bind(config.GetSection(InstanceNameConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<QueueMonitoringConfig>()
                .Bind(config.GetSection(QueueMonitoringConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<CentralPerformanceMonitorConfig>()
                .Bind(config.GetSection(CentralPerformanceMonitorConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddOptions<DataStorageConfig>()
                .Bind(config.GetSection(DataStorageConfig.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var tempProvider = services.BuildServiceProvider();

            _tradingSnapshotServiceOptions = tempProvider.GetRequiredService<IOptions<TradingSnapshotServiceConfig>>();
            _dataStorageConfig = tempProvider.GetRequiredService<IOptions<DataStorageConfig>>();
            var kalshiOptions = tempProvider.GetRequiredService<IOptions<KalshiConfig>>();
            var interestScoreOptions = tempProvider.GetRequiredService<IOptions<InterestScoreConfig>>();
            var dataConfigOptions = tempProvider.GetRequiredService<IOptions<BacklashBotDataConfig>>();
            var snapshotPeriodHelperOptions = tempProvider.GetRequiredService<IOptions<SnapshotPeriodHelperConfig>>();
            var snapshotGroupHelperOptions = tempProvider.GetRequiredService<IOptions<SnapshotGroupHelperConfig>>();

            services.AddScoped<IKalshiAPIService, KalshiAPIService>(); // Register with interface
            services.AddScoped<IServiceFactory, ServiceFactory>();
            services.AddSingleton<ICentralPerformanceMonitor>(sp => new CentralPerformanceMonitor(
                sp.GetRequiredService<ILogger<ICentralPerformanceMonitor>>(),
                sp.GetRequiredService<IOptions<GeneralExecutionConfig>>(),
                sp.GetRequiredService<IOptions<InstanceNameConfig>>().Value.Name,
                sp.GetRequiredService<IOptions<QueueMonitoringConfig>>(),
                sp.GetRequiredService<IOptions<CentralPerformanceMonitorConfig>>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IStatusTrackerService>()));


            _interestScoreService = new InterestScoreService(interestLoggerMock.Object, interestScoreOptions);

            // Register the mocks and options required by KalshiAPIService
            services.AddScoped(p => apiLoggerMock.Object);
            services.AddScoped(p => scopeManagerMock.Object);
            services.AddScoped(p => statusTrackerMock.Object);
            services.AddScoped(p => _interestScoreService);
            services.AddSingleton<IOptions<KalshiConfig>>(kalshiOptions);

            var connectionString = ConfigurationHelper.BuildConnectionString(config);
            Assert.That(connectionString, Is.Not.Null.And.Not.Empty, "DefaultConnection string is missing in appsettings.json");

            var dataConfig = dataConfigOptions.Value;
            _dataConfig = dataConfig;

            // Create database context with proper parameters
            var dbContextLoggerMock = new Mock<ILogger<BacklashBotContext>>();
            services.AddScoped<IBacklashBotContext>(provider => new BacklashBotContext(connectionString, dbContextLoggerMock.Object, dataConfig));

            // Create SqlDataService with proper parameters
            _sqlLoggerMock = new Mock<ILogger<SqlDataService>>();
            var performanceMetricsReceivers = new List<ISqlDataServicePerformanceMetrics>();
            _sqlDataService = new SqlDataService(connectionString, _sqlLoggerMock.Object, dataConfig, performanceMetricsReceivers);

            _serviceProvider = services.BuildServiceProvider();
            _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var centralPerformanceMonitor = (CentralPerformanceMonitor)_serviceProvider.GetRequiredService<ICentralPerformanceMonitor>();

            _snapshotPeriodHelper = new SnapshotPeriodHelper(snapshotPeriodHelperOptions.Value);

            _snapshotService = new TradingSnapshotService(snapshotLoggerMock.Object, _tradingSnapshotServiceOptions, _scopeFactory, centralPerformanceMonitor);
            _snapshotGroupHelper = new SnapshotGroupHelper(_scopeFactory, _snapshotPeriodHelper, _dataStorageConfig, snapshotGroupHelperOptions, centralPerformanceMonitor, marketAnalysisLoggerMock.Object);
            _overnightService = new OvernightActivitiesHelper(overnightLoggerMock.Object, _snapshotGroupHelper, _dataStorageConfig, _sqlDataService, null);

            _dbContext = new BacklashBotContext(connectionString, dbContextLoggerMock.Object, dataConfig);
        }


        /// <summary>
        /// Test method that executes the complete overnight task workflow.
        /// This includes market data refresh, interest score calculations, snapshot imports,
        /// market cleanup, and snapshot group generation.
        /// </summary>
        [Test]
        [Explicit]
        public async Task ExecuteOvernightTasks()
        {
            TestContext.Out.WriteLine("Starting execution of overnight tasks workflow...");
            TestContext.Out.WriteLine("This includes market data refresh, interest score calculations, snapshot imports, market cleanup, and snapshot group generation.");

            var scopeFactory = _serviceProvider!.GetRequiredService<IServiceScopeFactory>();

            try
            {
                await _overnightService.RunOvernightTasks(scopeFactory);
                TestContext.Out.WriteLine("Overnight tasks completed successfully.");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Overnight tasks execution failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test method that generates snapshot groups from raw market data.
        /// This process organizes snapshots into valid time periods for analysis,
        /// filtering out invalid data and creating structured groups for trading evaluation.
        /// </summary>
        [Test]
        [Explicit]
        public async Task GenerateSnapshotGroups()
        {
            TestContext.Out.WriteLine("Starting generation of snapshot groups from raw market data...");
            TestContext.Out.WriteLine("This organizes snapshots into valid time periods, filters invalid data, and creates structured groups for trading evaluation.");

            try
            {
                await _snapshotGroupHelper.GenerateSnapshotGroups();
                TestContext.Out.WriteLine("Snapshot groups generation completed successfully.");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Snapshot groups generation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test method that removes markets from the database that have ended
        /// but were never recorded with snapshot data. This cleanup operation
        /// helps maintain database integrity by removing stale market entries.
        /// </summary>
        [Test]
        [Explicit]
        public async Task DeleteUnrecordedMarkets()
        {
            TestContext.Out.WriteLine("Starting cleanup of unrecorded markets from database...");
            TestContext.Out.WriteLine("This removes markets that have ended but were never recorded with snapshot data to maintain database integrity.");

            try
            {
                await _overnightService.DeleteUnrecordedMarkets(_scopeFactory, new CancellationToken());
                TestContext.Out.WriteLine("Unrecorded markets cleanup completed successfully.");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Unrecorded markets cleanup failed: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Test method that removes processed snapshot data from disk storage.
        /// This cleanup operation deletes candlestick data files for markets that have
        /// completed their processing lifecycle, freeing up storage space.
        /// </summary>
        [Test]
        [Explicit]
        public async Task DeleteProcessedSnapshots()
        {
            TestContext.Out.WriteLine("Starting cleanup of processed snapshot data from disk storage...");
            TestContext.Out.WriteLine("This deletes candlestick data files for markets that have completed processing, freeing up storage space.");

            try
            {
                await _overnightService.DeleteProcessedSnapshots(_scopeFactory, new CancellationToken());
                TestContext.Out.WriteLine("Processed snapshots cleanup completed successfully.");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Processed snapshots cleanup failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test method that cleans up candlestick data folders for closed markets.
        /// This removes directories for markets that have ended to free up storage space.
        /// </summary>
        [Test]
        [Explicit]
        public async Task CleanUpClosedMarketCandlesticks()
        {
            TestContext.Out.WriteLine("Starting cleanup of candlestick data folders for closed markets...");
            TestContext.Out.WriteLine("This removes directories for markets that have ended to free up storage space.");

            try
            {
                await _overnightService.CleanUpClosedMarketCandlesticks(_scopeFactory, new CancellationToken());
                TestContext.Out.WriteLine("Closed market candlesticks cleanup completed successfully.");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Closed market candlesticks cleanup failed: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Cleans up test resources by disposing of the database context and service provider.
        /// Ensures proper resource cleanup after each test execution.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _sqlDataService.Dispose();
            _dbContext.Dispose();
            _serviceProvider!.Dispose();
        }
    }
}