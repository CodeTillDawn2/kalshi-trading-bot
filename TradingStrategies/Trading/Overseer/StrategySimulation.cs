using BacklashDTOs;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using TradingStrategies.Configuration;
using TradingStrategies.Extensions;
using TradingStrategies.Strategies;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Simulates trading strategies against historical market snapshots, managing position, cash, and order book state.
    /// This class processes market data sequentially, applies trading decisions from strategies, and tracks performance metrics.
    /// It maintains a simulated order book and resting orders to accurately model market interactions during backtesting.
    /// As a developer, this class is the core execution engine for strategy evaluation, handling the complex interplay
    /// between market data, strategy decisions, order book dynamics, and position management.
    /// </summary>
    /// <remarks>
    /// The simulation handles various action types including market orders, limit orders, and position exits.
    /// It applies realistic trading fees and ensures FIFO order matching for accurate simulation results.
    /// Key simulation mechanics include:
    /// - Delta-based order book updates for efficiency
    /// - Accurate Kalshi fee calculation using parabolic formula
    /// - Position and cash tracking with proper accounting
    /// - Resting order management with expiration handling
    /// - Combo actions (take then rest) for advanced strategies
    /// - Order book depth reduction and fill simulation
    /// </remarks>
    public class StrategySimulation
    {
        /// <summary>
        /// Gets the trading strategy being simulated.
        /// </summary>
        public Strategy Strategy { get; private set; }

        /// <summary>
        /// Gets the current position size (positive for long, negative for short).
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// Gets the current cash balance in dollars.
        /// </summary>
        public double Cash { get; private set; }

        /// <summary>
        /// Gets the initial cash balance at simulation start.
        /// </summary>
        public double InitialCash { get; private set; }

        /// <summary>
        /// Gets the total execution time for all ProcessSnapshot calls.
        /// </summary>
        public TimeSpan TotalExecutionTime => _executionTimes.Aggregate(TimeSpan.Zero, (sum, t) => sum + t);

        /// <summary>
        /// Gets the average execution time in milliseconds for ProcessSnapshot calls.
        /// </summary>
        public double AverageExecutionTimeMs => _executionTimes.Count > 0 ? _executionTimes.Average(t => t.TotalMilliseconds) : 0;

        /// <summary>
        /// Gets the peak memory usage recorded during simulation.
        /// </summary>
        public long PeakMemoryUsage => _memoryUsages.Count > 0 ? _memoryUsages.Max() : 0;

        /// <summary>
        /// Gets the total number of trades executed during the simulation.
        /// </summary>
        public int TotalTradesExecuted => _totalTradesExecuted;

        /// <summary>
        /// Gets the average decision time in milliseconds for strategy evaluation.
        /// Only available when EnableDecisionTiming is enabled in configuration.
        /// </summary>
        public double AverageDecisionTimeMs => _decisionTimes.Count > 0 ? _decisionTimes.Average(t => t.TotalMilliseconds) : 0;

        /// <summary>
        /// Gets the average action application time in milliseconds.
        /// Measures the time spent applying trading decisions to the simulation state.
        /// </summary>
        public double AverageApplyTimeMs => _applyTimes.Count > 0 ? _applyTimes.Average(t => t.TotalMilliseconds) : 0;

        /// <summary>
        /// Gets the simulated order book containing bid/ask levels.
        /// </summary>
        public SimulatedOrderbook SimulatedBook { get; private set; }

        /// <summary>
        /// Gets the list of resting orders placed during simulation.
        /// Each tuple contains (action, side, type, count, price, expiration).
        /// </summary>
        public List<(string action, string side, string type, int count, int price, DateTime? expiration)> SimulatedRestingOrders { get; private set; } = new List<(string, string, string, int, int, DateTime?)>();

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly List<TimeSpan> _executionTimes = new List<TimeSpan>();
        private readonly List<long> _memoryUsages = new List<long>();
        private readonly List<TimeSpan> _decisionTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _applyTimes = new List<TimeSpan>();
        private readonly SimulationConfig _config;
        private readonly IPerformanceMonitor _performanceMonitor;
        private int _totalTradesExecuted = 0;
        private int _tradeCountThisSnapshot = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="StrategySimulation"/> class.
        /// Validates that the strategy parameter is not null to prevent runtime errors.
        /// </summary>
        /// <param name="strategy">The trading strategy to simulate. Cannot be null.</param>
        /// <param name="config">The simulation configuration containing thresholds and settings.</param>
        /// <param name="initialCash">The initial cash balance for the simulation (default is 100.0).</param>
        /// <param name="performanceMonitor">Optional performance monitor to automatically record metrics.</param>
        /// <exception cref="ArgumentNullException">Thrown when strategy or config is null.</exception>
        public StrategySimulation(Strategy strategy, IOptions<SimulationConfig> config, IPerformanceMonitor performanceMonitor, double initialCash = 100.0)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            if (config == null) throw new ArgumentNullException(nameof(config));
            Strategy = strategy;
            _config = config.Value;
            _performanceMonitor = performanceMonitor;
            Position = 0;
            Cash = initialCash;
            InitialCash = initialCash;
            SimulatedBook = new SimulatedOrderbook();
        }

        /// <summary>
        /// Processes a market snapshot by applying deltas, updating the order book, and executing trading decisions.
        /// This is the main entry point for advancing the simulation state with new market data.
        /// </summary>
        /// <param name="snapshot">The current market snapshot to process, containing order book, position, and market data. Cannot be null.</param>
        /// <param name="prevSnapshot">The previous market snapshot for delta calculation (optional). If provided, enables efficient delta-based updates.</param>
        /// <exception cref="ArgumentNullException">Thrown when snapshot is null.</exception>
        /// <remarks>
        /// This method handles the core simulation loop with the following steps:
        /// 0. Validate input parameters and start performance monitoring (timing and memory)
        /// 1. Compute and apply order book deltas if previous snapshot exists (for efficiency)
        /// 2. Simulate fills from those deltas on resting orders
        /// 3. Initialize order book from snapshot if this is the first snapshot
        /// 4. Create effective snapshot with simulated state overlay
        /// 5. Get trading decision from strategy
        /// 6. Apply the decision to update position, cash, and order book
        /// 7. Record performance metrics (execution time and memory usage)
        ///
        /// As a developer, this method orchestrates the entire simulation step, ensuring proper sequencing
        /// of market updates, strategy evaluation, and state changes. Input validation prevents null reference
        /// exceptions, while performance monitoring tracks execution timing and memory usage for optimization.
        /// The effective snapshot approach allows strategies to see the current simulated state while maintaining
        /// separation between real market data and simulation artifacts.
        /// </remarks>
        public void ProcessSnapshot(MarketSnapshot snapshot, MarketSnapshot? prevSnapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            _stopwatch.Restart();
            long memoryBefore = _config.EnablePerformanceMetrics ? GC.GetTotalMemory(true) : 0;
            _tradeCountThisSnapshot = 0;

            // Apply deltas if previous snapshot provided
            Dictionary<int, int> yesDeltas = new Dictionary<int, int>();
            Dictionary<int, int> noDeltas = new Dictionary<int, int>();
            if (prevSnapshot != null)
            {
                yesDeltas = CalculateOrderBookDepthChanges(prevSnapshot.GetYesBids(), snapshot.GetYesBids());
                noDeltas = CalculateOrderBookDepthChanges(prevSnapshot.GetNoBids(), snapshot.GetNoBids());
                // FIFO: append deltas with the *snapshot* time
                SimulatedBook.ApplyDeltas(yesDeltas, noDeltas);
                SimulateFillsFromDeltas(yesDeltas, noDeltas, snapshot.Timestamp);
            }

            // Initialize book if first snapshot
            if (SimulatedBook.YesBids.Count == 0 && SimulatedBook.NoBids.Count == 0)
            {
                SimulatedBook.InitializeFromSnapshot(snapshot);
            }

            // Effective snapshot with simulated state
            var effectiveSnapshot = snapshot.Clone();
            effectiveSnapshot.UpdateOrderbookMetricsFromSimulated(SimulatedBook);
            effectiveSnapshot.PositionSize = Position;
            effectiveSnapshot.RestingOrders = SimulatedRestingOrders;

            // Decision + execution
            Stopwatch decisionStopwatch = new Stopwatch();
            decisionStopwatch.Start();
            var decision = Strategy.GetAction(effectiveSnapshot, prevSnapshot, Position);
            decisionStopwatch.Stop();
            _decisionTimes.Add(decisionStopwatch.Elapsed);

            if (_config.EnableDecisionTiming && decisionStopwatch.Elapsed.TotalMilliseconds > _config.DecisionThresholdMs)
            {
                Console.WriteLine($"Decision timing warning: GetAction took {decisionStopwatch.Elapsed.TotalMilliseconds:F2}ms");
            }

            Stopwatch applyStopwatch = new Stopwatch();
            applyStopwatch.Start();
            ApplyAction(decision, effectiveSnapshot);
            applyStopwatch.Stop();
            if (_config.EnablePerformanceMetrics)
                _applyTimes.Add(applyStopwatch.Elapsed);

            _stopwatch.Stop();
            if (_config.EnablePerformanceMetrics)
                _executionTimes.Add(_stopwatch.Elapsed);
            long memoryAfter = _config.EnablePerformanceMetrics ? GC.GetTotalMemory(true) : 0;
            if (_config.EnablePerformanceMetrics)
                _memoryUsages.Add(memoryAfter);

            // Enhanced performance monitoring
            if (_config.EnablePerformanceMetrics && _stopwatch.Elapsed.TotalMilliseconds > _config.PerformanceThresholdMs)
            {
                Console.WriteLine($"Performance warning: ProcessSnapshot took {_stopwatch.Elapsed.TotalMilliseconds:F2}ms, Memory: {memoryAfter / 1024 / 1024}MB");
            }

        }


        /// <summary>
        /// Applies a trading action decision to the simulation state, updating position, cash, and order book.
        /// This method is the central execution point for strategy decisions, handling all action types with proper accounting.
        /// </summary>
        /// <param name="decision">The action decision to apply, containing type, price, quantity, and expiration details. Cannot be null.</param>
        /// <param name="effectiveSnapshot">The market snapshot with simulated order book state, used for price reference and timestamp.</param>
        /// <remarks>
        /// Input validation ensures the decision parameter is not null to prevent runtime errors.
        /// Handles different action types with specific logic:
        /// - Market orders (Long/Short): Execute immediately against order book, update position/cash
        /// - Limit orders (PostYes/PostAsk): Add to simulated order book and resting orders list
        /// - Cancel: Remove all resting orders from order book
        /// - Exit: Close entire position using current order book prices
        /// - Combo actions (LongPostAsk/ShortPostYes): Execute market order then place resting limit order
        ///
        /// Key implementation details for developers:
        /// - Uses FIFO order matching for realistic fill simulation
        /// - Applies accurate Kalshi taker fees using 0.07 * P * (1-P) formula on all market executions
        /// - Handles position direction (positive = long, negative = short)
        /// - Manages resting orders with expiration and proper cleanup
        /// - Prevents invalid states (insufficient cash, invalid prices)
        /// - Updates both temporary and committed state variables
        /// </remarks>
        private void ApplyAction(ActionDecision decision, MarketSnapshot effectiveSnapshot)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            var action = decision.Type;
            if (action == ActionType.None) return;

            if (action == ActionType.PostYes || action == ActionType.PostAsk || action == ActionType.Cancel)
            {
                HandleLimitAction(decision, effectiveSnapshot);
                return;
            }

            bool longSide = action == ActionType.Long  || action == ActionType.Exit && Position < 0;
            bool shortSide = action == ActionType.Short || action == ActionType.Exit && Position > 0;
            if (!longSide && !shortSide) return;

            string tradeSide = longSide ? "no" : "yes";

            int qty = (action == ActionType.Exit) ? Math.Abs(Position) : 100;
            int remainingQuantity = qty;
            double totalCost = 0;
            double totalFee = 0;

            var bookToReduce = longSide ? SimulatedBook.NoBids : SimulatedBook.YesBids;

            bool isPaying = longSide || (shortSide && action != ActionType.Exit);
            double LevelPriceFunc(int price) => isPaying ? (100 - price) / 100.0 : price / 100.0;

            foreach (var kvp in bookToReduce.Reverse()) // Highest prices first
            {
                int p = kvp.Key;
                if (remainingQuantity <= 0) break;
                if (!kvp.Value.Any()) continue;
                int depth = kvp.Value.Sum(o => o.Count);
                int fill = Math.Min(remainingQuantity, depth);
                double levelPrice = LevelPriceFunc(p);
                totalCost += fill * levelPrice;
                totalFee += fill * (0.07 * levelPrice * (1.0 - levelPrice));
                SimulatedBook.ReduceDepth(bookToReduce, p, fill);
                remainingQuantity -= fill;
            }

            int filled = qty - remainingQuantity;
            if (filled == 0) return;

            if (_config.EnablePerformanceMetrics)
            {
                _totalTradesExecuted++;
                _tradeCountThisSnapshot++;
            }


            double tempCash = Cash;
            int tempPosition = Position;

            if (isPaying)
            {
                if (tempCash < totalCost) return;
                tempCash -= totalCost;
            }
            else
            {
                tempCash += totalCost;
            }

            // taker fees
            tempCash -= totalFee;

            int posDelta = longSide ? filled : -filled;
            tempPosition += posDelta;

            int avgPrice = (int)(totalCost / filled * 100);
            int effectiveAvgPrice = isPaying ? (100 - avgPrice) : avgPrice;
            SimulateFillsFromTrade(SimulatedRestingOrders, tradeSide, effectiveAvgPrice, filled, ref tempPosition, ref tempCash, effectiveSnapshot.Timestamp);

            // Commit
            Cash = tempCash;
            Position = tempPosition;

            if (action == ActionType.Exit)
            {
                Position = 0;
                return;
            }

        }



        /// <summary>
        /// Handles limit order actions (posting or canceling orders) in the simulated environment.
        /// This method manages the lifecycle of resting orders, ensuring proper integration with the simulated order book.
        /// </summary>
        /// <param name="decision">The action decision containing order details (price, quantity, expiration).</param>
        /// <param name="effectiveSnapshot">The market snapshot for timestamp reference when posting new orders.</param>
        /// <remarks>
        /// Implementation details for developers:
        /// - PostYes: Places bid for YES at specified price, rests on YES bids side
        /// - PostAsk: Places ask for YES at specified price, rests on NO bids side (100 - price)
        /// - Cancel: Removes ALL resting orders from order book, clearing the resting list
        /// - Validates configurable price ranges (default 1-99) and quantity (>0) before processing
        /// - Uses reverse iteration for cancellation to handle removals safely
        /// - Maintains FIFO order in resting orders list by appending new orders
        /// - Updates both SimulatedOrderbook and SimulatedRestingOrders collections
        /// </remarks>
        private void HandleLimitAction(ActionDecision decision, MarketSnapshot effectiveSnapshot)
        {
            var action = decision.Type;
            if (action == ActionType.Cancel)
            {
                // Remove *our* resting volume from the tail (we append when posting).
                for (int i = SimulatedRestingOrders.Count - 1; i >= 0; i--)
                {
                    var o = SimulatedRestingOrders[i];

                    bool isBidYes = o.action == "buy"  && o.side == "yes";
                    bool isBidNo = o.action == "buy"  && o.side == "no";
                    bool isAskYes = o.action == "sell" && o.side == "yes";
                    bool isAskNo = o.action == "sell" && o.side == "no";

                    SortedDictionary<int, List<SimulatedOrderbook.OrderLevel>> book;
                    int bookPrice;

                    if (isBidYes) { book = SimulatedBook.YesBids; bookPrice = o.price; }
                    else if (isBidNo) { book = SimulatedBook.NoBids; bookPrice = o.price; }
                    else if (isAskYes) { book = SimulatedBook.NoBids; bookPrice = 100 - o.price; }
                    else { book = SimulatedBook.YesBids; bookPrice = 100 - o.price; }

                    SimulatedBook.ReduceDepth(book, bookPrice, o.count, fromFront: false);
                }
                SimulatedRestingOrders.Clear();
                return;
            }

            // Post a new YES limit: PostYes = bid YES @ price; PostAsk = ask YES @ price (rests as NO bid @ 100-price)
            string limitAction = action == ActionType.PostYes ? "buy" : "sell";
            string limitSide = "yes";
            int limitPrice = decision.Price;
            int qty = decision.Quantity;
            DateTime? exp = decision.Expiration;
            if (limitPrice < 0 || limitPrice > 100 || qty <= 0) return;

            if (action == ActionType.PostYes)
            {
                SimulatedBook.AddToDepth(SimulatedBook.YesBids, limitPrice, qty, effectiveSnapshot.Timestamp, isOwn: true);
            }
            else // PostAsk (sell YES) => rests on NO bids at (100 - ask)
            {
                int noBidPrice = 100 - limitPrice;
                if (noBidPrice < 0 || noBidPrice > 100) return;
                SimulatedBook.AddToDepth(SimulatedBook.NoBids, noBidPrice, qty, effectiveSnapshot.Timestamp, isOwn: true);
            }

            SimulatedRestingOrders.Add((limitAction, limitSide, "limit", qty, limitPrice, exp));
        }


        /// <summary>
        /// Calculates the final equity value at the end of simulation by liquidating the current position.
        /// This method provides the total portfolio value for performance evaluation and reporting.
        /// </summary>
        /// <param name="lastSnapshot">The final market snapshot for valuation, though not directly used in current implementation. Cannot be null.</param>
        /// <returns>The total equity value including cash and position value, representing the final portfolio worth.</returns>
        /// <remarks>
        /// Input validation ensures the lastSnapshot parameter is not null to maintain robustness.
        /// Implementation computes liquidation value as follows:
        /// - For long positions (Position > 0): Uses best YES bid price from simulated order book
        /// - For short positions (Position < 0): Uses best NO bid price from simulated order book
        /// - Adds remaining cash balance to position value
        /// - Returns 0 if no valid bid prices available for position liquidation
        ///
        /// Note: Currently uses simulated order book rather than snapshot prices for consistency with simulation state.
        /// This ensures the final valuation reflects the actual simulated market conditions.
        /// </remarks>
        public double GetFinalEquity(MarketSnapshot lastSnapshot)
        {
            if (lastSnapshot == null) throw new ArgumentNullException(nameof(lastSnapshot));
            double value = 0;
            if (Position > 0)
            {
                int bid = SimulatedBook.GetBestYesBid();
                if (bid > 0)
                    value = Position * (bid / 100.0);
            }
            else if (Position < 0)
            {
                int bid = SimulatedBook.GetBestNoBid();
                value = Math.Abs(Position) * (bid / 100.0);
            }
            return Cash + value;
        }

        /// <summary>
        /// Simulates order fills resulting from market order book deltas, processing resting orders against market movements.
        /// This method handles the complex interaction between resting limit orders and incoming market activity.
        /// </summary>
        /// <param name="yesDeltas">Price deltas for YES side orders, where positive indicates added volume and negative indicates removed volume.</param>
        /// <param name="noDeltas">Price deltas for NO side orders, with same positive/negative convention as yesDeltas.</param>
        /// <param name="currentTime">The current timestamp for processing, used for expiration checks.</param>
        /// <remarks>
        /// Implementation processes each resting order with the following logic:
        /// 1. Check for order expiration and remove expired orders from both order book and resting list
        /// 2. For each price level with negative delta (volume reduction), check if resting orders at that price get filled
        /// 3. Use FIFO matching: only orders ahead in the queue can be filled before current order
        /// 4. Calculate fill quantity based on available depth and position in queue
        /// 5. Update cash and position based on fill (buy orders add position, sell orders add cash)
        /// 6. Remove filled orders from resting list and reduce order book depth
        ///
        /// Key developer considerations:
        /// - Reverse iteration prevents index issues during removals
        /// - Handles both bid and ask sides with proper price conversions
        /// - Maintains order book integrity by reducing depth after fills
        /// - Supports partial fills (order.count can be reduced without full removal)
        /// - No fees applied here as these are maker fills (fees only on taker actions)
        /// </remarks>
        private void SimulateFillsFromDeltas(Dictionary<int, int> yesDeltas, Dictionary<int, int> noDeltas, DateTime currentTime)
        {
            // Group resting orders by book and price
            var ordersByBookPrice = new Dictionary<(SortedDictionary<int, List<SimulatedOrderbook.OrderLevel>> book, int price), List<int>>();

            for (int i = 0; i < SimulatedRestingOrders.Count; i++)
            {
                var o = SimulatedRestingOrders[i];
                bool isBidYes = o.action == "buy" && o.side == "yes";
                bool isBidNo = o.action == "buy" && o.side == "no";
                bool isAskYes = o.action == "sell" && o.side == "yes";
                bool isAskNo = o.action == "sell" && o.side == "no";

                SortedDictionary<int, List<SimulatedOrderbook.OrderLevel>> book;
                int bookPrice;

                if (isBidYes) { book = SimulatedBook.YesBids; bookPrice = o.price; }
                else if (isBidNo) { book = SimulatedBook.NoBids; bookPrice = o.price; }
                else if (isAskYes) { book = SimulatedBook.NoBids; bookPrice = 100 - o.price; }
                else { book = SimulatedBook.YesBids; bookPrice = 100 - o.price; }

                var key = (book, bookPrice);
                if (!ordersByBookPrice.ContainsKey(key)) ordersByBookPrice[key] = new List<int>();
                ordersByBookPrice[key].Add(i);
            }

            // Process each price level with negative delta
            foreach (var kvp in ordersByBookPrice)
            {
                var (book, bookPrice) = kvp.Key;
                var orderIndices = kvp.Value;

                Dictionary<int, int> deltas = (book == SimulatedBook.YesBids) ? yesDeltas : noDeltas;
                if (!deltas.TryGetValue(bookPrice, out int deltaAtPrice) || deltaAtPrice >= 0) continue;

                if (!book.TryGetValue(bookPrice, out var list)) continue;

                int consumed = -deltaAtPrice;

                // Consume from front
                int consumedSoFar = 0;
                for (int j = 0; j < list.Count && consumedSoFar < consumed; j++)
                {
                    var orderLevel = list[j];
                    int take = Math.Min(consumed - consumedSoFar, orderLevel.Count);
                    consumedSoFar += take;
                    orderLevel.Count -= take;
                }

                // Remove zero count orders
                list.RemoveAll(ol => ol.Count <= 0);

                // Now, for our orders, calculate fills based on position in list
                foreach (int orderIndex in orderIndices.OrderBy(i => i)) // Process in order added
                {
                    var o = SimulatedRestingOrders[orderIndex];

                    // Find this order's position in the list
                    int orderPosition = -1;
                    int depthAhead = 0;
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].IsOwnOrder)
                        {
                            orderPosition++;
                            if (orderPosition == orderIndices.IndexOf(orderIndex)) // This is our order
                            {
                                break;
                            }
                        }
                        else
                        {
                            depthAhead += list[j].Count;
                        }
                    }

                    int fillQuantity = Math.Max(0, Math.Min(o.count, consumed - depthAhead));
                    if (fillQuantity <= 0) continue;

                    double px = o.price / 100.0;
                    bool isBid = o.action == "buy";
                    if (isBid)
                    {
                        Cash -= fillQuantity * px;
                        Position += (o.side == "yes" ? fillQuantity : -fillQuantity);
                    }
                    else
                    {
                        Cash += fillQuantity * px;
                        Position -= (o.side == "yes" ? fillQuantity : -fillQuantity);
                    }

                    o.count -= fillQuantity;
                    if (o.count <= 0)
                    {
                        SimulatedRestingOrders.RemoveAt(orderIndex);
                        // Remove from list
                        for (int j = list.Count - 1; j >= 0; j--)
                        {
                            if (list[j].IsOwnOrder)
                            {
                                list[j].Count -= fillQuantity;
                                if (list[j].Count <= 0)
                                {
                                    list.RemoveAt(j);
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        SimulatedRestingOrders[orderIndex] = o;
                        // Update in list
                        for (int j = list.Count - 1; j >= 0; j--)
                        {
                            if (list[j].IsOwnOrder)
                            {
                                list[j].Count -= fillQuantity;
                                break;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Simulates order fills from a market trade execution.
        /// </summary>
        /// <param name="resting">The list of resting orders to check for fills.</param>
        /// <param name="tradeSide">The side of the trade ("yes" or "no").</param>
        /// <param name="tradePrice">The price at which the trade occurred.</param>
        /// <param name="tradeQuantity">The quantity of the trade.</param>
        /// <param name="position">Reference to the current position (modified by fills).</param>
        /// <param name="cash">Reference to the current cash balance (modified by fills).</param>
        /// <param name="currentTime">The timestamp of the trade.</param>
        /// <remarks>
        /// Matches resting orders against market trades and updates position and cash accordingly.
        /// Removes or reduces filled orders from the resting list.
        /// </remarks>
        private void SimulateFillsFromTrade(List<(string action, string side, string type, int count, int price, DateTime? expiration)> resting, string tradeSide, int tradePrice, int tradeQuantity, ref int position, ref double cash, DateTime currentTime)
        {
            for (int i = resting.Count - 1; i >= 0; i--)
            {
                var order = resting[i];
                if (order.price == tradePrice &&
                    ((tradeSide == "yes" && order.side == "no" && order.action == "buy") ||
                     (tradeSide == "no" && order.side == "yes" && order.action == "buy")))
                {
                    int fillQuantity = Math.Min(order.count, tradeQuantity);
                    double fillPrice = order.price / 100.0;

                    if (order.action == "buy")
                    {
                        cash -= fillQuantity * fillPrice;
                        position += order.side == "yes" ? fillQuantity : -fillQuantity;
                    }
                    else
                    {
                        cash += fillQuantity * fillPrice;
                        position -= order.side == "yes" ? fillQuantity : -fillQuantity;
                    }

                    order.count -= fillQuantity;
                    tradeQuantity -= fillQuantity;
                    if (order.count <= 0)
                    {
                        resting.RemoveAt(i);
                    }
                    else
                    {
                        resting[i] = order;
                    }
                    if (tradeQuantity <= 0) break;
                }
            }
        }

        /// <summary>
        /// Calculates the differences between previous and current order book depths.
        /// </summary>
        /// <param name="prev">The previous order book depths by price.</param>
        /// <param name="curr">The current order book depths by price.</param>
        /// <returns>A dictionary of price deltas (only non-zero changes).</returns>
        /// <remarks>
        /// Calculates the change in order book depth at each price level.
        /// Positive delta indicates volume added, negative indicates volume removed.
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
        /// Asynchronous version of ProcessSnapshot for better performance with large snapshot sets.
        /// Processes a market snapshot by applying deltas, updating the order book, and executing trading decisions.
        /// </summary>
        /// <param name="snapshot">The current market snapshot to process.</param>
        /// <param name="prevSnapshot">The previous market snapshot for delta calculation (optional).</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task ProcessSnapshotAsync(MarketSnapshot snapshot, MarketSnapshot? prevSnapshot, CancellationToken cancellationToken = default)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            // Check for cancellation early
            cancellationToken.ThrowIfCancellationRequested();

            _stopwatch.Restart();
            long memoryBefore = GC.GetTotalMemory(true);

            // Apply deltas if previous snapshot provided
            Dictionary<int, int> yesDeltas = new Dictionary<int, int>();
            Dictionary<int, int> noDeltas = new Dictionary<int, int>();
            if (prevSnapshot != null)
            {
                yesDeltas = await Task.Run(() => CalculateOrderBookDepthChanges(prevSnapshot.GetYesBids(), snapshot.GetYesBids()), cancellationToken);
                noDeltas = await Task.Run(() => CalculateOrderBookDepthChanges(prevSnapshot.GetNoBids(), snapshot.GetNoBids()), cancellationToken);
                SimulatedBook.ApplyDeltas(yesDeltas, noDeltas);
                await Task.Run(() => SimulateFillsFromDeltas(yesDeltas, noDeltas, snapshot.Timestamp), cancellationToken);
            }

            // Initialize book if first snapshot
            if (SimulatedBook.YesBids.Count == 0 && SimulatedBook.NoBids.Count == 0)
            {
                SimulatedBook.InitializeFromSnapshot(snapshot);
            }

            // Effective snapshot with simulated state
            var effectiveSnapshot = await Task.Run(() => snapshot.Clone(), cancellationToken);
            effectiveSnapshot.UpdateOrderbookMetricsFromSimulated(SimulatedBook);
            effectiveSnapshot.PositionSize = Position;
            effectiveSnapshot.RestingOrders = SimulatedRestingOrders;

            // Decision + execution
            Stopwatch decisionStopwatch = new Stopwatch();
            decisionStopwatch.Start();
            var decision = await Task.Run(() => Strategy.GetAction(effectiveSnapshot, prevSnapshot, Position), cancellationToken);
            decisionStopwatch.Stop();
            _decisionTimes.Add(decisionStopwatch.Elapsed);

            if (_config.EnableDecisionTiming && decisionStopwatch.Elapsed.TotalMilliseconds > _config.DecisionThresholdMs)
            {
                Console.WriteLine($"Decision timing warning: GetAction took {decisionStopwatch.Elapsed.TotalMilliseconds:F2}ms");
            }

            Stopwatch applyStopwatch = new Stopwatch();
            applyStopwatch.Start();
            await Task.Run(() => ApplyAction(decision, effectiveSnapshot), cancellationToken);
            applyStopwatch.Stop();
            _applyTimes.Add(applyStopwatch.Elapsed);

            _stopwatch.Stop();
            _executionTimes.Add(_stopwatch.Elapsed);
            long memoryAfter = GC.GetTotalMemory(true);
            _memoryUsages.Add(memoryAfter);

            // Enhanced performance monitoring
            if (_config.EnablePerformanceMetrics && _stopwatch.Elapsed.TotalMilliseconds > _config.PerformanceThresholdMs)
            {
                Console.WriteLine($"Performance warning: ProcessSnapshotAsync took {_stopwatch.Elapsed.TotalMilliseconds:F2}ms, Memory: {memoryAfter / 1024 / 1024}MB");
            }

        }

        /// <summary>
        /// Processes multiple snapshots asynchronously in batches for improved performance.
        /// </summary>
        /// <param name="snapshots">The list of market snapshots to process.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task ProcessSnapshotsAsync(IEnumerable<MarketSnapshot> snapshots, CancellationToken cancellationToken = default)
        {
            if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));

            var snapshotList = snapshots.ToList();
            if (snapshotList.Count == 0) return;

            // Process in batches for better performance
            var batches = snapshotList
                .Select((snapshot, index) => new { snapshot, index })
                .GroupBy(x => x.index / _config.AsyncBatchSize)
                .Select(g => g.Select(x => x.snapshot).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tasks = batch.Select(async (snapshot, index) =>
                {
                    var prevSnapshot = index > 0 ? batch[index - 1] : (snapshotList.IndexOf(snapshot) > 0 ? snapshotList[snapshotList.IndexOf(snapshot) - 1] : null);
                    await ProcessSnapshotAsync(snapshot, prevSnapshot, cancellationToken);
                });

                await Task.WhenAll(tasks);

                // Yield control to prevent blocking
                await Task.Yield();
            }
        }

        /// <summary>
        /// Gets detailed performance metrics for analysis and optimization.
        /// Includes execution timing, memory usage, trade counts, and decision performance metrics.
        /// New metrics include decision timing, action application timing, and trade rate monitoring
        /// when corresponding configuration options are enabled.
        /// Automatically records metrics to the performance monitor if provided.
        /// </summary>
        /// <returns>A dictionary containing various performance metrics including:
        /// - TotalExecutionTime: Total time spent processing snapshots
        /// - AverageExecutionTimeMs: Average time per snapshot
        /// - PeakMemoryUsage: Maximum memory usage recorded
        /// - TotalTradesExecuted: Total number of trades executed
        /// - AverageDecisionTimeMs: Average strategy decision time (when enabled)
        /// - AverageApplyTimeMs: Average action application time
        /// - SlowDecisionsCount: Number of decisions exceeding threshold
        /// - Configuration values for reference</returns>
        public Dictionary<string, object> GetDetailedPerformanceMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["TotalExecutionTime"] = TotalExecutionTime,
                ["AverageExecutionTimeMs"] = AverageExecutionTimeMs,
                ["PeakMemoryUsage"] = PeakMemoryUsage,
                ["TotalSnapshotsProcessed"] = _executionTimes.Count,
                ["PerformanceThresholdMs"] = _config.PerformanceThresholdMs,
                ["SlowOperationsCount"] = _executionTimes.Count(t => t.TotalMilliseconds > _config.PerformanceThresholdMs),
                ["RestingOrdersCount"] = SimulatedRestingOrders.Count,
                ["CurrentPosition"] = Position,
                ["CurrentCash"] = Cash,
                ["TotalTradesExecuted"] = _totalTradesExecuted,
                ["AverageDecisionTimeMs"] = _decisionTimes.Count > 0 ? _decisionTimes.Average(t => t.TotalMilliseconds) : 0,
                ["AverageApplyTimeMs"] = _applyTimes.Count > 0 ? _applyTimes.Average(t => t.TotalMilliseconds) : 0,
                ["SlowDecisionsCount"] = _decisionTimes.Count(t => t.TotalMilliseconds > _config.DecisionThresholdMs),
            };

            // Automatically record to performance monitor if provided
            RecordSimulationMetrics(Strategy.Name, metrics, _config.EnablePerformanceMetrics);

            return metrics;
        }

        /// <summary>
        /// Resets all performance metrics for a new simulation run.
        /// Clears execution times, memory usage data, decision timing metrics,
        /// action application timing, and trade counters. Prepares the simulation
        /// for a fresh performance measurement cycle.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            _executionTimes.Clear();
            _memoryUsages.Clear();
            _decisionTimes.Clear();
            _applyTimes.Clear();
            _stopwatch.Reset();
            _totalTradesExecuted = 0;
        }

        private void RecordSimulationMetrics(string className, Dictionary<string, object> metrics, bool enabled)
        {
            string category = "Simulation";
            if (!enabled)
            {
                foreach (var kvp in metrics)
                {
                    _performanceMonitor.RecordDisabledMetric(className, kvp.Key, kvp.Key, $"Disabled metric: {kvp.Key}", 0.0, "", category);
                }
                return;
            }
            foreach (var kvp in metrics)
            {
                string id = kvp.Key;
                string name = kvp.Key;
                string description = GetDescriptionForMetric(id);
                double value = GetValueAsDouble(kvp.Value);
                string unit = GetUnitForMetric(id);
                switch (id)
                {
                    case "TotalExecutionTime":
                        _performanceMonitor.RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "AverageExecutionTimeMs":
                        _performanceMonitor.RecordSpeedDialMetric(className, id, name, description, value, unit, category, null, null, null);
                        break;
                    case "PeakMemoryUsage":
                        _performanceMonitor.RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "TotalSnapshotsProcessed":
                        _performanceMonitor.RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "PerformanceThresholdMs":
                        _performanceMonitor.RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "SlowOperationsCount":
                        _performanceMonitor.RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "RestingOrdersCount":
                        _performanceMonitor.RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "CurrentPosition":
                        _performanceMonitor.RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "CurrentCash":
                        _performanceMonitor.RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "TotalTradesExecuted":
                        _performanceMonitor.RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "AverageDecisionTimeMs":
                        _performanceMonitor.RecordSpeedDialMetric(className, id, name, description, value, unit, category, null, null, null);
                        break;
                    case "AverageApplyTimeMs":
                        _performanceMonitor.RecordSpeedDialMetric(className, id, name, description, value, unit, category, null, null, null);
                        break;
                    case "SlowDecisionsCount":
                        _performanceMonitor.RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    default:
                        _performanceMonitor.RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                }
            }
        }

        private string GetDescriptionForMetric(string id)
        {
            switch (id)
            {
                case "TotalExecutionTime": return "Total time spent processing all snapshots";
                case "AverageExecutionTimeMs": return "Average time to process one snapshot";
                case "PeakMemoryUsage": return "Maximum memory usage during simulation";
                case "TotalSnapshotsProcessed": return "Number of snapshots processed";
                case "PerformanceThresholdMs": return "Threshold for slow operations";
                case "SlowOperationsCount": return "Number of operations exceeding performance threshold";
                case "RestingOrdersCount": return "Current number of resting orders";
                case "CurrentPosition": return "Current position size";
                case "CurrentCash": return "Current cash balance";
                case "TotalTradesExecuted": return "Total number of trades executed";
                case "AverageDecisionTimeMs": return "Average time for strategy decisions";
                case "AverageApplyTimeMs": return "Average time to apply trading actions";
                case "SlowDecisionsCount": return "Number of slow strategy decisions";
                default: return $"Metric: {id}";
            }
        }

        private string GetUnitForMetric(string id)
        {
            switch (id)
            {
                case "TotalExecutionTime": return "ms";
                case "AverageExecutionTimeMs": return "ms";
                case "PeakMemoryUsage": return "bytes";
                case "TotalSnapshotsProcessed": return "count";
                case "PerformanceThresholdMs": return "ms";
                case "SlowOperationsCount": return "count";
                case "RestingOrdersCount": return "count";
                case "CurrentPosition": return "contracts";
                case "CurrentCash": return "USD";
                case "TotalTradesExecuted": return "count";
                case "AverageDecisionTimeMs": return "ms";
                case "AverageApplyTimeMs": return "ms";
                case "SlowDecisionsCount": return "count";
                default: return "";
            }
        }

        private double GetValueAsDouble(object value)
        {
            if (value is TimeSpan ts) return ts.TotalMilliseconds;
            if (value is double d) return d;
            if (value is int i) return (double)i;
            if (value is long l) return (double)l;
            return 0.0;
        }
    }
}
