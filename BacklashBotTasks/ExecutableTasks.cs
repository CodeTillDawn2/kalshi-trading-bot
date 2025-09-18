using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotData.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using BacklashDTOs.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashCommon.Services;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashBot.Helpers;
using BacklashDTOs;
using BacklashBot.State.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingSimulator.Strategies;
using TradingStrategies.Classification;
using TradingStrategies.Configuration;
using BacklashBot.Management;
using BacklashBotData.Data.Interfaces;
using BacklashBotData.Data;


namespace KalshiBotTasks
{
    /// <summary>
    /// Test fixture class for executing and validating trading simulator tasks.
    /// This class provides comprehensive testing capabilities for overnight activities,
    /// snapshot processing, market data validation, and discrepancy reporting.
    /// It serves as an integration test suite for the trading bot's core operational workflows.
    /// </summary>
    [TestFixture]
    public class ExecutableTasks
    {
        private TradingSnapshotService _snapshotService;
        private OvernightActivitiesHelper _overnightService;
        private IInterestScoreService _interestScoreService;
        private SnapshotPeriodHelper _snapshotPeriodHelper;
        private IOptions<ExecutionConfig> _executionConfig;
        private Mock<ILogger<SqlDataService>> _sqlLoggerMock;
        private MarketAnalysisHelper _marketAnalysisHelper;
        private IBacklashBotContext _dbContext;
        private ServiceProvider? _serviceProvider;
        private IOptions<SnapshotConfig> _snapshotOptions;
        private int _missingOrderbookCount;
        private int _overlappingPriceCount;
        private int _rateDiscrepancyCount; // New counter
        private readonly Dictionary<string, List<MissingOrderbookMetadata>> _missingOrderbooks = new();
        private readonly Dictionary<string, List<OverlappingPriceMetadata>> _overlappingPrices = new();
        private readonly Dictionary<string, List<RateDiscrepancyMetadata>> _rateDiscrepancies = new(); // New dictionary
        private IConfigurationRoot config;
        private IServiceScopeFactory _scopeFactory;
        private SqlDataService _sqlDataService;


        /// <summary>
        /// Metadata container for tracking snapshots with missing orderbook data.
        /// Used to identify and report market snapshots that lack essential orderbook information.
        /// </summary>
        private class MissingOrderbookMetadata
        {
            /// <summary>
            /// The market ticker symbol where the missing orderbook was detected.
            /// </summary>
            public string? MarketTicker { get; set; }

            /// <summary>
            /// The timestamp when the snapshot was taken.
            /// </summary>
            public DateTime SnapshotDate { get; set; }

            /// <summary>
            /// The snapshot version number, if available.
            /// </summary>
            public int? SnapshotVersion { get; set; }
        }

        /// <summary>
        /// Metadata container for tracking price overlaps in market snapshots.
        /// Used to identify instances where Yes and No bid prices are identical, indicating potential data issues.
        /// </summary>
        private class OverlappingPriceMetadata
        {
            /// <summary>
            /// The market ticker symbol where the price overlap was detected.
            /// </summary>
            public string? MarketTicker { get; set; }

            /// <summary>
            /// The timestamp when the snapshot was taken.
            /// </summary>
            public DateTime SnapshotDate { get; set; }

            /// <summary>
            /// The overlapping price value where BestYesBid equals BestNoBid.
            /// </summary>
            public int OverlappingPrice { get; set; }
        }

        /// <summary>
        /// Metadata container for tracking rate discrepancies in market snapshots.
        /// Used to identify inconsistencies between velocity and rate calculations for trading activity.
        /// </summary>
        private class RateDiscrepancyMetadata
        {
            /// <summary>
            /// The market ticker symbol where the rate discrepancy was detected.
            /// </summary>
            public string? MarketTicker { get; set; }

            /// <summary>
            /// The timestamp when the snapshot was taken.
            /// </summary>
            public DateTime SnapshotDate { get; set; }

            /// <summary>
            /// The sum of velocity values for Yes and No bid positions.
            /// </summary>
            public double VelocitySum { get; set; }

            /// <summary>
            /// The sum of order and trade volume rates.
            /// </summary>
            public double RateSum { get; set; }

            /// <summary>
            /// The absolute difference between velocity sum and rate sum.
            /// </summary>
            public double Difference { get; set; }
        }

        /// <summary>
        /// Initializes the test fixture by setting up dependency injection services,
        /// configuring application settings, and preparing mock objects for testing.
        /// This method creates a comprehensive service provider with all required dependencies
        /// for executing trading simulator tasks and overnight activities.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: false)
                .Build();
            this.config = configuration;

            this.config = configuration;

            var snapshotConfig = config.GetSection("Snapshots").Get<SnapshotConfig>()!;
            var tradingConfig = config.GetSection("TradingConfig").Get<TradingConfig>()!;
            var kalshiConfig = config.GetSection("Kalshi").Get<KalshiConfig>()!; // Add this for KalshiConfig
            _snapshotOptions = Options.Create(snapshotConfig);
            _executionConfig = Options.Create(config.GetSection("Execution").Get<ExecutionConfig>()!);
            var kalshiOptions = Options.Create(kalshiConfig); // Create options for KalshiConfig

            var snapshotLoggerMock = new Mock<ILogger<TradingSnapshotService>>();
            var apiLoggerMock = new Mock<ILogger<IKalshiAPIService>>(); // Align to IKalshiAPIService to match constructor
            var overnightLoggerMock = new Mock<ILogger<OvernightActivitiesHelper>>();
            var interestLoggerMock = new Mock<ILogger<InterestScoreService>>();
            var tradingLoggerMock = new Mock<ILogger<TradingStrategy<MarketSnapshot>>>();
            var marketAnalysisLoggerMock = new Mock<ILogger<MarketAnalysisHelper>>();

            // Add mocks for missing dependencies
            var scopeManagerMock = new Mock<IScopeManagerService>();
            var statusTrackerMock = new Mock<IStatusTrackerService>();

            var services = new ServiceCollection();
            services.AddScoped<IBacklashBotContext>(provider => new BacklashBotContext(config));
            services.AddScoped<IKalshiAPIService, KalshiAPIService>(); // Register with interface
            services.AddScoped<IServiceFactory, ServiceFactory>();
            services.AddScoped<CentralPerformanceMonitor>();
            services.AddSingleton<IConfiguration>(config);

            // Create InterestScoreConfig options for testing
            var interestScoreConfig = new InterestScoreConfig
            {
                CacheDurationHours = 6,
                EnablePerformanceMetrics = true,
                MaxPerformanceMetricsHistory = 1000
            };
            var interestScoreOptions = Options.Create(interestScoreConfig);

            _interestScoreService = new InterestScoreService(interestLoggerMock.Object, interestScoreOptions);

            // Register the mocks and options required by KalshiAPIService
            services.AddScoped(p => apiLoggerMock.Object);
            services.AddScoped(p => scopeManagerMock.Object);
            services.AddScoped(p => statusTrackerMock.Object);
            services.AddScoped(p => _interestScoreService);
            services.AddSingleton<IOptions<KalshiConfig>>(kalshiOptions);


            var connectionString = config.GetConnectionString("DefaultConnection");
            Assert.That(connectionString, Is.Not.Null.And.Not.Empty, "DefaultConnection string is missing in appsettings.local.json");
            _sqlLoggerMock = new Mock<ILogger<SqlDataService>>();
            _sqlDataService = new SqlDataService(config, _sqlLoggerMock.Object);

            _serviceProvider = services.BuildServiceProvider();
            _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var centralPerformanceMonitor = _serviceProvider.GetRequiredService<CentralPerformanceMonitor>();

            _snapshotPeriodHelper = new SnapshotPeriodHelper(_snapshotOptions.Value);
            _marketAnalysisHelper = new MarketAnalysisHelper(_scopeFactory, _snapshotPeriodHelper, _snapshotService, _executionConfig, null, marketAnalysisLoggerMock.Object);
            _overnightService = new OvernightActivitiesHelper(overnightLoggerMock.Object, _interestScoreService, _marketAnalysisHelper, _executionConfig, _sqlDataService);
            _snapshotService = new TradingSnapshotService(snapshotLoggerMock.Object, _snapshotOptions, Options.Create(tradingConfig), _scopeFactory, this.config, centralPerformanceMonitor);

            _dbContext = new BacklashBotContext(config);
            _missingOrderbookCount = 0;
            _overlappingPriceCount = 0;
            _rateDiscrepancyCount = 0; // Initialize new counter
        }


        /// <summary>
        /// Test method that executes the complete overnight task workflow.
        /// This includes market data refresh, interest score calculations, snapshot imports,
        /// market cleanup, and snapshot group generation.
        /// </summary>
        [Test]
        public async Task ExecuteOvernightTasks()
        {
            var scopeFactory = _serviceProvider!.GetRequiredService<IServiceScopeFactory>();
            await _overnightService.RunOvernightTasks(scopeFactory);
        }

        /// <summary>
        /// Test method that generates snapshot groups from raw market data.
        /// This process organizes snapshots into valid time periods for analysis,
        /// filtering out invalid data and creating structured groups for trading evaluation.
        /// </summary>
        [Test]
        public async Task GenerateSnapshotGroups()
        {
            await _marketAnalysisHelper.GenerateSnapshotGroups();
        }

        /// <summary>
        /// Test method that removes markets from the database that have ended
        /// but were never recorded with snapshot data. This cleanup operation
        /// helps maintain database integrity by removing stale market entries.
        /// </summary>
        [Test]
        public async Task DeleteUnrecordedMarkets()
        {
            await _overnightService.DeleteUnrecordedMarkets(_scopeFactory, new CancellationToken());
        }


        /// <summary>
        /// Test method that removes processed snapshot data from disk storage.
        /// This cleanup operation deletes candlestick data files for markets that have
        /// completed their processing lifecycle, freeing up storage space.
        /// </summary>
        [Test]
        public async Task DeleteProcessedSnapshots()
        {
            await _overnightService.DeleteProcessedSnapshots(_scopeFactory, new CancellationToken());
        }


        /// <summary>
        /// Test method that performs comprehensive snapshot upgrade and validation processing.
        /// This method processes unvalidated snapshots in batches, performs schema upgrades,
        /// validates data integrity, and generates discrepancy reports. It handles market
        /// categorization, JSON serialization updates, and comprehensive error tracking.
        /// </summary>
        [Test]
        public async Task UpgradeSnapshots()
        {
            bool performValidation = false; // Flag to enable/disable validation
            bool saveUpdatedJson = true;
            var startTime = DateTime.UtcNow;
            int batchSize = 2000;
            int totalProcessed = 0;
            int totalValidated = 0;
            int totalErrors = 0;

            TestContext.Out.WriteLine($"Starting snapshot upgrade process at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            while (true)
            {
                var allUnvalidatedSnapshots = await _dbContext.GetSnapshots(isValidated: false, endDate: startTime, MaxRecords: batchSize);
                int recordsReturned = allUnvalidatedSnapshots.Count;

                if (recordsReturned == 0)
                {
                    TestContext.Out.WriteLine("No more unvalidated snapshots found. Upgrade process complete.");
                    break;
                }

                TestContext.Out.WriteLine($"Processing batch of {recordsReturned} snapshots...");

                var marketGroups = allUnvalidatedSnapshots
                    .GroupBy(x => x.MarketTicker)
                    .OrderBy(g => g.Key)
                    .ToList();

                foreach (var marketGroup in marketGroups)
                {
                    string marketName = marketGroup.Key;
                    var snapshots = marketGroup.OrderBy(x => x.SnapshotDate).ToList();

                    TestContext.Out.WriteLine($"Processing market: {marketName} ({snapshots.Count} snapshots)");

                    try
                    {
                        using var dbContext = new BacklashBotContext(config);

                        // Get market info for category
                        string? marketCategory = null;
                        var market = await dbContext.GetMarketByTicker(marketName);
                        if (market?.Event == null)
                        {
                            TestContext.Out.WriteLine($"Warning: Market {marketName} has no associated event. Skipping.");
                            totalErrors += snapshots.Count;
                            continue;
                        }

                        marketCategory = market.Event.category;
                        if (string.IsNullOrEmpty(market.category))
                        {
                            market.category = marketCategory;
                            await dbContext.AddOrUpdateMarket(market);
                        }

                        // Load snapshots into cache
                        var cacheSnapshotDict = await _snapshotService.LoadManySnapshots(snapshots, true);
                        if (cacheSnapshotDict == null)
                        {
                            TestContext.Out.WriteLine($"Error: Failed to load snapshots for market {marketName}");
                            totalErrors += snapshots.Count;
                            continue;
                        }

                        // JSON serialization options for schema upgrade
                        var jsonOptions = new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            Converters = { new SimplifiedTupleConverter() }
                        };

                        var validatedSnapshots = new List<BacklashDTOs.Data.SnapshotDTO>();

                        foreach (var snapshotList in cacheSnapshotDict.Values)
                        {
                            foreach (var cacheSnapshot in snapshotList)
                            {
                                if (cacheSnapshot == null)
                                {
                                    TestContext.Out.WriteLine($"Warning: Null snapshot found for market {marketName}");
                                    totalErrors++;
                                    continue;
                                }

                                try
                                {
                                    bool isValid = true;

                                    if (performValidation)
                                    {
                                        var validationResult = SnapshotDiscrepancyValidator.ValidateDiscrepancies(cacheSnapshot);

                                        if (!validationResult.IsValid)
                                        {
                                            TestContext.Out.WriteLine($"Snapshot {cacheSnapshot.MarketTicker} at {cacheSnapshot.Timestamp:yyyy-MM-dd HH:mm:ss} failed validation:");
                                            if (validationResult.IsOrderbookMissing)
                                            {
                                                TestContext.Out.WriteLine("  - Missing orderbook data");
                                                LogMissingOrderbook(cacheSnapshot.MarketTicker!, cacheSnapshot.Timestamp);
                                            }
                                            if (validationResult.DoPricesOverlap)
                                            {
                                                TestContext.Out.WriteLine("  - Overlapping prices");
                                                LogOverlappingPrice(cacheSnapshot.MarketTicker!, cacheSnapshot.Timestamp, cacheSnapshot.BestYesBid);
                                            }
                                            if (validationResult.IsRateDiscrepancy)
                                            {
                                                double velocitySum = cacheSnapshot.VelocityPerMinute_Top_Yes_Bid + cacheSnapshot.VelocityPerMinute_Bottom_Yes_Bid;
                                                double rateSum = cacheSnapshot.OrderVolumePerMinute_YesBid + cacheSnapshot.TradeVolumePerMinute_Yes;
                                                double diff = Math.Abs(velocitySum - rateSum);
                                                TestContext.Out.WriteLine($"  - Rate discrepancy: Velocity={velocitySum:F2}, Rate={rateSum:F2}, Diff={diff:F2}");
                                                LogRateDiscrepancy(cacheSnapshot.MarketTicker!, cacheSnapshot.Timestamp, velocitySum, rateSum, diff);
                                            }
                                            isValid = false;
                                        }
                                    }

                                    if (isValid)
                                    {
                                        var snapshotToUpdate = snapshots.FirstOrDefault(x =>
                                            x.MarketTicker == cacheSnapshot.MarketTicker &&
                                            x.SnapshotDate == cacheSnapshot.Timestamp);

                                        if (snapshotToUpdate != null)
                                        {
                                            snapshotToUpdate.IsValidated = true;
                                            if (saveUpdatedJson)
                                            {
                                                // Schema upgrade: re-serialize to clean JSON and add market category
                                                cacheSnapshot.MarketCategory = marketCategory;
                                                snapshotToUpdate.RawJSON = JsonSerializer.Serialize(cacheSnapshot, jsonOptions);
                                            }
                                            validatedSnapshots.Add(snapshotToUpdate);
                                            totalValidated++;
                                        }
                                    }
                                    else
                                    {
                                        totalErrors++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    TestContext.Out.WriteLine($"Error processing snapshot for {marketName} at {cacheSnapshot.Timestamp}: {ex.Message}");
                                    totalErrors++;
                                }
                            }
                        }

                        // Bulk update validated snapshots
                        if (validatedSnapshots.Any())
                        {
                            await _dbContext.AddOrUpdateSnapshots(validatedSnapshots);
                            TestContext.Out.WriteLine($"Updated {validatedSnapshots.Count} snapshots for market {marketName}");
                        }

                        totalProcessed += snapshots.Count;
                    }
                    catch (Exception ex)
                    {
                        TestContext.Out.WriteLine($"Error processing market {marketName}: {ex.Message}");
                        totalErrors += snapshots.Count;
                    }
                }

                TestContext.Out.WriteLine($"Batch complete. Total processed: {totalProcessed}, Validated: {totalValidated}, Errors: {totalErrors}");
            }

            TestContext.Out.WriteLine($"Snapshot upgrade complete. Final stats - Processed: {totalProcessed}, Validated: {totalValidated}, Errors: {totalErrors}");

            // Export discrepancy reports if validation was performed
            if (performValidation)
            {
                await ExportDiscrepancyReport();
            }
        }

        /// <summary>
        /// Generates a detailed discrepancy report for a specific market.
        /// This method creates a comprehensive text file containing validation results,
        /// including overlapping prices, rate discrepancies, and missing orderbook data
        /// with detailed breakdowns by date and type.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol for the report.</param>
        /// <param name="validCount">Number of valid snapshots processed.</param>
        /// <param name="orderbookMissingCount">Number of snapshots with missing orderbook data.</param>
        /// <param name="overlappingPriceCount">Number of snapshots with overlapping prices.</param>
        /// <param name="rateDiscrepancyCount">Number of snapshots with rate discrepancies.</param>
        /// <param name="errorCount">Number of snapshots that failed processing.</param>
        /// <param name="missingOrderbooks">List of missing orderbook metadata.</param>
        /// <param name="overlappingPrices">List of overlapping price metadata.</param>
        /// <param name="rateDiscrepancies">List of rate discrepancy metadata.</param>
        private async Task GenerateMarketDiscrepancyReport(
            string marketTicker,
            int validCount,
            int orderbookMissingCount,
            int overlappingPriceCount,
            int rateDiscrepancyCount, // New parameter
            int errorCount,
            List<MissingOrderbookMetadata> missingOrderbooks,
            List<OverlappingPriceMetadata> overlappingPrices,
            List<RateDiscrepancyMetadata> rateDiscrepancies) // New parameter
        {
            var reportPath = Path.Combine(TestContext.CurrentContext.TestDirectory, $"DiscrepancyReport_{marketTicker}.txt");
            using var writer = new StreamWriter(reportPath, false);

            await writer.WriteLineAsync($"Discrepancy Report for {marketTicker}");
            await writer.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync(new string('-', 50));

            await writer.WriteLineAsync("Validation Summary");
            await writer.WriteLineAsync($"Valid Snapshots: {validCount}");
            await writer.WriteLineAsync($"Missing Orderbooks: {orderbookMissingCount}");
            await writer.WriteLineAsync($"Overlapping Prices: {overlappingPriceCount}");
            await writer.WriteLineAsync($"Rate Discrepancies: {rateDiscrepancyCount}"); // New summary line
            await writer.WriteLineAsync($"Errors: {errorCount}");
            await writer.WriteLineAsync(new string('-', 50));


            await writer.WriteLineAsync("Overlapping Price Discrepancies");
            await writer.WriteLineAsync(new string('=', 50));
            if (overlappingPrices.Any())
            {
                var dateGroups = overlappingPrices
                    .GroupBy(d => d.SnapshotDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key:yyyy-MM-dd}: {g.Count()} overlapping prices");
                await writer.WriteLineAsync("Overlapping Prices by Date:");
                await writer.WriteLineAsync(string.Join("\n", dateGroups));
                await writer.WriteLineAsync();

                var versionGroups = overlappingPrices
                    .GroupBy(d => d.MarketTicker)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {g.Count()} overlapping prices");
                await writer.WriteLineAsync("Overlapping Prices by Snapshot Version:");
                await writer.WriteLineAsync(string.Join("\n", versionGroups));
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("Details:");
                foreach (var overlap in overlappingPrices.OrderBy(d => d.SnapshotDate))
                {
                    await writer.WriteLineAsync($"  - Date: {overlap.SnapshotDate:yyyy-MM-dd HH:mm:ss}");
                    await writer.WriteLineAsync($"    Overlapping Price: {overlap.OverlappingPrice} (BestYesBid = BestNoBid)");
                }
            }
            else
            {
                await writer.WriteLineAsync("None");
            }
            await writer.WriteLineAsync(new string('-', 50));

            await writer.WriteLineAsync("Rate Discrepancies"); // New section
            await writer.WriteLineAsync(new string('=', 50));
            if (rateDiscrepancies.Any())
            {
                var dateGroups = rateDiscrepancies
                    .GroupBy(d => d.SnapshotDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key:yyyy-MM-dd}: {g.Count()} rate discrepancies");
                await writer.WriteLineAsync("Rate Discrepancies by Date:");
                await writer.WriteLineAsync(string.Join("\n", dateGroups));
                await writer.WriteLineAsync();

                var versionGroups = rateDiscrepancies
                    .GroupBy(d => d.MarketTicker)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {g.Count()} rate discrepancies");
                await writer.WriteLineAsync("Rate Discrepancies by Snapshot Version:");
                await writer.WriteLineAsync(string.Join("\n", versionGroups));
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("Details:");
                foreach (var discrepancy in rateDiscrepancies.OrderBy(d => d.SnapshotDate))
                {
                    await writer.WriteLineAsync($"  - Date: {discrepancy.SnapshotDate:yyyy-MM-dd HH:mm:ss}");
                    await writer.WriteLineAsync($"    Velocity Sum: {discrepancy.VelocitySum:F2}");
                    await writer.WriteLineAsync($"    Rate Sum: {discrepancy.RateSum:F2}");
                    await writer.WriteLineAsync($"    Difference: {discrepancy.Difference:F2}");
                }
            }
            else
            {
                await writer.WriteLineAsync("None");
            }
            await writer.WriteLineAsync(new string('-', 50));

            await writer.WriteLineAsync("Missing Orderbooks");
            await writer.WriteLineAsync(new string('=', 50));
            if (missingOrderbooks.Any())
            {
                var dateGroups = missingOrderbooks
                    .GroupBy(d => d.SnapshotDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key:yyyy-MM-dd}: {g.Count()} missing");
                await writer.WriteLineAsync("Missing Orderbooks by Date:");
                await writer.WriteLineAsync(string.Join("\n", dateGroups));
                await writer.WriteLineAsync();

                var versionGroups = missingOrderbooks
                    .GroupBy(d => d.MarketTicker)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {g.Count()} missing");
                await writer.WriteLineAsync("Missing Orderbooks by Snapshot Version:");
                await writer.WriteLineAsync(string.Join("\n", versionGroups));
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("Details:");
                foreach (var item in missingOrderbooks.OrderBy(d => d.SnapshotDate))
                {
                    await writer.WriteLineAsync($"  - Date: {item.SnapshotDate:yyyy-MM-dd HH:mm:ss}");
                }
            }
            else
            {
                await writer.WriteLineAsync("None");
            }
            await writer.WriteLineAsync(new string('-', 50));
        }


        /// <summary>
        /// Logs an overlapping price issue where Yes and No bid prices are identical.
        /// This indicates a potential data integrity problem in the market snapshot.
        /// </summary>
        /// <param name="marketTicker">The market ticker where the overlap occurred.</param>
        /// <param name="snapshotDate">The timestamp of the snapshot.</param>
        /// <param name="overlappingPrice">The price value that is identical for both Yes and No bids.</param>
        private void LogOverlappingPrice(string marketTicker, DateTime snapshotDate, int overlappingPrice)
        {
            var metadata = new OverlappingPriceMetadata
            {
                MarketTicker = marketTicker,
                SnapshotDate = snapshotDate,
                OverlappingPrice = overlappingPrice
            };
            lock (_overlappingPrices)
            {
                if (!_overlappingPrices.ContainsKey(marketTicker))
                {
                    _overlappingPrices[marketTicker] = new List<OverlappingPriceMetadata>();
                }
                _overlappingPrices[marketTicker].Add(metadata);
            }
            _overlappingPriceCount++;
        }

        /// <summary>
        /// Logs a snapshot that is missing essential orderbook data.
        /// This indicates incomplete market data that cannot be properly analyzed.
        /// </summary>
        /// <param name="marketTicker">The market ticker with missing orderbook data.</param>
        /// <param name="snapshotDate">The timestamp of the incomplete snapshot.</param>
        private void LogMissingOrderbook(string marketTicker, DateTime snapshotDate)
        {
            var metadata = new MissingOrderbookMetadata
            {
                MarketTicker = marketTicker,
                SnapshotDate = snapshotDate
            };
            lock (_missingOrderbooks)
            {
                if (!_missingOrderbooks.ContainsKey(marketTicker))
                {
                    _missingOrderbooks[marketTicker] = new List<MissingOrderbookMetadata>();
                }
                _missingOrderbooks[marketTicker].Add(metadata);
            }
            _missingOrderbookCount++;
        }

        /// <summary>
        /// Logs a rate discrepancy between velocity and volume calculations.
        /// This indicates potential inconsistencies in the trading activity metrics.
        /// </summary>
        /// <param name="marketTicker">The market ticker where the discrepancy occurred.</param>
        /// <param name="snapshotDate">The timestamp of the snapshot.</param>
        /// <param name="velocitySum">The sum of velocity values for bid positions.</param>
        /// <param name="rateSum">The sum of order and trade volume rates.</param>
        /// <param name="difference">The absolute difference between velocity and rate sums.</param>
        private void LogRateDiscrepancy(string marketTicker, DateTime snapshotDate, double velocitySum, double rateSum, double difference)
        {
            var metadata = new RateDiscrepancyMetadata
            {
                MarketTicker = marketTicker,
                SnapshotDate = snapshotDate,
                VelocitySum = velocitySum,
                RateSum = rateSum,
                Difference = difference
            };
            lock (_rateDiscrepancies)
            {
                if (!_rateDiscrepancies.ContainsKey(marketTicker))
                {
                    _rateDiscrepancies[marketTicker] = new List<RateDiscrepancyMetadata>();
                }
                _rateDiscrepancies[marketTicker].Add(metadata);
            }
            _rateDiscrepancyCount++;
        }

        /// <summary>
        /// Exports a comprehensive discrepancy report to file system.
        /// This method generates detailed reports for all markets with validation issues,
        /// including overlapping prices, rate discrepancies, and missing orderbooks.
        /// Reports are saved as text files in the test directory for analysis.
        /// </summary>
        private async Task ExportDiscrepancyReport()
        {
            var priceReportPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DiscrepancyReport.txt");
            using (var priceWriter = new StreamWriter(priceReportPath, false))
            {
                await priceWriter.WriteLineAsync("Price Discrepancy Report");
                await priceWriter.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                await priceWriter.WriteLineAsync(new string('-', 50));


                await priceWriter.WriteLineAsync("Overlapping Price Discrepancies");
                await priceWriter.WriteLineAsync(new string('=', 50));
                var overlappingDateGroups = _overlappingPrices.Values
                    .SelectMany(d => d)
                    .GroupBy(d => d.SnapshotDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key:yyyy-MM-dd}: {g.Count()} overlapping prices");
                await priceWriter.WriteLineAsync("Overlapping Prices by Date:");
                await priceWriter.WriteLineAsync(string.Join("\n", overlappingDateGroups.Any() ? overlappingDateGroups : new[] { "None" }));
                await priceWriter.WriteLineAsync();

                var overlappingVersionGroups = _overlappingPrices.Values
                    .SelectMany(d => d)
                    .GroupBy(d => d.MarketTicker)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {g.Count()} overlapping prices");
                await priceWriter.WriteLineAsync("Overlapping Prices by Snapshot Version:");
                await priceWriter.WriteLineAsync(string.Join("\n", overlappingVersionGroups.Any() ? overlappingVersionGroups : new[] { "None" }));
                await priceWriter.WriteLineAsync(new string('-', 50));

                foreach (var market in _overlappingPrices.Keys.OrderBy(k => k))
                {
                    var overlaps = _overlappingPrices[market];
                    var minDate = overlaps.Min(d => d.SnapshotDate);
                    var maxDate = overlaps.Max(d => d.SnapshotDate);

                    await priceWriter.WriteLineAsync($"Market Ticker: {market}");
                    await priceWriter.WriteLineAsync($"Overlapping Price Count: {overlaps.Count}");
                    await priceWriter.WriteLineAsync($"Date Range: {minDate:yyyy-MM-dd HH:mm:ss} to {maxDate:yyyy-MM-dd HH:mm:ss}");
                    await priceWriter.WriteLineAsync("Details:");

                    foreach (var overlap in overlaps.OrderBy(d => d.SnapshotDate))
                    {
                        await priceWriter.WriteLineAsync($"  - Date: {overlap.SnapshotDate:yyyy-MM-dd HH:mm:ss}");
                        await priceWriter.WriteLineAsync($"    Overlapping Price: {overlap.OverlappingPrice} (BestYesBid = BestNoBid)");
                    }
                    await priceWriter.WriteLineAsync(new string('-', 50));
                }
                await priceWriter.WriteLineAsync($"Total Markets Affected: {_overlappingPrices.Keys.Count}");
                await priceWriter.WriteLineAsync($"Total Overlapping Prices: {_overlappingPriceCount}");
                await priceWriter.WriteLineAsync(new string('-', 50));

                await priceWriter.WriteLineAsync("Rate Discrepancies"); // New section
                await priceWriter.WriteLineAsync(new string('=', 50));
                var rateDateGroups = _rateDiscrepancies.Values
                    .SelectMany(d => d)
                    .GroupBy(d => d.SnapshotDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key:yyyy-MM-dd}: {g.Count()} rate discrepancies");
                await priceWriter.WriteLineAsync("Rate Discrepancies by Date:");
                await priceWriter.WriteLineAsync(string.Join("\n", rateDateGroups.Any() ? rateDateGroups : new[] { "None" }));
                await priceWriter.WriteLineAsync();

                var rateVersionGroups = _rateDiscrepancies.Values
                    .SelectMany(d => d)
                    .GroupBy(d => d.MarketTicker)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {g.Count()} rate discrepancies");
                await priceWriter.WriteLineAsync("Rate Discrepancies by Snapshot Version:");
                await priceWriter.WriteLineAsync(string.Join("\n", rateVersionGroups.Any() ? rateVersionGroups : new[] { "None" }));
                await priceWriter.WriteLineAsync(new string('-', 50));

                foreach (var market in _rateDiscrepancies.Keys.OrderBy(k => k))
                {
                    var discrepancies = _rateDiscrepancies[market];
                    var minDate = discrepancies.Min(d => d.SnapshotDate);
                    var maxDate = discrepancies.Max(d => d.SnapshotDate);

                    await priceWriter.WriteLineAsync($"Market Ticker: {market}");
                    await priceWriter.WriteLineAsync($"Rate Discrepancy Count: {discrepancies.Count}");
                    await priceWriter.WriteLineAsync($"Date Range: {minDate:yyyy-MM-dd HH:mm:ss} to {maxDate:yyyy-MM-dd HH:mm:ss}");
                    await priceWriter.WriteLineAsync("Details:");

                    foreach (var discrepancy in discrepancies.OrderBy(d => d.SnapshotDate))
                    {
                        await priceWriter.WriteLineAsync($"  - Date: {discrepancy.SnapshotDate:yyyy-MM-dd HH:mm:ss}");
                        await priceWriter.WriteLineAsync($"    Velocity Sum: {discrepancy.VelocitySum:F2}");
                        await priceWriter.WriteLineAsync($"    Rate Sum: {discrepancy.RateSum:F2}");
                        await priceWriter.WriteLineAsync($"    Difference: {discrepancy.Difference:F2}");
                    }
                    await priceWriter.WriteLineAsync(new string('-', 50));
                }
                await priceWriter.WriteLineAsync($"Total Markets Affected: {_rateDiscrepancies.Keys.Count}");
                await priceWriter.WriteLineAsync($"Total Rate Discrepancies: {_rateDiscrepancyCount}");
            }

            var orderbookReportPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "MissingOrderbookReport.txt");
            using (var orderbookWriter = new StreamWriter(orderbookReportPath, false))
            {
                await orderbookWriter.WriteLineAsync("Missing Orderbook Report");
                await orderbookWriter.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                await orderbookWriter.WriteLineAsync(new string('-', 50));

                await orderbookWriter.WriteLineAsync("Missing Orderbooks");
                await orderbookWriter.WriteLineAsync(new string('=', 50));
                var orderbookDateGroups = _missingOrderbooks.Values
                    .SelectMany(d => d)
                    .GroupBy(d => d.SnapshotDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key:yyyy-MM-dd}: {g.Count()} missing");
                await orderbookWriter.WriteLineAsync("Missing Orderbooks by Date:");
                await orderbookWriter.WriteLineAsync(string.Join("\n", orderbookDateGroups.Any() ? orderbookDateGroups : new[] { "None" }));
                await orderbookWriter.WriteLineAsync();

                var orderbookVersionGroups = _missingOrderbooks.Values
                    .SelectMany(d => d)
                    .GroupBy(d => d.MarketTicker)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {g.Count()} missing");
                await orderbookWriter.WriteLineAsync("Missing Orderbooks by Snapshot Version:");
                await orderbookWriter.WriteLineAsync(string.Join("\n", orderbookVersionGroups.Any() ? orderbookVersionGroups : new[] { "None" }));
                await orderbookWriter.WriteLineAsync(new string('-', 50));

                foreach (var market in _missingOrderbooks.Keys.OrderBy(k => k))
                {
                    var missing = _missingOrderbooks[market];
                    var minDate = missing.Min(d => d.SnapshotDate);
                    var maxDate = missing.Max(d => d.SnapshotDate);

                    await orderbookWriter.WriteLineAsync($"Market Ticker: {market}");
                    await orderbookWriter.WriteLineAsync($"Missing Count: {missing.Count}");
                    await orderbookWriter.WriteLineAsync($"Date Range: {minDate:yyyy-MM-dd HH:mm:ss} to {maxDate:yyyy-MM-dd HH:mm:ss}");
                    await orderbookWriter.WriteLineAsync("Details:");

                    foreach (var item in missing.OrderBy(d => d.SnapshotDate))
                    {
                        await orderbookWriter.WriteLineAsync($"  - Date: {item.SnapshotDate:yyyy-MM-dd HH:mm:ss}");
                    }
                    await orderbookWriter.WriteLineAsync(new string('-', 50));
                }
                await orderbookWriter.WriteLineAsync($"Total Markets Affected: {_missingOrderbooks.Keys.Count}");
                await orderbookWriter.WriteLineAsync($"Total Missing Orderbooks: {_missingOrderbookCount}");
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
