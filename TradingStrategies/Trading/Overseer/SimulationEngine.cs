using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashPatterns;
using static TradingStrategies.Trading.Overseer.ReportGenerator;
using TradingStrategies.Extensions;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Helpers;
using TradingStrategies.Trading.Overseer;
using TradingStrategies.Configuration;
using static BacklashInterfaces.Enums.StrategyEnums;
using static TradingStrategies.Trading.Overseer.ReportGenerator;
using System.Diagnostics;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Core engine for executing trading strategy simulations against historical market data.
    /// This class orchestrates the complete simulation lifecycle, processing market snapshots sequentially,
    /// applying trading strategies, simulating order execution and fills, managing position and cash flow,
    /// and generating detailed event logs for backtesting and performance analysis.
    /// </summary>
    /// <remarks>
    /// The SimulationEngine serves as the central component in the trading simulation pipeline,
    /// bridging raw market data with strategy logic to produce realistic trading outcomes.
    /// It handles complex scenarios including multi-strategy execution, order book simulation,
    /// resting order management, and risk controls.
    /// </remarks>
    public class SimulationEngine
    {
        private readonly MarketTypeService _marketTypeService;
        private readonly PatternDetectionService _patternDetectionService;
        private readonly PerformanceMonitor? _performanceMonitor;
        private readonly bool _enablePerformanceMetrics;

        /// <summary>
        /// Initializes a new instance of the SimulationEngine with required services.
        /// </summary>
        /// <param name="configuration">The configuration instance for reading settings from appsettings.json.</param>
        /// <param name="performanceMonitor">Optional performance monitor for recording execution times.</param>
        /// <remarks>
        /// Creates instances of MarketTypeService and PatternDetectionService for
        /// market classification and pattern recognition during simulation.
        /// </remarks>
        public SimulationEngine(IConfiguration configuration, PerformanceMonitor? performanceMonitor = null)
        {
            var tradingConfig = new TradingConfig();
            configuration.GetSection("TradingConfig").Bind(tradingConfig);
            _marketTypeService = new MarketTypeService(tradingConfig);
            StrategySelectionHelper.SetConfiguration(tradingConfig);
            _patternDetectionService = new PatternDetectionService(configuration, performanceMonitor);
            _performanceMonitor = performanceMonitor;
            _enablePerformanceMetrics = configuration.GetValue<bool>("SimulationEngine:EnablePerformanceMetrics", true);
        }

       /// <summary>
       /// Gets the execution time of the last simulation run.
       /// </summary>
       public TimeSpan LastExecutionTime { get; private set; }

       /// <summary>
       /// Gets the approximate memory usage difference of the last simulation run.
       /// </summary>
       public long LastMemoryUsed { get; private set; }

       /// <summary>
       /// Gets the throughput of snapshots processed per second in the last simulation run.
       /// </summary>
       public double LastThroughputSnapshotsPerSecond { get; private set; }

       /// <summary>
       /// Gets the I/O read time in the last simulation run (placeholder for future I/O tracking).
       /// </summary>
       public TimeSpan LastIOReadTime { get; private set; }

       /// <summary>
       /// Gets the I/O write time in the last simulation run (placeholder for future I/O tracking).
       /// </summary>
       public TimeSpan LastIOWriteTime { get; private set; }

       /// <summary>
       /// Gets the pattern performance monitor for accessing performance metrics.
       /// </summary>
       public PerformanceMonitor? PerformanceMonitor => _performanceMonitor;

       /// <summary>
        /// Executes a complete trading simulation against a sequence of market snapshots.
        /// </summary>
        /// <param name="scenario">The trading scenario containing strategies organized by market conditions.</param>
        /// <param name="snapshots">The sequence of market snapshots to simulate against, in chronological order.</param>
        /// <param name="isSingleStrategy">Whether to run in single-strategy mode (modifies paths in-place) or multi-strategy mode (creates new path branches).</param>
        /// <returns>A list of completed simulation paths, each representing a possible outcome of the trading scenario.</returns>
        /// <remarks>
        /// This method orchestrates the entire simulation process:
        /// 1. Initializes simulation paths with starting conditions
        /// 2. Processes each snapshot sequentially, applying market changes and strategy decisions
        /// 3. Manages order book state, position tracking, and cash flow
        /// 4. Generates detailed event logs for analysis
        /// 5. Handles strategy branching in multi-strategy mode
        /// 6. Collects performance metrics (execution time and memory usage)
        ///
        /// The simulation supports realistic trading mechanics including:
        /// - Order book depth changes and fill simulation
        /// - Resting order management and expiration
        /// - Risk controls and position limits
        /// - Multiple concurrent strategy paths
        /// </remarks>
        public List<SimulationPath> RunSimulation(Scenario scenario, List<MarketSnapshot> snapshots, bool isSingleStrategy)
        {
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
            if (scenario.StrategiesByMarketConditions == null) throw new ArgumentException("Scenario StrategiesByMarketConditions cannot be null");
            var stopwatch = Stopwatch.StartNew();
            long memoryBefore = GC.GetTotalMemory(false);
            if (snapshots.Count == 0) return new List<SimulationPath>();

            var initialStrategiesByMarketConditions = scenario.StrategiesByMarketConditions.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<Strategy>(kvp.Value)
            );
            var initialPath = new SimulationPath(initialStrategiesByMarketConditions, 0, 100.0)
            {
                Events = new List<SimulationEventLog>(),
                SimulatedBook = null,
                SimulatedRestingOrders = new List<(string action, string side, string type, int count, int price, DateTime? expiration)>()
            };
            var activePaths = new List<SimulationPath> { initialPath };

            MarketSnapshot prevSnapshot = null;

            foreach (var snapshot in snapshots)
            {
                SetMarketType(snapshot);

                var (yesDeltas, noDeltas) = ComputeOrderBookDeltasIfPreviousSnapshotExists(prevSnapshot, snapshot);
                ApplyOrderBookDeltasAndSimulateFills(activePaths, yesDeltas, noDeltas, snapshot.Timestamp);

                var newPaths = new List<SimulationPath>();

                foreach (var path in activePaths)
                {
                    ExpireRestingOrders(path, snapshot.Timestamp);
                    var book = GetOrCreateSimulatedOrderBook(path, snapshot);

                    var effectiveSnapshot = CreateEffectiveSnapshot(snapshot, book, path);

                    var currentMarketConditions = ParseMarketTypeFromString(effectiveSnapshot.MarketType);

                    if (!path.StrategiesByMarketConditions.TryGetValue(currentMarketConditions, out var activeStrategies) || !activeStrategies.Any())
                    {
                        var newPath = HandleScenarioWithNoActiveStrategies(path, effectiveSnapshot, currentMarketConditions, book, isSingleStrategy);
                        newPaths.Add(newPath);
                        continue;
                    }

                    var actionGroups = GroupStrategiesByAction(activeStrategies, effectiveSnapshot, prevSnapshot, path.Position);

                    foreach (var kvp in actionGroups)
                    {
                        var newPath = ProcessStrategyActionGroup(path, kvp.Key, kvp.Value, effectiveSnapshot, prevSnapshot,
                            currentMarketConditions, book, isSingleStrategy);
                        if (newPath != null)
                        {
                            newPaths.Add(newPath);
                        }
                    }
                }

                activePaths = newPaths;
                prevSnapshot = snapshot;
            }

            stopwatch.Stop();
            long memoryAfter = GC.GetTotalMemory(false);
            if (_enablePerformanceMetrics)
            {
                LastExecutionTime = stopwatch.Elapsed;
                LastMemoryUsed = memoryAfter - memoryBefore;
                LastThroughputSnapshotsPerSecond = snapshots.Count / LastExecutionTime.TotalSeconds;
                LastIOReadTime = TimeSpan.Zero; // Placeholder for future I/O tracking
                LastIOWriteTime = TimeSpan.Zero; // Placeholder for future I/O tracking

                // Record metrics to PerformanceMonitor if available and enabled
                if (_performanceMonitor != null && _performanceMonitor.EnablePerformanceMetrics)
                {
                    var metrics = new Dictionary<string, object>
                    {
                        ["TotalExecutionTime"] = LastExecutionTime,
                        ["MemoryUsed"] = LastMemoryUsed,
                        ["ThroughputSnapshotsPerSecond"] = LastThroughputSnapshotsPerSecond,
                        ["SnapshotsProcessed"] = snapshots.Count,
                        ["IOReadTime"] = LastIOReadTime,
                        ["IOWriteTime"] = LastIOWriteTime,
                        ["EnablePerformanceMetrics"] = _enablePerformanceMetrics
                    };
                    _performanceMonitor.RecordSimulationMetrics("SimulationEngine", metrics, _enablePerformanceMetrics);
                }
            }

            // Post MarketTypeService metrics automatically
            _marketTypeService.PostMetrics(_performanceMonitor);

            // Post StrategySelectionHelper metrics automatically
            StrategySelectionHelper.PostMetrics(_performanceMonitor);

            return activePaths;
        }

        /// <summary>
        /// Assigns a market type classification to the given snapshot using the market type service.
        /// </summary>
        /// <param name="snapshot">The market snapshot to classify.</param>
        /// <remarks>
        /// This method delegates to the MarketTypeService to determine the appropriate market type
        /// (e.g., trending, ranging, volatile) based on the snapshot's characteristics.
        /// The market type influences which trading strategies are activated.
        /// </remarks>
        private void SetMarketType(MarketSnapshot snapshot)
        {
            _marketTypeService.AssignMarketTypeToSnapshot(snapshot);
        }

        /// <summary>
        /// Computes order book deltas between consecutive snapshots when a previous snapshot exists.
        /// </summary>
        /// <param name="prevSnapshot">The previous market snapshot for comparison.</param>
        /// <param name="snapshot">The current market snapshot.</param>
        /// <returns>A tuple containing delta dictionaries for Yes and No sides of the order book.</returns>
        /// <remarks>
        /// Deltas represent changes in order book depth at each price level between snapshots.
        /// Positive deltas indicate increased depth, negative indicate decreased depth.
        /// These deltas are used to simulate realistic order book changes during simulation.
        /// </remarks>
        private (Dictionary<int, int> yesDeltas, Dictionary<int, int> noDeltas) ComputeOrderBookDeltasIfPreviousSnapshotExists(MarketSnapshot prevSnapshot, MarketSnapshot snapshot)
        {
            Dictionary<int, int> yesDeltas = new Dictionary<int, int>();
            Dictionary<int, int> noDeltas = new Dictionary<int, int>();
            if (prevSnapshot != null)
            {
                yesDeltas = CalculateOrderBookDepthChanges(prevSnapshot.GetYesBids(), snapshot.GetYesBids());
                noDeltas = CalculateOrderBookDepthChanges(prevSnapshot.GetNoBids(), snapshot.GetNoBids());
            }
            return (yesDeltas, noDeltas);
        }

        /// <summary>
        /// Applies order book deltas to all active simulation paths and simulates resulting fills.
        /// </summary>
        /// <param name="activePaths">The list of currently active simulation paths.</param>
        /// <param name="yesDeltas">Delta changes for the Yes side of the order book.</param>
        /// <param name="noDeltas">Delta changes for the No side of the order book.</param>
        /// <param name="timestamp">The timestamp of the current snapshot.</param>
        /// <remarks>
        /// This method updates each path's simulated order book with the computed deltas,
        /// then simulates any fills that occur due to the order book changes.
        /// This creates realistic order book dynamics during simulation.
        /// </remarks>
        private void ApplyOrderBookDeltasAndSimulateFills(List<SimulationPath> activePaths, Dictionary<int, int> yesDeltas, Dictionary<int, int> noDeltas, DateTime timestamp)
        {
            if (yesDeltas == null || noDeltas == null) return;

            foreach (var path in activePaths)
            {
                if (path.SimulatedBook != null)
                {
                    path.SimulatedBook.ApplyDeltas(yesDeltas, noDeltas);
                    SimulateOrderFillsFromOrderBookDeltas(path, yesDeltas, noDeltas, timestamp);
                }
            }
        }

        /// <summary>
        /// Removes expired resting orders from the simulation path and updates the order book accordingly.
        /// </summary>
        /// <param name="path">The simulation path containing resting orders to check.</param>
        /// <param name="timestamp">The current timestamp for expiration comparison.</param>
        /// <remarks>
        /// Iterates through resting orders in reverse to safely remove expired orders.
        /// When an order expires, its depth is removed from the simulated order book
        /// to maintain accurate book state.
        /// </remarks>
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

        /// <summary>
        /// Retrieves or initializes the simulated order book for a simulation path.
        /// </summary>
        /// <param name="path">The simulation path that may need an order book.</param>
        /// <param name="snapshot">The market snapshot to initialize the book from if needed.</param>
        /// <returns>The simulated order book for the path, either existing or newly initialized.</returns>
        /// <remarks>
        /// If the path doesn't have a simulated book yet, creates one and initializes it
        /// with the current snapshot's order book data. This ensures each path has
        /// its own independent order book state for accurate simulation.
        /// </remarks>
        private SimulatedOrderbook GetOrCreateSimulatedOrderBook(SimulationPath path, MarketSnapshot snapshot)
        {
            if (path.SimulatedBook == null)
            {
                var book = new SimulatedOrderbook();
                book.InitializeFromSnapshot(snapshot);
                path.SimulatedBook = book;
            }
            return path.SimulatedBook;
        }

        /// <summary>
        /// Creates an effective market snapshot that combines raw snapshot data with simulated state.
        /// </summary>
        /// <param name="snapshot">The base market snapshot from the data source.</param>
        /// <param name="book">The simulated order book with current state.</param>
        /// <param name="path">The simulation path containing position and resting order information.</param>
        /// <returns>A cloned snapshot updated with simulated metrics and position data.</returns>
        /// <remarks>
        /// The effective snapshot represents the market state as seen by the strategy,
        /// including simulated order book changes, current position, and resting orders.
        /// This provides strategies with accurate information for decision making.
        /// </remarks>
        private MarketSnapshot CreateEffectiveSnapshot(MarketSnapshot snapshot, SimulatedOrderbook book, SimulationPath path)
        {
            var effectiveSnapshot = snapshot.Clone();
            effectiveSnapshot.UpdateOrderbookMetricsFromSimulated(book);
            effectiveSnapshot.PositionSize = path.Position;
            effectiveSnapshot.RestingOrders = path.SimulatedRestingOrders;
            return effectiveSnapshot;
        }

        /// <summary>
        /// Parses a market type string into the corresponding MarketType enum value.
        /// </summary>
        /// <param name="marketType">The string representation of the market type.</param>
        /// <returns>The parsed MarketType enum value.</returns>
        /// <remarks>
        /// Delegates to the MarketTypeService for consistent parsing logic.
        /// This ensures market type classification is handled uniformly across the system.
        /// </remarks>
        private MarketType ParseMarketTypeFromString(string marketType)
        {
            return _marketTypeService.ConvertStringToMarketType(marketType);
        }

        /// <summary>
        /// Handles the case where no strategies are available for the current market conditions.
        /// </summary>
        /// <param name="path">The current simulation path.</param>
        /// <param name="effectiveSnapshot">The effective market snapshot with simulated state.</param>
        /// <param name="currentMarketConditions">The current market type classification.</param>
        /// <param name="book">The simulated order book.</param>
        /// <param name="isSingleStrategy">Whether running in single-strategy mode.</param>
        /// <returns>A new or modified simulation path with no-action event logged.</returns>
        /// <remarks>
        /// When no strategies match the current market conditions, this method:
        /// - Maintains the existing path state
        /// - Logs a "None" action event with current market metrics
        /// - Detects and includes any candlestick patterns
        /// - Preserves resting orders and position information
        /// </remarks>
        private SimulationPath HandleScenarioWithNoActiveStrategies(SimulationPath path, MarketSnapshot effectiveSnapshot, MarketType currentMarketConditions, SimulatedOrderbook book, bool isSingleStrategy)
        {
            Dictionary<MarketType, HashSet<Strategy>> newStrategiesByMarketConditions;
            SimulatedOrderbook actionBook;
            List<(string, string, string, int, int, DateTime?)> actionResting;
            List<SimulationEventLog> actionEvents;

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
                actionEvents = new List<SimulationEventLog>(path.Events);
            }

            // No risk management needed

            var (restingYes, restingNo) = SummarizeRestingOrders(actionResting);

            var patterns = DetectPatterns(effectiveSnapshot);

            var eventLog = new SimulationEventLog
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
                    SimulatedRestingOrders = actionResting
                };
            }
        }

        /// <summary>
        /// Groups active strategies by their recommended actions for the current market conditions.
        /// </summary>
        /// <param name="activeStrategies">The set of strategies available for the current market type.</param>
        /// <param name="effectiveSnapshot">The effective market snapshot with simulated state.</param>
        /// <param name="previousEffectiveSnapshot">The previous effective snapshot for trend analysis.</param>
        /// <param name="simulationPosition">The current position in the simulation.</param>
        /// <returns>A dictionary mapping action types to lists of strategies and their decisions.</returns>
        /// <remarks>
        /// This method queries each active strategy for its recommended action given the current
        /// market state and position. Strategies are grouped by action type to handle conflicts
        /// and execute related actions together.
        /// </remarks>
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

        /// <summary>
        /// Processes a group of strategies that recommend the same action type.
        /// </summary>
        /// <param name="path">The current simulation path.</param>
        /// <param name="action">The action type being executed.</param>
        /// <param name="strategiesWithDecisions">The strategies and their decisions for this action.</param>
        /// <param name="effectiveSnapshot">The effective market snapshot with simulated state.</param>
        /// <param name="previousEffectiveSnapshot">The previous effective snapshot for trend analysis.</param>
        /// <param name="currentMarketConditions">The current market type classification.</param>
        /// <param name="book">The simulated order book.</param>
        /// <param name="maxRisk">The maximum risk limit for the simulation.</param>
        /// <param name="isSingleStrategy">Whether running in single-strategy mode.</param>
        /// <returns>A new simulation path with the action executed, or null if the action was skipped.</returns>
        /// <remarks>
        /// This method handles the execution of a specific action type across multiple strategies:
        /// - Manages position flipping if needed (e.g., going from short to long)
        /// - Executes the specific action using HandleSpecificAction
        /// - Updates strategy sets for the current market conditions
        /// - Logs the action event with comprehensive market metrics
        /// - Returns null if the action execution was skipped due to constraints
        /// </remarks>
        private SimulationPath ProcessStrategyActionGroup(SimulationPath path, ActionType action, List<(Strategy strategy, ActionDecision decision)> strategiesWithDecisions,
       MarketSnapshot effectiveSnapshot, MarketSnapshot? previousEffectiveSnapshot,
       MarketType currentMarketConditions, SimulatedOrderbook book, bool isSingleStrategy)
        {
            var decision = strategiesWithDecisions.First().decision;

            SimulatedOrderbook actionBook;
            List<(string action, string side, string type, int count, int price, DateTime? expiration)> actionResting;
            List<SimulationEventLog> actionEvents;
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
                actionEvents = new List<SimulationEventLog>(path.Events);
                newStrategiesByMarketConditions = path.StrategiesByMarketConditions.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HashSet<Strategy>(kvp.Value)
                );
            }

            newStrategiesByMarketConditions[currentMarketConditions] = new HashSet<Strategy>(strategiesWithDecisions.Select(t => t.strategy));

            int newPosition = path.Position;
            double newCash = path.Cash;

            bool needsFlip = (action == ActionType.Long && path.Position < 0) || (action == ActionType.Short && path.Position > 0);
            bool skipAdd = false;
            if (needsFlip)
            {
                skipAdd = ExecuteSpecificTradingAction(ActionType.Exit, new HashSet<Strategy>(strategiesWithDecisions.Select(t => t.strategy)),
                    effectiveSnapshot, previousEffectiveSnapshot, actionBook, actionResting, ref newPosition, ref newCash, path, effectiveSnapshot.Timestamp, path.Position);
                if (skipAdd || newPosition == path.Position) return null;
            }
            else
            {
                skipAdd = ExecuteSpecificTradingAction(action, new HashSet<Strategy>(strategiesWithDecisions.Select(t => t.strategy)),
                    effectiveSnapshot, previousEffectiveSnapshot, actionBook, actionResting, ref newPosition, ref newCash, path, effectiveSnapshot.Timestamp, path.Position);
                if (skipAdd) return null;
            }

            var (restingYes, restingNo) = SummarizeRestingOrders(actionResting);

            var patterns = DetectPatterns(effectiveSnapshot);

            var eventLog = new SimulationEventLog
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
                return newPath;
            }
        }

        /// <summary>
        /// Executes a specific trading action with realistic order book simulation and risk management.
        /// </summary>
        /// <param name="action">The type of action to execute (Long, Short, Exit, PostYes, PostAsk, etc.).</param>
        /// <param name="strategiesForAction">The strategies recommending this action.</param>
        /// <param name="effectiveSnapshot">The effective market snapshot with simulated state.</param>
        /// <param name="previousEffectiveSnapshot">The previous effective snapshot for trend analysis.</param>
        /// <param name="actionBook">The simulated order book for this action execution.</param>
        /// <param name="actionResting">The list of resting orders for this action.</param>
        /// <param name="newPosition">Reference to the position that will be updated by the action.</param>
        /// <param name="newCash">Reference to the cash balance that will be updated by the action.</param>
        /// <param name="newRisk">Reference to the risk level that will be updated by the action.</param>
        /// <param name="path">The original simulation path for context and totals tracking.</param>
        /// <param name="timestamp">The timestamp of the action execution.</param>
        /// <param name="maxRisk">The maximum risk limit for the simulation.</param>
        /// <param name="simulationPosition">The current position at the time of decision.</param>
        /// <returns>True if the action should be skipped, false if it was executed.</returns>
        /// <remarks>
        /// This method handles the detailed execution of various trading actions:
        /// - Market orders (Long, Short, Exit) with realistic order book traversal
        /// - Limit orders (PostYes, PostAsk) for resting orders
        /// - Combo actions (LongPostAsk, ShortPostYes) combining market and limit orders
        /// - Order cancellation
        ///
        /// The method includes risk management (cash and risk limits), realistic pricing,
        /// taker fees, and proper position tracking. It simulates fills by traversing
        /// the order book from best prices outward, respecting available depth and limits.
        /// </remarks>
        private bool ExecuteSpecificTradingAction(
     ActionType action,
     HashSet<Strategy> strategiesForAction,
     MarketSnapshot effectiveSnapshot,
     MarketSnapshot? previousEffectiveSnapshot,
     SimulatedOrderbook actionBook,
     List<(string action, string side, string type, int count, int price, DateTime? expiration)> actionResting,
     ref int newPosition,
     ref double newCash,
     SimulationPath path,
     DateTime timestamp,
     int simulationPosition)
        {
            // Combo actions: execute market take first, then replace resting with 100% of current position
            if (action == ActionType.LongPostAsk || action == ActionType.ShortPostYes)
            {
                var takeAction = (action == ActionType.LongPostAsk) ? ActionType.Long : ActionType.Short;
                ExecuteSpecificTradingAction(takeAction, strategiesForAction, effectiveSnapshot, previousEffectiveSnapshot,
                    actionBook, actionResting, ref newPosition, ref newCash, path, timestamp, simulationPosition);

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

                double remainingCash = newCash;

                for (int p = 99; p >= 1; p--)
                {
                    if (remainingQty <= 0) break;
                    if (bookToReduce[p] == null || bookToReduce[p].Count == 0) continue;
                    int depth = bookToReduce[p].Sum(o => o.count);
                    double levelPrice = LevelPriceFunc(p);
                    int maxFillByCash = isPaying ? (int)Math.Floor(remainingCash / levelPrice) : depth;
                    int maxFill = Math.Min(depth, maxFillByCash);
                    int fill = Math.Min(remainingQty, maxFill);

                    totalCost += fill * levelPrice;
                    actionBook.ReduceDepth(bookToReduce, p, fill);
                    remainingQty -= fill;

                    if (isPaying)
                    {
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
                    SimulateOrderFillsFromMarketTrade(actionResting, longSide ? "no" : "yes", effectiveTradePrice, fill, ref newPosition, ref newCash, timestamp);
                }

                int filled = qty - remainingQty;

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

        /// <summary>
        /// Creates a summary string of resting orders grouped by price level for reporting.
        /// </summary>
        /// <param name="restingOrders">The list of resting orders to summarize.</param>
        /// <returns>A tuple containing summary strings for Yes and No side resting orders.</returns>
        /// <remarks>
        /// Groups limit orders by price level and aggregates quantities.
        /// Returns formatted strings like "50:100, 51:50" representing price:quantity pairs.
        /// Used for logging and reporting resting order state in event logs.
        /// </remarks>
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

        /// <summary>
        /// Simulates fills for resting orders based on order book deltas from market changes.
        /// </summary>
        /// <param name="path">The simulation path containing resting orders and position state.</param>
        /// <param name="yesDeltas">Delta changes for the Yes side order book.</param>
        /// <param name="noDeltas">Delta changes for the No side order book.</param>
        /// <param name="currentTime">The current timestamp for expiration checks.</param>
        /// <remarks>
        /// Processes resting orders to simulate fills when order book depth decreases at their price levels.
        /// Handles order expiration, updates position and cash balances, and removes or updates filled orders.
        /// This creates realistic order execution based on market depth changes.
        /// </remarks>
        private void SimulateOrderFillsFromOrderBookDeltas(SimulationPath path, Dictionary<int, int> yesDeltas, Dictionary<int, int> noDeltas, DateTime currentTime)
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

        /// <summary>
        /// Simulates fills for resting orders when a market trade occurs at a specific price.
        /// </summary>
        /// <param name="resting">The list of resting orders to check for fills.</param>
        /// <param name="tradeSide">The side of the market trade ("yes" or "no").</param>
        /// <param name="tradePrice">The price at which the trade occurred.</param>
        /// <param name="tradeQty">The quantity of the market trade.</param>
        /// <param name="position">Reference to the position to update with fills.</param>
        /// <param name="cash">Reference to the cash balance to update with fills.</param>
        /// <param name="timestamp">The timestamp of the trade (unused in current implementation).</param>
        /// <remarks>
        /// Matches resting limit orders against market trades at the same price level.
        /// Updates position and cash for filled orders, and removes or reduces order quantities.
        /// Processes orders in reverse to safely modify the list during iteration.
        /// </remarks>
        private void SimulateOrderFillsFromMarketTrade(List<(string action, string side, string type, int count, int price, DateTime? expiration)> resting, string tradeSide, int tradePrice, int tradeQty, ref int position, ref double cash, DateTime timestamp)
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

        /// <summary>
        /// Computes the differences (deltas) between two order book depth dictionaries.
        /// </summary>
        /// <param name="prev">The previous order book depth dictionary.</param>
        /// <param name="curr">The current order book depth dictionary.</param>
        /// <returns>A dictionary containing only price levels with non-zero deltas.</returns>
        /// <remarks>
        /// Calculates depth changes at each price level between snapshots.
        /// Only includes price levels where depth actually changed to optimize storage and processing.
        /// Used to simulate realistic order book evolution during simulation.
        /// </remarks>
        private Dictionary<int, int> CalculateOrderBookDepthChanges(Dictionary<int, int> prev, Dictionary<int, int> curr)
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

        /// <summary>
        /// Detects candlestick patterns in the market snapshot using the pattern detection service.
        /// </summary>
        /// <param name="snapshot">The market snapshot to analyze for patterns.</param>
        /// <returns>A list of detected pattern definitions.</returns>
        /// <remarks>
        /// Delegates pattern detection to the specialized PatternDetectionService.
        /// Patterns are used in strategy decision making and logged in event records
        /// for analysis of pattern-based trading signals.
        /// Records comprehensive performance metrics to performance monitor if available.
        /// </remarks>
        private List<BacklashPatterns.PatternDefinitions.PatternDefinition> DetectPatterns(MarketSnapshot snapshot)
        {
            var result = _patternDetectionService.DetectPatterns(snapshot);

            // Record comprehensive performance metrics if monitor is available and metrics were collected
            if (_performanceMonitor != null && result.ExecutionTimeMs.HasValue)
            {
                var metricsDict = new Dictionary<string, object>
                {
                    ["MethodName"] = "PatternDetectionService.DetectPatterns",
                    ["TotalExecutionTimeMs"] = result.ExecutionTimeMs.Value,
                    ["TotalItemsProcessed"] = result.TotalCandlesProcessed ?? 0,
                    ["TotalItemsFound"] = result.TotalPatternsFound ?? 0,
                    ["ItemCheckTimes"] = result.PatternCheckTimes,
                    ["Timestamp"] = DateTime.UtcNow
                };

                _performanceMonitor.RecordSimulationMetrics("PatternDetectionService", metricsDict, _enablePerformanceMetrics);
            }

            return result.Patterns;
        }
    }
}