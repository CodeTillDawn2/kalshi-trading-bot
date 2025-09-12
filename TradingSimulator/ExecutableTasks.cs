using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using BacklashDTOs.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashBot.State.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingSimulator.Strategies;
using TradingStrategies.Classification;
using TradingStrategies.Configuration;


namespace TradingSimulator.Executable
{
    [TestFixture]
    public class ExecutableTasks
    {
        private TradingSnapshotService _snapshotService;
        private OvernightActivitiesHelper _overnightService;
        private SnapshotPeriodHelper _snapshotPeriodAnalyzer;
        private IInterestScoreService _interestScoreService;
        private SnapshotPeriodHelper _snapshotPeriodHelper;
        private IOptions<ExecutionConfig> _executionConfig;
        private Mock<ILogger<SqlDataService>> _sqlLoggerMock;
        private MarketAnalysisHelper _marketAnalysisHelper;
        private IKalshiBotContext _dbContext;
        private ServiceProvider? _serviceProvider;
        private IOptions<SnapshotConfig> _snapshotOptions;
        private int _priceOffCount_major;
        private int _orderbookMissingCount;
        private int _overlappingPriceCount;
        private int _rateDiscrepancyCount; // New counter
        private readonly Dictionary<string, List<DiscrepancyMetadata>> _majorDiscrepancies = new();
        private readonly Dictionary<string, List<MissingOrderbookMetadata>> _missingOrderbooks = new();
        private readonly Dictionary<string, List<OverlappingPriceMetadata>> _overlappingPrices = new();
        private readonly Dictionary<string, List<RateDiscrepancyMetadata>> _rateDiscrepancies = new(); // New dictionary
        private IConfigurationRoot config;
        private IServiceScopeFactory _scopeFactory;
        private SqlDataService _sqlDataService;

        private class DiscrepancyMetadata
        {
            public string MarketTicker { get; set; }
            public DateTime SnapshotDate { get; set; }
            public int ExpectedYesBid { get; set; }
            public int ActualYesBid { get; set; }
            public int ExpectedNoBid { get; set; }
            public int ActualNoBid { get; set; }
        }

        private class MissingOrderbookMetadata
        {
            public string MarketTicker { get; set; }
            public DateTime SnapshotDate { get; set; }
            public int? SnapshotVersion { get; set; }
        }

        private class OverlappingPriceMetadata
        {
            public string MarketTicker { get; set; }
            public DateTime SnapshotDate { get; set; }
            public int OverlappingPrice { get; set; }
        }

        private class RateDiscrepancyMetadata // New metadata class
        {
            public string MarketTicker { get; set; }
            public DateTime SnapshotDate { get; set; }
            public double VelocitySum { get; set; }
            public double RateSum { get; set; }
            public double Difference { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: false)
                .Build();

            var snapshotConfig = config.GetSection("Snapshots").Get<SnapshotConfig>();
            var tradingConfig = config.GetSection("TradingConfig").Get<TradingConfig>();
            var kalshiConfig = config.GetSection("Kalshi").Get<KalshiConfig>(); // Add this for KalshiConfig
            _snapshotOptions = Options.Create(snapshotConfig);
            _executionConfig = Options.Create(config.GetSection("Execution").Get<ExecutionConfig>());
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

            _snapshotPeriodHelper = new SnapshotPeriodHelper();

            var services = new ServiceCollection();
            services.AddScoped<IKalshiBotContext>(provider => new KalshiBotContext(config));
            services.AddScoped<IKalshiAPIService, KalshiAPIService>(); // Register with interface
            services.AddScoped<IServiceFactory, ServiceFactory>();
            services.AddScoped<CentralPerformanceMonitor>();
            services.AddSingleton<IConfiguration>(config);

            _interestScoreService = new InterestScoreService(interestLoggerMock.Object);

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

            _marketAnalysisHelper = new MarketAnalysisHelper(_scopeFactory, _snapshotPeriodHelper, _snapshotService, _executionConfig, marketAnalysisLoggerMock.Object);
            _overnightService = new OvernightActivitiesHelper(overnightLoggerMock.Object, _interestScoreService, _marketAnalysisHelper, _executionConfig, _sqlDataService);
            _snapshotService = new TradingSnapshotService(snapshotLoggerMock.Object, _snapshotOptions, Options.Create(tradingConfig), _scopeFactory);
            _snapshotPeriodAnalyzer = new SnapshotPeriodHelper();

            _dbContext = new KalshiBotContext(config);
            _priceOffCount_major = 0;
            _orderbookMissingCount = 0;
            _overlappingPriceCount = 0;
            _rateDiscrepancyCount = 0; // Initialize new counter
        }


        [Test]
        public async Task ExecuteOvernightTasks()
        {
            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            await _overnightService.RunOvernightTasks(scopeFactory);
        }

        [Test]
        public async Task GenerateSnapshotGroups()
        {
            await _marketAnalysisHelper.GenerateSnapshotGroups();
        }

        [Test]
        public async Task DeleteUnrecordedMarkets()
        {
            await _overnightService.DeleteUnrecordedMarkets(_scopeFactory, new CancellationToken());
        }


        [Test]
        public async Task DeleteProcessedSnapshots()
        {
            await _overnightService.DeleteProcessedSnapshots(_scopeFactory, new CancellationToken());
        }


        [Test]
        public async Task UpgradeSnapshots()
        {
            bool SaveUpdatedJSON = true;
            var startTime = DateTime.UtcNow;
            int RecordsReturned = -1;

            while (RecordsReturned != 0)
            {
                var allUnvalidatedSnapshots = await _dbContext.GetSnapshots(isValidated: false, endDate: startTime,
                    MaxRecords: 2000);
                RecordsReturned = allUnvalidatedSnapshots.Count;

                foreach (var marketName in allUnvalidatedSnapshots.Select(x => x.MarketTicker).Distinct().OrderBy(m => m))
                {
                    int validCount = 0;
                    int orderbookMissingCount = 0;
                    int overlappingPriceCount = 0;
                    int rateDiscrepancyCount = 0; // New counter
                    int errorCount = 0;
                    var majorDiscrepancies = new List<DiscrepancyMetadata>();
                    var missingOrderbooks = new List<MissingOrderbookMetadata>();
                    var overlappingPrices = new List<OverlappingPriceMetadata>();
                    var rateDiscrepancies = new List<RateDiscrepancyMetadata>(); // New list

                    try
                    {
                        using var dbContext = new KalshiBotContext(config);
                        var snapshotData = allUnvalidatedSnapshots.Where(x => x.MarketTicker == marketName).OrderBy(x => x.SnapshotDate).ToList();
                        var snapshots = snapshotData.ToList();

                        if (!snapshots.Any())
                        {
                            continue;
                        }

                        var cacheSnapshotDict = await _snapshotService.LoadManySnapshots(snapshots, true);
                        Assert.That(cacheSnapshotDict, Is.Not.Null);

                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            Converters = { new SimplifiedTupleConverter() }
                        };

                        string? marketCategory = null;
                        var market = await dbContext.GetMarketByTicker_cached(marketName);
                        if (market.Event == null)
                        {
                            errorCount++;
                            continue;
                        }
                        if (market != null && market.Event != null)
                        {
                            marketCategory = market.Event.category;
                            if (market.category == "")
                            {
                                market.category = marketCategory;
                                await dbContext.AddOrUpdateMarket(market);
                            }


                        }

                        foreach (var snapshotList in cacheSnapshotDict.Values)
                        {
                            foreach (var cacheSnapshot in snapshotList)
                            {
                                if (cacheSnapshot == null)
                                {
                                    continue;
                                }

                                try
                                {
                                    //var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(cacheSnapshot);

                                    //if (result.IsOrderbookMissing)
                                    //{
                                    //    orderbookMissingCount++;
                                    //    LogMissingOrderbook(cacheSnapshot.MarketTicker, cacheSnapshot.Timestamp);
                                    //    missingOrderbooks.Add(new MissingOrderbookMetadata
                                    //    {
                                    //        MarketTicker = cacheSnapshot.MarketTicker,
                                    //        SnapshotDate = cacheSnapshot.Timestamp
                                    //    });
                                    //}

                                    //if (result.DoPricesOverlap)
                                    //{
                                    //    overlappingPriceCount++;
                                    //    LogOverlappingPrice(cacheSnapshot.MarketTicker, cacheSnapshot.Timestamp, cacheSnapshot.BestYesBid);
                                    //    overlappingPrices.Add(new OverlappingPriceMetadata
                                    //    {
                                    //        MarketTicker = cacheSnapshot.MarketTicker,
                                    //        SnapshotDate = cacheSnapshot.Timestamp,
                                    //        OverlappingPrice = cacheSnapshot.BestYesBid
                                    //    });
                                    //}

                                    //if (result.IsRateDiscrepancy)
                                    //{
                                    //    rateDiscrepancyCount++;
                                    //    double velocitySum = (cacheSnapshot.VelocityPerMinute_Top_Yes_Bid) + (cacheSnapshot.VelocityPerMinute_Bottom_Yes_Bid);
                                    //    double rateSum = (cacheSnapshot.OrderVolumePerMinute_YesBid) + (cacheSnapshot.TradeVolumePerMinute_Yes);
                                    //    double diff = Math.Abs(velocitySum - rateSum);
                                    //    LogRateDiscrepancy(cacheSnapshot.MarketTicker, cacheSnapshot.Timestamp, velocitySum, rateSum, diff);
                                    //    rateDiscrepancies.Add(new RateDiscrepancyMetadata
                                    //    {
                                    //        MarketTicker = cacheSnapshot.MarketTicker,
                                    //        SnapshotDate = cacheSnapshot.Timestamp,
                                    //        VelocitySum = velocitySum,
                                    //        RateSum = rateSum,
                                    //        Difference = diff
                                    //    });
                                    //}



                                    //if (result.IsValid)
                                    //{
                                    var snapshotToUpdate = snapshots.FirstOrDefault(x => x.MarketTicker == cacheSnapshot.MarketTicker && x.SnapshotDate == cacheSnapshot.Timestamp);
                                    if (snapshotToUpdate != null)
                                    {
                                        snapshotToUpdate.IsValidated = true;
                                        if (SaveUpdatedJSON)
                                        {
                                            cacheSnapshot.MarketCategory = marketCategory;
                                            snapshotToUpdate.RawJSON = JsonSerializer.Serialize(cacheSnapshot, options);
                                        }
                                    }
                                    validCount++;
                                    //}
                                }
                                catch
                                {
                                    errorCount++;
                                }
                            }
                        }

                        if (snapshots.Any(x => x.IsValidated == true))
                        {
                            snapshots = snapshots.Where(x => x.IsValidated == true).ToList();
                            await _dbContext.AddOrUpdateSnapshots(snapshots);

                            //foreach (var snapshotToUpdate in snapshots.Where(x => x.IsValidated == true))
                            //{
                            //    try
                            //    {
                            //        await _dbContext.AddOrUpdateSnapshot(snapshotToUpdate);
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        errorCount++;
                            //    }
                            //}

                        }

                        //await ExportDiscrepancyReportForMarket(
                        //    marketName,
                        //    validCount,
                        //    orderbookMissingCount,
                        //    overlappingPriceCount,
                        //    rateDiscrepancyCount, // Add new parameter
                        //    errorCount,
                        //    majorDiscrepancies,
                        //    missingOrderbooks,
                        //    overlappingPrices,
                        //    rateDiscrepancies // Add new parameter
                        //);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                    }
                }
            }



            await ExportDiscrepancyReport(); // Update global report
        }

        private async Task ExportDiscrepancyReportForMarket(
            string marketTicker,
            int validCount,
            int orderbookMissingCount,
            int overlappingPriceCount,
            int rateDiscrepancyCount, // New parameter
            int errorCount,
            List<DiscrepancyMetadata> majorDiscrepancies,
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

            await writer.WriteLineAsync("Major Price Discrepancies");
            await writer.WriteLineAsync(new string('=', 50));
            if (majorDiscrepancies.Any())
            {
                var dateGroups = majorDiscrepancies
                    .GroupBy(d => d.SnapshotDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key:yyyy-MM-dd}: {g.Count()} discrepancies");
                await writer.WriteLineAsync("Discrepancies by Date:");
                await writer.WriteLineAsync(string.Join("\n", dateGroups));
                await writer.WriteLineAsync();

                var versionGroups = majorDiscrepancies
                    .GroupBy(d => d.MarketTicker)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {g.Count()} discrepancies");
                await writer.WriteLineAsync("Discrepancies by Snapshot Version:");
                await writer.WriteLineAsync(string.Join("\n", versionGroups));
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("Details:");
                foreach (var discrepancy in majorDiscrepancies.OrderBy(d => d.SnapshotDate))
                {
                    await writer.WriteLineAsync($"  - Date: {discrepancy.SnapshotDate:yyyy-MM-dd HH:mm:ss}");
                    await writer.WriteLineAsync($"    Yes Bid: Expected={discrepancy.ExpectedYesBid}, Actual={discrepancy.ActualYesBid}");
                    await writer.WriteLineAsync($"    No Bid: Expected={discrepancy.ExpectedNoBid}, Actual={discrepancy.ActualNoBid}");
                }
            }
            else
            {
                await writer.WriteLineAsync("None");
            }
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

        private void LogMajorDiscrepancy(string marketTicker, DateTime snapshotDate, int? snapshotVersion,
            int expectedYesBid, int actualYesBid, int expectedNoBid, int actualNoBid)
        {
            var metadata = new DiscrepancyMetadata
            {
                MarketTicker = marketTicker,
                SnapshotDate = snapshotDate,
                ExpectedYesBid = expectedYesBid,
                ActualYesBid = actualYesBid,
                ExpectedNoBid = expectedNoBid,
                ActualNoBid = actualNoBid
            };
            lock (_majorDiscrepancies)
            {
                if (!_majorDiscrepancies.ContainsKey(marketTicker))
                {
                    _majorDiscrepancies[marketTicker] = new List<DiscrepancyMetadata>();
                }
                _majorDiscrepancies[marketTicker].Add(metadata);
            }
            _priceOffCount_major++;
        }

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
            _orderbookMissingCount++;
        }

        private void LogRateDiscrepancy(string marketTicker, DateTime snapshotDate, double velocitySum, double rateSum, double difference) // New method
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

        private async Task ExportDiscrepancyReport()
        {
            var priceReportPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DiscrepancyReport.txt");
            using (var priceWriter = new StreamWriter(priceReportPath, false))
            {
                await priceWriter.WriteLineAsync("Price Discrepancy Report");
                await priceWriter.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                await priceWriter.WriteLineAsync(new string('-', 50));

                await priceWriter.WriteLineAsync("Major Price Discrepancies");
                await priceWriter.WriteLineAsync(new string('=', 50));
                var priceDateGroups = _majorDiscrepancies.Values
                    .SelectMany(d => d)
                    .GroupBy(d => d.SnapshotDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key:yyyy-MM-dd}: {g.Count()} discrepancies");
                await priceWriter.WriteLineAsync("Discrepancies by Date:");
                await priceWriter.WriteLineAsync(string.Join("\n", priceDateGroups.Any() ? priceDateGroups : new[] { "None" }));
                await priceWriter.WriteLineAsync();

                var priceVersionGroups = _majorDiscrepancies.Values
                    .SelectMany(d => d)
                    .GroupBy(d => d.MarketTicker)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {g.Count()} discrepancies");
                await priceWriter.WriteLineAsync("Discrepancies by Snapshot Version:");
                await priceWriter.WriteLineAsync(string.Join("\n", priceVersionGroups.Any() ? priceVersionGroups : new[] { "None" }));
                await priceWriter.WriteLineAsync(new string('-', 50));

                foreach (var market in _majorDiscrepancies.Keys.OrderBy(k => k))
                {
                    var discrepancies = _majorDiscrepancies[market];
                    var minDate = discrepancies.Min(d => d.SnapshotDate);
                    var maxDate = discrepancies.Max(d => d.SnapshotDate);

                    await priceWriter.WriteLineAsync($"Market Ticker: {market}");
                    await priceWriter.WriteLineAsync($"Discrepancy Count: {discrepancies.Count}");
                    await priceWriter.WriteLineAsync($"Date Range: {minDate:yyyy-MM-dd HH:mm:ss} to {maxDate:yyyy-MM-dd HH:mm:ss}");
                    await priceWriter.WriteLineAsync("Details:");

                    foreach (var discrepancy in discrepancies.OrderBy(d => d.SnapshotDate))
                    {
                        await priceWriter.WriteLineAsync($"  - Date: {discrepancy.SnapshotDate:yyyy-MM-dd HH:mm:ss}");
                        await priceWriter.WriteLineAsync($"    Yes Bid: Expected={discrepancy.ExpectedYesBid}, Actual={discrepancy.ActualYesBid}");
                        await priceWriter.WriteLineAsync($"    No Bid: Expected={discrepancy.ExpectedNoBid}, Actual={discrepancy.ActualNoBid}");
                    }
                    await priceWriter.WriteLineAsync(new string('-', 50));
                }
                await priceWriter.WriteLineAsync($"Total Markets Affected: {_majorDiscrepancies.Keys.Count}");
                await priceWriter.WriteLineAsync($"Total Discrepancies: {_priceOffCount_major}");
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
                await orderbookWriter.WriteLineAsync($"Total Missing Orderbooks: {_orderbookMissingCount}");
            }
        }

        [TearDown]
        public void TearDown()
        {
            _sqlDataService.Dispose();
            _dbContext.Dispose();
            _serviceProvider.Dispose();
        }
    }
}
