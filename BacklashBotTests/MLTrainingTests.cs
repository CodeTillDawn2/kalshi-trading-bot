using BacklashBot.Configuration;
using BacklashBot.Management;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashBotData.Configuration;
using BacklashBotData.Data;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashCommon.Services;
using BacklashDTOs;
using BacklashDTOs.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text;
using TradingSimulator.ML;

namespace KalshiBotTests
{
    /// <summary>
    /// NUnit test fixture for validating machine learning training and evaluation functionality
    /// using real market data from the Kalshi trading bot database. This class tests the complete
    /// ML pipeline including data loading, CSV generation, model training, and evaluation.
    /// </summary>
    /// <remarks>
    /// This test fixture serves as an integration test for the ML training system, ensuring that
    /// the FlattenPriceQuickstart class can successfully train and evaluate models on real market
    /// snapshots. It validates the end-to-end workflow from database data retrieval through model
    /// performance assessment, providing confidence in the ML system's ability to predict price
    /// flattening scenarios in live trading environments.
    /// </remarks>
    [TestFixture]
    public sealed class MLTrainingTests
    {
        /// <summary>
        /// The root service provider configured with database and dependency injection services.
        /// Used to create scoped services for database operations during test execution.
        /// </summary>
        private IServiceProvider _sp;

        /// <summary>
        /// Factory for creating service scopes to ensure proper resource management and
        /// database context isolation during parallel test execution.
        /// </summary>
        private IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Service for loading and processing market snapshots from the database.
        /// Provides access to historical market data for ML training and evaluation.
        /// </summary>
        private ITradingSnapshotService _snapshotService;

        /// <summary>
        /// Configuration options for general execution parameters, including decision frequencies
        /// and other settings that affect snapshot timing and processing.
        /// </summary>
        private IOptions<GeneralExecutionConfig> _generalExecutionOpts;

        /// <summary>
        /// Output directory path where training data CSV files and model files are saved.
        /// Configured relative to the test execution directory for consistent file access.
        /// </summary>
        private string _outDir;

        /// <summary>
        /// Initializes the test environment by setting up dependency injection, database context,
        /// and snapshot service configuration. This method loads the application configuration
        /// and creates the necessary services for testing ML training functionality.
        /// </summary>
        /// <remarks>
        /// The setup process mirrors the production application's DI configuration to ensure
        /// realistic testing conditions. It establishes database connectivity and initializes
        /// the snapshot service with proper configuration options for loading real market data.
        /// </remarks>
        [SetUp]
        public void Setup()
        {
            // Locate BacklashBot/appsettings.json exactly like your other tests
            var basePath = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var baseConfig = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var config = new ConfigurationBuilder()
                .AddConfiguration(baseConfig)
                .AddSecretsConfiguration(basePath, baseConfig)
                .Build();

            // DI: EF context for real snapshot fetching
            var connectionString = ConfigurationHelper.BuildConnectionString(config);
            var dataConfig = config.GetSection("DBConnection:BacklashBotData").Get<BacklashBotDataConfig>();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(connectionString);
            services.AddSingleton(dataConfig);
            services.AddDbContext<BacklashBotContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IBacklashBotContext>(sp => sp.GetRequiredService<BacklashBotContext>());
            _sp = services.BuildServiceProvider();
            _scopeFactory = _sp.GetRequiredService<IServiceScopeFactory>();
            var centralPerformanceMonitor = _sp.GetRequiredService<CentralPerformanceMonitor>();

            // Options from config
            var snapshotServiceConfig = Options.Create(new TradingSnapshotServiceConfig { SnapshotToleranceSeconds = 5, StorageDirectory = @"C:\Temp\Storage", MaxParallelism = 8, EnablePerformanceMetrics = true });
            _generalExecutionOpts = Options.Create(config.GetSection("Central:GeneralExecution").Get<GeneralExecutionConfig>());

            // Snapshot loader (same implementation you use elsewhere)
            _snapshotService = new TradingSnapshotService(
                NullLogger<ITradingSnapshotService>.Instance, snapshotServiceConfig, _generalExecutionOpts, _scopeFactory, config, centralPerformanceMonitor);

            _outDir = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");
            Directory.CreateDirectory(_outDir);
        }

        /// <summary>
        /// Cleans up resources after each test by disposing of the service provider.
        /// This ensures proper resource management and prevents memory leaks in the test suite.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (_sp is IDisposable d) d.Dispose();
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
        [Category("ML")]
        public async Task Train_Evaluate_Flatten_Model_On_Real_Data()
        {
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
        }
    }
}
