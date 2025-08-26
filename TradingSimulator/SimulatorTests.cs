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

        public void EnsureInitialized()
        {
            if (_scopeFactory == null) Setup();
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


        private async Task<(double finalPnL, List<PricePoint> bidPoints, List<PricePoint> askPoints,
         List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
         List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints)>
     ProcessMarketAsync(
         string marketTicker,
         List<MarketSnapshot> marketSnapshots,
         Dictionary<MarketType, List<Strategy>> strategiesDict,
         IServiceScopeFactory scopeFactory,
         string progressPrefix = "",
         bool writeToFile = false,
         SnapshotGroupDTO? group = null,
         bool ignoreProcessedCache = false)
        {
            if (!ignoreProcessedCache && _processedMarkets.Contains(marketTicker))
            {
                OnTestProgress?.Invoke($"{progressPrefix}Skipping cached market: {marketTicker}");
                return (0, null, null, null, null, null, null, null, null);
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

                    var bidPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesBid, " Best Bid")).ToList();
                    var askPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesAsk, " Best Ask")).ToList();

                    var buyPoints = new List<PricePoint>();
                    var sellPoints = new List<PricePoint>();
                    var exitPoints = new List<PricePoint>();
                    var intendedLongPoints = new List<PricePoint>();
                    var intendedShortPoints = new List<PricePoint>();

                    int prevPos = 0;
                    foreach (var ev in eventLogs)
                    {
                        string prefix = " ";

                        // trade markers (existing)
                        if (ev.Position != prevPos)
                        {
                            if (ev.Position > prevPos)
                                buyPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesAsk, prefix + ev.Memo));
                            else
                                sellPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesBid, prefix + ev.Memo));
                        }
                        else
                        {
                            if (ev.Action == "Long")
                                intendedLongPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesAsk, prefix + "Intended Long: " + ev.Memo));
                            else if (ev.Action == "Short")
                                intendedShortPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesBid, prefix + "Intended Short: " + ev.Memo));
                        }

                        // NEW: explicit exits (close long at bid; close short at ask)
                        bool isExit = string.Equals(ev.Action, "Exit", StringComparison.OrdinalIgnoreCase)
                                      || (prevPos != 0 && ev.Position == 0);
                        if (isExit)
                        {
                            if (prevPos > 0)
                                exitPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesBid, prefix + "Exit Long: " + ev.Memo));
                            else if (prevPos < 0)
                                exitPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesAsk, prefix + "Exit Short: " + ev.Memo));
                        }

                        prevPos = ev.Position;
                    }

                    var eventPoints = eventLogs
                        .Select(ev => new PricePoint(ev.Timestamp, (ev.BestYesBid + ev.BestYesAsk) / 2.0, " " + ev.Memo))
                        .ToList();

                    return (finalPnL, bidPoints, askPoints, buyPoints, sellPoints, exitPoints, eventPoints, intendedLongPoints, intendedShortPoints);
                }
            }
            finally
            {
                OnTestProgress?.Invoke($"{progressPrefix}Cleared memory for market: {marketTicker}");
            }
            return (0, null, null, null, null, null, null, null, null);
        }



        private void SaveMarketDataToFile(
        string marketTicker, double finalPnL,
        List<PricePoint> bidPoints, List<PricePoint> askPoints,
        List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
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
                ExitPoints = exitPoints,
                EventPoints = eventPoints,
                IntendedLongPoints = intendedLongPoints,
                IntendedShortPoints = intendedShortPoints
            };

            var json = JsonSerializer.Serialize(cachedData);
            Directory.CreateDirectory(_cacheDirectory);
            var filePath = Path.Combine(_cacheDirectory, $"{marketTicker}{fileNameSuffix}.json");
            File.WriteAllText(filePath, json);

            var baseTicker = System.Text.RegularExpressions.Regex.Replace(marketTicker, @"_(\d+)$", "");
            var groupFiles = Directory.GetFiles(_cacheDirectory, $"{baseTicker}_*.json");

            var merged = new CachedMarketData
            {
                Market = baseTicker,
                PnL = 0,
                BidPoints = new List<PricePoint>(),
                AskPoints = new List<PricePoint>(),
                BuyPoints = new List<PricePoint>(),
                SellPoints = new List<PricePoint>(),
                ExitPoints = new List<PricePoint>(),
                EventPoints = new List<PricePoint>(),
                IntendedLongPoints = new List<PricePoint>(),
                IntendedShortPoints = new List<PricePoint>()
            };

            foreach (var gf in groupFiles)
            {
                var gj = File.ReadAllText(gf);
                var gd = JsonSerializer.Deserialize<CachedMarketData>(gj);
                if (gd == null) continue;
                merged.PnL += gd.PnL;
                if (gd.BidPoints != null) merged.BidPoints.AddRange(gd.BidPoints);
                if (gd.AskPoints != null) merged.AskPoints.AddRange(gd.AskPoints);
                if (gd.BuyPoints != null) merged.BuyPoints.AddRange(gd.BuyPoints);
                if (gd.SellPoints != null) merged.SellPoints.AddRange(gd.SellPoints);
                if (gd.ExitPoints != null) merged.ExitPoints.AddRange(gd.ExitPoints);
                if (gd.EventPoints != null) merged.EventPoints.AddRange(gd.EventPoints);
                if (gd.IntendedLongPoints != null) merged.IntendedLongPoints.AddRange(gd.IntendedLongPoints);
                if (gd.IntendedShortPoints != null) merged.IntendedShortPoints.AddRange(gd.IntendedShortPoints);
            }

            merged.BidPoints = merged.BidPoints.OrderBy(p => p.Date).ToList();
            merged.AskPoints = merged.AskPoints.OrderBy(p => p.Date).ToList();
            merged.BuyPoints = merged.BuyPoints.OrderBy(p => p.Date).ToList();
            merged.SellPoints = merged.SellPoints.OrderBy(p => p.Date).ToList();
            merged.ExitPoints = merged.ExitPoints.OrderBy(p => p.Date).ToList();
            merged.EventPoints = merged.EventPoints.OrderBy(p => p.Date).ToList();
            merged.IntendedLongPoints = merged.IntendedLongPoints.OrderBy(p => p.Date).ToList();
            merged.IntendedShortPoints = merged.IntendedShortPoints.OrderBy(p => p.Date).ToList();

            var canonicalPath = Path.Combine(_cacheDirectory, $"{baseTicker}.json");
            File.WriteAllText(canonicalPath, JsonSerializer.Serialize(merged));
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
    List<string>? marketsToRun = null,
    int? maxGroups = null)
        {
            // map provided setKey -> family
            var family = MapFamilyFromSetKey(setKey);

            // resolve strategies + param sets for that family
            var (strategiesList, paramSets, label) = ResolveFamily(family);

            // find the requested weight set by name (exact, case-insensitive)
            var idx = paramSets.FindIndex(ps =>
                string.Equals(ps.Name, weightName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                throw new InvalidOperationException($"Weight set '{weightName}' not found in {label}.");

            // prep context once
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            // create DTO for just this one set
            var (name, parameters) = paramSets[idx];
            var dto = new WeightSetDTO
            {
                StrategyName = name,
                Weights = JsonSerializer.Serialize(parameters),
                LastRun = DateTime.UtcNow,
                WeightSetMarkets = new List<WeightSetMarketDTO>()
            };

            // filter groups/markets
            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun).ConfigureAwait(false);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();

            OnTestProgress?.Invoke($"{label}/{dto.StrategyName}: 1 strategy set × {uniqueMarkets.Count} markets");

            // iterate markets
            for (int mIdx = 0; mIdx < uniqueMarkets.Count; mIdx++)
            {
                var market = uniqueMarkets[mIdx];
                var marketGroups = filteredGroups.Where(g => g.MarketTicker == market).ToList();

                var allSnapshotData = new List<SnapshotDTO>();
                foreach (var g in marketGroups)
                {
                    var snaps = await context.GetSnapshots_cached(
                        marketTicker: g.MarketTicker,
                        startDate: g.StartTime,
                        endDate: g.EndTime).ConfigureAwait(false);
                    allSnapshotData.AddRange(snaps);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cache = await _snapshotService.LoadManySnapshots(allSnapshotData).ConfigureAwait(false);
                var marketSnapshots = cache
                    .SelectMany(kvp => kvp.Value)
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (marketSnapshots.Count == 0) continue;

                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                var strategies = strategiesList[idx]; // only the selected set
                OnTestProgress?.Invoke($"[{label}/{dto.StrategyName}] market {market} ({mIdx + 1}/{uniqueMarkets.Count})");

                var (finalPnL, bid, ask, buy, sell, exit, ev, il, ishort) =
                    await ProcessMarketAsync(
                        market, marketSnapshots, strategies, _scopeFactory,
                        progressPrefix: $"[{label}/{dto.StrategyName}] ",
                        writeToFile: writeToFile, group: groupForId,
                        ignoreProcessedCache: true).ConfigureAwait(false);

                if (bid != null)
                {
                    if (writeToFile)
                        SaveMarketDataToFile(market, finalPnL, bid, ask, buy, sell, exit, ev, il, ishort,
                            fileNameSuffix: $"_{label}_{dto.StrategyName}");

                    dto.WeightSetMarkets.Add(new WeightSetMarketDTO
                    {
                        MarketTicker = market,
                        PnL = (decimal)finalPnL,
                        LastRun = DateTime.UtcNow
                    });

                    OnProfitLossUpdate?.Invoke(market, finalPnL);
                }

                OnMarketProcessed?.Invoke(market);
                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cache.Clear();
            }

            // save the one set
            using var saveScope = _scopeFactory.CreateScope();
            var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            await saveContext.AddOrUpdateWeightSet(dto).ConfigureAwait(false);

            OnTestProgress?.Invoke($"Saved {label}/{dto.StrategyName} ({dto.WeightSetMarkets.Count}/{uniqueMarkets.Count} markets)");
            OnTestProgress?.Invoke($"{label}/{dto.StrategyName}: completed");
        }
        
        private StrategyFamily MapFamilyFromSetKey(string setKey)
        {
            if (string.IsNullOrWhiteSpace(setKey)) throw new ArgumentException("setKey is required.", nameof(setKey));
            var k = setKey.Trim().ToLowerInvariant();
            if (k.Contains("breakout")) return StrategyFamily.Breakout;
            if (k.Contains("bollinger")) return StrategyFamily.Bollinger;
            if (k.Contains("flowmo") || k.Contains("flow")) return StrategyFamily.FlowMo;
            if (k.Contains("nothing")) return StrategyFamily.NothingHappens;
            if (k.Contains("momentum")) return StrategyFamily.Momentum;
            // specific short names
            if (k is "b2" or "breakout2") return StrategyFamily.Breakout;
            if (k is "bb" or "bollingerbreakout") return StrategyFamily.Bollinger;
            throw new ArgumentOutOfRangeException(nameof(setKey), $"Unrecognized setKey: {setKey}");
        }


        public async Task RunMultipleAllStrategiesForGuiAsync(
            bool writeToFile,
            int? maxGroups = null,
            List<string>? marketsToRun = null)
        {
            // run all families automatically
            var families = new[] {
        //StrategyFamily.Bollinger,
        //StrategyFamily.FlowMo,
        //StrategyFamily.Breakout,
        StrategyFamily.Momentum,
        //StrategyFamily.NothingHappens
    };

            foreach (var fam in families)
            {
                await RunMultipleForGuiAsync(
                    family: fam,
                    writeToFile: writeToFile,
                    maxGroups: maxGroups,
                    marketsToRun: marketsToRun).ConfigureAwait(false);
            }
        }

        public async Task<HashSet<string>> GetSnapshotGroupNames()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            return await context.GetSnapshotGroupNames();
        }

  
        public enum StrategyFamily
        {
            Bollinger,
            FlowMo,
            Breakout,
            NothingHappens,
            Momentum
        }

        private (List<Dictionary<MarketType, List<Strategy>>> Strategies,
                 List<(string Name, object Parameters)> ParamSets,
                 string Label)
        ResolveFamily(StrategyFamily family)
        {
            var helper = new StrategySelectionHelper();

            switch (family)
            {
                case StrategyFamily.Bollinger:
                    return (
                        helper.GetTrainingMappings("Bollinger"),
                        (StrategySelectionHelper.BollingerParameterSets
                            ?? throw new InvalidOperationException("BollingerParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "Bollinger"
                    );

                case StrategyFamily.FlowMo:
                    return (
                        helper.GetTrainingMappings("FlowMo"),
                        (StrategySelectionHelper.FlowMomentumParameterSets
                            ?? throw new InvalidOperationException("FlowMomentumParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "FlowMo"
                    );

                case StrategyFamily.Breakout:
                    return (
                        helper.GetBreakoutStrategiesForTraining(),
                        (StrategySelectionHelper.BreakoutParameterSets
                            ?? throw new InvalidOperationException("BreakoutParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "Breakout"
                    );

                case StrategyFamily.NothingHappens:
                    return (
                        helper.GetNothingEverHappensStrategiesForTraining(),
                        (StrategySelectionHelper.NothingEverHappensParameterSets
                            ?? throw new InvalidOperationException("NothingEverHappensParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "NothingHappens"
                    );
                case StrategyFamily.Momentum:
                    return (
                        helper.GetMomentumTradingStrategiesForTraining(),
                        (StrategySelectionHelper.MomentumTradingParameterSets
                            ?? throw new InvalidOperationException("MomentumTradingParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "Momentum"
                    );
                default:
                    throw new ArgumentOutOfRangeException(nameof(family));
            }
        }


        public async Task RunMultipleForGuiAsync(
            StrategyFamily family,
            bool writeToFile,
            int? maxGroups = null,
            List<string>? marketsToRun = null)
        {
            var (strategiesList, paramSets, label) = ResolveFamily(family);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var weightSetDtos = new List<WeightSetDTO>(paramSets.Count);
            for (int i = 0; i < paramSets.Count; i++)
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

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun).ConfigureAwait(false);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();

            OnTestProgress?.Invoke($"{label}: {strategiesList.Count} strategy sets × {uniqueMarkets.Count} markets");

            for (int mIdx = 0; mIdx < uniqueMarkets.Count; mIdx++)
            {
                var market = uniqueMarkets[mIdx];
                var marketGroups = filteredGroups.Where(g => g.MarketTicker == market).ToList();

                var allSnapshotData = new List<SnapshotDTO>();
                foreach (var g in marketGroups)
                {
                    var snaps = await context.GetSnapshots_cached(
                        marketTicker: g.MarketTicker,
                        startDate: g.StartTime,
                        endDate: g.EndTime).ConfigureAwait(false);
                    allSnapshotData.AddRange(snaps);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cache = await _snapshotService.LoadManySnapshots(allSnapshotData).ConfigureAwait(false);
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

                    OnTestProgress?.Invoke($"[{label}/{dto.StrategyName}] market {market} ({mIdx + 1}/{uniqueMarkets.Count}) set {setIdx + 1}/{strategiesList.Count}");

                    var (finalPnL, bid, ask, buy, sell, exit, ev, il, ishort) =
                        await ProcessMarketAsync(
                            market, marketSnapshots, strategies, _scopeFactory,
                            progressPrefix: $"[{label}/{dto.StrategyName}] ",
                            writeToFile: writeToFile, group: groupForId,
                            ignoreProcessedCache: true).ConfigureAwait(false);

                    if (bid != null)
                    {
                        if (writeToFile)
                            SaveMarketDataToFile(market, finalPnL, bid, ask, buy, sell, exit, ev, il, ishort,
                                fileNameSuffix: $"_{label}_{dto.StrategyName}");

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
                await saveContext.AddOrUpdateWeightSet(dto).ConfigureAwait(false);
                OnTestProgress?.Invoke($"Saved {label}/{dto.StrategyName} ({dto.WeightSetMarkets.Count}/{uniqueMarkets.Count} markets)");
            }

            OnTestProgress?.Invoke($"{label}: all strategy sets completed");
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