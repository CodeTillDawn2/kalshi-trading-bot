using BacklashDTOs;
using BacklashDTOs.Data;
using TradingStrategies;
using TradingStrategies.Trading.Overseer;
using TradingStrategies.Trading.Helpers;
using TradingSimulator.TestObjects;
using System.Text.Json;
using static BacklashInterfaces.Enums.StrategyEnums;
using TradingSimulator.Simulator;
using Microsoft.Extensions.DependencyInjection;
using TradingStrategies.Strategies;

namespace TradingSimulator
{
    public class MarketProcessor
    {
        private readonly TradingOverseer _overseer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HashSet<string> _processedMarkets;
        private readonly string _cacheDirectory;
        private readonly SimulatorReporting _simulatorReporting;

        public event Action<string> OnTestProgress;

        public MarketProcessor(
            TradingOverseer overseer,
            IServiceScopeFactory scopeFactory,
            HashSet<string> processedMarkets,
            string cacheDirectory,
            SimulatorReporting simulatorReporting)
        {
            _overseer = overseer;
            _scopeFactory = scopeFactory;
            _processedMarkets = processedMarkets;
            _cacheDirectory = cacheDirectory;
            _simulatorReporting = simulatorReporting;
        }

        public async Task<(double finalPnL, int finalPosition, double finalAverageCost, List<PricePoint> bidPoints, List<PricePoint> askPoints,
            List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
            List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
            List<PricePoint> positionPoints, List<PricePoint> averageCostPoints, List<PricePoint> restingOrdersPoints,
            List<PricePoint> discrepancyPoints, List<PricePoint> patternPoints)>
        ProcessMarketAsync(
            string marketTicker,
            List<MarketSnapshot> marketSnapshots,
            Dictionary<MarketType, List<Strategy>> strategiesDict,
            string progressPrefix = "",
            bool writeToFile = false,
            bool detectVelocityDiscrepancies = false,
            SnapshotGroupDTO? group = null,
            bool ignoreProcessedCache = false)
        {
            if (!ignoreProcessedCache && _processedMarkets.Contains(marketTicker))
            {
                OnTestProgress?.Invoke($"{progressPrefix}Skipping cached market: {marketTicker}");
                return (0, 0, 0.0, null, null, null, null, null, null, null, null, null, null, null, null, null);
            }

            OnTestProgress?.Invoke($"{progressPrefix}Processing market: {marketTicker}");

            try
            {
                // Set market types
                var helper = new MarketTypeHelper();
                foreach (var s in marketSnapshots) s.MarketType = helper.GetMarketType(s).ToString();

                // Detect discrepancies
                List<PricePoint> discrepancyPoints = new List<PricePoint>();
                if (detectVelocityDiscrepancies)
                {
                    discrepancyPoints = _simulatorReporting.DetectVelocityDiscrepancies(marketSnapshots, writeToFile);
                    OnTestProgress?.Invoke($"{progressPrefix}Detected {discrepancyPoints.Count} orderbook discrepancies in {marketTicker}.");
                }

                // Run simulation
                var scenario = new Scenario(strategiesDict);
                var pathData = await Task.Run(() => _overseer.TestScenario(scenario, marketSnapshots, writeToFile, 100, group));

                if (pathData.Any())
                {
                    var bestPath = pathData.OrderByDescending(p => p.performance.Equity).First();
                    var eventLogs = bestPath.events;

                    return ProcessEventLogs(marketSnapshots, eventLogs, marketTicker, writeToFile, progressPrefix, group);
                }
            }
            catch (Exception ex)
            {
                OnTestProgress?.Invoke($"{progressPrefix}Error processing {marketTicker}: {ex.Message}");
            }

            return (0, 0, 0.0, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }

        private (double, int, double, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>,
            List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>,
            List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>)
        ProcessEventLogs(
            List<MarketSnapshot> marketSnapshots,
            List<ReportGenerator.EventLog> eventLogs,
            string marketTicker,
            bool writeToFile,
            string progressPrefix,
            SnapshotGroupDTO? group)
        {
            var bestPath = new { performance = new { PnL = 0.0, SimulatedPosition = 0, AverageCost = 0.0 }, events = eventLogs };
            var finalPnL = bestPath.performance.PnL;
            var finalPosition = bestPath.performance.SimulatedPosition;
            var finalAverageCost = bestPath.performance.AverageCost;

            // Create price points
            var (bidPoints, askPoints) = CreateBidAskPoints(marketSnapshots);
            var (buyPoints, sellPoints, exitPoints, intendedLongPoints, intendedShortPoints) = CreateTradePoints(eventLogs);
            var eventPoints = CreateEventPoints(eventLogs);
            var (positionPoints, averageCostPoints) = CreatePositionPoints(eventLogs);
            var restingOrdersPoints = CreateRestingOrdersPoints(eventLogs);
            var patternPoints = CreatePatternPoints(eventLogs);

            // Save to file if requested
            if (writeToFile)
            {
                SaveMarketDataToFile(marketTicker, finalPnL, finalPosition, finalAverageCost,
                    bidPoints, askPoints, buyPoints, sellPoints, exitPoints, eventPoints,
                    intendedLongPoints, intendedShortPoints, positionPoints, averageCostPoints,
                    restingOrdersPoints, new List<PricePoint>(), patternPoints,
                    group != null ? $"_{Path.GetFileNameWithoutExtension(group.JsonPath)}" : "");
            }

            return (finalPnL, finalPosition, finalAverageCost, bidPoints, askPoints, buyPoints, sellPoints,
                exitPoints, eventPoints, intendedLongPoints, intendedShortPoints, positionPoints,
                averageCostPoints, restingOrdersPoints, new List<PricePoint>(), patternPoints);
        }

        private (List<PricePoint>, List<PricePoint>) CreateBidAskPoints(List<MarketSnapshot> marketSnapshots)
        {
            var bidPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesBid, " Best Bid")).ToList();
            var askPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesAsk, " Best Ask")).ToList();
            return (bidPoints, askPoints);
        }

        private (List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>)
        CreateTradePoints(List<ReportGenerator.EventLog> eventLogs)
        {
            var buyPoints = new List<PricePoint>();
            var sellPoints = new List<PricePoint>();
            var exitPoints = new List<PricePoint>();
            var intendedLongPoints = new List<PricePoint>();
            var intendedShortPoints = new List<PricePoint>();

            int prevPos = 0;
            foreach (var ev in eventLogs)
            {
                string prefix = " ";

                // Trade markers
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

                // Explicit exits
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

            return (buyPoints, sellPoints, exitPoints, intendedLongPoints, intendedShortPoints);
        }

        private List<PricePoint> CreateEventPoints(List<ReportGenerator.EventLog> eventLogs)
        {
            return eventLogs
                .Select(ev => new PricePoint(ev.Timestamp, (ev.BestYesBid + ev.BestYesAsk) / 2.0, " " + ev.Memo))
                .ToList();
        }

        private (List<PricePoint>, List<PricePoint>) CreatePositionPoints(List<ReportGenerator.EventLog> eventLogs)
        {
            var positionPoints = eventLogs
                .Select(ev => new PricePoint(ev.Timestamp, ev.Position, $"Position: {ev.Position}"))
                .ToList();

            var averageCostPoints = eventLogs
                .Select(ev => new PricePoint(ev.Timestamp, ev.AverageCost, $"Avg Cost: {ev.AverageCost:F2}"))
                .ToList();

            return (positionPoints, averageCostPoints);
        }

        private List<PricePoint> CreateRestingOrdersPoints(List<ReportGenerator.EventLog> eventLogs)
        {
            return eventLogs
                .Select(ev =>
                {
                    int totalResting = 0;

                    // Parse RestingYesBids
                    if (!string.IsNullOrEmpty(ev.RestingYesBids) && ev.RestingYesBids != "N/A")
                    {
                        var yesOrders = ev.RestingYesBids.Split(',');
                        foreach (var order in yesOrders)
                        {
                            var parts = order.Trim().Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int qty))
                            {
                                totalResting += qty;
                            }
                        }
                    }

                    // Parse RestingNoBids
                    if (!string.IsNullOrEmpty(ev.RestingNoBids) && ev.RestingNoBids != "N/A")
                    {
                        var noOrders = ev.RestingNoBids.Split(',');
                        foreach (var order in noOrders)
                        {
                            var parts = order.Trim().Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int qty))
                            {
                                totalResting += qty;
                            }
                        }
                    }

                    return new PricePoint(ev.Timestamp, totalResting, $"Resting Orders: {totalResting}");
                })
                .ToList();
        }

        private List<PricePoint> CreatePatternPoints(List<ReportGenerator.EventLog> eventLogs)
        {
            var patternPoints = new List<PricePoint>();
            foreach (var ev in eventLogs)
            {
                if (ev.Patterns != null && ev.Patterns.Any())
                {
                    foreach (var pattern in ev.Patterns)
                    {
                        patternPoints.Add(new PricePoint(ev.Timestamp, 0, $"Pattern: {pattern.Name}"));
                    }
                }
            }
            return patternPoints;
        }

        public void SaveMarketDataToFile(
            string marketTicker, double finalPnL, int finalPosition, double finalAverageCost,
            List<PricePoint> bidPoints, List<PricePoint> askPoints,
            List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
            List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
            List<PricePoint> positionPoints, List<PricePoint> averageCostPoints, List<PricePoint> restingOrdersPoints,
            List<PricePoint> discrepancyPoints, List<PricePoint> patternPoints,
            string fileNameSuffix = "")
        {
            var cachedData = new CachedMarketData
            {
                Market = marketTicker,
                PnL = finalPnL,
                SimulatedPosition = finalPosition,
                AverageCost = finalAverageCost,
                BidPoints = bidPoints,
                AskPoints = askPoints,
                BuyPoints = buyPoints,
                SellPoints = sellPoints,
                ExitPoints = exitPoints,
                EventPoints = eventPoints,
                IntendedLongPoints = intendedLongPoints,
                IntendedShortPoints = intendedShortPoints,
                PositionPoints = positionPoints,
                AverageCostPoints = averageCostPoints,
                RestingOrdersPoints = restingOrdersPoints,
                DiscrepancyPoints = discrepancyPoints,
                PatternPoints = patternPoints
            };

            var json = JsonSerializer.Serialize(cachedData);
            Directory.CreateDirectory(_cacheDirectory);
            var filePath = Path.Combine(_cacheDirectory, $"{marketTicker}{fileNameSuffix}.json");
            File.WriteAllText(filePath, json);
        }
    }
}