using BacklashBot.Configuration;
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
using BacklashDTOs.Data;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotData.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using TradingSimulator.ML;
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
        private string _outDir;


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

            // Create performance monitor mock
            var performanceMonitorMock = new Mock<IPerformanceMonitor>();

            _interestScoreService = new InterestScoreService(interestLoggerMock.Object, interestScoreOptions, performanceMonitorMock.Object);

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
            services.AddScoped<IBacklashBotContext>(provider => new BacklashBotContext(connectionString, dbContextLoggerMock.Object, dataConfig, provider.GetRequiredService<IPerformanceMonitor>()));

            // Create SqlDataService with proper parameters
            _sqlLoggerMock = new Mock<ILogger<SqlDataService>>();
            _sqlDataService = new SqlDataService(connectionString, _sqlLoggerMock.Object, dataConfig, performanceMonitorMock.Object);

            _serviceProvider = services.BuildServiceProvider();
            _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var centralPerformanceMonitor = _serviceProvider.GetRequiredService<CentralPerformanceMonitor>();

            _snapshotPeriodHelper = new SnapshotPeriodHelper(snapshotPeriodHelperOptions.Value);

            _snapshotGroupHelper = new SnapshotGroupHelper(_scopeFactory, _snapshotPeriodHelper, _dataStorageConfig, snapshotGroupHelperOptions, centralPerformanceMonitor, marketAnalysisLoggerMock.Object);
            _overnightService = new OvernightActivitiesHelper(overnightLoggerMock.Object, _snapshotGroupHelper, _dataStorageConfig, _sqlDataService, performanceMonitorMock.Object);
            _snapshotService = new TradingSnapshotService(snapshotLoggerMock.Object, _tradingSnapshotServiceOptions, _scopeFactory, centralPerformanceMonitor);

            _dbContext = new BacklashBotContext(connectionString, dbContextLoggerMock.Object, dataConfig, performanceMonitorMock.Object);

            _outDir = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");
            Directory.CreateDirectory(_outDir);
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
        /// End-to-end test that validates the complete ML training pipeline for price flattening prediction.
        /// Loads real market data, trains a LightGBM model, evaluates performance, and makes predictions.
        /// </summary>
        /// <remarks>
        /// This test serves as the primary validation for the ML components used in production trading.
        /// It ensures that:
        /// - Real market data can be loaded and processed correctly
        /// - The ML model can be trained on historical data with reasonable performance
        /// - Model evaluation metrics are within acceptable ranges
        /// - Predictions can be generated for new data points
        ///
        /// The test uses conservative hyperparameters suitable for initial validation and can be
        /// extended with additional test cases for different market conditions or model configurations.
        /// </remarks>
        [Test]
        [Explicit]
        [Category("ML")]
        public async Task Train_Evaluate_Flatten_Model_On_Real_Data()
        {
            TestContext.Out.WriteLine("Testing end-to-end ML training pipeline for price flattening prediction.");
            // 1) Load multiple markets from your real DB
            var markets = await LoadRealMarketsAsync(minRecordedHours: 1.0);

            // 2) Write a training CSV for the quickstart
            var csvPath = WriteCsvForQuickstart(markets, "ml_training_snapshots.csv");

            // 3) Train + Evaluate using the LightGBM quickstart
            var modelPath = Path.Combine(_outDir, "flatten_price_model.zip");
            var cfg = new FlattenPriceConfig
            {
                HorizonMinutes = 15,     // lookahead for peak
                MinSlopeYes = 0.0,       // include all rows (tune later)
                MinTopVelYes = 0.0,
                UseYesSideOnly = true,   // start simple
                LabelIsDelta = true,     // learn delta = peak - current
                MinRowsToTrain = 200,    // safety
                Seed = 1337,
                NumberOfLeaves = 64,
                MinExamplesPerLeaf = 10,
                NumIterations = 300,
                LearningRate = 0.05
            };

            var summary = FlattenPriceQuickstart.TrainFromCsv(csvPath, modelPath, cfg);
            TestContext.Out.WriteLine($"TrainRows={summary.TrainRows}, MAE={summary.MAE:F3}, RMSE={summary.RMSE:F3}, R2={summary.R2:F3}");

            // 4) Sanity: Evaluate on the same CSV (or point at another CSV if you prefer)
            var eval = FlattenPriceQuickstart.EvaluateFromCsv(csvPath, modelPath, cfg);
            TestContext.Out.WriteLine($"[Eval] MAE={eval.MAE:F3}, RMSE={eval.RMSE:F3}, R2={eval.R2:F3}");

            // 5) Optional: Predict for the latest row
            var latest = FlattenPriceQuickstart.PredictLatestFromCsv(csvPath, modelPath, cfg);
            TestContext.Out.WriteLine($"Latest: cur={latest.CurrentYes:F2}, ?={latest.PredictedDelta:F2}, flat={latest.PredictedFlatten:F2}");
            TestContext.Out.WriteLine("Result: ML training pipeline completed successfully.");
        }

        /// <summary>
        /// Loads real market data from the database and consolidates it into time-series snapshots per market.
        /// Filters markets by recording duration and selects the most comprehensive datasets for ML training.
        /// </summary>
        /// <param name="maxMarkets">Maximum number of markets to load (default: 5).</param>
        /// <param name="minRecordedHours">Minimum hours of recorded data required for a market to be included (default: 1.0).</param>
        /// <returns>A list of read-only lists containing consolidated market snapshots, one per selected market.</returns>
        /// <remarks>
        /// This method queries the database for snapshot groups, filters for sufficiently long recordings,
        /// and consolidates multiple recording sessions per market into single time-ordered sequences.
        /// Markets are selected based on total recording time to ensure comprehensive training data.
        /// Only markets with at least 50 snapshots are returned to avoid noisy or incomplete series.
        /// </remarks>
        private async Task<List<IReadOnlyList<MarketSnapshot>>> LoadRealMarketsAsync(
            int maxMarkets = 5, double minRecordedHours = 1.0)
        {
            var results = new List<IReadOnlyList<MarketSnapshot>>(maxMarkets);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

            // Get all snapshot groups and filter to sufficiently long recordings
            var groups = await db.GetSnapshotGroups().ConfigureAwait(false);
            var good = groups.Where(g => (g.EndTime - g.StartTime).TotalHours >= minRecordedHours).ToList();

            // Choose up to N markets with the most coverage
            var markets = good
                .GroupBy(g => g.MarketTicker)
                .OrderByDescending(grp => grp.Sum(x => (x.EndTime - x.StartTime).TotalMinutes))
                .Take(maxMarkets)
                .Select(grp => new { Ticker = grp.Key, Groups = grp.OrderBy(x => x.StartTime).ToList() })
                .ToList();

            foreach (var m in markets)
            {
                var allDto = new List<SnapshotDTO>();
                foreach (var g in m.Groups)
                {
                    var snaps = await db.GetSnapshots(marketTicker: g.MarketTicker, startDate: g.StartTime, endDate: g.EndTime).ConfigureAwait(false);
                    allDto.AddRange(snaps);
                }

                allDto = allDto.OrderBy(x => x.SnapshotDate).ToList();

                // Convert SnapshotDTO -> MarketSnapshot using your snapshot loader
                var cache = await _snapshotService.LoadManySnapshots(allDto).ConfigureAwait(false);
                var ms = cache.SelectMany(kvp => kvp.Value)
                              .Where(s => s != null && s.Timestamp > DateTime.MinValue)
                              .OrderBy(s => s.Timestamp)
                              .ToList();

                if (ms.Count > 50) // avoid tiny/noisy series
                    results.Add(ms);
            }

            return results;
        }

        /// <summary>
        /// Converts market snapshot data into CSV format compatible with the FlattenPriceQuickstart ML training pipeline.
        /// Writes all market series to a single CSV file with standardized column headers and null-safe numeric conversion.
        /// </summary>
        /// <param name="markets">Collection of market snapshot series to write to CSV.</param>
        /// <param name="fileName">Name of the output CSV file (will be placed in the test output directory).</param>
        /// <returns>Full path to the created CSV file.</returns>
        /// <remarks>
        /// This method formats market data according to the expected input schema for FlattenPriceQuickstart,
        /// including timestamp formatting, null-safe float conversion, and proper CSV escaping.
        /// All market series are concatenated into a single training file, allowing the ML model to learn
        /// patterns across different markets while maintaining temporal ordering within each series.
        /// </remarks>
        private string WriteCsvForQuickstart(IEnumerable<IReadOnlyList<MarketSnapshot>> markets, string fileName)
        {
            var path = Path.Combine(_outDir, fileName);
            using var sw = new StreamWriter(path, false, new UTF8Encoding(false));

            // Header (matches CsvLoader mapping in FlattenPriceQuickstart.cs)
            sw.WriteLine(string.Join(",",
                "Timestamp",
                "BestYesBid", "BestNoBid",
                "VelocityPerMinute_Top_Yes_Bid", "LevelCount_Top_Yes_Bid",
                "VelocityPerMinute_Bottom_Yes_Bid", "LevelCount_Bottom_Yes_Bid",
                "VelocityPerMinute_Top_No_Bid", "LevelCount_Top_No_Bid",
                "VelocityPerMinute_Bottom_No_Bid", "LevelCount_Bottom_No_Bid",
                "YesSpread", "DepthAtBestYesBid", "YesBidSlopePerMinute_Short", "NoBidSlopePerMinute_Short"
                , "YesBidSlopePerMinute_Medium", "NoBidSlopePerMinute_Medium"));

            foreach (var series in markets)
            {
                foreach (var s in series)
                {
                    // Null-safe numeric access (assume zero if missing)
                    float F(double? v) => v.HasValue ? (float)v.Value : 0f;

                    sw.WriteLine(string.Join(",",
                        s.Timestamp.ToString("o"),
                        s.BestYesBid,
                        s.BestNoBid,
                        F(s.VelocityPerMinute_Top_Yes_Bid), s.LevelCount_Top_Yes_Bid,
                        F(s.VelocityPerMinute_Bottom_Yes_Bid), s.LevelCount_Bottom_Yes_Bid,
                        F(s.VelocityPerMinute_Top_No_Bid), s.LevelCount_Top_No_Bid,
                        F(s.VelocityPerMinute_Bottom_No_Bid), s.LevelCount_Bottom_No_Bid,
                        s.YesSpread,
                        s.DepthAtBestYesBid,
                        F(s.YesBidSlopePerMinute_Short),
                        F(s.NoBidSlopePerMinute_Short),
                        F(s.YesBidSlopePerMinute_Medium),
                        F(s.NoBidSlopePerMinute_Medium)
                        ));
                }
            }
            return path;
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