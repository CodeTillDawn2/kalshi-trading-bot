// Refactored TradingOverseer.cs with improved separation of concerns
using Microsoft.Extensions.DependencyInjection;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Overseer;
using TradingStrategies.Extensions;
using static BacklashInterfaces.Enums.StrategyEnums;
using static TradingStrategies.Trading.Overseer.ReportGenerator;

namespace TradingStrategies
{
    public class TradingOverseer
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITradingSnapshotService _snapshotService;
        private readonly SimulationEngine _simulationEngine;
        private readonly EquityCalculator _equityCalculator;
        private readonly string _cacheDirectory = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");

        public TradingOverseer(IServiceScopeFactory scopeFactory, ITradingSnapshotService snapshotService)
        {
            _scopeFactory = scopeFactory;
            _snapshotService = snapshotService;
            _simulationEngine = new SimulationEngine();
            _equityCalculator = new EquityCalculator();
        }

        private record SnapshotGroupTemp(string MarketTicker, DateTime StartTime, DateTime EndTime);

        public List<(PathPerformance performance, List<EventLog> events)> TestScenario(Scenario scenario, List<MarketSnapshot> snapshots, bool writeToFile, double initialCash = 100.0, SnapshotGroupDTO? group = null)
        {
            if (snapshots == null || snapshots.Count == 0) return new List<(PathPerformance, List<EventLog>)>();

            bool isSingleStrategy = scenario.StrategiesByMarketConditions.Values.All(hs => hs.Count <= 1);

            var activePaths = _simulationEngine.RunSimulation(scenario, snapshots, isSingleStrategy);

            var pathData = GenerateReportsAndPerformances(group, activePaths, snapshots, initialCash, writeToFile);

            return pathData;
        }

        private List<(PathPerformance performance, List<EventLog> events)> GenerateReportsAndPerformances(SnapshotGroupDTO group, List<SimulationPath> activePaths, List<MarketSnapshot> snapshots, double initialCash, bool writeToFile)
        {
            string outputDir = _cacheDirectory;
            string uniqueId = group != null ? Path.GetFileNameWithoutExtension(group.JsonPath) : snapshots.FirstOrDefault()?.MarketTicker;

            var finalSnapshot = snapshots.Last();
            var firstSnapshot = snapshots.First();
            var sortedPaths = activePaths.OrderByDescending(p => GetEquity(p, finalSnapshot)).ToList();
            var topPaths = sortedPaths.Take(3).ToList();

            var pathData = new List<(PathPerformance, List<EventLog>)>();

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

                var performance = new PathPerformance
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
                    var pathSpecificPaths = GetPathSpecificPaths(path.StrategiesByMarketConditions);
                    var detailedReport = reportGen.GenerateDetailedPerformanceReport(uniqueId, eventLogs, initialCash, pathSpecificPaths, writeToFile, outputDir);
                    if (writeToFile) File.WriteAllText(performanceFile, detailedReport);
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

        private double GetEquity(SimulationPath path, MarketSnapshot lastSnapshot)
        {
            return _equityCalculator.GetEquity(path, lastSnapshot);
        }

        private Dictionary<string, ReportGenerator.PathInfo> GetPathSpecificPaths(Dictionary<MarketType, HashSet<Strategy>> strategiesByMarketConditions)
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
