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
using TradingSimulator.Strategies;
using TradingStrategies.ML.SpikePrediction;


namespace BacklashBotTasks
{
    /// <summary>
    /// Test fixture class for machine learning tasks.
    /// This class provides testing capabilities for ML model training and evaluation.
    /// </summary>
    [TestFixture]
    public class MLTasks
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


        /// <summary>
        /// Initializes the test fixture by setting up dependency injection services,
        /// configuring application settings, and preparing mock objects for testing.
        /// This method creates a comprehensive service provider with all required dependencies
        /// for executing ML tasks.
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
            services.AddScoped<IKalshiAPIService, KalshiAPIService>(); // Register with interface
            services.AddScoped<IServiceFactory, ServiceFactory>();
            services.AddScoped<CentralPerformanceMonitor>();
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

            var tempProvider = services.BuildServiceProvider();

            _tradingSnapshotServiceOptions = tempProvider.GetRequiredService<IOptions<TradingSnapshotServiceConfig>>();
            _dataStorageConfig = tempProvider.GetRequiredService<IOptions<DataStorageConfig>>();
            var kalshiOptions = tempProvider.GetRequiredService<IOptions<KalshiConfig>>();
            var interestScoreOptions = tempProvider.GetRequiredService<IOptions<InterestScoreConfig>>();
            var dataConfigOptions = tempProvider.GetRequiredService<IOptions<BacklashBotDataConfig>>();
            var snapshotPeriodHelperOptions = tempProvider.GetRequiredService<IOptions<SnapshotPeriodHelperConfig>>();
            var snapshotGroupHelperOptions = tempProvider.GetRequiredService<IOptions<SnapshotGroupHelperConfig>>();

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
            var centralPerformanceMonitor = _serviceProvider.GetRequiredService<CentralPerformanceMonitor>();

            _snapshotPeriodHelper = new SnapshotPeriodHelper(snapshotPeriodHelperOptions.Value);

            _snapshotGroupHelper = new SnapshotGroupHelper(_scopeFactory, _snapshotPeriodHelper, _snapshotService, _dataStorageConfig, snapshotGroupHelperOptions, centralPerformanceMonitor, marketAnalysisLoggerMock.Object);
            _overnightService = new OvernightActivitiesHelper(overnightLoggerMock.Object, _snapshotGroupHelper, _dataStorageConfig, _sqlDataService, null);
            _snapshotService = new TradingSnapshotService(snapshotLoggerMock.Object, _tradingSnapshotServiceOptions, _scopeFactory, centralPerformanceMonitor);

            _dbContext = new BacklashBotContext(connectionString, dbContextLoggerMock.Object, dataConfig);
        }

        private IConfigurationRoot config;

        /// <summary>
        /// Test method that creates and trains the spike prediction ML model.
        /// This method loads historical market data, trains the model using ML.NET,
        /// evaluates performance, and saves the trained model to disk.
        /// </summary>
        [Test]
        [Explicit]
        public async Task CreateSpikePredictionModel()
        {
            TestContext.Out.WriteLine("Starting spike prediction model creation...");
            TestContext.Out.WriteLine("This loads historical market data, trains the ML model, evaluates performance, and saves the trained model to disk.");

            // Get available market tickers from database
            var markets = await _dbContext.GetMarkets();
            var marketTickers = markets
                .Where(m => m.market_ticker != null)
                .Select(m => m.market_ticker!)
                .ToList();

            if (!marketTickers.Any())
            {
                TestContext.Out.WriteLine("No market tickers found in database. Skipping model creation.");
                return;
            }

            TestContext.Out.WriteLine($"Found {marketTickers.Count} markets for training data");

            // Create model configuration
            var config = new SpikePredictionConfig
            {
                SpikeThreshold = 0.15, // 15% price spike threshold
                PredictionWindow = TimeSpan.FromMinutes(10),
                LagMinutes = 5,
                ModelPath = "spike_prediction_model.zip",
                PredictionThreshold = 0.7,
                NumberOfTrees = 100,
                MinimumLeafSize = 10,
                MaximumDepth = 10
            };

            // Create model instance
            var model = new SpikePredictionModel(_spikeLoggerMock.Object, _snapshotService, config);

            try
            {
                // Load training data
                TestContext.Out.WriteLine("Loading training data...");
                var trainingData = await model.LoadTrainingDataAsync(
                    marketTickers,
                    DateTime.UtcNow.AddDays(-30), // Last 30 days
                    DateTime.UtcNow);

                if (!trainingData.Any())
                {
                    TestContext.Out.WriteLine("No training data available. Skipping model training.");
                    return;
                }

                TestContext.Out.WriteLine($"Loaded {trainingData.Count} training samples");

                // Split data for training and testing
                var splitIndex = (int)(trainingData.Count * 0.8);
                var trainData = trainingData.Take(splitIndex).ToList();
                var testData = trainingData.Skip(splitIndex).ToList();

                TestContext.Out.WriteLine($"Training set: {trainData.Count} samples, Test set: {testData.Count} samples");

                // Train model
                TestContext.Out.WriteLine("Training model...");
                model.TrainModel(trainData);

                // Evaluate model
                TestContext.Out.WriteLine("Evaluating model performance...");
                model.EvaluateModel(testData);

                // Save model
                TestContext.Out.WriteLine("Saving trained model...");
                model.SaveModel();

                TestContext.Out.WriteLine("Spike prediction model creation completed successfully!");
                TestContext.Out.WriteLine($"Model saved to: {config.ModelPath}");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Error during model creation: {ex.Message}");
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