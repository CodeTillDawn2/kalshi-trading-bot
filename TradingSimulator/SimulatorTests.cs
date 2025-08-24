// SimulatorTests.cs
// Updated to avoid redundant data loads for markets, remove parallel operations, and maintain separate strategy set methods

using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmokehouseBot.Configuration;
using SmokehouseBot.Management;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using TradingStrategies;
using TradingStrategies.Classification;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;
using Microsoft.EntityFrameworkCore;
using static TradingStrategies.Trading.Overseer.ReportGenerator;
using static SmokehouseInterfaces.Enums.StrategyEnums;
using TradingSimulator.Strategies;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Helpers;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using TradingSimulator.TestObjects;
using static TradingStrategies.Strategies.Strats.BollingerBreakout;
using System.Threading;

namespace TradingSimulator.Simulator
{
    public delegate void TradingStrategyFunc<T>(T currentData, T previousData, SnapshotConfig config, TradingContext context);

    public class TradingContext
    {
        public Dictionary<string, double> SharedVariables { get; set; } = new Dictionary<string, double>();
        public TradingDecision Decision { get; set; } = new TradingDecision();
    }

    [TestFixture]
    public class SimulatorTests
    {
        private ITradingSnapshotService _snapshotService;
        private ISnapshotPeriodHelper _snapshotPeriodHelper;
        private IServiceFactory _serviceFactory;
        private TradingOverseer _overseer;
        private Mock<ILogger<ITradingSnapshotService>> _snapshotLoggerMock;
        private Mock<ILogger<IInterestScoreService>> _interestScoreLoggerMock;
        private Mock<ILogger<TradingStrategy<MarketSnapshot>>> _strategyLoggerMock;
        private IOptions<SnapshotConfig> _snapshotOptions;
        private IOptions<TradingConfig> _tradingOptions;
        private IServiceScopeFactory _scopeFactory;
        private IKalshiBotContext _dbContext;
        private MarketAnalysisHelper _marketAnalysisHelper;
        private IOptions<ExecutionConfig> _executionConfig;
        private HashSet<string> _processedMarkets;
        private readonly string _cacheDirectory = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");
        private Mock<ILogger<SqlDataService>> _sqlLoggerMock;
        public event Action<string> OnTestProgress;
        public event Action<string, double> OnProfitLossUpdate;
        public event Action<string> OnMarketProcessed;

        private SqlDataService _sqlDataService;

        // SimulatorTests.cs
        [SetUp]
        public void Setup()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SmokehouseBot"));
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: false)
                .Build();

            _snapshotLoggerMock = new Mock<ILogger<ITradingSnapshotService>>();
            _serviceFactory = new Mock<IServiceFactory>().Object;
            _strategyLoggerMock = new Mock<ILogger<TradingStrategy<MarketSnapshot>>>();
            _interestScoreLoggerMock = new Mock<ILogger<IInterestScoreService>>();

            var snapshotConfig = config.GetSection("Snapshots").Get<SnapshotConfig>();
            var tradingConfig = config.GetSection("TradingConfig").Get<TradingConfig>();
            _snapshotOptions = Options.Create(snapshotConfig);
            _tradingOptions = Options.Create(tradingConfig);
            _executionConfig = Options.Create(config.GetSection("Execution").Get<ExecutionConfig>());

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddDbContext<KalshiBotContext>(options => options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
            services.AddScoped<IKalshiBotContext>(sp => sp.GetRequiredService<KalshiBotContext>());
            var serviceProvider = services.BuildServiceProvider();

            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            // FIX: init order — construct helpers AFTER these are ready
            _snapshotPeriodHelper = new SnapshotPeriodHelper();
            _snapshotService = new TradingSnapshotService(_snapshotLoggerMock.Object, _snapshotOptions, _tradingOptions, _scopeFactory);
            _overseer = new TradingOverseer(_scopeFactory, _snapshotService);
            _marketAnalysisHelper = new MarketAnalysisHelper(_scopeFactory, _snapshotPeriodHelper, _snapshotService, _executionConfig);

            _dbContext = serviceProvider.GetRequiredService<IKalshiBotContext>();
            _sqlLoggerMock = new Mock<ILogger<SqlDataService>>();
            _sqlDataService = new SqlDataService(config, _sqlLoggerMock.Object);

            _processedMarkets = new HashSet<string>();
            Directory.CreateDirectory(_cacheDirectory); // ensure output dir exists

            OnTestProgress?.Invoke("Setup completed.");
        }


        public void SetProcessedMarkets(List<string> markets)
        {
            _processedMarkets = new HashSet<string>(markets);
        }

        private static void SimplePriceDropStrategy(MarketSnapshot current, MarketSnapshot previous, SnapshotConfig config, TradingContext context)
        {
            var currentBid = current.BestYesBid;
            var previousBid = previous.BestYesBid;
            var priceChangePercent = ((double)currentBid - previousBid) / previousBid * 100;
            context.SharedVariables["PriceChangePercent"] = priceChangePercent;
            if (priceChangePercent <= -5.0)
                context.Decision.AddSignal("PriceDrop", 1.0);
        }

        private static void SimplePriceRiseStrategy(MarketSnapshot current, MarketSnapshot previous, SnapshotConfig config, TradingContext context)
        {
            var currentBid = current.BestYesBid;
            var previousBid = previous.BestYesBid;
            var priceChangePercent = ((double)currentBid - previousBid) / previousBid * 100;
            context.SharedVariables["PriceChangePercent"] = priceChangePercent;
            if (priceChangePercent >= 5.0)
                context.Decision.AddSignal("PriceRise", 1.0);
        }

        private static IEnumerable<(string Name, TradingStrategyFunc<MarketSnapshot> Func)> GetTradingStrategies()
        {
            yield return ("SimplePriceDropStrategy", SimplePriceDropStrategy);
            yield return ("SimplePriceRiseStrategy", SimplePriceRiseStrategy);
        }

        // SimulatorTests.cs
        private async Task<List<SnapshotGroupDTO>> GetFilteredSnapshotGroupsAsync(
            IKalshiBotContext context, int? maxGroups, List<string>? marketsToRun)
        {
            var groups = await context.GetSnapshotGroups_cached(maxGroups: maxGroups).ConfigureAwait(false);
            var filtered = new List<SnapshotGroupDTO>();
            foreach (var g in groups)
            {
                var recorded = g.EndTime - g.StartTime;
                if (recorded.TotalHours < 1) continue;              // FIX: TotalHours
                if (marketsToRun != null && !marketsToRun.Contains(g.MarketTicker)) continue;
                filtered.Add(g);
            }
            return filtered;
        }


        // SimulatorTests.cs
        private async Task<(double finalPnL, List<PricePoint> bidPoints, List<PricePoint> askPoints,
            List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> eventPoints,
            List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints)>
            ProcessMarketAsync(
                string marketTicker,
                List<MarketSnapshot> marketSnapshots,
                Dictionary<MarketType, List<Strategy>> strategiesDict,
                IServiceScopeFactory scopeFactory,
                string progressPrefix = "",
                bool writeToFile = false,
                SnapshotGroupDTO? group = null,
                bool ignoreProcessedCache = false)                   // FIX: opt-out of global skip cache
        {
            if (!ignoreProcessedCache && _processedMarkets.Contains(marketTicker))
            {
                OnTestProgress?.Invoke($"{progressPrefix}Skipping cached market: {marketTicker}");
                return (0, null, null, null, null, null, null, null);
            }
            OnTestProgress?.Invoke($"{progressPrefix}Processing market: {marketTicker}");

            try
            {
                var helper = new MarketTypeHelper();
                foreach (var s in marketSnapshots) s.MarketType = helper.GetMarketType(s).ToString();

                var scenario = new Scenario(strategiesDict);
                var pathData = await Task.Run(() => _overseer.TestScenario(scenario, marketSnapshots, writeToFile, 100, group));

                if (pathData.Any())
                {
                    var bestPath = pathData.OrderByDescending(p => p.performance.Equity).First();
                    var eventLogs = bestPath.events;
                    var finalPnL = bestPath.performance.PnL;

                    var bidPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesBid, "")).ToList();
                    var askPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesAsk, "")).ToList();

                    var buyPoints = new List<PricePoint>();
                    var sellPoints = new List<PricePoint>();
                    var intendedLongPoints = new List<PricePoint>();
                    var intendedShortPoints = new List<PricePoint>();
                    int prevPos = 0;
                    foreach (var ev in eventLogs)
                    {
                        if (ev.Position != prevPos)
                        {
                            if (ev.Position > prevPos) buyPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesAsk, ev.Memo));
                            else sellPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesBid, ev.Memo));
                        }
                        else
                        {
                            if (ev.Action == "Long") intendedLongPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesAsk, "Intended Long: " + ev.Memo));
                            else if (ev.Action == "Short") intendedShortPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesBid, "Intended Short: " + ev.Memo));
                        }
                        prevPos = ev.Position;
                    }

                    var eventPoints = eventLogs.Select(ev => new PricePoint(ev.Timestamp, (ev.BestYesBid + ev.BestYesAsk) / 2.0, ev.Memo)).ToList();

                    return (finalPnL, bidPoints, askPoints, buyPoints, sellPoints, eventPoints, intendedLongPoints, intendedShortPoints);
                }
            }
            finally
            {
                OnTestProgress?.Invoke($"{progressPrefix}Cleared memory for market: {marketTicker}");
            }
            return (0, null, null, null, null, null, null, null);
        }


        private void SaveMarketDataToFile(
            string marketTicker, double finalPnL,
            List<PricePoint> bidPoints, List<PricePoint> askPoints,
            List<PricePoint> buyPoints, List<PricePoint> sellPoints,
            List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
            string fileNameSuffix = "")
        {
            var cachedData = new CachedMarketData
            {
                Market = marketTicker,
                PnL = finalPnL,
                BidPoints = bidPoints,
                AskPoints = askPoints,
                BuyPoints = buyPoints,
                SellPoints = sellPoints,
                EventPoints = eventPoints,
                IntendedLongPoints = intendedLongPoints,
                IntendedShortPoints = intendedShortPoints
            };

            var json = JsonSerializer.Serialize(cachedData);
            Directory.CreateDirectory(_cacheDirectory);            
            var filePath = Path.Combine(_cacheDirectory, $"{marketTicker}{fileNameSuffix}.json");
            File.WriteAllText(filePath, json);
        }


        [Test]
        [TestCase("KXPRESNOMR-28-NH")]
        public async Task TestMarketScore(string ticker)
        {
            using var scope = _scopeFactory.CreateScope();
            var logger = _interestScoreLoggerMock.Object;
            var interestScoreHelper = new InterestScoreService(logger);
            var dbContext = scope.ServiceProvider.GetRequiredService<KalshiBotContext>();

            var (testScore, testParts) = await interestScoreHelper.CalculateMarketInterestScoreAsync(
                dbContext,
                ticker,
                spreadTightnessWeight: 0.2,
                spreadWidthWeight: 0.2,
                volumeWeight: 0.33,
                volumePercentileWeight: 0.15,
                liquidityPercentileWeight: 0.06,
                openInterestPercentileWeight: 0.06);

            double testRawScore = testParts.spreadTightness +
                testParts.spreadWidth +
                testParts.volume +
                testParts.volumePercentile +
                testParts.liquidityPercentile +
                testParts.openInterestPercentile;
            double testCloseTimePoints = testScore - Math.Round(testRawScore, 2);

            string testOutput = $"Test run for {ticker}:\n" +
                $"Score: {testScore:F4}\n" +
                $"Close Time Points: {testCloseTimePoints:F4}\n" +
                $"SpreadTightness: {testParts.spreadTightness:F4}\n" +
                $"SpreadWidth: {testParts.spreadWidth:F4}\n" +
                $"Volume: {testParts.volume:F4}\n" +
                $"VolumePercentile: {testParts.volumePercentile:F4}\n" +
                $"LiquidityPercentile: {testParts.liquidityPercentile:F4}\n" +
                $"OpenInterestPercentile: {testParts.openInterestPercentile:F4}\n" +
                $"Raw Score (Sum of Parts): {testRawScore:F4}";

            OnTestProgress?.Invoke(testOutput);
            TestContext.WriteLine(testOutput);

            var (controlScore, controlParts) = await interestScoreHelper.CalculateMarketInterestScoreAsync(
                dbContext,
                ticker,
                spreadTightnessWeight: 0.2,
                spreadWidthWeight: 0.2,
                volumeWeight: 0.2,
                volumePercentileWeight: 0.2,
                liquidityPercentileWeight: 0.1,
                openInterestPercentileWeight: 0.1);

            double controlRawScore = controlParts.spreadTightness +
                controlParts.spreadWidth +
                controlParts.volume +
                controlParts.volumePercentile +
                controlParts.liquidityPercentile +
                controlParts.openInterestPercentile;
            double controlCloseTimePoints = controlScore - Math.Round(controlRawScore, 2);

            string controlOutput = $"\nControl run for {ticker}:\n" +
                $"Score: {controlScore:F4}\n" +
                $"Close Time Points: {controlCloseTimePoints:F4}\n" +
                $"SpreadTightness: {controlParts.spreadTightness:F4}\n" +
                $"SpreadWidth: {controlParts.spreadWidth:F4}\n" +
                $"Volume: {controlParts.volume:F4}\n" +
                $"VolumePercentile: {controlParts.volumePercentile:F4}\n" +
                $"LiquidityPercentile: {controlParts.liquidityPercentile:F4}\n" +
                $"OpenInterestPercentile: {controlParts.openInterestPercentile:F4}\n" +
                $"Raw Score (Sum of Parts): {controlRawScore:F4}";

            OnTestProgress?.Invoke(controlOutput);
            TestContext.WriteLine(controlOutput);
        }

        [Test]
        public async Task TestOvernightActivities()
        {
            using var scope = _scopeFactory.CreateScope();
            var logger = new Mock<ILogger<OvernightActivitiesHelper>>().Object;
            var interestScoreHelper = new InterestScoreService(_interestScoreLoggerMock.Object);
            var overnightHelper = new OvernightActivitiesHelper(logger, interestScoreHelper, _marketAnalysisHelper, _executionConfig, _sqlDataService);

            OnTestProgress?.Invoke("Starting overnight activities test.");
            await overnightHelper.RunOvernightTasks(_scopeFactory);
            OnTestProgress?.Invoke("Overnight activities test completed.");
        }

        [Test]
        public async Task TestTradingStrategy([ValueSource(nameof(GetTradingStrategies))] (string Name, TradingStrategyFunc<MarketSnapshot> Func) strategy)
        {
            var strategyTest = new TradingStrategy<MarketSnapshot>(
                _snapshotService,
                _snapshotPeriodHelper,
                _strategyLoggerMock.Object,
                _snapshotOptions,
                _tradingOptions,
                _scopeFactory,
                _dbContext,
                new List<(string, TradingStrategyFunc<MarketSnapshot>)> { (strategy.Name, strategy.Func) });

            strategyTest.OnTestProgress += message => OnTestProgress?.Invoke(message);
            strategyTest.OnProfitLossUpdate += (market, revenue) => OnProfitLossUpdate?.Invoke(market, revenue);
            strategyTest.OnMarketProcessed += market => OnMarketProcessed?.Invoke(market);

            await strategyTest.RunStrategyTestAsync();
        }

        public async Task RunStrategiesForGuiAsync()
        {
            var strategies = GetTradingStrategies().ToList();
            var strategyTest = new TradingStrategy<MarketSnapshot>(
                _snapshotService,
                _snapshotPeriodHelper,
                _strategyLoggerMock.Object,
                _snapshotOptions,
                _tradingOptions,
                _scopeFactory,
                _dbContext,
                strategies);

            strategyTest.OnTestProgress += message => OnTestProgress?.Invoke(message);
            strategyTest.OnProfitLossUpdate += (market, revenue) => OnProfitLossUpdate?.Invoke(market, revenue);
            strategyTest.OnMarketProcessed += market => OnMarketProcessed?.Invoke(market);

            await strategyTest.RunStrategyTestAsync();
        }

        [Test]
        public async Task TestMarketPriceHistory()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<KalshiBotContext>();

                var groups = await context.GetSnapshotGroups_cached();
                var filteredGroups = new List<SnapshotGroupDTO>();

                foreach (var group in groups)
                {
                    TimeSpan recordedHours = group.EndTime - group.StartTime;
                    if (recordedHours.Hours < 1) continue;
                    int count = groups.Count(x => x.MarketTicker == group.MarketTicker);
                    if (count > 1) continue;
                    int minYes = groups.Where(x => x.MarketTicker == group.MarketTicker).Min(x => x.YesEnd);
                    int minNo = groups.Where(x => x.MarketTicker == group.MarketTicker).Min(x => x.NoEnd);
                    if (minYes != 0 && minNo != 0) continue;
                    filteredGroups.Add(group);
                }

                foreach (var group in filteredGroups)
                {
                    OnTestProgress?.Invoke($"Processing market: {group.MarketTicker}");
                    List<SnapshotDTO> snapshotData = await context.GetSnapshots_cached(
                        marketTicker: group.MarketTicker,
                        startDate: group.StartTime,
                        endDate: group.EndTime);
                    snapshotData = snapshotData
                        .OrderBy(x => x.SnapshotDate).ToList();
                    var snapshots = snapshotData.ToList();

                    var cacheSnapshotDict = await _snapshotService.LoadManySnapshots(snapshots);
                    var marketSnapshots = cacheSnapshotDict
                        .SelectMany(kvp => kvp.Value.Select(cs => cs))
                        .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                        .OrderBy(ms => ms.Timestamp)
                        .ToList();

                    if (!marketSnapshots.Any())
                    {
                        OnTestProgress?.Invoke($"No valid snapshots for {group.MarketTicker}");
                        continue;
                    }

                    OnMarketProcessed?.Invoke(group.MarketTicker);
                }

                OnTestProgress?.Invoke("Market price history test completed.");
            }
            catch (Exception ex)
            {
                OnTestProgress?.Invoke($"Error: {ex.Message}");
                Assert.Fail(ex.ToString());
            }
        }

        public async Task RunSelectedSetForGuiAsync(
    string setKey,
    string weightName,
    bool writeToFile,
    int? maxGroups = null,
    List<string>? marketsToRun = null)
        {
            OnTestProgress?.Invoke($"Starting {setKey}:{weightName} test for GUI.");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var helper = new StrategySelectionHelper();
            var strategiesDict = helper.GetMapping(setKey, weightName); // unified mapping

            // Fetch the typed weights and serialize them for persistence
            string weightsJson = setKey switch
            {
                "Breakout2" => JsonSerializer.Serialize(
                    StrategySelectionHelper.BreakoutParameterSets.First(x => x.Name == weightName).Parameters),
                "Bollinger" => JsonSerializer.Serialize(
                    StrategySelectionHelper.BollingerParameterSets.First(x => x.Name == weightName).Parameters),
                "Nothing" => JsonSerializer.Serialize(
                    StrategySelectionHelper.NothingEverHappensParameterSets.First(x => x.Name == weightName).Parameters),
                _ => throw new ArgumentException($"Unknown set '{setKey}'", nameof(setKey))
            };

            var weightSetDto = new WeightSetDTO
            {
                StrategyName = weightName,
                Weights = weightsJson,
                LastRun = DateTime.UtcNow,
                WeightSetMarkets = new List<WeightSetMarketDTO>()
            };

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun);
            var uniqueMarketTickers = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();
            int totalMarkets = uniqueMarketTickers.Count;
            int processedMarkets = 0;

            foreach (var marketTicker in uniqueMarketTickers)
            {
                if (_processedMarkets.Contains(marketTicker))
                {
                    OnTestProgress?.Invoke($"Skipping already processed market: {marketTicker}");
                    continue;
                }

                OnTestProgress?.Invoke($"Loading market data for {marketTicker}");

                var marketGroups = filteredGroups.Where(g => g.MarketTicker == marketTicker).ToList();
                List<SnapshotDTO> allSnapshotData = new();
                foreach (var group in marketGroups)
                {
                    var snapshotData = await context.GetSnapshots_cached(
                        marketTicker: group.MarketTicker,
                        startDate: group.StartTime,
                        endDate: group.EndTime);
                    allSnapshotData.AddRange(snapshotData);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cacheSnapshotDict = await _snapshotService.LoadManySnapshots(allSnapshotData);
                var marketSnapshots = cacheSnapshotDict
                    .SelectMany(kvp => kvp.Value.Select(cs => cs))
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (!marketSnapshots.Any())
                {
                    OnTestProgress?.Invoke($"No valid snapshots for {marketTicker}. Skipping.");
                    continue;
                }

                var groupForId = new SnapshotGroupDTO { MarketTicker = marketTicker, JsonPath = $"{marketTicker}.json" };

                (double finalPnL, List<PricePoint> bidPoints, List<PricePoint> askPoints,
                 List<PricePoint> buyPoints, List<PricePoint> sellPoints,
                 List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints)
                    = await ProcessMarketAsync(
                        marketTicker, marketSnapshots, strategiesDict, _scopeFactory,
                        progressPrefix: $"Strategy set {setKey}/{weightName}: ", writeToFile: writeToFile, group: groupForId);

                if (bidPoints != null)
                {
                    if (writeToFile)
                        SaveMarketDataToFile(marketTicker, finalPnL, bidPoints, askPoints, buyPoints, sellPoints,
                                             eventPoints, intendedLongPoints, intendedShortPoints,
                                             fileNameSuffix: $"_{setKey}_{weightName}");

                    weightSetDto.WeightSetMarkets.Add(new WeightSetMarketDTO
                    {
                        MarketTicker = marketTicker,
                        PnL = (decimal)finalPnL,
                        LastRun = DateTime.UtcNow
                    });

                    OnProfitLossUpdate?.Invoke(marketTicker, finalPnL);
                    OnMarketProcessed?.Invoke(marketTicker);
                }

                processedMarkets++;
                OnTestProgress?.Invoke($"Progress: {processedMarkets}/{totalMarkets} markets processed.");

                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cacheSnapshotDict.Clear();
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    using var saveScope = _scopeFactory.CreateScope();
                    var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                    await saveContext.AddOrUpdateWeightSet(weightSetDto);
                    OnTestProgress?.Invoke($"Saved WeightSet for {setKey}/{weightName} with {weightSetDto.WeightSetMarkets.Count} market results to database.");
                }
                catch (Exception ex)
                {
                    OnTestProgress?.Invoke($"Error saving WeightSet for {setKey}/{weightName}: {ex.Message}");
                }
            });

            OnTestProgress?.Invoke($"{setKey}/{weightName} test for GUI completed.");
        }
        public async Task RunTrainingForGuiAsync(string setKey, bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
        {
            switch (setKey)
            {
                case "Breakout2":
                    await RunMultipleBreakoutForGuiAsync(writeToFile, maxGroups, marketsToRun);
                    break;
                case "Nothing":
                    await RunMultipleNothingHappensForGuiAsync(writeToFile, maxGroups, marketsToRun);
                    break;
                case "Bollinger":
                    // You already expose the mappings via helper.GetTrainingMappings("Bollinger");
                    // If you have no Bollinger runner yet, mirror RunMultipleBreakoutForGuiAsync
                    // using GetBollingerBreakoutStrategiesForTraining().
                    var helper = new StrategySelectionHelper();
                    var strategiesList = helper.GetTrainingMappings("Bollinger");
                    // reuse your existing Breakout loop as a template
                    // (copy of RunMultipleBreakoutForGuiAsync body, swapping the strategiesList)
                    throw new NotImplementedException("Add Bollinger runner using GetBollingerBreakoutStrategiesForTraining().");
                default:
                    throw new ArgumentException($"Unknown set '{setKey}'", nameof(setKey));
            }
        }

        // SimulatorTests.cs
        public async Task RunMultipleBollingerForGuiAsync(bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var helper = new StrategySelectionHelper();
            var strategiesList = helper.GetTrainingMappings("Bollinger");
            var paramSets = StrategySelectionHelper.BollingerParameterSets
                ?? throw new InvalidOperationException("BollingerParameterSets is null.");

            var weightSetDtos = new List<WeightSetDTO>();
            for (int i = 0; i < strategiesList.Count; i++)
            {
                var (name, parameters) = paramSets[i];
                weightSetDtos.Add(new WeightSetDTO
                {
                    StrategyName = name,
                    Weights = JsonSerializer.Serialize(parameters),
                    LastRun = DateTime.UtcNow,
                    WeightSetMarkets = new List<WeightSetMarketDTO>()
                });
            }

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();

            int totalSets = strategiesList.Count;
            int totalMarkets = uniqueMarkets.Count;
            OnTestProgress?.Invoke($"Bollinger: {totalSets} strategy sets × {totalMarkets} markets");

            for (int mIdx = 0; mIdx < uniqueMarkets.Count; mIdx++)
            {
                var market = uniqueMarkets[mIdx];
                var marketGroups = filteredGroups.Where(g => g.MarketTicker == market).ToList();

                var allSnapshotData = new List<SnapshotDTO>();
                foreach (var g in marketGroups)
                {
                    var snap = await context.GetSnapshots_cached(marketTicker: g.MarketTicker, startDate: g.StartTime, endDate: g.EndTime);
                    allSnapshotData.AddRange(snap);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cache = await _snapshotService.LoadManySnapshots(allSnapshotData);
                var marketSnapshots = cache
                    .SelectMany(kvp => kvp.Value)
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (marketSnapshots.Count == 0) continue;

                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                for (int setIdx = 0; setIdx < strategiesList.Count; setIdx++)
                {
                    var strategies = strategiesList[setIdx];
                    var dto = weightSetDtos[setIdx];

                    OnTestProgress?.Invoke($"Strategy Set {dto.StrategyName} ({setIdx + 1}/{totalSets}) processing market {market} ({mIdx + 1}/{totalMarkets})");

                    var (finalPnL, bid, ask, buy, sell, ev, il, ishort) =
                        await ProcessMarketAsync(
                            market, marketSnapshots, strategies, _scopeFactory,
                            progressPrefix: $"[Bollinger/{dto.StrategyName}] ",
                            writeToFile: writeToFile, group: groupForId,
                            ignoreProcessedCache: true);                    // do not skip within training runs

                    if (bid != null)
                    {
                        if (writeToFile)
                            SaveMarketDataToFile(market, finalPnL, bid, ask, buy, sell, ev, il, ishort,
                                fileNameSuffix: $"_Bollinger_{dto.StrategyName}");

                        dto.WeightSetMarkets.Add(new WeightSetMarketDTO
                        {
                            MarketTicker = market,
                            PnL = (decimal)finalPnL,
                            LastRun = DateTime.UtcNow
                        });

                        OnProfitLossUpdate?.Invoke(market, finalPnL);
                    }
                }

                OnMarketProcessed?.Invoke(market);

                // clear per market to keep memory in check
                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cache.Clear();
            }

            // Persist each weight set (awaited, no fire-and-forget)
            foreach (var dto in weightSetDtos)
            {
                using var saveScope = _scopeFactory.CreateScope();
                var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                await saveContext.AddOrUpdateWeightSet(dto);
                OnTestProgress?.Invoke($"Saved Bollinger/{dto.StrategyName} ({dto.WeightSetMarkets.Count}/{uniqueMarkets.Count} markets)");
            }

            OnTestProgress?.Invoke("Bollinger: all strategy sets completed");
        }




        // SimulatorTests.cs
        public async Task RunMultipleFlowMoForGuiAsync(bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var helper = new StrategySelectionHelper();
            var strategiesList = helper.GetTrainingMappings("FlowMo");
            var paramSets = StrategySelectionHelper.FlowMomentumParameterSets
                ?? throw new InvalidOperationException("FlowMomentumParameterSets is null.");

            var weightSetDtos = new List<WeightSetDTO>();
            for (int i = 0; i < strategiesList.Count; i++)
            {
                var (name, parameters) = paramSets[i];
                weightSetDtos.Add(new WeightSetDTO
                {
                    StrategyName = name,
                    Weights = JsonSerializer.Serialize(parameters),
                    LastRun = DateTime.UtcNow,
                    WeightSetMarkets = new List<WeightSetMarketDTO>()
                });
            }

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();

            int totalSets = strategiesList.Count;
            int totalMarkets = uniqueMarkets.Count;
            OnTestProgress?.Invoke($"FlowMo: {totalSets} strategy sets × {totalMarkets} markets");

            // MARKETS → SETS
            for (int mIdx = 0; mIdx < uniqueMarkets.Count; mIdx++)
            {
                var market = uniqueMarkets[mIdx];
                var marketGroups = filteredGroups.Where(g => g.MarketTicker == market).ToList();

                var allSnapshotData = new List<SnapshotDTO>();
                foreach (var g in marketGroups)
                {
                    var snap = await context.GetSnapshots_cached(marketTicker: g.MarketTicker, startDate: g.StartTime, endDate: g.EndTime);
                    allSnapshotData.AddRange(snap);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cache = await _snapshotService.LoadManySnapshots(allSnapshotData);
                var marketSnapshots = cache
                    .SelectMany(kvp => kvp.Value)
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (marketSnapshots.Count == 0) continue;

                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                for (int setIdx = 0; setIdx < strategiesList.Count; setIdx++)
                {
                    var strategies = strategiesList[setIdx];
                    var dto = weightSetDtos[setIdx];

                    OnTestProgress?.Invoke($"Strategy Set {dto.StrategyName} ({setIdx + 1}/{totalSets}) processing market {market} ({mIdx + 1}/{totalMarkets})");

                    var (finalPnL, bid, ask, buy, sell, ev, il, ishort) =
                        await ProcessMarketAsync(
                            market, marketSnapshots, strategies, _scopeFactory,
                            progressPrefix: $"[FlowMo/{dto.StrategyName}] ",
                            writeToFile: writeToFile, group: groupForId,
                            ignoreProcessedCache: true);

                    if (bid != null)
                    {
                        if (writeToFile)
                            SaveMarketDataToFile(market, finalPnL, bid, ask, buy, sell, ev, il, ishort,
                                fileNameSuffix: $"_FlowMo_{dto.StrategyName}");

                        dto.WeightSetMarkets.Add(new WeightSetMarketDTO
                        {
                            MarketTicker = market,
                            PnL = (decimal)finalPnL,
                            LastRun = DateTime.UtcNow
                        });

                        OnProfitLossUpdate?.Invoke(market, finalPnL);
                    }
                }

                OnMarketProcessed?.Invoke(market);

                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cache.Clear();
            }

            foreach (var dto in weightSetDtos)
            {
                using var saveScope = _scopeFactory.CreateScope();
                var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                await saveContext.AddOrUpdateWeightSet(dto);
                OnTestProgress?.Invoke($"Saved FlowMo/{dto.StrategyName} ({dto.WeightSetMarkets.Count}/{uniqueMarkets.Count} markets)");
            }

            OnTestProgress?.Invoke("FlowMo: all strategy sets completed");
        }




        public async Task RunWeightsForGuiAsync(bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
        {
            OnTestProgress?.Invoke("Starting weight test for GUI.");

            string weightName = "B2_AntiSpike_C";

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var strategyHelper = new StrategySelectionHelper();
            var strategiesDict = strategyHelper.GetBreakoutStrategy(weightName);

            var paramSets = StrategySelectionHelper.BreakoutParameterSets
                ?? throw new InvalidOperationException("BreakoutParameterSets is null.");

            var (strategyName, parameters) = paramSets.First(x => x.Name == weightName);

            var weightSetDto = new WeightSetDTO
            {
                StrategyName = strategyName,
                Weights = JsonSerializer.Serialize(parameters),
                LastRun = DateTime.UtcNow,
                WeightSetMarkets = new List<WeightSetMarketDTO>()
            };

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun);
            var uniqueMarketTickers = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();
            int totalMarkets = uniqueMarketTickers.Count;

            int processedMarkets = 0;

            foreach (var marketTicker in uniqueMarketTickers)
            {
                if (_processedMarkets.Contains(marketTicker))
                {
                    OnTestProgress?.Invoke($"Skipping already processed market: {marketTicker}");
                    continue;
                }

                OnTestProgress?.Invoke($"Loading market data for {marketTicker}");

                var marketGroups = filteredGroups.Where(g => g.MarketTicker == marketTicker).ToList();
                List<SnapshotDTO> allSnapshotData = new List<SnapshotDTO>();
                foreach (var group in marketGroups)
                {
                    var snapshotData = await context.GetSnapshots_cached(
                        marketTicker: group.MarketTicker,
                        startDate: group.StartTime,
                        endDate: group.EndTime);
                    allSnapshotData.AddRange(snapshotData);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cacheSnapshotDict = await _snapshotService.LoadManySnapshots(allSnapshotData);
                var marketSnapshots = cacheSnapshotDict
                    .SelectMany(kvp => kvp.Value.Select(cs => cs))
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (!marketSnapshots.Any())
                {
                    OnTestProgress?.Invoke($"No valid snapshots for {marketTicker}. Skipping.");
                    continue;
                }

                // Create a dummy SnapshotGroupDTO for uniqueId derivation
                var groupForId = new SnapshotGroupDTO { MarketTicker = marketTicker, JsonPath = $"{marketTicker}.json" };

                (double finalPnL, List<PricePoint> bidPoints, List<PricePoint> askPoints, List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints) =
                    await ProcessMarketAsync(marketTicker, marketSnapshots, strategiesDict, _scopeFactory,
                        progressPrefix: $"Strategy set {strategyName}: ", writeToFile: writeToFile, group: groupForId);

                if (bidPoints != null)
                {
                    if (writeToFile) SaveMarketDataToFile(marketTicker, finalPnL, bidPoints, askPoints, buyPoints, sellPoints, eventPoints, intendedLongPoints, intendedShortPoints);

                    weightSetDto.WeightSetMarkets.Add(new WeightSetMarketDTO
                    {
                        MarketTicker = marketTicker,
                        PnL = (decimal)finalPnL,
                        LastRun = DateTime.UtcNow
                    });

                    OnProfitLossUpdate?.Invoke(marketTicker, finalPnL);
                    OnMarketProcessed?.Invoke(marketTicker);
                }

                processedMarkets++;
                OnTestProgress?.Invoke($"Progress: {processedMarkets}/{totalMarkets} markets processed.");

                // Clear market snapshots to free memory
                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cacheSnapshotDict.Clear();
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                    await context.AddOrUpdateWeightSet(weightSetDto);
                    OnTestProgress?.Invoke($"Saved WeightSet for strategy {strategyName} with {weightSetDto.WeightSetMarkets.Count} market results to database.");
                }
                catch (Exception ex)
                {
                    OnTestProgress?.Invoke($"Error saving WeightSet for strategy {strategyName} to database: {ex.Message}");
                }
            });

            OnTestProgress?.Invoke("Breakout test for GUI completed.");
        }

        public async Task RunMultipleBreakoutForGuiAsync(bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
        {
            OnTestProgress?.Invoke("Starting multiple Breakout tests for GUI.");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var strategyHelper = new StrategySelectionHelper();
            var strategiesList = strategyHelper.GetBreakoutStrategiesForTraining();

            var paramSets = StrategySelectionHelper.BreakoutParameterSets
                ?? throw new InvalidOperationException("BreakoutParameterSets is null.");

            var weightSetDtos = new List<WeightSetDTO>();
            for (int strategyIndex = 0; strategyIndex < strategiesList.Count; strategyIndex++)
            {
                var (strategyName, parameters) = paramSets[strategyIndex];
                weightSetDtos.Add(new WeightSetDTO
                {
                    StrategyName = strategyName,
                    Weights = JsonSerializer.Serialize(parameters),
                    LastRun = DateTime.UtcNow,
                    WeightSetMarkets = new List<WeightSetMarketDTO>()
                });
            }

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun);
            var uniqueMarketTickers = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();
            int totalMarkets = uniqueMarketTickers.Count;
            int processedMarkets = 0;

            foreach (var marketTicker in uniqueMarketTickers)
            {
                if (_processedMarkets.Contains(marketTicker))
                {
                    OnTestProgress?.Invoke($"Skipping already processed market: {marketTicker}");
                    continue;
                }

                OnTestProgress?.Invoke($"Loading market data for {marketTicker}");

                var marketGroups = filteredGroups.Where(g => g.MarketTicker == marketTicker).ToList();
                List<SnapshotDTO> allSnapshotData = new List<SnapshotDTO>();
                foreach (var group in marketGroups)
                {
                    var snapshotData = await context.GetSnapshots_cached(
                        marketTicker: group.MarketTicker,
                        startDate: group.StartTime,
                        endDate: group.EndTime);
                    allSnapshotData.AddRange(snapshotData);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cacheSnapshotDict = await _snapshotService.LoadManySnapshots(allSnapshotData);
                var marketSnapshots = cacheSnapshotDict
                    .SelectMany(kvp => kvp.Value.Select(cs => cs))
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (!marketSnapshots.Any())
                {
                    OnTestProgress?.Invoke($"No valid snapshots for {marketTicker}. Skipping.");
                    continue;
                }

                // Create a dummy SnapshotGroupDTO for uniqueId derivation
                var groupForId = new SnapshotGroupDTO { MarketTicker = marketTicker, JsonPath = $"{marketTicker}.json" };

                for (int strategyIndex = 0; strategyIndex < strategiesList.Count; strategyIndex++)
                {
                    var strategiesDict = strategiesList[strategyIndex];
                    var strategyName = weightSetDtos[strategyIndex].StrategyName;
                    OnTestProgress?.Invoke($"Processing Breakout strategy set {strategyName} ({strategyIndex + 1}/{strategiesList.Count}) for market {marketTicker}");

                    (double finalPnL, List<PricePoint> bidPoints, List<PricePoint> askPoints, List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints) =
                        await ProcessMarketAsync(marketTicker, marketSnapshots, strategiesDict, _scopeFactory,
                            progressPrefix: $"Breakout strategy {strategyName}: ", writeToFile: writeToFile, group: groupForId);

                    if (bidPoints != null)
                    {
                        if (writeToFile) SaveMarketDataToFile(marketTicker, finalPnL, bidPoints, askPoints, buyPoints, sellPoints, eventPoints, intendedLongPoints, intendedShortPoints, fileNameSuffix: $"_Breakout_{strategyName}");

                        weightSetDtos[strategyIndex].WeightSetMarkets.Add(new WeightSetMarketDTO
                        {
                            MarketTicker = marketTicker,
                            PnL = (decimal)finalPnL,
                            LastRun = DateTime.UtcNow
                        });

                        OnProfitLossUpdate?.Invoke(marketTicker, finalPnL);
                    }
                }

                _processedMarkets.Add(marketTicker);
                OnMarketProcessed?.Invoke(marketTicker);

                processedMarkets++;
                OnTestProgress?.Invoke($"Progress: {processedMarkets}/{totalMarkets} markets processed for Breakout strategies.");

                // Clear market snapshots to free memory
                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cacheSnapshotDict.Clear();
            }

            // Save all WeightSetDTOs
            foreach (var weightSetDto in weightSetDtos)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var saveScope = _scopeFactory.CreateScope();
                        var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                        await saveContext.AddOrUpdateWeightSet(weightSetDto);
                        OnTestProgress?.Invoke($"Saved WeightSet for Breakout strategy {weightSetDto.StrategyName} with {weightSetDto.WeightSetMarkets.Count} market results to database.");
                    }
                    catch (Exception ex)
                    {
                        OnTestProgress?.Invoke($"Error saving WeightSet for Breakout strategy {weightSetDto.StrategyName} to database: {ex.Message}");
                    }
                });
            }

            OnTestProgress?.Invoke("Multiple Breakout tests for GUI completed.");
        }

        public async Task RunFlowMoForGuiAsync(bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
        {
            OnTestProgress?.Invoke("Starting multiple FlowMo tests for GUI.");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var strategyHelper = new StrategySelectionHelper();
            var strategiesList = strategyHelper.GetFlowMomentumStrategiesForTraining();

            var paramSets = StrategySelectionHelper.FlowMomentumParameterSets
                ?? throw new InvalidOperationException("BreakoutParameterSets is null.");

            var weightSetDtos = new List<WeightSetDTO>();
            for (int strategyIndex = 0; strategyIndex < strategiesList.Count; strategyIndex++)
            {
                var (strategyName, parameters) = paramSets[strategyIndex];
                weightSetDtos.Add(new WeightSetDTO
                {
                    StrategyName = strategyName,
                    Weights = JsonSerializer.Serialize(parameters),
                    LastRun = DateTime.UtcNow,
                    WeightSetMarkets = new List<WeightSetMarketDTO>()
                });
            }

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun);
            var uniqueMarketTickers = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();
            int totalMarkets = uniqueMarketTickers.Count;
            int processedMarkets = 0;

            foreach (var marketTicker in uniqueMarketTickers)
            {
                if (_processedMarkets.Contains(marketTicker))
                {
                    OnTestProgress?.Invoke($"Skipping already processed market: {marketTicker}");
                    continue;
                }

                OnTestProgress?.Invoke($"Loading market data for {marketTicker}");

                var marketGroups = filteredGroups.Where(g => g.MarketTicker == marketTicker).ToList();
                List<SnapshotDTO> allSnapshotData = new List<SnapshotDTO>();
                foreach (var group in marketGroups)
                {
                    var snapshotData = await context.GetSnapshots_cached(
                        marketTicker: group.MarketTicker,
                        startDate: group.StartTime,
                        endDate: group.EndTime);
                    allSnapshotData.AddRange(snapshotData);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cacheSnapshotDict = await _snapshotService.LoadManySnapshots(allSnapshotData);
                var marketSnapshots = cacheSnapshotDict
                    .SelectMany(kvp => kvp.Value.Select(cs => cs))
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (!marketSnapshots.Any())
                {
                    OnTestProgress?.Invoke($"No valid snapshots for {marketTicker}. Skipping.");
                    continue;
                }

                // Create a dummy SnapshotGroupDTO for uniqueId derivation
                var groupForId = new SnapshotGroupDTO { MarketTicker = marketTicker, JsonPath = $"{marketTicker}.json" };

                for (int strategyIndex = 0; strategyIndex < strategiesList.Count; strategyIndex++)
                {
                    var strategiesDict = strategiesList[strategyIndex];
                    var strategyName = weightSetDtos[strategyIndex].StrategyName;
                    OnTestProgress?.Invoke($"Processing Breakout strategy set {strategyName} ({strategyIndex + 1}/{strategiesList.Count}) for market {marketTicker}");

                    (double finalPnL, List<PricePoint> bidPoints, List<PricePoint> askPoints, List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints) =
                        await ProcessMarketAsync(marketTicker, marketSnapshots, strategiesDict, _scopeFactory,
                            progressPrefix: $"Breakout strategy {strategyName}: ", writeToFile: writeToFile, group: groupForId);

                    if (bidPoints != null)
                    {
                        if (writeToFile) SaveMarketDataToFile(marketTicker, finalPnL, bidPoints, askPoints, buyPoints, sellPoints, eventPoints, intendedLongPoints, intendedShortPoints, fileNameSuffix: $"_Breakout_{strategyName}");

                        weightSetDtos[strategyIndex].WeightSetMarkets.Add(new WeightSetMarketDTO
                        {
                            MarketTicker = marketTicker,
                            PnL = (decimal)finalPnL,
                            LastRun = DateTime.UtcNow
                        });

                        OnProfitLossUpdate?.Invoke(marketTicker, finalPnL);
                    }
                }

                _processedMarkets.Add(marketTicker);
                OnMarketProcessed?.Invoke(marketTicker);

                processedMarkets++;
                OnTestProgress?.Invoke($"Progress: {processedMarkets}/{totalMarkets} markets processed for Breakout strategies.");

                // Clear market snapshots to free memory
                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cacheSnapshotDict.Clear();
            }

            // Save all WeightSetDTOs
            foreach (var weightSetDto in weightSetDtos)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var saveScope = _scopeFactory.CreateScope();
                        var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                        await saveContext.AddOrUpdateWeightSet(weightSetDto);
                        OnTestProgress?.Invoke($"Saved WeightSet for Breakout strategy {weightSetDto.StrategyName} with {weightSetDto.WeightSetMarkets.Count} market results to database.");
                    }
                    catch (Exception ex)
                    {
                        OnTestProgress?.Invoke($"Error saving WeightSet for Breakout strategy {weightSetDto.StrategyName} to database: {ex.Message}");
                    }
                });
            }

            OnTestProgress?.Invoke("Multiple Breakout tests for GUI completed.");
        }

        public async Task RunMultipleNothingHappensForGuiAsync(bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
        {
            OnTestProgress?.Invoke("Starting multiple Nothing Happens tests for GUI.");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var strategyHelper = new StrategySelectionHelper();
            var strategiesList = strategyHelper.GetNothingEverHappensStrategiesForTraining();

            var paramSets = StrategySelectionHelper.NothingEverHappensParameterSets
                ?? throw new InvalidOperationException("NothingEverHappensParameterSets is null.");

            var weightSetDtos = new List<WeightSetDTO>();
            for (int strategyIndex = 0; strategyIndex < strategiesList.Count; strategyIndex++)
            {
                var (strategyName, parameters) = paramSets[strategyIndex];
                weightSetDtos.Add(new WeightSetDTO
                {
                    StrategyName = strategyName,
                    Weights = JsonSerializer.Serialize(parameters),
                    LastRun = DateTime.UtcNow,
                    WeightSetMarkets = new List<WeightSetMarketDTO>()
                });
            }

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun);
            var uniqueMarketTickers = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();
            int totalMarkets = uniqueMarketTickers.Count;
            int processedMarkets = 0;

            foreach (var marketTicker in uniqueMarketTickers)
            {
                if (_processedMarkets.Contains(marketTicker))
                {
                    OnTestProgress?.Invoke($"Skipping already processed market: {marketTicker}");
                    continue;
                }

                OnTestProgress?.Invoke($"Loading market data for {marketTicker}");

                var marketGroups = filteredGroups.Where(g => g.MarketTicker == marketTicker).ToList();
                List<SnapshotDTO> allSnapshotData = new List<SnapshotDTO>();
                foreach (var group in marketGroups)
                {
                    var snapshotData = await context.GetSnapshots_cached(
                        marketTicker: group.MarketTicker,
                        startDate: group.StartTime,
                        endDate: group.EndTime);
                    allSnapshotData.AddRange(snapshotData);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cacheSnapshotDict = await _snapshotService.LoadManySnapshots(allSnapshotData);
                var marketSnapshots = cacheSnapshotDict
                    .SelectMany(kvp => kvp.Value.Select(cs => cs))
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (!marketSnapshots.Any())
                {
                    OnTestProgress?.Invoke($"No valid snapshots for {marketTicker}. Skipping.");
                    continue;
                }

                // Create a dummy SnapshotGroupDTO for uniqueId derivation
                var groupForId = new SnapshotGroupDTO { MarketTicker = marketTicker, JsonPath = $"{marketTicker}.json" };

                for (int strategyIndex = 0; strategyIndex < strategiesList.Count; strategyIndex++)
                {
                    var strategiesDict = strategiesList[strategyIndex];
                    var strategyName = weightSetDtos[strategyIndex].StrategyName;
                    OnTestProgress?.Invoke($"Processing Nothing Happens strategy set {strategyName} ({strategyIndex + 1}/{strategiesList.Count}) for market {marketTicker}");

                    (double finalPnL, List<PricePoint> bidPoints, List<PricePoint> askPoints, List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints) =
                        await ProcessMarketAsync(marketTicker, marketSnapshots, strategiesDict, _scopeFactory,
                            progressPrefix: $"Nothing Happens strategy {strategyName}: ", writeToFile: writeToFile, group: groupForId);

                    if (bidPoints != null)
                    {
                        if (writeToFile) SaveMarketDataToFile(marketTicker, finalPnL, bidPoints, askPoints, buyPoints, sellPoints, eventPoints, intendedLongPoints, intendedShortPoints, fileNameSuffix: $"_NothingHappens_{strategyName}");

                        weightSetDtos[strategyIndex].WeightSetMarkets.Add(new WeightSetMarketDTO
                        {
                            MarketTicker = marketTicker,
                            PnL = (decimal)finalPnL,
                            LastRun = DateTime.UtcNow
                        });

                        OnProfitLossUpdate?.Invoke(marketTicker, finalPnL);
                    }
                }

                _processedMarkets.Add(marketTicker);
                OnMarketProcessed?.Invoke(marketTicker);

                processedMarkets++;
                OnTestProgress?.Invoke($"Progress: {processedMarkets}/{totalMarkets} markets processed for Nothing Happens strategies.");

                // Clear market snapshots to free memory
                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cacheSnapshotDict.Clear();
            }

            // Save all WeightSetDTOs
            foreach (var weightSetDto in weightSetDtos)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var saveScope = _scopeFactory.CreateScope();
                        var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                        await saveContext.AddOrUpdateWeightSet(weightSetDto);
                        OnTestProgress?.Invoke($"Saved WeightSet for Nothing Happens strategy {weightSetDto.StrategyName} with {weightSetDto.WeightSetMarkets.Count} market results to database.");
                    }
                    catch (Exception ex)
                    {
                        OnTestProgress?.Invoke($"Error saving WeightSet for Nothing Happens strategy {weightSetDto.StrategyName} to database: {ex.Message}");
                    }
                });
            }


            OnTestProgress?.Invoke("Multiple Nothing Happens tests for GUI completed.");
        }

        public async Task RunMultipleAllStrategiesForGuiAsync(bool writeToFile, int? maxGroups = null, List<string>? marketsToRun = null)
        {
            OnTestProgress?.Invoke("Starting all multiple strategy tests for GUI.");

            // Reset processed markets to ensure clean state
            _processedMarkets.Clear();

            await RunMultipleFlowMoForGuiAsync(writeToFile, maxGroups, marketsToRun);
            //await RunMultipleBreakoutForGuiAsync(writeToFile, maxGroups, marketsToRun);
            //await RunMultipleNothingHappensForGuiAsync(writeToFile, maxGroups, marketsToRun);

            OnTestProgress?.Invoke("All multiple strategy tests for GUI completed.");
        }

        public async Task<HashSet<string>> GetSnapshotGroupNames()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            return await context.GetSnapshotGroupNames();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
            _sqlDataService.Dispose();
            OnTestProgress?.Invoke("TearDown completed.");
        }
    }
}