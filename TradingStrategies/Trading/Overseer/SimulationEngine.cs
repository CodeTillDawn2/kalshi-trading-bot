using Microsoft.Extensions.DependencyInjection;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashPatterns;
using TradingStrategies.Extensions;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Helpers;
using TradingStrategies.Trading.Overseer;
using static BacklashInterfaces.Enums.StrategyEnums;
using static TradingStrategies.Trading.Overseer.ReportGenerator;

namespace TradingStrategies.Trading.Overseer
{
    public class SimulationEngine
    {
        private readonly MarketTypeService _marketTypeService;
        private readonly PatternDetectionService _patternDetectionService;

        public SimulationEngine()
        {
            _marketTypeService = new MarketTypeService();
            _patternDetectionService = new PatternDetectionService();
        }

        public List<SimulationPath> RunSimulation(Scenario scenario, List<MarketSnapshot> snapshots, bool isSingleStrategy)
        {
            if (snapshots == null || snapshots.Count == 0) return new List<SimulationPath>();

            var initialStrategiesByMarketConditions = scenario.StrategiesByMarketConditions.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<Strategy>(kvp.Value)
            );
            var initialPath = new SimulationPath(initialStrategiesByMarketConditions, 0, 100.0)
            {
                Events = new List<ReportGenerator.EventLog>(),
                SimulatedBook = null,
                SimulatedRestingOrders = new List<(string action, string side, string type, int count, int price, DateTime? expiration)>()
            };
            var activePaths = new List<SimulationPath> { initialPath };

            MarketSnapshot prevSnapshot = null;
            double maxRisk = 10.0;

            foreach (var snapshot in snapshots)
            {
                SetMarketType(snapshot);

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

                    var actionGroups = GroupStrategiesByAction(activeStrategies, effectiveSnapshot, prevSnapshot, path.Position);

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

            return activePaths;
        }

        private void SetMarketType(MarketSnapshot snapshot)
        {
            _marketTypeService.AssignMarketTypeToSnapshot(snapshot);
        }

        private (Dictionary<int, int> yesDeltas, Dictionary<int, int> noDeltas) ComputeDeltasIfApplicable(MarketSnapshot prevSnapshot, MarketSnapshot snapshot)
        {
            Dictionary<int, int> yesDeltas = new Dictionary<int, int>();
            Dictionary<int, int> noDeltas = new Dictionary<int, int>();
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
            if (path.SimulatedBook == null)
            {
                var book = new SimulatedOrderbook();
                book.InitializeFromSnapshot(snapshot);
                path.SimulatedBook = book;
            }
            return path.SimulatedBook;
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
            return _marketTypeService.ConvertStringToMarketType(marketType);
        }

        private SimulationPath HandleNoStrategies(SimulationPath path, MarketSnapshot effectiveSnapshot, MarketType currentMarketConditions, SimulatedOrderbook book, bool isSingleStrategy)
        {
            Dictionary<MarketType, HashSet<Strategy>> newStrategiesByMarketConditions;
            SimulatedOrderbook actionBook;
            List<(string, string, string, int, int, DateTime?)> actionResting;
            List<ReportGenerator.EventLog> actionEvents;

            if (isSingleStrategy)
            {
                newStrategiesByMarketConditions = path.StrategiesByMarketConditions;
                actionBook = book;
                actionResting = path.SimulatedRestingOrders;
                actionEvents = path.Events;
                path.SimulatedBook = actionBook;
                path.SimulatedRestingOrders = actionResting;
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

            path.CurrentRisk = path.CurrentRisk;

            var (restingYes, restingNo) = SummarizeRestingOrders(actionResting);

            var patterns = DetectPatterns(effectiveSnapshot);

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
                Memo = "",
                AverageCost = path.AverageCost,
                Patterns = patterns
            };

            actionEvents.Add(eventLog);

            if (isSingleStrategy)
            {
                return path;
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

        private Dictionary<ActionType, List<(Strategy strategy, ActionDecision decision)>> GroupStrategiesByAction(
            HashSet<Strategy> activeStrategies,
            MarketSnapshot effectiveSnapshot,
            MarketSnapshot previousEffectiveSnapshot,
            int simulationPosition)
        {
            var actionGroups = new Dictionary<ActionType, List<(Strategy strategy, ActionDecision decision)>>();

            foreach (var strategy in activeStrategies)
            {
                var decision = strategy.GetAction(effectiveSnapshot, previousEffectiveSnapshot, simulationPosition);
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
            List<(string action, string side, string type, int count, int price, DateTime? expiration)> actionResting;
            List<ReportGenerator.EventLog> actionEvents;
            Dictionary<MarketType, HashSet<Strategy>> newStrategiesByMarketConditions;

            if (isSingleStrategy)
            {
                actionBook = book;
                actionResting = path.SimulatedRestingOrders;
                actionEvents = path.Events;
                newStrategiesByMarketConditions = path.StrategiesByMarketConditions;
                path.SimulatedBook = actionBook;
                path.SimulatedRestingOrders = actionResting;
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

            bool needsFlip = (action == ActionType.Long && path.Position < 0) || (action == ActionType.Short && path.Position > 0);
            bool skipAdd = false;
            if (needsFlip)
            {
                skipAdd = HandleSpecificAction(ActionType.Exit, new HashSet<Strategy>(strategiesWithDecisions.Select(t => t.strategy)),
                    effectiveSnapshot, previousEffectiveSnapshot, actionBook, actionResting, ref newPosition, ref newCash, ref newRisk, path, effectiveSnapshot.Timestamp, maxRisk, path.Position);
                if (skipAdd || newPosition == path.Position) return null;
            }
            else
            {
                skipAdd = HandleSpecificAction(action, new HashSet<Strategy>(strategiesWithDecisions.Select(t => t.strategy)),
                    effectiveSnapshot, previousEffectiveSnapshot, actionBook, actionResting, ref newPosition, ref newCash, ref newRisk, path, effectiveSnapshot.Timestamp, maxRisk, path.Position);
                if (skipAdd) return null;
            }

            var (restingYes, restingNo) = SummarizeRestingOrders(actionResting);

            var patterns = DetectPatterns(effectiveSnapshot);

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
                Strategies = string.Join(", ", strategiesWithDecisions.Select(s => s.strategy.Name)),
                BestYesBid = effectiveSnapshot.BestYesBid,
                BestYesAsk = effectiveSnapshot.BestYesAsk,
                BestNoBid = effectiveSnapshot.BestNoBid,
                NoSpread = effectiveSnapshot.NoSpread,
                DepthAtBestYesBid = effectiveSnapshot.DepthAtBestYesBid,
                DepthAtBestNoBid = effectiveSnapshot.DepthAtBestNoBid,
                TotalYesBidContracts = effectiveSnapshot.TotalBidContracts_Yes,
                TotalNoBidContracts = effectiveSnapshot.TotalBidContracts_No,
                BidImbalance = effectiveSnapshot.BidCountImbalance,
                RestingYesBids = restingYes,
                RestingNoBids = restingNo,
                Memo = decision.Memo,
                AverageCost = path.AverageCost,
                Patterns = patterns
            };

            actionEvents.Add(eventLog);

            if (isSingleStrategy)
            {
                path.Position = newPosition;
                path.Cash = newCash;
                path.CurrentRisk = newRisk;
                return path;
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

        private bool HandleSpecificAction(
    ActionType action,
    HashSet<Strategy> strategiesForAction,
    MarketSnapshot effectiveSnapshot,
    MarketSnapshot? previousEffectiveSnapshot,
    SimulatedOrderbook actionBook,
    List<(string action, string side, string type, int count, int price, DateTime? expiration)> actionResting,
    ref int newPosition,
    ref double newCash,
    ref double newRisk,
    SimulationPath path,
    DateTime timestamp,
    double maxRisk,
    int simulationPosition)
        {
            // Combo actions: execute market take first, then replace resting with 100% of current position
            if (action == ActionType.LongPostAsk || action == ActionType.ShortPostYes)
            {
                var takeAction = (action == ActionType.LongPostAsk) ? ActionType.Long : ActionType.Short;
                HandleSpecificAction(takeAction, strategiesForAction, effectiveSnapshot, previousEffectiveSnapshot,
                    actionBook, actionResting, ref newPosition, ref newCash, ref newRisk, path, timestamp, maxRisk, simulationPosition);

                int desiredQty = Math.Abs(newPosition);
                if (desiredQty <= 0) return false;

                // Pre-cancel any existing resting orders (replace, don't stack)
                foreach (var order in actionResting)
                {
                    bool isYesSide = order.side == "yes";
                    int bookPrice =
                        (order.action == "sell" && isYesSide) ? (100 - order.price) :
                        (order.action == "buy"  && !isYesSide) ? (100 - order.price) :
                        order.price;

                    var targetBook =
                        (order.action == "buy"  && isYesSide) || (order.action == "sell" && !isYesSide)
                            ? actionBook.YesBids
                            : actionBook.NoBids;

                    actionBook.ReduceDepth(targetBook, bookPrice, order.count);
                }
                actionResting.Clear();

                var decision = strategiesForAction.First().GetAction(effectiveSnapshot, previousEffectiveSnapshot, simulationPosition);

                if (action == ActionType.LongPostAsk && newPosition > 0)
                {
                    int sellPriceYes = decision.Price;
                    int noBidPx = 100 - sellPriceYes;
                    if (sellPriceYes > 0 && sellPriceYes < 100 && noBidPx >= 1 && noBidPx <= 99)
                    {
                        actionBook.AddToDepth(actionBook.NoBids, noBidPx, desiredQty, timestamp);
                        actionResting.Add(("sell", "yes", "limit", desiredQty, sellPriceYes, decision.Expiration));
                    }
                }
                else if (action == ActionType.ShortPostYes && newPosition < 0)
                {
                    int yesBidPx = decision.Price;
                    if (yesBidPx > 0 && yesBidPx < 100)
                    {
                        actionBook.AddToDepth(actionBook.YesBids, yesBidPx, desiredQty, timestamp);
                        actionResting.Add(("buy", "yes", "limit", desiredQty, yesBidPx, decision.Expiration));
                    }
                }
                return false;
            }

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
                        if (shortSide)
                            path.TotalReceived += fill * levelPrice;
                        else
                            path.TotalPaid += fill * levelPrice;
                    }
                    else
                    {
                        path.TotalReceived += fill * levelPrice;
                    }

                    int effectiveTradePrice = isPaying ? (100 - p) : p;
                    SimulateFillsFromTrade(actionResting, longSide ? "no" : "yes", effectiveTradePrice, fill, ref newPosition, ref newCash, timestamp);
                }

                int filled = qty - remainingQty;

                if (action != ActionType.Exit) newRisk += totalCost; else newRisk = 0;

                if (isPaying) newCash -= totalCost; else newCash += totalCost;

                int posDelta = longSide ? filled : -filled;
                newPosition += posDelta;

                newCash -= 0.07 * totalCost; // taker fees
            }
            else if (action == ActionType.PostYes)
            {
                var decision = strategiesForAction.First().GetAction(effectiveSnapshot, previousEffectiveSnapshot, simulationPosition);
                int limitPrice = decision.Price;
                int postQty = decision.Quantity;
                DateTime? exp = decision.Expiration;
                if (limitPrice > 0 && postQty > 0)
                {
                    actionBook.AddToDepth(actionBook.YesBids, limitPrice, postQty, effectiveSnapshot.Timestamp);
                    actionResting.Add(("buy", "yes", "limit", postQty, limitPrice, exp));
                }
            }
            else if (action == ActionType.PostAsk)
            {
                var decision = strategiesForAction.First().GetAction(effectiveSnapshot, previousEffectiveSnapshot, simulationPosition);
                int sellPrice = decision.Price;
                int bidPrice = 100 - sellPrice;
                int postQty = decision.Quantity;
                DateTime? exp = decision.Expiration;
                if (bidPrice > 0 && postQty > 0)
                {
                    actionBook.AddToDepth(actionBook.NoBids, bidPrice, postQty, effectiveSnapshot.Timestamp);
                    actionResting.Add(("sell", "yes", "limit", postQty, sellPrice, exp));
                }
            }
            else if (action == ActionType.Cancel)
            {
                foreach (var order in actionResting)
                {
                    bool isYesSide = order.side == "yes";
                    int bookPrice =
                        (order.action == "sell" && isYesSide) ? (100 - order.price) :
                        (order.action == "buy"  && !isYesSide) ? (100 - order.price) :
                        order.price;

                    var targetBook =
                        (order.action == "buy"  && isYesSide) || (order.action == "sell" && !isYesSide)
                            ? actionBook.YesBids
                            : actionBook.NoBids;

                    actionBook.ReduceDepth(targetBook, bookPrice, order.count);
                }
                actionResting.Clear();
            }

            return false;
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
                    else
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
                    else
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

        private List<BacklashPatterns.PatternDefinitions.PatternDefinition> DetectPatterns(MarketSnapshot snapshot)
        {
            return _patternDetectionService.DetectPatterns(snapshot);
        }
    }
}