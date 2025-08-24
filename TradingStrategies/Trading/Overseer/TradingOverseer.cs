// Updated TradingOverseer.cs with Memo added to EventLog
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Helpers;
using SmokehouseBot.Services.Interfaces;
using TradingStrategies.Trading.Overseer;
using TradingStrategies.Classification;
using static SmokehouseInterfaces.Enums.StrategyEnums;
using static TradingStrategies.Trading.Overseer.ReportGenerator;
using TradingStrategies.Extensions;

namespace TradingStrategies
{
    public class TradingOverseer
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITradingSnapshotService _snapshotService;
        private StrategySelectionHelper _strategySelectionHelper;
        // Cache for market types: maps (MarketTicker, Timestamp) to MarketType for per-snapshot caching
        private readonly Dictionary<(string Ticker, DateTime Timestamp), MarketType> _marketTypeCache;

        public TradingOverseer(IServiceScopeFactory scopeFactory, ITradingSnapshotService snapshotService)
        {
            _scopeFactory = scopeFactory;
            _snapshotService = snapshotService;
            _strategySelectionHelper = new StrategySelectionHelper();
            _marketTypeCache = new Dictionary<(string, DateTime), MarketType>();
        }

        private record SnapshotGroupTemp(string MarketTicker, DateTime StartTime, DateTime EndTime);

        public List<(PathPerformance performance, List<EventLog> events)> TestScenario(Scenario scenario, List<MarketSnapshot> snapshots, bool writeToFile, double initialCash = 100.0, SnapshotGroupDTO? group = null)
        {
            if (snapshots == null || snapshots.Count == 0) return new List<(PathPerformance, List<EventLog>)>();

            // Detect single-strategy mode to enable in-place mutations (no cloning)
            bool isSingleStrategy = scenario.StrategiesByMarketConditions.Values.All(hs => hs.Count <= 1);

            var initialStrategiesByMarketConditions = scenario.StrategiesByMarketConditions.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<Strategy>(kvp.Value)
            );
            var initialPath = new SimulationPath(initialStrategiesByMarketConditions, 0, initialCash)
            {
                Events = new List<ReportGenerator.EventLog>(),
                SimulatedBook = null,
                SimulatedRestingOrders = new List<(string action, string side, string type, int count, int price, DateTime? expiration)>()
            };
            var activePaths = new List<SimulationPath> { initialPath };
            var helper = new MarketTypeHelper();

            MarketSnapshot prevSnapshot = null;
            double maxRisk = 10.0;

            foreach (var snapshot in snapshots)
            {
                SetMarketType(snapshot, helper);
                var (yesDeltas, noDeltas) = ComputeDeltasIfApplicable(prevSnapshot, snapshot);

                ApplyDeltasAndSimulateFills(activePaths, yesDeltas, noDeltas, snapshot.Timestamp);

                var newPaths = new List<SimulationPath>();

                foreach (var path in activePaths)
                {
                    ExpireRestingOrders(path, snapshot.Timestamp);
                    var book = GetOrInitializeBook(path, snapshot);

                    var effectiveSnapshot = CreateEffectiveSnapshot(snapshot, book, path);

                    var currentMarketConditions = ParseMarketConditions(effectiveSnapshot.MarketType);

                    if (!path.StrategiesByMarketConditions.TryGetValue(currentMarketConditions, out var activeStrategies) || !activeStrategies.Any())
                    {
                        var newPath = HandleNoStrategies(path, effectiveSnapshot, currentMarketConditions, book, isSingleStrategy);
                        newPaths.Add(newPath);
                        continue;
                    }

                    var actionGroups = GroupStrategiesByAction(activeStrategies, effectiveSnapshot, prevSnapshot);

                    foreach (var kvp in actionGroups)
                    {
                        var newPath = HandleActionGroup(path, kvp.Key, kvp.Value, effectiveSnapshot, prevSnapshot,
                            currentMarketConditions, book, maxRisk, isSingleStrategy);
                        if (newPath != null)
                        {
                            newPaths.Add(newPath);
                        }
                    }
                }

                activePaths = newPaths;
                prevSnapshot = snapshot;
            }

            var pathData = GenerateReportsAndPerformances(group, activePaths, snapshots, initialCash, writeToFile);

            return pathData;
        }

        private void SetMarketType(MarketSnapshot snapshot, MarketTypeHelper helper)
        {
            try
            {
                var key = (snapshot.MarketTicker, snapshot.Timestamp);
                if (!_marketTypeCache.TryGetValue(key, out var cachedType))
                {
                    cachedType = helper.GetMarketType(snapshot);
                    _marketTypeCache[key] = cachedType;
                }
                snapshot.MarketType = cachedType.ToString();
            }
            catch
            {
                snapshot.MarketType = "Unknown";
            }
        }

        private (Dictionary<int, int> yesDeltas, Dictionary<int, int> noDeltas) ComputeDeltasIfApplicable(MarketSnapshot prevSnapshot, MarketSnapshot snapshot)
        {
            Dictionary<int, int> yesDeltas = null;
            Dictionary<int, int> noDeltas = null;
            if (prevSnapshot != null)
            {
                yesDeltas = ComputeDeltas(prevSnapshot.GetYesBids(), snapshot.GetYesBids());
                noDeltas = ComputeDeltas(prevSnapshot.GetNoBids(), snapshot.GetNoBids());
            }
            return (yesDeltas, noDeltas);
        }

        private void ApplyDeltasAndSimulateFills(List<SimulationPath> activePaths, Dictionary<int, int> yesDeltas, Dictionary<int, int> noDeltas, DateTime timestamp)
        {
            if (yesDeltas == null || noDeltas == null) return;

            foreach (var path in activePaths)
            {
                if (path.SimulatedBook != null)
                {
                    path.SimulatedBook.ApplyDeltas(yesDeltas, noDeltas);
                    SimulateFillsFromDeltas(path, yesDeltas, noDeltas, timestamp);
                }
            }
        }

        private void ExpireRestingOrders(SimulationPath path, DateTime timestamp)
        {
            for (int i = path.SimulatedRestingOrders.Count - 1; i >= 0; i--)
            {
                var order = path.SimulatedRestingOrders[i];
                if (order.expiration.HasValue && order.expiration < timestamp)
                {
                    var targetBook = (order.action == "buy" && order.side == "yes") || (order.action == "sell" && order.side == "no") ? path.SimulatedBook.YesBids : path.SimulatedBook.NoBids;
                    path.SimulatedBook.ReduceDepth(targetBook, order.price, order.count);
                    path.SimulatedRestingOrders.RemoveAt(i);
                }
            }
        }

        private SimulatedOrderbook GetOrInitializeBook(SimulationPath path, MarketSnapshot snapshot)
        {
            SimulatedOrderbook book;
            if (path.SimulatedBook == null)
            {
                book = new SimulatedOrderbook();
                book.InitializeFromSnapshot(snapshot);
            }
            else
            {
                book = path.SimulatedBook;
            }
            return book;
        }

        private MarketSnapshot CreateEffectiveSnapshot(MarketSnapshot snapshot, SimulatedOrderbook book, SimulationPath path)
        {
            var effectiveSnapshot = snapshot.Clone();
            effectiveSnapshot.UpdateOrderbookMetricsFromSimulated(book);
            effectiveSnapshot.PositionSize = path.Position;
            effectiveSnapshot.RestingOrders = path.SimulatedRestingOrders;
            return effectiveSnapshot;
        }

        private MarketType ParseMarketConditions(string marketType)
        {
            if (!Enum.TryParse<MarketType>(marketType, true, out var currentMarketConditions))
            {
                currentMarketConditions = MarketType.Undefined;
            }
            return currentMarketConditions;
        }

        private SimulationPath HandleNoStrategies(SimulationPath path, MarketSnapshot effectiveSnapshot, MarketType currentMarketConditions, SimulatedOrderbook book, bool isSingleStrategy)
        {
            Dictionary<MarketType, HashSet<Strategy>> newStrategiesByMarketConditions;
            SimulatedOrderbook actionBook;
            List<(string, string, string, int, int, DateTime?)> actionResting;
            List<ReportGenerator.EventLog> actionEvents;

            if (isSingleStrategy)
            {
                // Mutate in-place for efficiency
                newStrategiesByMarketConditions = path.StrategiesByMarketConditions;
                actionBook = book; // Reference, no clone
                actionResting = path.SimulatedRestingOrders; // Reference
                actionEvents = path.Events; // Reference
            }
            else
            {
                newStrategiesByMarketConditions = path.StrategiesByMarketConditions.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HashSet<Strategy>(kvp.Value)
                );
                actionBook = book.Clone();
                actionResting = path.SimulatedRestingOrders.Select(o => o).ToList();
                actionEvents = new List<ReportGenerator.EventLog>(path.Events);
            }

            path.CurrentRisk = path.CurrentRisk; // No change

            var (restingYes, restingNo) = SummarizeRestingOrders(actionResting);

            var eventLog = new ReportGenerator.EventLog
            {
                Timestamp = effectiveSnapshot.Timestamp,
                MarketType = effectiveSnapshot.MarketType ?? "Unknown",
                Action = ActionType.None.ToString(),
                Position = path.Position,
                Cash = path.Cash,
                Liquidity = effectiveSnapshot.CalculateLiquidityScore(),
                YesSpread = actionBook.GetBestYesAsk() - actionBook.GetBestYesBid(),
                TradeRate = (effectiveSnapshot.TradeRatePerMinute_Yes) + (effectiveSnapshot.TradeRatePerMinute_No),
                RSI = effectiveSnapshot.RSI_Medium ?? 0,
                Strategies = "",
                BestYesBid = effectiveSnapshot.BestYesBid,
                BestNoBid = effectiveSnapshot.BestNoBid,
                NoSpread = effectiveSnapshot.NoSpread,
                DepthAtBestYesBid = effectiveSnapshot.DepthAtBestYesBid,
                DepthAtBestNoBid = effectiveSnapshot.DepthAtBestNoBid,
                TotalYesBidContracts = effectiveSnapshot.TotalBidContracts_Yes,
                TotalNoBidContracts = effectiveSnapshot.TotalBidContracts_No,
                BidImbalance = effectiveSnapshot.BidCountImbalance,
                TradeRatePerMinute_Yes = effectiveSnapshot.TradeRatePerMinute_Yes,
                TradeRatePerMinute_No = effectiveSnapshot.TradeRatePerMinute_No,
                TradeVolumePerMinute_Yes = effectiveSnapshot.TradeVolumePerMinute_Yes,
                TradeVolumePerMinute_No = effectiveSnapshot.TradeVolumePerMinute_No,
                TradeCount_Yes = effectiveSnapshot.TradeCount_Yes,
                TradeCount_No = effectiveSnapshot.TradeCount_No,
                AverageTradeSize_Yes = effectiveSnapshot.AverageTradeSize_Yes,
                AverageTradeSize_No = effectiveSnapshot.AverageTradeSize_No,
                RestingYesBids = restingYes,
                RestingNoBids = restingNo,
                Memo = "" // No memo for no-action
            };

            actionEvents.Add(eventLog);

            if (isSingleStrategy)
            {
                return path; // Return mutated path
            }
            else
            {
                return new SimulationPath(newStrategiesByMarketConditions, path.Position, path.Cash)
                {
                    Events = actionEvents,
                    SimulatedBook = actionBook,
                    SimulatedRestingOrders = actionResting,
                    CurrentRisk = path.CurrentRisk
                };
            }
        }

        private Dictionary<ActionType, List<(Strategy strategy, ActionDecision decision)>> GroupStrategiesByAction(HashSet<Strategy> activeStrategies, 
            MarketSnapshot effectiveSnapshot, MarketSnapshot previousEffectiveSnapshot)
        {
            var actionGroups = new Dictionary<ActionType, List<(Strategy strategy, ActionDecision decision)>>();

            foreach (var strategy in activeStrategies)
            {
                var decision = strategy.GetAction(effectiveSnapshot, previousEffectiveSnapshot);
                var action = decision.Type;
                if (!actionGroups.ContainsKey(action))
                    actionGroups[action] = new List<(Strategy strategy, ActionDecision decision)>();
                actionGroups[action].Add((strategy, decision));
            }

            return actionGroups;
        }

        private SimulationPath HandleActionGroup(SimulationPath path, ActionType action, List<(Strategy strategy, ActionDecision decision)> strategiesWithDecisions, 
            MarketSnapshot effectiveSnapshot, MarketSnapshot? previousEffectiveSnapshot,
            MarketType currentMarketConditions, SimulatedOrderbook book, double maxRisk, bool isSingleStrategy)
        {
            var decision = strategiesWithDecisions.First().decision;

            SimulatedOrderbook actionBook;
            List<(string, string, string, int, int, DateTime?)> actionResting;
            List<ReportGenerator.EventLog> actionEvents;
            Dictionary<MarketType, HashSet<Strategy>> newStrategiesByMarketConditions;

            if (isSingleStrategy)
            {
                // Mutate in-place
                actionBook = book; // Reference
                actionResting = path.SimulatedRestingOrders; // Reference
                actionEvents = path.Events; // Reference
                newStrategiesByMarketConditions = path.StrategiesByMarketConditions;
            }
            else
            {
                actionBook = book.Clone();
                actionResting = path.SimulatedRestingOrders.Select(o => o).ToList();
                actionEvents = new List<ReportGenerator.EventLog>(path.Events);
                newStrategiesByMarketConditions = path.StrategiesByMarketConditions.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HashSet<Strategy>(kvp.Value)
                );
            }

            newStrategiesByMarketConditions[currentMarketConditions] = new HashSet<Strategy>(strategiesWithDecisions.Select(t => t.strategy));

            int newPosition = path.Position;
            double newCash = path.Cash;
            double newRisk = path.CurrentRisk;

            // Handle implicit exit if flipping position direction
            bool needsFlip = (action == ActionType.Long && path.Position < 0) || (action == ActionType.Short && path.Position > 0);
            bool skipAdd = false;
            if (needsFlip)
            {
                skipAdd = HandleSpecificAction(ActionType.Exit, new HashSet<Strategy>(strategiesWithDecisions.Select(t => t.strategy)), 
                    effectiveSnapshot, previousEffectiveSnapshot,
                    actionBook, actionResting, ref newPosition, ref newCash, ref newRisk, path, effectiveSnapshot.Timestamp, maxRisk);
                if (skipAdd || newPosition == path.Position)
                {
                    return null;
                }
            }
            else
            {
                skipAdd = HandleSpecificAction(action, new HashSet<Strategy>(strategiesWithDecisions.Select(t => t.strategy)), 
                    effectiveSnapshot, previousEffectiveSnapshot,
                    actionBook, actionResting, ref newPosition, ref newCash, ref newRisk, path, effectiveSnapshot.Timestamp, maxRisk);
                if (skipAdd) return null;
            }

            var (restingYes, restingNo) = SummarizeRestingOrders(actionResting);

            var eventLog = new ReportGenerator.EventLog
            {
                Timestamp = effectiveSnapshot.Timestamp,
                MarketType = effectiveSnapshot.MarketType ?? "Unknown",
                Action = action.ToString(),
                Position = newPosition,
                Cash = newCash,
                Liquidity = effectiveSnapshot.CalculateLiquidityScore(),
                YesSpread = actionBook.GetBestYesAsk() - actionBook.GetBestYesBid(),
                TradeRate = (effectiveSnapshot.TradeRatePerMinute_Yes) + (effectiveSnapshot.TradeRatePerMinute_No),
                RSI = effectiveSnapshot.RSI_Medium ?? 0,
                Strategies = string.Join(", ", strategiesWithDecisions.Select(t => t.strategy).SelectMany(s => s.Strats.Select(e => e.GetType().Name)).Distinct()),
                BestYesBid = effectiveSnapshot.BestYesBid,
                BestYesAsk = effectiveSnapshot.BestYesAsk,
                BestNoBid = effectiveSnapshot.BestNoBid,
                BestNoAsk = effectiveSnapshot.BestNoAsk,
                NoSpread = effectiveSnapshot.NoSpread,
                DepthAtBestYesBid = effectiveSnapshot.DepthAtBestYesBid,
                DepthAtBestNoBid = effectiveSnapshot.DepthAtBestNoBid,
                TotalYesBidContracts = effectiveSnapshot.TotalBidContracts_Yes,
                TotalNoBidContracts = effectiveSnapshot.TotalBidContracts_No,
                BidImbalance = effectiveSnapshot.BidCountImbalance,
                TradeRatePerMinute_Yes = effectiveSnapshot.TradeRatePerMinute_Yes,
                TradeRatePerMinute_No = effectiveSnapshot.TradeRatePerMinute_No,
                TradeVolumePerMinute_Yes = effectiveSnapshot.TradeVolumePerMinute_Yes,
                TradeVolumePerMinute_No = effectiveSnapshot.TradeVolumePerMinute_No,
                TradeCount_Yes = effectiveSnapshot.TradeCount_Yes,
                TradeCount_No = effectiveSnapshot.TradeCount_No,
                AverageTradeSize_Yes = effectiveSnapshot.AverageTradeSize_Yes,
                AverageTradeSize_No = effectiveSnapshot.AverageTradeSize_No,
                RestingYesBids = restingYes,
                RestingNoBids = restingNo,
                Memo = decision.Memo
            };

            actionEvents.Add(eventLog);

            if (isSingleStrategy)
            {
                path.Position = newPosition;
                path.Cash = newCash;
                path.CurrentRisk = newRisk;
                return path; // Return mutated path
            }
            else
            {
                var newPath = new SimulationPath(newStrategiesByMarketConditions, newPosition, newCash)
                {
                    Events = actionEvents,
                    SimulatedBook = actionBook,
                    SimulatedRestingOrders = actionResting
                };
                newPath.CurrentRisk = newRisk;
                return newPath;
            }
        }

        private bool HandleSpecificAction(ActionType action, HashSet<Strategy> strategiesForAction, MarketSnapshot effectiveSnapshot, MarketSnapshot? previousEffectiveSnapshot,
            SimulatedOrderbook actionBook, List<(string action, string side, string type, int count, int price, DateTime? expiration)> actionResting, ref int newPosition, ref double newCash, ref double newRisk, SimulationPath path, DateTime timestamp, double maxRisk)
        {
            int qty = 0;
            if (action == ActionType.Long || action == ActionType.Short || action == ActionType.Exit)
            {
                bool longSide = action == ActionType.Long || (action == ActionType.Exit && path.Position < 0);
                bool shortSide = action == ActionType.Short || (action == ActionType.Exit && path.Position > 0);

                qty = (action == ActionType.Exit) ? Math.Abs(path.Position) : int.MaxValue;
                int remainingQty = qty;
                double totalCost = 0.0;

                var bookToReduce = longSide ? actionBook.NoBids : actionBook.YesBids;

                bool isPaying = action != ActionType.Exit;

                double LevelPriceFunc(int price) => isPaying ? (100 - price) / 100.0 : price / 100.0;

                double remainingRisk = maxRisk - newRisk;
                double remainingCash = newCash;

                for (int p = 99; p >= 1; p--)
                {
                    if (remainingQty <= 0) break;
                    if (bookToReduce[p] == null || bookToReduce[p].Count == 0) continue;
                    int depth = bookToReduce[p].Sum(o => o.count);
                    double levelPrice = LevelPriceFunc(p);
                    int maxFillByRisk = isPaying ? (int)Math.Floor(remainingRisk / levelPrice) : depth;
                    int maxFillByCash = isPaying ? (int)Math.Floor(remainingCash / levelPrice) : depth;
                    int maxFill = Math.Min(depth, Math.Min(maxFillByRisk, maxFillByCash));
                    int fill = Math.Min(remainingQty, maxFill);

                    totalCost += fill * levelPrice;
                    actionBook.ReduceDepth(bookToReduce, p, fill);
                    remainingQty -= fill;

                    if (isPaying)
                    {
                        remainingRisk -= fill * levelPrice;
                        remainingCash -= fill * levelPrice;
                    }

                    int effectiveTradePrice = isPaying ? (100 - p) : p;
                    SimulateFillsFromTrade(actionResting, longSide ? "no" : "yes", effectiveTradePrice, fill, ref newPosition, ref newCash, timestamp);
                }

                int filled = qty - remainingQty;

                if (action != ActionType.Exit)
                {
                    newRisk += totalCost;
                }
                else
                {
                    newRisk = 0;
                }

                if (isPaying)
                {
                    newCash -= totalCost;
                }
                else
                {
                    newCash += totalCost;
                }

                int posDelta = longSide ? filled : -filled;
                newPosition += posDelta;

                // Apply taker fees
                newCash -= 0.0007 * totalCost;
            }
            else if (action == ActionType.PostYes)
            {
                var decision = strategiesForAction.First().GetAction(effectiveSnapshot, previousEffectiveSnapshot);
                int limitPrice = decision.Price;
                qty = decision.Qty;
                DateTime? exp = decision.Expiration;
                if (limitPrice > 0 && qty > 0)
                {
                    actionBook.AddToDepth(actionBook.YesBids, limitPrice, qty, effectiveSnapshot.Timestamp);
                    actionResting.Add(("buy", "yes", "limit", qty, limitPrice, exp));
                }
            }
            else if (action == ActionType.PostAsk)
            {
                var decision = strategiesForAction.First().GetAction(effectiveSnapshot, previousEffectiveSnapshot);
                int sellPrice = decision.Price;
                int bidPrice = 100 - sellPrice;
                qty = decision.Qty;
                DateTime? exp = decision.Expiration;
                if (bidPrice > 0 && qty > 0)
                {
                    actionBook.AddToDepth(actionBook.NoBids, bidPrice, qty, effectiveSnapshot.Timestamp);
                    actionResting.Add(("sell", "yes", "limit", qty, sellPrice, exp));
                }
            }
            else if (action == ActionType.Cancel)
            {
                foreach (var order in actionResting)
                {
                    var targetBook = (order.action == "buy" && order.side == "yes") || (order.action == "sell" && order.side == "no") ? actionBook.YesBids : actionBook.NoBids;
                    actionBook.ReduceDepth(targetBook, order.price, order.count);
                }
                actionResting.Clear();
            }

            return false; // Always return false (never skip the path)
        }

        private (string, string) SummarizeRestingOrders(List<(string action, string side, string type, int count, int price, DateTime? expiration)> restingOrders)
        {
            var yesBidsSummary = new Dictionary<int, int>();
            var noBidsSummary = new Dictionary<int, int>();
            foreach (var order in restingOrders)
            {
                if (order.type != "limit") continue;
                if (order.action == "buy" && order.side == "yes")
                {
                    if (!yesBidsSummary.ContainsKey(order.price)) yesBidsSummary[order.price] = 0;
                    yesBidsSummary[order.price] += order.count;
                }
                else if (order.action == "sell" && order.side == "yes")
                {
                    int noPrice = 100 - order.price;
                    if (!noBidsSummary.ContainsKey(noPrice)) noBidsSummary[noPrice] = 0;
                    noBidsSummary[noPrice] += order.count;
                }
            }
            string restingYes = string.Join(", ", yesBidsSummary.OrderBy(k => k.Key).Select(kv => $"{kv.Key}:{kv.Value}"));
            string restingNo = string.Join(", ", noBidsSummary.OrderBy(k => k.Key).Select(kv => $"{kv.Key}:{kv.Value}"));
            return (restingYes, restingNo);
        }

        private void SimulateFillsFromDeltas(SimulationPath path, Dictionary<int, int> yesDeltas, Dictionary<int, int> noDeltas, DateTime currentTime)
        {
            var resting = path.SimulatedRestingOrders;
            for (int i = resting.Count - 1; i >= 0; i--)
            {
                var order = resting[i];
                if (order.expiration.HasValue && order.expiration < currentTime)
                {
                    // Expire and cancel
                    var book = path.SimulatedBook;
                    var targetBook = (order.action == "buy" && order.side == "yes") || (order.action == "sell" && order.side == "no") ? book.YesBids : book.NoBids;
                    book.ReduceDepth(targetBook, order.price, order.count);
                    resting.RemoveAt(i);
                    continue;
                }

                Dictionary<int, int> relevantDeltas = (order.action == "buy" && order.side == "yes") || (order.action == "sell" && order.side == "no") ? yesDeltas : noDeltas;
                if (relevantDeltas.TryGetValue(order.price, out int delta) && delta < 0)
                {
                    int fillQty = Math.Min(order.count, -delta);
                    double fillPrice = order.price / 100.0;

                    if (order.action == "buy")
                    {
                        path.Cash -= fillQty * fillPrice;
                        path.Position += order.side == "yes" ? fillQty : -fillQty;
                    }
                    else // sell
                    {
                        path.Cash += fillQty * fillPrice;
                        path.Position -= order.side == "yes" ? fillQty : -fillQty;
                    }

                    order.count -= fillQty;
                    if (order.count <= 0)
                    {
                        resting.RemoveAt(i);
                    }
                    else
                    {
                        resting[i] = order;
                    }
                }
            }
        }

        private void SimulateFillsFromTrade(List<(string action, string side, string type, int count, int price, DateTime? expiration)> resting, string tradeSide, int tradePrice, int tradeQty, ref int position, ref double cash, DateTime timestamp)
        {
            for (int i = resting.Count - 1; i >= 0; i--)
            {
                var order = resting[i];
                if (order.price == tradePrice &&
                    ((tradeSide == "yes" && order.side == "yes" && order.action == "buy") ||
                     (tradeSide == "no" && order.side == "yes" && order.action == "sell")))
                {
                    int fillQty = Math.Min(order.count, tradeQty);
                    double fillPrice = order.price / 100.0;

                    if (order.action == "buy")
                    {
                        cash -= fillQty * fillPrice;
                        position += order.side == "yes" ? fillQty : -fillQty;
                    }
                    else // sell
                    {
                        cash += fillQty * fillPrice;
                        position -= order.side == "yes" ? fillQty : -fillQty;
                    }

                    order.count -= fillQty;
                    tradeQty -= fillQty;
                    if (order.count <= 0)
                    {
                        resting.RemoveAt(i);
                    }
                    else
                    {
                        resting[i] = order;
                    }
                    if (tradeQty <= 0) break;
                }
            }
        }

        private List<(PathPerformance performance, List<EventLog> events)> GenerateReportsAndPerformances(SnapshotGroupDTO group, List<SimulationPath> activePaths, List<MarketSnapshot> snapshots, double initialCash, bool writeToFile)
        {
            string outputDir = @"C:\BotTestingOutput";
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

                var pathTaken = string.Join(" → ", eventLogs.Select(e => e.MarketType).Distinct());
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
                    EndType = endType
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

        private ActionDecision GetLiveAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, Dictionary<MarketType, Strategy> strategiesByMarketConditions)
        {
            var helper = new MarketTypeHelper();
            snapshot.MarketType = helper.GetMarketType(snapshot).ToString();
            if (!Enum.TryParse<MarketType>(snapshot.MarketType, true, out var currentMarketConditions))
            {
                throw new ArgumentException($"Unknown MarketType: {snapshot.MarketType}");
            }

            if (strategiesByMarketConditions.TryGetValue(currentMarketConditions, out var strategy))
            {
                return strategy.GetAction(snapshot, previousSnapshot); // Return full Decision
            }

            return new ActionDecision { Type = ActionType.None, Memo = "Default GetLiveAction" };
        }

        private Dictionary<int, int> ComputeDeltas(Dictionary<int, int> prev, Dictionary<int, int> curr)
        {
            var deltas = new Dictionary<int, int>();
            var allPrices = new HashSet<int>(prev.Keys.Concat(curr.Keys));
            foreach (var price in allPrices)
            {
                int prevD = prev.GetValueOrDefault(price, 0);
                int currD = curr.GetValueOrDefault(price, 0);
                int delta = currD - prevD;
                if (delta != 0)
                {
                    deltas[price] = delta;
                }
            }
            return deltas;
        }

        private double GetEquity(SimulationPath path, MarketSnapshot lastSnapshot)
        {
            double equity = path.Cash;
            if (path.SimulatedBook == null)
                return equity;

            int bestYesBid = path.SimulatedBook.GetBestYesBid();
            int bestNoBid = path.SimulatedBook.GetBestNoBid();
            int bestYesAsk = bestNoBid > 0 ? 100 - bestNoBid : 100;
            int bestNoAsk = bestYesBid > 0 ? 100 - bestYesBid : 100;

            bool natural = bestYesBid == 0 || bestNoBid == 0;
            if (natural)
            {
                if (path.Position > 0)
                {
                    equity += path.Position * (bestNoBid == 0 ? 1.0 : 0.0);
                }
                else if (path.Position < 0)
                {
                    equity += Math.Abs(path.Position) * (bestYesBid == 0 ? 1.0 : 0.0);
                }
            }
            else
            {
                double midYes = (bestYesBid + bestYesAsk) / 2 / 100.0;
                double midNo = (bestNoBid + bestNoAsk) / 2 / 100.0;
                if (path.Position > 0)
                    equity += path.Position * midYes;
                else if (path.Position < 0)
                    equity += Math.Abs(path.Position) * midNo;
            }
            return equity;
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