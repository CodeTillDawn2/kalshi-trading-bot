/// <summary>
/// Orchestrates trading scenario simulations and performance analysis for the Kalshi trading bot.
/// This class serves as the central coordinator for running trading strategies against historical market snapshots,
/// generating detailed performance reports, and calculating equity metrics. It integrates with the simulation engine,
/// equity calculator, and report generator to provide comprehensive backtesting capabilities.
/// Supports asynchronous operations for efficient performance monitoring and analysis.
/// </summary>
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Overseer;
using TradingStrategies.Extensions;
using static BacklashInterfaces.Enums.StrategyEnums;
using static TradingStrategies.Trading.Overseer.ReportGenerator;
using System.Threading.Tasks;

namespace TradingStrategies
{
    public class TradingOverseer
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITradingSnapshotService _snapshotService;
        private readonly SimulationEngine _simulationEngine;
        private readonly EquityCalculator _equityCalculator;
        private readonly ILogger<TradingOverseer> _logger;
        private readonly string _cacheDirectory = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");

        /// <summary>
        /// Initializes a new instance of the TradingOverseer class.
        /// Sets up dependencies for simulation, equity calculation, and snapshot services.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes to resolve dependencies.</param>
        /// <param name="snapshotService">Service for managing trading snapshot data.</param>
        /// <param name="logger">Logger for recording warnings and errors.</param>
        public TradingOverseer(IServiceScopeFactory scopeFactory, ITradingSnapshotService snapshotService, ILogger<TradingOverseer> logger)
        {
            _scopeFactory = scopeFactory;
            _snapshotService = snapshotService;
            _logger = logger;
            _simulationEngine = new SimulationEngine();
            _equityCalculator = new EquityCalculator();
        }

        private record SnapshotMetadata(string MarketTicker, DateTime StartTime, DateTime EndTime);


        /// <summary>
        /// Executes a trading scenario simulation against a series of market snapshots.
        /// This method orchestrates the complete simulation process, including running strategies,
        /// generating performance reports, and returning detailed results for analysis.
        /// </summary>
        /// <param name="scenario">The trading scenario containing strategies and market conditions to simulate.</param>
        /// <param name="snapshots">List of market snapshots representing historical market data.</param>
        /// <param name="writeToFile">Whether to write detailed reports to file output.</param>
        /// <param name="initialCash">Starting cash amount for the simulation (default: 100.0).</param>
        /// <param name="group">Optional snapshot group metadata for organizing results.</param>
        /// <returns>A task that returns a list of tuples containing performance metrics and event logs for each simulation path.</returns>
        public async Task<List<(ReportGenerator.PathPerformance performance, List<ReportGenerator.EventLog> events)>> TestScenario(Scenario scenario, List<MarketSnapshot> snapshots, bool writeToFile, double initialCash = 100.0, SnapshotGroupDTO? group = null)
        {
            if (scenario == null)
            {
                _logger.LogWarning("Scenario parameter is null. Returning empty results.");
                return new List<(PathPerformance, List<EventLog>)>();
            }

            if (scenario.StrategiesByMarketConditions == null || !scenario.StrategiesByMarketConditions.Any())
            {
                _logger.LogWarning("Scenario does not contain valid strategies. Returning empty results.");
                return new List<(PathPerformance, List<EventLog>)>();
            }

            if (snapshots == null)
            {
                _logger.LogWarning("Snapshots list is null. Returning empty results.");
                return new List<(PathPerformance, List<EventLog>)>();
            }

            if (snapshots.Count == 0)
            {
                _logger.LogWarning("Snapshots list is empty. Returning empty results.");
                return new List<(ReportGenerator.PathPerformance, List<ReportGenerator.EventLog>)>();
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            bool isSingleStrategy = scenario.StrategiesByMarketConditions.Values.All(hs => hs.Count <= 1);

            var activePaths = _simulationEngine.RunSimulation(scenario, snapshots, isSingleStrategy);

            var pathData = await GeneratePerformanceReportsAndMetrics(group, activePaths, snapshots, initialCash, writeToFile);

            stopwatch.Stop();
            _logger.LogDebug("Simulation execution time: {Elapsed} ms, Snapshots processed: {Count}, Memory usage: {Memory} bytes", stopwatch.Elapsed.TotalMilliseconds, snapshots.Count, GC.GetTotalMemory(true));

            return pathData;
        }


        /// <summary>
        /// Generates comprehensive performance reports and metrics for simulation paths.
        /// Processes each simulation path to calculate equity, trades, and market conditions,
        /// then creates detailed CSV reports for top-performing paths and summary reports.
        /// Writes detailed performance reports to files if specified, sorts paths by final equity,
        /// and generates reports for the top three paths while outputting summary information to the console.
        /// Utilizes asynchronous file I/O for efficient handling of large datasets.
        /// </summary>
        /// <param name="group">Snapshot group metadata for file naming and organization.</param>
        /// <param name="activePaths">List of simulation paths from the simulation engine.</param>
        /// <param name="snapshots">Original market snapshots used in the simulation.</param>
        /// <param name="initialCash">Starting cash amount for equity calculations.</param>
        /// <param name="writeToFile">Whether to write reports to the file system.</param>
        /// <returns>A task that returns a list of performance metrics and event logs for each path.</returns>
        private async Task<List<(ReportGenerator.PathPerformance performance, List<ReportGenerator.EventLog> events)>> GeneratePerformanceReportsAndMetrics(SnapshotGroupDTO? group, List<SimulationPath> activePaths, List<MarketSnapshot> snapshots, double initialCash, bool writeToFile)
        {
            string outputDir = _cacheDirectory;
            string uniqueId = group != null ? Path.GetFileNameWithoutExtension(group.JsonPath) : snapshots.FirstOrDefault()?.MarketTicker ?? "Unknown";

            var finalSnapshot = snapshots.Last();
            var firstSnapshot = snapshots.First();
            var sortedPaths = activePaths.OrderByDescending(p => GetEquity(p, finalSnapshot)).ToList();
            var topPaths = sortedPaths.Take(3).ToList();

            var pathData = new List<(ReportGenerator.PathPerformance, List<ReportGenerator.EventLog>)>();

            for (int i = 0; i < sortedPaths.Count; i++)
            {
                var path = sortedPaths[i];
                var eventLogs = path.Events;
                if (eventLogs.Count == 0) continue;

                var snapshotsPerType = eventLogs.GroupBy(e => e.MarketType).ToDictionary(g => g.Key, g => g.Count());

                var pathTaken = string.Join(" ? ", eventLogs.Select(e => e.MarketType).Distinct());
                var finalEquity = GetEquity(path, finalSnapshot);
                var pnl = finalEquity - initialCash;
                int trades = 0;
                for (int j = 1; j < eventLogs.Count; j++)
                {
                    if (Math.Abs(eventLogs[j].Position - eventLogs[j - 1].Position) > 0)
                    {
                        trades++;
                    }
                }

                var endType = (finalSnapshot.BestYesBid == 0 || finalSnapshot.BestNoBid == 0) ? "Natural" : "Abrupt";

                var performance = new ReportGenerator.PathPerformance
                {
                    MarketId = uniqueId,
                    PathTaken = pathTaken,
                    SnapshotsPerType = snapshotsPerType,
                    PnL = pnl,
                    Equity = finalEquity,
                    Trades = trades,
                    StartYesBid = firstSnapshot.BestYesBid,
                    StartNoBid = firstSnapshot.BestNoBid,
                    EndYesBid = finalSnapshot.BestYesBid,
                    EndNoBid = finalSnapshot.BestNoBid,
                    EndType = endType,
                    SimulatedPosition = path.Position,
                    AverageCost = path.AverageCost
                };

                pathData.Add((performance, eventLogs));

                if (i < 3)
                {
                    var performanceFile = Path.Combine(outputDir, $"{uniqueId}_DetailedPerformance_Path{i + 1}.csv");
                    var reportGen = new ReportGenerator();
                    var pathSpecificPaths = GetStrategyPathsByMarketType(path.StrategiesByMarketConditions);
                    var detailedReport = reportGen.GenerateDetailedPerformanceReport(uniqueId, eventLogs, initialCash, pathSpecificPaths, writeToFile, outputDir);
                    if (writeToFile) await File.WriteAllTextAsync(performanceFile, detailedReport);
                    Console.WriteLine(detailedReport);
                }
            }

            if (topPaths.Any())
            {
                var bestPath = topPaths[0];
                var eventLogs = bestPath.Events;
                var reportGen = new ReportGenerator();
                var snapshotsPerType = eventLogs.GroupBy(e => e.MarketType ?? "Unknown").ToDictionary(g => g.Key, g => g.Count());
                var pathTaken = string.Join(" - ", eventLogs.Select(e => e.MarketType ?? "Unknown").Distinct());
                var finalEquity = GetEquity(bestPath, finalSnapshot);
                var finalPnl = finalEquity - initialCash;
                var notes = "Best path selected based on final equity. Losses from price crash to $0.00 in LowLiquidity; no resolution gain on held position. Trending maintained equity; unwind partially mitigated.";
                var finalReport = reportGen.GenerateFinalPerformanceReport(uniqueId, pathTaken, snapshotsPerType, finalPnl, finalEquity, notes, writeToFile, outputDir);

                Console.WriteLine(finalReport);
            }

            return pathData;
        }

        /// <summary>
        /// Calculates the current equity value for a simulation path at a given market snapshot.
        /// Delegates to the equity calculator to determine the total value including cash and position.
        /// </summary>
        /// <param name="path">The simulation path containing position and cash information.</param>
        /// <param name="lastSnapshot">The market snapshot to use for pricing calculations.</param>
        /// <returns>The calculated equity value.</returns>
        private double GetEquity(SimulationPath path, MarketSnapshot lastSnapshot)
        {
            return _equityCalculator.GetEquity(path, lastSnapshot);
        }

        /// <summary>
        /// Extracts strategy information organized by market type for reporting purposes.
        /// Creates a mapping of market types to their associated strategy names for use in performance reports.
        /// </summary>
        /// <param name="strategiesByMarketConditions">Dictionary mapping market types to sets of strategies.</param>
        /// <returns>Dictionary mapping market type strings to path information containing strategy names.</returns>
        private Dictionary<string, ReportGenerator.PathInfo> GetStrategyPathsByMarketType(Dictionary<MarketType, HashSet<Strategy>> strategiesByMarketConditions)
        {
            var result = new Dictionary<string, ReportGenerator.PathInfo>();
            foreach (var kv in strategiesByMarketConditions)
            {
                var marketStr = kv.Key.ToString();
                var strats = kv.Value.SelectMany(s => s.Strats.Select(e => e.GetType().Name)).Distinct().ToList();
                result[marketStr] = new ReportGenerator.PathInfo { Strats = strats };
            }
            return result;
        }

    }
}
