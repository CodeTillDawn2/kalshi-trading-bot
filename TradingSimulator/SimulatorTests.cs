// SimulatorTests.cs
// Updated to avoid redundant data loads for markets, remove parallel operations, and maintain separate strategy set methods

using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
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
using System.Text.Json;
using System.Text.RegularExpressions;
using TradingSimulator.Strategies;
using TradingSimulator.TestObjects;
using TradingStrategies;
using TradingStrategies.Classification;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;
using TradingStrategies.ML;
using TradingStrategies.Strategies;
using TradingStrategies.Strategies.Strats;
using TradingStrategies.Trading.Helpers;
using static SmokehouseInterfaces.Enums.StrategyEnums;

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
        private SimulatorReporting _simulatorReporting;

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
            _simulatorReporting = new SimulatorReporting();

            _dbContext = serviceProvider.GetRequiredService<IKalshiBotContext>();
            _sqlLoggerMock = new Mock<ILogger<SqlDataService>>();
            _sqlDataService = new SqlDataService(config, _sqlLoggerMock.Object);

            _processedMarkets = new HashSet<string>();
            Directory.CreateDirectory(_cacheDirectory); // ensure output dir exists

            OnTestProgress?.Invoke("Setup completed.");
        }

        public void EnsureInitialized()
        {
            if (_scopeFactory == null) Setup();
        }


        public async Task<List<SnapshotGroupDTO>> GetFilteredSnapshotGroupsAsync(
            IKalshiBotContext context, List<string>? marketsToRun)
        {
            var groups = await context.GetSnapshotGroups_cached().ConfigureAwait(false);
            var filtered = new List<SnapshotGroupDTO>();
            foreach (var g in groups)
            {
                var recorded = g.EndTime - g.StartTime;
                if (recorded.TotalHours < 1) continue;  // Skip if insufficient duration

                if (marketsToRun != null)
                {
                    var baseTicker = Regex.Replace(g.MarketTicker ?? "", @"_(\d+)$", "");
                    if (!marketsToRun.Contains(baseTicker, StringComparer.OrdinalIgnoreCase)) continue;
                }

                filtered.Add(g);
            }
            return filtered;
        }


        private async Task<(double finalPnL, List<PricePoint> bidPoints, List<PricePoint> askPoints,
         List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
         List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
         List<PricePoint> discrepancyPoints)>
     ProcessMarketAsync(
         string marketTicker,
         List<MarketSnapshot> marketSnapshots,
         Dictionary<MarketType, List<Strategy>> strategiesDict,
         IServiceScopeFactory scopeFactory,
         string progressPrefix = "",
         bool writeToFile = false,
         bool detectVelocityDiscrepancies = false,
         SnapshotGroupDTO? group = null,
         bool ignoreProcessedCache = false)
        {
            if (!ignoreProcessedCache && _processedMarkets.Contains(marketTicker))
            {
                OnTestProgress?.Invoke($"{progressPrefix}Skipping cached market: {marketTicker}");
                return (0, null, null, null, null, null, null, null, null, null);
            }
            OnTestProgress?.Invoke($"{progressPrefix}Processing market: {marketTicker}");

            try
            {
                var helper = new MarketTypeHelper();
                foreach (var s in marketSnapshots) s.MarketType = helper.GetMarketType(s).ToString();

                // Detect discrepancies
                List<PricePoint> discrepancyPoints = new List<PricePoint>();
                if (detectVelocityDiscrepancies)
                {
                    discrepancyPoints = _simulatorReporting.DetectVelocityDiscrepancies(marketSnapshots, writeToFile);
                    OnTestProgress?.Invoke($"{progressPrefix}Detected {discrepancyPoints.Count} orderbook discrepancies in {marketTicker}.");
                }
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

                    return (finalPnL, bidPoints, askPoints, buyPoints, sellPoints, exitPoints, eventPoints, intendedLongPoints, intendedShortPoints, discrepancyPoints);
                }
            }
            finally
            {

            }
            return (0, null, null, null, null, null, null, null, null, null);
        }



        private void SaveMarketDataToFile(
            string marketTicker, double finalPnL,
            List<PricePoint> bidPoints, List<PricePoint> askPoints,
            List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
            List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
            List<PricePoint> discrepancyPoints,
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
                IntendedShortPoints = intendedShortPoints,
                DiscrepancyPoints = discrepancyPoints
            };

            var json = JsonSerializer.Serialize(cachedData);
            Directory.CreateDirectory(_cacheDirectory);
            var filePath = Path.Combine(_cacheDirectory, $"{marketTicker}{fileNameSuffix}.json");
            File.WriteAllText(filePath, json);
        }

        public async Task<List<MarketSnapshot>> ReturnSnapshotsForMarket(string marketName)
        {
            // prep context once
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, marketsToRun: new() { marketName }).ConfigureAwait(false);
            var marketGroups = filteredGroups.Where(g => g.MarketTicker == marketName).ToList();

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
                .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue)
                .OrderBy(ms => ms.Timestamp)
                .ToList();


            return marketSnapshots;
        }


        public async Task RunSelectedSetForGuiAsync(
            string setKey,
            string weightName,
            bool writeToFile,
            List<string>? marketsToRun = null)
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
            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, marketsToRun).ConfigureAwait(false);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();

            OnTestProgress?.Invoke($"{label}/{dto.StrategyName}: 1 strategy set × {uniqueMarkets.Count} markets");

            // NEW: Running counter for discrepancies
            int totalDiscrepancies = 0;

            // Clear cache directory if writeToFile is true
            if (writeToFile)
            {
                if (Directory.Exists(_cacheDirectory))
                {
                    foreach (var file in Directory.GetFiles(_cacheDirectory))
                    {
                        File.Delete(file);
                    }
                    OnTestProgress?.Invoke("Cleared cache directory before processing.");
                }
            }

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
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                var strategies = strategiesList[idx]; // only the selected set
                OnTestProgress?.Invoke($"[{label}/{dto.StrategyName}] market {market} ({mIdx + 1}/{uniqueMarkets.Count})");

                var (finalPnL, bid, ask, buy, sell, exit, ev, il, ishort, disc) =
                    await ProcessMarketAsync(
                        market, marketSnapshots, strategies, _scopeFactory,
                        progressPrefix: $"[{label}/{dto.StrategyName}] ",
                        writeToFile: writeToFile, detectVelocityDiscrepancies: true, group: groupForId,
                        ignoreProcessedCache: true).ConfigureAwait(false);

                totalDiscrepancies += disc?.Count ?? 0;

              
                if (writeToFile)
                    SaveMarketDataToFile(market, finalPnL, bid, ask, buy, sell, exit, ev, il, ishort,
                        disc, fileNameSuffix: $"_{label}_{dto.StrategyName}");

                dto.WeightSetMarkets.Add(new WeightSetMarketDTO
                {
                    MarketTicker = market,
                    PnL = (decimal)finalPnL,
                    LastRun = DateTime.UtcNow
                });

                OnProfitLossUpdate?.Invoke(market, finalPnL);
                

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
            OnTestProgress?.Invoke($"Total discrepancies across all markets: {totalDiscrepancies} (widespread if >10% of snapshots).");
            OnTestProgress?.Invoke($"{label}/{dto.StrategyName}: completed");
        }

        private StrategyFamily MapFamilyFromSetKey(string setKey)
        {
            if (string.IsNullOrWhiteSpace(setKey)) throw new ArgumentException("setKey is required.", nameof(setKey));
            var k = setKey.Trim().ToLowerInvariant();
            if (k.Contains("breakout")) return StrategyFamily.Breakout;
            if (k.Contains("bollinger")) return StrategyFamily.Bollinger;
            if (k.Contains("flowmo")) return StrategyFamily.FlowMo;
            if (k.Contains("tryagain")) return StrategyFamily.TryAgain;
            if (k.Contains("slomo")) return StrategyFamily.SloMo;
            if (k.Contains("nothing")) return StrategyFamily.NothingHappens;
            if (k.Contains("momentum")) return StrategyFamily.Momentum;
            if (k.Contains("ml") || k == "mlshared") return StrategyFamily.MLShared;
            // specific short names
            if (k is "b2" or "breakout2") return StrategyFamily.Breakout;
            if (k is "bb" or "bollingerbreakout") return StrategyFamily.Bollinger;
            throw new ArgumentOutOfRangeException(nameof(setKey), $"Unrecognized setKey: {setKey}");
        }


        public async Task RunMultipleAllStrategiesForGuiAsync(
            bool writeToFile,
            List<string>? marketsToRun = null)
        {
            // run all families automatically
            var families = new[] {
                //StrategyFamily.Bollinger,
                //StrategyFamily.FlowMo,
                //StrategyFamily.Breakout,
                //StrategyFamily.Momentum,
                //StrategyFamily.SloMo,
                //StrategyFamily.TryAgain,
                StrategyFamily.MLShared,
                //StrategyFamily.NothingHappens
            };

            foreach (var fam in families)
            {
                await RunMultipleForGuiAsync(
                    family: fam,
                    writeToFile: writeToFile,
                    marketsToRun: marketsToRun).ConfigureAwait(false);
            }
        }

        public async Task<HashSet<string>> GetSnapshotGroupNames()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            return await context.GetSnapshotGroupNames();
        }

        public async Task<List<string>> GetValidBaseMarketsAsync(List<string>? basesToInclude = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            // Reuse the existing filtering logic, passing basesToInclude as marketsToRun
            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, basesToInclude).ConfigureAwait(false);

            // Extract base tickers (remove trailing _num if present), filter distinct non-empty, sort case-insensitively
            var validBases = filteredGroups
                .Select(g => Regex.Replace(g.MarketTicker ?? "", @"_(\d+)$", ""))
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            validBases.Sort(StringComparer.OrdinalIgnoreCase);
            return validBases;
        }

        public enum StrategyFamily
        {
            Bollinger,
            FlowMo,
            TryAgain,
            SloMo,
            Breakout,
            NothingHappens,
            MLShared,
            Momentum
        }

        private (List<Dictionary<MarketType, List<Strategy>>> Strategies,
         List<(string Name, object Parameters)> ParamSets,
         string Label)
ResolveFamily(StrategyFamily family)
        {
            var helper = new StrategySelectionHelper();  // Instantiate here for access to instance methods

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
                case StrategyFamily.TryAgain:
                    return (
                        helper.GetTrainingMappings("TryAgain"),
                        (TryAgainStrat.TryAgainStratParameterSets
                            ?? throw new InvalidOperationException("TryAgainStratParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "TryAgain"
                    );
                case StrategyFamily.SloMo:
                    return (
                        helper.GetTrainingMappings("SloMo"),
                        (SlopeMomentumStrat.SlopeMomentumParameterSets
                            ?? throw new InvalidOperationException("SlopeMomentumParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "SloMo"
                    );
                case StrategyFamily.Breakout:
                    return (
                        helper.GetTrainingMappings("Breakout2"),  // Note: Uses "Breakout2" key as per helper
                        (StrategySelectionHelper.BreakoutParameterSets
                            ?? throw new InvalidOperationException("BreakoutParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "Breakout"
                    );
                case StrategyFamily.NothingHappens:
                    return (
                        helper.GetTrainingMappings("Nothing"),
                        (StrategySelectionHelper.NothingEverHappensParameterSets
                            ?? throw new InvalidOperationException("NothingEverHappensParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "NothingHappens"
                    );
                case StrategyFamily.Momentum:
                    return (
                        helper.GetTrainingMappings("Momentum"),
                        (StrategySelectionHelper.MomentumTradingParameterSets
                            ?? throw new InvalidOperationException("MomentumTradingParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "Momentum"
                    );
                case StrategyFamily.MLShared:  // New case, using the updated helper
                    return (
                        helper.GetTrainingMappings("MLShared"),
                        (MLEntrySeekerShared.MLSharedParameterSets
                            ?? throw new InvalidOperationException("MLSharedParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "MLShared"
                    );
                default:
                    throw new ArgumentOutOfRangeException(nameof(family));
            }
        }

        /// <summary>
        /// Runs offline training, evaluation, and online simulation for MLShared strategy.
        /// Produces only a best-fit report ranking parameter sets by proximity to large spikes.
        /// Includes diagnostic logging to console to identify issues with entry generation.
        /// </summary>
        public async Task RunMLTrainingAndSimulationForGuiAsync(
            bool writeToFile,
            List<string>? marketsToRun = null)
        {
            var label = "MLShared";
            var helper = new StrategySelectionHelper();
            var paramSets = MLEntrySeekerShared.MLSharedParameterSets;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            // Initialize DTOs for results
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

            // Load snapshots
            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, marketsToRun).ConfigureAwait(false);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();
            var dataset = new Dictionary<string, List<MarketSnapshot>>();
            foreach (var market in uniqueMarkets)
            {
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
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();
                if (marketSnapshots.Any())
                {
                    Console.WriteLine($"Loaded {marketSnapshots.Count} snapshots for {market}");
                    dataset[market] = marketSnapshots;
                }
            }

            if (!dataset.Any())
            {
                OnTestProgress?.Invoke("No valid data found for ML training and simulation.");
                Console.WriteLine("No snapshots loaded for any markets.");
                return;
            }

            // Split dataset: 80% train, 20% test
            var trainData = new Dictionary<string, List<MarketSnapshot>>();
            var testData = new Dictionary<string, List<MarketSnapshot>>();
            foreach (var kvp in dataset)
            {
                var snapshots = kvp.Value;
                int splitIdx = (int)(snapshots.Count * 0.8);
                trainData[kvp.Key] = snapshots.Take(splitIdx).ToList();
                testData[kvp.Key] = snapshots.Skip(splitIdx).ToList();
                Console.WriteLine($"Split for {kvp.Key}: {trainData[kvp.Key].Count} train, {testData[kvp.Key].Count} test");
            }

            // Offline training and evaluation
            var strategiesList = new List<Dictionary<MarketType, List<Strategy>>>();
            var metrics = new List<(string Name, double AvgEntryScore, double AvgPeakSize, double AvgTimeToPeak)>();
            for (int i = 0; i < paramSets.Count; i++)
            {
                var (name, parameters) = paramSets[i];
                var mlStrat = new MLEntrySeekerShared(
                    name: name,
                    evaluationOnly: false,
                    weight: 1.0,
                    p: parameters);
                mlStrat.PreTrain(trainData);
                var strategiesDict = helper.GetMLSharedStrategy(name);
                strategiesList.Add(strategiesDict);
                metrics.Add((name, 0.0, 0.0, 0.0));
            }

            // Clear ResearchBus before run
            ResearchBus.Clear();

            for (int mIdx = 0; mIdx < uniqueMarkets.Count; mIdx++)
            {
                var market = uniqueMarkets[mIdx];
                var marketSnapshots = dataset[market];
                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                for (int setIdx = 0; setIdx < strategiesList.Count; setIdx++)
                {
                    var strategies = strategiesList[setIdx];
                    var dto = weightSetDtos[setIdx];
                    var (finalPnL, _, _, _, _, _, _, _, _, _) = await ProcessMarketAsync(
                        market, marketSnapshots, strategies, _scopeFactory,
                        progressPrefix: "",
                        writeToFile: false,
                        detectVelocityDiscrepancies: true,
                        group: groupForId,
                        ignoreProcessedCache: true).ConfigureAwait(false);

                    dto.WeightSetMarkets.Add(new WeightSetMarketDTO
                    {
                        MarketTicker = market,
                        PnL = (decimal)finalPnL,
                        LastRun = DateTime.UtcNow
                    });
                }
            }

            // Generate best-fit report
            if (!ResearchBus.Entries.Any())
            {
                OnTestProgress?.Invoke("No entries detected. Check dataset for price movements or adjust thresholds.");
                Console.WriteLine("ResearchBus is empty. No entries logged during simulation.");
                return;
            }

            var entryMetrics = ResearchBus.Entries
                .GroupBy(e => e.ParameterSet)
                .Select(g =>
                {
                    var avgScore = g.Average(e => e.Score);
                    var avgPeakSize = g.Average(e => e.PeakSizeTicks);
                    var avgTimeToPeak = g.Average(e => e.TimeToPeak.TotalSeconds);
                    var composite = avgPeakSize / (avgTimeToPeak + 1e-6);
                    Console.WriteLine($"Metrics for {g.Key}: AvgScore={avgScore:F3}, AvgPeakSize={avgPeakSize:F3}, AvgTimeToPeak={avgTimeToPeak:F3}, Composite={composite:F3}");
                    return (Name: g.Key, AvgEntryScore: avgScore, AvgPeakSize: avgPeakSize, AvgTimeToPeak: avgTimeToPeak, Composite: composite);
                })
                .ToList();

            // Merge with metrics
            for (int i = 0; i < metrics.Count; i++)
            {
                var entry = entryMetrics.FirstOrDefault(em => em.Name == metrics[i].Name);
                if (entry.Name != null)
                {
                    metrics[i] = (metrics[i].Name, entry.AvgEntryScore, entry.AvgPeakSize, entry.AvgTimeToPeak);
                }
            }

            // Rank by composite
            var rankedMetrics = metrics
                .Select(m => (m.Name, m.AvgEntryScore, m.AvgPeakSize, m.AvgTimeToPeak, Composite: m.AvgPeakSize / (m.AvgTimeToPeak + 1e-6)))
                .OrderByDescending(m => m.Composite)
                .ToList();

            // Log report to GUI
            OnTestProgress?.Invoke("\n=== Best-Fit Parameter Set Report (Based on Proximity to Large Spikes) ===");
            OnTestProgress?.Invoke("Rank | ParameterSet | AvgEntryScore | AvgPeakSize | AvgTimeToPeak (sec) | Composite (PeakSize/TimeToPeak) | Parameters");
            int rank = 1;
            double prevComposite = double.MaxValue;
            for (int i = 0; i < rankedMetrics.Count; i++)
            {
                var m = rankedMetrics[i];
                if (Math.Abs(m.Composite - prevComposite) > 1e-6) rank = i + 1;
                prevComposite = m.Composite;
                var paramSet = paramSets.FirstOrDefault(ps => ps.Name == m.Name).Parameters;
                var paramStr = string.Join(", ", paramSet.Select(kv => $"{kv.Key}={kv.Value:F3}"));
                OnTestProgress?.Invoke($"{rank} | {m.Name} | {m.AvgEntryScore:F3} | {m.AvgPeakSize:F3} | {m.AvgTimeToPeak:F3} | {m.Composite:F3} | {paramStr}");
            }

            // Save report to file if requested
            if (writeToFile)
            {
                string reportPath = Path.Combine(_cacheDirectory, $"MLShared_BestFitReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                using var sw = new StreamWriter(reportPath);
                sw.WriteLine("Rank,ParameterSet,AvgEntryScore,AvgPeakSize,AvgTimeToPeak,Composite,Parameters");
                prevComposite = double.MaxValue;
                rank = 1;
                for (int i = 0; i < rankedMetrics.Count; i++)
                {
                    var m = rankedMetrics[i];
                    if (Math.Abs(m.Composite - prevComposite) > 1e-6) rank = i + 1;
                    prevComposite = m.Composite;
                    var paramSet = paramSets.FirstOrDefault(ps => ps.Name == m.Name).Parameters;
                    var paramStr = string.Join("; ", paramSet.Select(kv => $"{kv.Key}={kv.Value:F3}"));
                    sw.WriteLine($"{rank},{m.Name},{m.AvgEntryScore:F3},{m.AvgPeakSize:F3},{m.AvgTimeToPeak:F3},{m.Composite:F3},\"{paramStr}\"");
                }
                OnTestProgress?.Invoke($"Saved best-fit report to {reportPath}");
            }

            foreach (var dto in weightSetDtos)
            {
                using var saveScope = _scopeFactory.CreateScope();
                var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                await saveContext.AddOrUpdateWeightSet(dto).ConfigureAwait(false);
            }
        }

        public async Task RunMultipleForGuiAsync(
            StrategyFamily family,
            bool writeToFile,
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

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, marketsToRun).ConfigureAwait(false);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();

            OnTestProgress?.Invoke($"{label}: {strategiesList.Count} strategy sets × {uniqueMarkets.Count} markets");

            // Running counter for discrepancies
            int totalDiscrepancies = 0;

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
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                for (int setIdx = 0; setIdx < strategiesList.Count; setIdx++)
                {
                    var strategies = strategiesList[setIdx];
                    var dto = weightSetDtos[setIdx];

                    OnTestProgress?.Invoke($"[{label}/{dto.StrategyName}] market {market} ({mIdx + 1}/{uniqueMarkets.Count}) set {setIdx + 1}/{strategiesList.Count}");

                    var (finalPnL, bid, ask, buy, sell, exit, ev, il, ishort, disc) =
                        await ProcessMarketAsync(
                            market, marketSnapshots, strategies, _scopeFactory,
                            progressPrefix: $"[{label}/{dto.StrategyName}] ",
                            writeToFile: writeToFile, detectVelocityDiscrepancies: false, group: groupForId,
                            ignoreProcessedCache: true).ConfigureAwait(false);

                    totalDiscrepancies += disc?.Count ?? 0;


                    if (writeToFile)
                        SaveMarketDataToFile(market, finalPnL, bid, ask, buy, sell, exit, ev, il, ishort,
                            disc, fileNameSuffix: $"_{label}_{dto.StrategyName}");

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

            foreach (var dto in weightSetDtos)
            {
                using var saveScope = _scopeFactory.CreateScope();
                var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                await saveContext.AddOrUpdateWeightSet(dto).ConfigureAwait(false);
                OnTestProgress?.Invoke($"Saved {label}/{dto.StrategyName} ({dto.WeightSetMarkets.Count}/{uniqueMarkets.Count} markets)");
            }

            string csvPath = Path.Combine(_cacheDirectory, $"{label}_ResearchBus_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            ResearchBus.DumpCsv(csvPath);
            OnTestProgress?.Invoke($"Dumped ResearchBus to {csvPath}");

            OnTestProgress?.Invoke($"Total discrepancies across all markets: {totalDiscrepancies} (widespread if >10% of snapshots).");
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