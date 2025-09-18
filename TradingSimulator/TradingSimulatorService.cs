// <summary>
// TradingSimulatorService is the core orchestrator for running trading strategy simulations and backtesting operations.
// It manages the complete lifecycle of evaluating trading strategies against historical market snapshots, including
// data loading, strategy execution, performance analysis, and result reporting. The service integrates with
// DataLoader for data access, MarketProcessor for simulation execution, StrategyResolver for strategy configuration,
// and provides comprehensive GUI integration through events and progress reporting.
// </summary>

using KalshiBotData.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using BacklashDTOs.Configuration;
using BacklashBot.Management;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using TradingSimulator.Strategies;
using TradingStrategies;
using TradingStrategies.Classification;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;
using TradingStrategies.ML;
using TradingStrategies.Strategies;
using TradingStrategies.Strategies.Strats;
using TradingStrategies.Trading.Helpers;
using TradingStrategies.Trading.Overseer;
using static BacklashInterfaces.Enums.StrategyEnums;
using TradingSimulator.Simulator;
using BacklashBotData.Data.Interfaces;
using BacklashBotData.Data;
using BacklashCommon.Services;
using BacklashCommon.Helpers;

namespace TradingSimulator
{
    /// <summary>
    /// Delegate for trading strategy functions that process market data snapshots.
    /// </summary>
    /// <typeparam name="T">The type of market data being processed.</typeparam>
    /// <param name="currentData">The current market snapshot data.</param>
    /// <param name="previousData">The previous market snapshot data for comparison.</param>
    /// <param name="context">The trading context containing shared variables and decisions.</param>
    public delegate void TradingStrategyFunc<T>(T currentData, T previousData, TradingContext context);

    /// <summary>
    /// Context object containing shared variables and trading decisions for strategy execution.
    /// </summary>
    public class TradingContext
    {
        /// <summary>
        /// Gets or sets the dictionary of shared variables accessible across trading strategies.
        /// </summary>
        public Dictionary<string, double> SharedVariables { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets or sets the current trading decision made by the strategy.
        /// </summary>
        public TradingDecision Decision { get; set; } = new TradingDecision();
    }

    /// <summary>
    /// Core service for orchestrating trading strategy simulations and backtesting operations.
    /// This service manages the complete lifecycle of running trading strategies against historical market snapshots,
    /// including data loading, strategy execution, performance analysis, and result reporting.
    /// It integrates with various components like DataLoader, MarketProcessor, and StrategyResolver to provide
    /// comprehensive simulation capabilities for evaluating trading strategies.
    /// Features include configurable cache directory and timeout values, input validation with warnings for invalid parameters,
    /// and async operations for high-throughput scenarios.
    /// </summary>
    public class TradingSimulatorService
    {
        private ITradingSnapshotService _snapshotService;
        private ISnapshotPeriodHelper _snapshotPeriodHelper;
        private IServiceFactory _serviceFactory;
        private TradingOverseer _overseer;
        private Mock<ILogger<ITradingSnapshotService>> _snapshotLoggerMock;
        private Mock<ILogger<IInterestScoreService>> _interestScoreLoggerMock;
        private Mock<ILogger<TradingStrategy<MarketSnapshot>>> _strategyLoggerMock;
        private IServiceScopeFactory _scopeFactory;
        private IBacklashBotContext _dbContext;
        private SnapshotGroupHelper _marketAnalysisHelper;
        private IOptions<GeneralExecutionConfig> _executionConfig;
        private IOptions<TradingSimulatorServiceConfig> _simulatorOptions;
        private HashSet<string> _processedMarkets;
        private string _cacheDirectory;
        private Mock<ILogger<SqlDataService>> _sqlLoggerMock;
        /// <summary>
        /// Event raised to report test progress information.
        /// </summary>
        public event Action<string> OnTestProgress;

        /// <summary>
        /// Event raised when profit/loss is updated for a market.
        /// </summary>
        public event Action<string, double> OnProfitLossUpdate;

        /// <summary>
        /// Event raised when a market has been processed.
        /// </summary>
        public event Action<string> OnMarketProcessed;
        private SimulatorReporting _simulatorReporting;

        private SqlDataService _sqlDataService;
        private PerformanceMonitor _performanceMonitor;

        /// <summary>
        /// Formats a file name using the configured pattern and provided parameters.
        /// </summary>
        private string FormatFileName(string pattern, Dictionary<string, string> parameters)
        {
            string result = pattern;
            foreach (var param in parameters)
            {
                result = result.Replace($"{{{param.Key}}}", param.Value);
            }
            return result;
        }

        // Helper classes
        private DataLoader _dataLoader;
        private MarketProcessor _marketProcessor;
        private StrategyResolver _strategyResolver;

        /// <summary>
        /// Initializes the trading simulator service by setting up dependencies, configuration, and database context.
        /// This method configures the service collection, builds the dependency injection container, loads simulator-specific
        /// configuration options (cache directory, timeouts), and initializes all required services and helpers for simulation operations.
        /// </summary>
        public void Setup()
        {
            // Use the current directory (TradingGUI's directory) for configuration
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            _snapshotLoggerMock = new Mock<ILogger<ITradingSnapshotService>>();
            _serviceFactory = new Mock<IServiceFactory>().Object;
            _strategyLoggerMock = new Mock<ILogger<TradingStrategy<MarketSnapshot>>>();
            _interestScoreLoggerMock = new Mock<ILogger<IInterestScoreService>>();
            var marketAnalysisLoggerMock = new Mock<ILogger<SnapshotGroupHelper>>();
            var overseerLoggerMock = new Mock<ILogger<TradingOverseer>>();

            var simulatorConfig = config.GetSection("TradingSimulatorService").Get<TradingSimulatorServiceConfig>();
            _executionConfig = Options.Create(config.GetSection("GeneralExecution").Get<GeneralExecutionConfig>());
            _simulatorOptions = Options.Create(simulatorConfig);

            // Configure DataLoaderConfig
            var dataLoaderConfig = config.GetSection("SnapshotHandling:DataLoader").Get<DataLoaderConfig>() ?? new DataLoaderConfig();
            var dataLoaderOptions = Options.Create(dataLoaderConfig);

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddDbContext<BacklashBotContext>(options => options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
            services.AddScoped<IBacklashBotContext>(sp => sp.GetRequiredService<BacklashBotContext>());
            var serviceProvider = services.BuildServiceProvider();

            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            _snapshotPeriodHelper = new SnapshotPeriodHelper(Options.Create(new SnapshotPeriodHelperConfig { SmallGapMinutes = 10.0, MaxActiveGapHours = 1.0, PriceChangeThreshold = 3 }).Value);
            _snapshotService = new TradingSnapshotService(_snapshotLoggerMock.Object, Options.Create(new TradingSnapshotServiceConfig { SnapshotToleranceSeconds = 5, StorageDirectory = @"C:\Temp\Storage", MaxParallelism = 8, EnablePerformanceMetrics = true }), Options.Create(new BacklashDTOs.Configuration.GeneralExecutionConfig { HardDataStorageLocation = @"C:\Temp\Storage" }), _scopeFactory, config, null);

            // Initialize performance monitor first
            _performanceMonitor = new PerformanceMonitor();

            _overseer = new TradingOverseer(_scopeFactory, _snapshotService, config, overseerLoggerMock.Object, _performanceMonitor, true);
            _marketAnalysisHelper = new SnapshotGroupHelper(_scopeFactory, _snapshotPeriodHelper, _snapshotService, _executionConfig, null, null, null, marketAnalysisLoggerMock.Object);
            _simulatorReporting = new SimulatorReporting();

            _dbContext = serviceProvider.GetRequiredService<IBacklashBotContext>();
            _sqlLoggerMock = new Mock<ILogger<SqlDataService>>();
            _sqlDataService = new SqlDataService(config, _sqlLoggerMock.Object);

            _processedMarkets = new HashSet<string>();
            _cacheDirectory = _simulatorOptions.Value.CacheDirectory;
            Directory.CreateDirectory(_cacheDirectory); // ensure output dir exists

            // Initialize helper classes
            _dataLoader = new DataLoader(_snapshotService, dataLoaderOptions);
            var marketProcessorConfig = new MarketProcessorConfig
            {
                CacheDirectory = _cacheDirectory,
                ProcessingTimeoutSeconds = _simulatorOptions.Value.ProcessingTimeoutSeconds
            };
            _marketProcessor = new MarketProcessor(_overseer, _scopeFactory, _processedMarkets, marketProcessorConfig, _simulatorReporting, _performanceMonitor);
            _marketProcessor.OnTestProgress += msg => OnTestProgress?.Invoke(msg);
            _strategyResolver = new StrategyResolver();

            OnTestProgress?.Invoke("Setup completed.");
        }

        /// <summary>
        /// Ensures the service is properly initialized by calling Setup if the scope factory is not available.
        /// This method provides a safe way to initialize the service on-demand without redundant setup calls.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_scopeFactory == null) Setup();
        }


        /// <summary>
        /// Retrieves filtered snapshot groups based on the provided market list and context.
        /// This method delegates to the DataLoader to filter snapshot groups by market names and duration requirements.
        /// </summary>
        /// <param name="context">The database context for accessing snapshot data.</param>
        /// <param name="marketsToRun">Optional list of market names to filter by. If null, all markets are included.</param>
        /// <returns>A list of filtered SnapshotGroupDTO objects containing market snapshot metadata.</returns>
        public async Task<List<SnapshotGroupDTO>> GetFilteredSnapshotGroupsAsync(
            IBacklashBotContext context, List<string>? marketsToRun)
        {
            return await _dataLoader.GetFilteredSnapshotGroupsAsync(context, marketsToRun);
        }






        /// <summary>
        /// Retrieves all market snapshots for a specific market name.
        /// This method validates the market name (logs warning and returns empty list for invalid names),
        /// then loads historical snapshot data for the specified market, filtering and ordering the results chronologically.
        /// </summary>
        /// <param name="marketName">The name of the market to retrieve snapshots for. Must not be null or empty.</param>
        /// <returns>A list of MarketSnapshot objects ordered by timestamp for the specified market.</returns>
        public async Task<List<MarketSnapshot>> GetSnapshotsForMarket(string marketName)
        {
            if (string.IsNullOrWhiteSpace(marketName))
            {
                OnTestProgress?.Invoke("Warning: marketName is null or empty. Returning empty list.");
                return new List<MarketSnapshot>();
            }

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
            return await _dataLoader.LoadSnapshotsForMarketAsync(context, marketName);
        }


        /// <summary>
        /// Runs a specific strategy set for GUI display, processing the selected parameter set against the specified markets.
        /// This method validates inputs (setKey, weightName, market names), resolves the strategy family from the set key,
        /// finds the matching weight set, and executes the simulation for all specified markets, optionally writing results
        /// to files and reporting progress. Invalid inputs are logged as warnings and skipped.
        /// </summary>
        /// <param name="setKey">The key identifying the strategy family (e.g., "bollinger", "breakout"). Must not be null or empty.</param>
        /// <param name="weightName">The name of the specific parameter set within the strategy family. Must not be null or empty.</param>
        /// <param name="writeToFile">Whether to save detailed market data to JSON files in the cache directory.</param>
        /// <param name="marketsToRun">Optional list of market names to process. Invalid names are filtered out with warnings. If null, processes all available markets.</param>
        public async Task RunSelectedSetForGuiAsync(
            string setKey,
            string weightName,
            bool writeToFile,
            List<string>? marketsToRun = null)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(setKey))
            {
                OnTestProgress?.Invoke("Warning: setKey is null or empty. Skipping execution.");
                return;
            }

            if (string.IsNullOrWhiteSpace(weightName))
            {
                OnTestProgress?.Invoke("Warning: weightName is null or empty. Skipping execution.");
                return;
            }

            // Validate marketsToRun
            if (marketsToRun != null)
            {
                var invalidMarkets = marketsToRun.Where(m => string.IsNullOrWhiteSpace(m)).ToList();
                if (invalidMarkets.Any())
                {
                    OnTestProgress?.Invoke($"Warning: Found {invalidMarkets.Count} invalid market names (null or empty). Filtering them out.");
                    marketsToRun = marketsToRun.Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
                }
            }

            // map provided setKey -> family
            var family = MapFamilyFromSetKey(setKey);

            // resolve strategies + param sets for that family
            var (strategiesList, paramSets, label) = ResolveFamily(family);

            // find the requested weight set by name (exact, case-insensitive)
            var idx = paramSets.FindIndex(ps =>
                string.Equals(ps.Name, weightName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                OnTestProgress?.Invoke($"Warning: Weight set '{weightName}' not found in {label}. Skipping execution.");
                return;
            }

            // prep context once
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

            // create DTO for just this one set
            var (name, parameters) = paramSets[idx];
            var dto = new WeightSetDTO
            {
                StrategyName = name,
                Weights = JsonSerializer.Serialize(parameters),
                LastRun = DateTime.UtcNow,
                WeightSetMarkets = new List<WeightSetMarketDTO>()
            };

            // Load snapshot data using DataLoader
            var dataset = await _dataLoader.LoadSnapshotsForMarketsAsync(context, marketsToRun ?? new List<string>());

            OnTestProgress?.Invoke($"{label}/{dto.StrategyName}: 1 strategy set {dataset.Count} markets");

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
            var marketList = dataset.Keys.ToList();
            for (int mIdx = 0; mIdx < marketList.Count; mIdx++)
            {
                var market = marketList[mIdx];
                var marketSnapshots = dataset[market];

                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                var strategies = strategiesList[idx]; // only the selected set
                OnTestProgress?.Invoke($"[{label}/{dto.StrategyName}] market {market} ({mIdx + 1}/{marketList.Count})");

                var (finalPnL, finalPosition, finalAverageCost, bid, ask, buy, sell, exit, ev, il, ishort, pos, avgCost, rest, disc, patterns) =
                    await _marketProcessor.ProcessMarketAsync(
                        market, marketSnapshots, strategies,
                        progressPrefix: $"[{label}/{dto.StrategyName}] ",
                        writeToFile: writeToFile, group: groupForId,
                        ignoreProcessedCache: true);

                totalDiscrepancies += disc?.Count ?? 0;


                if (writeToFile)
                {
                    var fileName = FormatFileName(_simulatorOptions.Value.MarketDataFileNamePattern, new Dictionary<string, string>
                    {
                        { "market", market },
                        { "label", label },
                        { "strategy", dto.StrategyName },
                        { "timestamp", DateTime.Now.ToString("yyyyMMdd_HHmmss") }
                    });
                    _marketProcessor.SaveMarketDataToFile(market, finalPnL, finalPosition, finalAverageCost, bid, ask, buy, sell, exit, ev, il, ishort,
                        pos, avgCost, rest, disc, patterns, fileNameSuffix: fileName);
                }

                dto.WeightSetMarkets.Add(new WeightSetMarketDTO
                {
                    MarketTicker = market,
                    PnL = (decimal)finalPnL,
                    LastRun = DateTime.UtcNow
                });

                OnProfitLossUpdate?.Invoke(market, finalPnL);


                OnMarketProcessed?.Invoke(market);
                marketSnapshots.Clear();
            }

            // save the one set
            using var saveScope = _scopeFactory.CreateScope();
            var saveContext = saveScope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
            await saveContext.AddOrUpdateWeightSet(dto).ConfigureAwait(false);

            OnTestProgress?.Invoke($"Saved {label}/{dto.StrategyName} ({dto.WeightSetMarkets.Count}/{marketList.Count} markets)");
            OnTestProgress?.Invoke($"Total discrepancies across all markets: {totalDiscrepancies} (widespread if >{_simulatorOptions.Value.DiscrepancyThresholdPercentage}% of snapshots).");
            OnTestProgress?.Invoke($"{label}/{dto.StrategyName}: completed");
        }

        private StrategyFamily MapFamilyFromSetKey(string setKey)
        {
            return _strategyResolver.MapFamilyFromSetKey(setKey);
        }


        /// <summary>
        /// Runs all enabled strategy families sequentially for GUI display.
        /// This method iterates through a predefined list of strategy families and executes each one,
        /// allowing comprehensive testing of multiple trading approaches against the same market data.
        /// </summary>
        /// <param name="writeToFile">Whether to save detailed market data to JSON files for each strategy.</param>
        /// <param name="marketsToRun">Optional list of market names to process. If null, processes all available markets.</param>
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

        /// <summary>
        /// Retrieves the names of all available snapshot groups from the database.
        /// This method provides a list of market names that have associated snapshot data for simulation.
        /// </summary>
        /// <returns>A HashSet of unique market names that have snapshot groups available.</returns>
        public async Task<HashSet<string>> GetSnapshotGroupNames()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
            return await context.GetSnapshotGroupNames();
        }

        /// <summary>
        /// Retrieves a list of valid base market names that have sufficient snapshot data for simulation.
        /// This method validates the basesToInclude list (filtering out invalid names with warnings), filters snapshot groups
        /// by duration and optionally by the provided list of base markets, then extracts the base market names
        /// (removing numeric suffixes) and returns them sorted.
        /// </summary>
        /// <param name="basesToInclude">Optional list of base market names to filter by. Invalid names are filtered out with warnings. If null, includes all valid bases.</param>
        /// <returns>A sorted list of valid base market names with available snapshot data.</returns>
        public async Task<List<string>> GetValidBaseMarketsAsync(List<string>? basesToInclude = null)
        {
            // Validate basesToInclude
            if (basesToInclude != null)
            {
                var invalidBases = basesToInclude.Where(b => string.IsNullOrWhiteSpace(b)).ToList();
                if (invalidBases.Any())
                {
                    OnTestProgress?.Invoke($"Warning: Found {invalidBases.Count} invalid base market names (null or empty). Filtering them out.");
                    basesToInclude = basesToInclude.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
                }
            }

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

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

        /// <summary>
        /// Enumeration of available trading strategy families for simulation.
        /// </summary>
        public enum StrategyFamily
        {
            /// <summary>
            /// Bollinger Bands based trading strategies.
            /// </summary>
            Bollinger,

            /// <summary>
            /// Flow momentum based trading strategies.
            /// </summary>
            FlowMo,

            /// <summary>
            /// Try again pattern based trading strategies.
            /// </summary>
            TryAgain,

            /// <summary>
            /// Slow momentum based trading strategies.
            /// </summary>
            SloMo,

            /// <summary>
            /// Breakout pattern based trading strategies.
            /// </summary>
            Breakout,

            /// <summary>
            /// Nothing happens pattern based trading strategies.
            /// </summary>
            NothingHappens,

            /// <summary>
            /// Machine learning shared entry seeker strategies.
            /// </summary>
            MLShared,

            /// <summary>
            /// Momentum based trading strategies.
            /// </summary>
            Momentum
        }

        private (List<Dictionary<MarketType, List<Strategy>>> Strategies,
         List<(string Name, object Parameters)> ParamSets,
         string Label)
ResolveFamily(StrategyFamily family)
        {
            return _strategyResolver.ResolveFamily(family);
        }

        /// <summary>
        /// Runs machine learning training and simulation for the MLShared strategy family.
        /// This method performs offline training on historical data, evaluates parameter sets,
        /// and generates a best-fit report ranking strategies by their proximity to large price spikes.
        /// The process includes data splitting (80% train, 20% test), strategy evaluation, and comprehensive reporting.
        /// </summary>
        /// <param name="writeToFile">Whether to save the best-fit report to a CSV file in the cache directory.</param>
        /// <param name="marketsToRun">Optional list of market names to process. If null, processes all available markets.</param>
        public async Task RunMLTrainingAndSimulationForGuiAsync(
            bool writeToFile,
            List<string>? marketsToRun = null)
        {
            // Validate marketsToRun
            if (marketsToRun != null)
            {
                var invalidMarkets = marketsToRun.Where(m => string.IsNullOrWhiteSpace(m)).ToList();
                if (invalidMarkets.Any())
                {
                    OnTestProgress?.Invoke($"Warning: Found {invalidMarkets.Count} invalid market names (null or empty). Filtering them out.");
                    marketsToRun = marketsToRun.Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
                }
            }

            var label = "MLShared";
            var helper = new StrategySelectionHelper();
            var paramSets = MLEntrySeekerShared.MLSharedParameterSets;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

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

            // Load snapshots using DataLoader
            var dataset = await _dataLoader.LoadSnapshotsForMarketsAsync(context, marketsToRun ?? new List<string>());
            foreach (var kvp in dataset)
            {
                OnTestProgress?.Invoke($"Loaded {kvp.Value.Count} snapshots for {kvp.Key}");
            }

            if (!dataset.Any())
            {
                OnTestProgress?.Invoke("No valid data found for ML training and simulation.");
                OnTestProgress?.Invoke("No snapshots loaded for any markets.");
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
                OnTestProgress?.Invoke($"Split for {kvp.Key}: {trainData[kvp.Key].Count} train, {testData[kvp.Key].Count} test");
            }

            // Offline training and evaluation
            var strategiesList = new List<Dictionary<MarketType, List<Strategy>>>();
            var metrics = new List<(string Name, double AvgEntryScore, double AvgPeakSize, double AvgTimeToPeak)>();
            for (int i = 0; i < paramSets.Count; i++)
            {
                try
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
                catch (Exception ex)
                {
                    OnTestProgress?.Invoke($"Error during ML training for parameter set {i}: {ex.Message}. Skipping this set.");
                    // Add empty strategy to maintain indexing
                    strategiesList.Add(new Dictionary<MarketType, List<Strategy>>());
                    metrics.Add(($"Error_{i}", 0.0, 0.0, 0.0));
                }
            }

            // Clear ResearchBus before run
            ResearchBus.Clear();

            var marketList = dataset.Keys.ToList();
            for (int mIdx = 0; mIdx < marketList.Count; mIdx++)
            {
                var market = marketList[mIdx];
                var marketSnapshots = dataset[market];
                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                for (int setIdx = 0; setIdx < strategiesList.Count; setIdx++)
                {
                    var strategies = strategiesList[setIdx];
                    var dto = weightSetDtos[setIdx];
                    var (finalPnL, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _) = await _marketProcessor.ProcessMarketAsync(
                        market, marketSnapshots, strategies,
                        progressPrefix: "",
                        writeToFile: false,
                        group: groupForId,
                        ignoreProcessedCache: true);

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
                OnTestProgress?.Invoke("ResearchBus is empty. No entries logged during simulation.");
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
                string reportPath = Path.Combine(_cacheDirectory, FormatFileName(_simulatorOptions.Value.BestFitReportFileNamePattern, new Dictionary<string, string>
                {
                    { "label", label },
                    { "timestamp", DateTime.Now.ToString("yyyyMMdd_HHmmss") }
                }));
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
                var saveContext = saveScope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                await saveContext.AddOrUpdateWeightSet(dto).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Runs all parameter sets for a specific strategy family against the provided markets.
        /// This method validates market names (filtering out invalid ones with warnings), executes comprehensive backtesting
        /// for all available parameter combinations within a strategy family, generating performance reports and optionally
        /// saving detailed results to files.
        /// </summary>
        /// <param name="family">The strategy family to execute (e.g., Bollinger, Breakout, MLShared).</param>
        /// <param name="writeToFile">Whether to save detailed market data to JSON files for each parameter set.</param>
        /// <param name="marketsToRun">Optional list of market names to process. Invalid names are filtered out with warnings. If null, processes all available markets.</param>
        public async Task RunMultipleForGuiAsync(
            StrategyFamily family,
            bool writeToFile,
            List<string>? marketsToRun = null)
        {
            // Validate marketsToRun
            if (marketsToRun != null)
            {
                var invalidMarkets = marketsToRun.Where(m => string.IsNullOrWhiteSpace(m)).ToList();
                if (invalidMarkets.Any())
                {
                    OnTestProgress?.Invoke($"Warning: Found {invalidMarkets.Count} invalid market names (null or empty). Filtering them out.");
                    marketsToRun = marketsToRun.Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
                }
            }

            var (strategiesList, paramSets, label) = ResolveFamily(family);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

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

            // Load snapshot data using DataLoader
            var dataset = await _dataLoader.LoadSnapshotsForMarketsAsync(context, marketsToRun ?? new List<string>());

            OnTestProgress?.Invoke($"{label}: {strategiesList.Count} strategy sets {dataset.Count} markets");

            int totalDiscrepancies = 0;

            var marketList = dataset.Keys.ToList();
            for (int mIdx = 0; mIdx < marketList.Count; mIdx++)
            {
                var market = marketList[mIdx];
                var marketSnapshots = dataset[market];

                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                for (int setIdx = 0; setIdx < strategiesList.Count; setIdx++)
                {
                    var strategies = strategiesList[setIdx];
                    var dto = weightSetDtos[setIdx];

                    OnTestProgress?.Invoke($"[{label}/{dto.StrategyName}] market {market} ({mIdx + 1}/{marketList.Count}) set {setIdx + 1}/{strategiesList.Count}");

                    (double finalPnL, int finalPosition, double finalAverageCost, List<PricePoint> bid, List<PricePoint> ask, List<PricePoint> buy, List<PricePoint> sell, List<PricePoint> exit, List<PricePoint> ev, List<PricePoint> il, List<PricePoint> ishort, List<PricePoint> pos, List<PricePoint> avgCost, List<PricePoint> rest, List<PricePoint> disc, List<PricePoint> patterns) =
                        await _marketProcessor.ProcessMarketAsync(
                            market, marketSnapshots, strategies,
                            progressPrefix: $"[{label}/{dto.StrategyName}] ",
                            writeToFile: writeToFile, group: groupForId,
                            ignoreProcessedCache: true);

                    totalDiscrepancies += disc?.Count ?? 0;


                    if (writeToFile)
                    {
                        var fileName = FormatFileName(_simulatorOptions.Value.MarketDataFileNamePattern, new Dictionary<string, string>
                        {
                            { "market", market },
                            { "label", label },
                            { "strategy", dto.StrategyName },
                            { "timestamp", DateTime.Now.ToString("yyyyMMdd_HHmmss") }
                        });
                        _marketProcessor.SaveMarketDataToFile(market, finalPnL, finalPosition, finalAverageCost, bid, ask, buy, sell, exit, ev, il, ishort,
                            pos, avgCost, rest, disc, patterns, fileNameSuffix: fileName);
                    }

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
            }

            foreach (var dto in weightSetDtos)
            {
                using var saveScope = _scopeFactory.CreateScope();
                var saveContext = saveScope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                await saveContext.AddOrUpdateWeightSet(dto).ConfigureAwait(false);
                OnTestProgress?.Invoke($"Saved {label}/{dto.StrategyName} ({dto.WeightSetMarkets.Count}/{marketList.Count} markets)");
            }

            string csvPath = Path.Combine(_cacheDirectory, FormatFileName(_simulatorOptions.Value.ResearchBusFileNamePattern, new Dictionary<string, string>
            {
                { "label", label },
                { "timestamp", DateTime.Now.ToString("yyyyMMdd_HHmmss") }
            }));
            ResearchBus.DumpCsv(csvPath);
            OnTestProgress?.Invoke($"Dumped ResearchBus to {csvPath}");

            OnTestProgress?.Invoke($"Total discrepancies across all markets: {totalDiscrepancies} (widespread if >10% of snapshots).");
            OnTestProgress?.Invoke($"{label}: all strategy sets completed");
        }



        /// <summary>
        /// Cleans up resources and disposes of database contexts and services.
        /// This method should be called when the simulator service is no longer needed to ensure
        /// proper resource cleanup and prevent memory leaks.
        /// </summary>
        public void TearDown()
        {
            _dbContext.Dispose();
            _sqlDataService.Dispose();
            OnTestProgress?.Invoke("TearDown completed.");
        }
    }
}
