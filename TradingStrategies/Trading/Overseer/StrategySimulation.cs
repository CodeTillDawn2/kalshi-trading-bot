using BacklashDTOs;
using TradingStrategies.Extensions;
using TradingStrategies.Strategies;
using static BacklashInterfaces.Enums.StrategyEnums;
using System.Diagnostics;
using System.Linq;

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
    /// - Realistic fee calculation (0.07% taker fees)
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

        /// <summary>
        /// Initializes a new instance of the <see cref="StrategySimulation"/> class.
        /// Validates that the strategy parameter is not null to prevent runtime errors.
        /// </summary>
        /// <param name="strategy">The trading strategy to simulate. Cannot be null.</param>
        /// <param name="initialCash">The initial cash balance for the simulation (default is 100.0).</param>
        /// <exception cref="ArgumentNullException">Thrown when strategy is null.</exception>
        public StrategySimulation(Strategy strategy, double initialCash = 100.0)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            Strategy = strategy;
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
            long memoryBefore = GC.GetTotalMemory(true);

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
            if (SimulatedBook.YesBids.All(b => b == null || b.Count == 0) && SimulatedBook.NoBids.All(b => b == null || b.Count == 0))
            {
                SimulatedBook.InitializeFromSnapshot(snapshot);
            }

            // Effective snapshot with simulated state
            var effectiveSnapshot = snapshot.Clone();
            effectiveSnapshot.UpdateOrderbookMetricsFromSimulated(SimulatedBook);
            effectiveSnapshot.PositionSize = Position;
            effectiveSnapshot.RestingOrders = SimulatedRestingOrders;

            // Decision + execution
            var decision = Strategy.GetAction(effectiveSnapshot, prevSnapshot, Position);
            ApplyAction(decision, effectiveSnapshot);

            _stopwatch.Stop();
            _executionTimes.Add(_stopwatch.Elapsed);
            long memoryAfter = GC.GetTotalMemory(true);
            _memoryUsages.Add(memoryAfter);
        }


        /// <summary>
        /// Applies a trading action decision to the simulation state, updating position, cash, and order book.
        /// This method is the central execution point for strategy decisions, handling all action types with proper accounting.
        /// </summary>
        /// <param name="decision">The action decision to apply, containing type, price, quantity, and expiration details.</param>
        /// <param name="effectiveSnapshot">The market snapshot with simulated order book state, used for price reference and timestamp.</param>
        /// <remarks>
        /// Handles different action types with specific logic:
        /// - Market orders (Long/Short): Execute immediately against order book, update position/cash
        /// - Limit orders (PostYes/PostAsk): Add to simulated order book and resting orders list
        /// - Cancel: Remove all resting orders from order book
        /// - Exit: Close entire position using current order book prices
        /// - Combo actions (LongPostAsk/ShortPostYes): Execute market order then place resting limit order
        ///
        /// Key implementation details for developers:
        /// - Uses FIFO order matching for realistic fill simulation
        /// - Applies 0.07% taker fees on all market executions
        /// - Handles position direction (positive = long, negative = short)
        /// - Manages resting orders with expiration and proper cleanup
        /// - Prevents invalid states (insufficient cash, invalid prices)
        /// - Updates both temporary and committed state variables
        /// </remarks>
        private void ApplyAction(ActionDecision decision, MarketSnapshot effectiveSnapshot)
        {
            var action = decision.Type;
            if (action == ActionType.None) return;

            bool isComboLongPostAsk = action == ActionType.LongPostAsk;
            bool isComboShortPostYes = action == ActionType.ShortPostYes;

            if (action == ActionType.PostYes || action == ActionType.PostAsk || action == ActionType.Cancel)
            {
                HandleLimitAction(decision, effectiveSnapshot);
                return;
            }

            bool longSide = action == ActionType.Long  || action == ActionType.Exit && Position < 0 || isComboLongPostAsk;
            bool shortSide = action == ActionType.Short || action == ActionType.Exit && Position > 0 || isComboShortPostYes;
            if (!longSide && !shortSide) return;

            string tradeSide = longSide ? "no" : "yes";

            int qty = (action == ActionType.Exit) ? Math.Abs(Position) : 1;
            int remainingQuantity = qty;
            double totalCost = 0;

            var bookToReduce = longSide ? SimulatedBook.NoBids : SimulatedBook.YesBids;

            bool isPaying = longSide || (shortSide && action != ActionType.Exit);
            double LevelPriceFunc(int price) => isPaying ? (100 - price) / 100.0 : price / 100.0;

            for (int p = 99; p >= 1; p--)
            {
                if (remainingQuantity <= 0) break;
                if (bookToReduce[p] == null || bookToReduce[p].Count == 0) continue;
                int depth = bookToReduce[p].Sum(o => o.count);
                int fill = Math.Min(remainingQuantity, depth);
                double levelPrice = LevelPriceFunc(p);
                totalCost += fill * levelPrice;
                SimulatedBook.ReduceDepth(bookToReduce, p, fill);
                remainingQuantity -= fill;
            }

            int filled = qty - remainingQuantity;
            if (filled == 0) return;

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
            tempCash -= 0.07 * totalCost;

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

            // Pre-cancel any existing resting orders (replace, don't stack)
            if (isComboLongPostAsk || isComboShortPostYes)
            {
                foreach (var order in SimulatedRestingOrders)
                {
                    bool isYesSide = order.side == "yes";
                    int bookPrice =
                        (order.action == "sell" && isYesSide) ? (100 - order.price) :
                        (order.action == "buy"  && !isYesSide) ? (100 - order.price) :
                        order.price;

                    var targetBook =
                        (order.action == "buy"  && isYesSide) || (order.action == "sell" && !isYesSide)
                        ? SimulatedBook.YesBids
                        : SimulatedBook.NoBids;

                    SimulatedBook.ReduceDepth(targetBook, bookPrice, order.count);
                }
                SimulatedRestingOrders.Clear();
            }

            // Combo  take then rest  sized to 100% of current position
            if (isComboLongPostAsk && Position > 0)
            {
                int sellYesPrice = decision.Price;   // 1..99 (YES ask)
                int noBidPrice = 100 - sellYesPrice;
                int postQuantity = Position;         // 100% of position
                if (sellYesPrice > 0 && sellYesPrice < 100 && noBidPrice >= 1 && noBidPrice <= 99 && postQuantity > 0)
                {
                    if (SimulatedBook.NoBids[noBidPrice] == null)
                        SimulatedBook.NoBids[noBidPrice] = new List<(int count, DateTime timestamp)>();
                    SimulatedBook.NoBids[noBidPrice].Add((postQuantity, effectiveSnapshot.Timestamp));
                    SimulatedRestingOrders.Add(("sell", "yes", "limit", postQuantity, sellYesPrice, decision.Expiration));
                }
            }
            else if (isComboShortPostYes && Position < 0)
            {
                int yesBidPrice = decision.Price;    // 1..99 (YES bid)
                int postQuantity = -Position;         // 100% of position
                if (yesBidPrice > 0 && yesBidPrice < 100 && postQuantity > 0)
                {
                    if (SimulatedBook.YesBids[yesBidPrice] == null)
                        SimulatedBook.YesBids[yesBidPrice] = new List<(int count, DateTime timestamp)>();
                    SimulatedBook.YesBids[yesBidPrice].Add((postQuantity, effectiveSnapshot.Timestamp));
                    SimulatedRestingOrders.Add(("buy", "yes", "limit", postQuantity, yesBidPrice, decision.Expiration));
                }
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
        /// - Validates price ranges (1-99) and quantity (>0) before processing
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

                    List<(int count, DateTime timestamp)>[] book;
                    int bookPrice;

                    if (isBidYes) { book = SimulatedBook.YesBids; bookPrice = o.price; }
                    else if (isBidNo) { book = SimulatedBook.NoBids; bookPrice = o.price; }
                    else if (isAskYes) { book = SimulatedBook.NoBids; bookPrice = 100 - o.price; }
                    else { book = SimulatedBook.YesBids; bookPrice = 100 - o.price; }

                    int toCancel = o.count;
                    if (bookPrice >= 1 && bookPrice <= 99 && book[bookPrice] != null)
                    {
                        var lst = book[bookPrice];
                        for (int j = lst.Count - 1; j >= 0 && toCancel > 0; j--)
                        {
                            var e = lst[j];
                            int take = Math.Min(toCancel, e.count);
                            e.count -= take;
                            toCancel -= take;
                            if (e.count <= 0) lst.RemoveAt(j); else lst[j] = e;
                        }
                        if (lst.Count == 0) book[bookPrice] = new List<(int count, DateTime timestamp)>();
                    }
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
            if (limitPrice <= 0 || limitPrice >= 100 || qty <= 0) return;

            if (action == ActionType.PostYes)
            {
                if (SimulatedBook.YesBids[limitPrice] == null)
                    SimulatedBook.YesBids[limitPrice] = new List<(int count, DateTime timestamp)>();
                SimulatedBook.YesBids[limitPrice].Add((qty, effectiveSnapshot.Timestamp));
            }
            else // PostAsk (sell YES) => rests on NO bids at (100 - ask)
            {
                int noBidPrice = 100 - limitPrice;
                if (noBidPrice < 1 || noBidPrice > 99) return;
                if (SimulatedBook.NoBids[noBidPrice] == null)
                    SimulatedBook.NoBids[noBidPrice] = new List<(int count, DateTime timestamp)>();
                SimulatedBook.NoBids[noBidPrice].Add((qty, effectiveSnapshot.Timestamp));
            }

            SimulatedRestingOrders.Add((limitAction, limitSide, "limit", qty, limitPrice, exp));
        }


        /// <summary>
        /// Calculates the final equity value at the end of simulation by liquidating the current position.
        /// This method provides the total portfolio value for performance evaluation and reporting.
        /// </summary>
        /// <param name="lastSnapshot">The final market snapshot for valuation, though not directly used in current implementation.</param>
        /// <returns>The total equity value including cash and position value, representing the final portfolio worth.</returns>
        /// <remarks>
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
            for (int i = SimulatedRestingOrders.Count - 1; i >= 0; i--)
            {
                var o = SimulatedRestingOrders[i];

                bool isBidYes = o.action == "buy"  && o.side == "yes";
                bool isBidNo = o.action == "buy"  && o.side == "no";
                bool isAskYes = o.action == "sell" && o.side == "yes";
                bool isAskNo = o.action == "sell" && o.side == "no";

                List<(int count, DateTime timestamp)>[] book;
                Dictionary<int, int> deltas;
                int bookPrice;

                if (isBidYes) { book = SimulatedBook.YesBids; deltas = yesDeltas; bookPrice = o.price; }
                else if (isBidNo) { book = SimulatedBook.NoBids; deltas = noDeltas; bookPrice = o.price; }
                else if (isAskYes) { book = SimulatedBook.NoBids; deltas = noDeltas; bookPrice = 100 - o.price; }
                else { book = SimulatedBook.YesBids; deltas = yesDeltas; bookPrice = 100 - o.price; }

                // Handle expiration: remove from the back (our own orders)
                if (o.expiration.HasValue && o.expiration < currentTime)
                {
                    int toCancel = o.count;
                    if (bookPrice >= 1 && bookPrice <= 99 && book[bookPrice] != null)
                    {
                        var lst = book[bookPrice];
                        for (int j = lst.Count - 1; j >= 0 && toCancel > 0; j--)
                        {
                            var e = lst[j];
                            int take = Math.Min(toCancel, e.count);
                            e.count -= take;
                            toCancel -= take;
                            if (e.count <= 0) lst.RemoveAt(j); else lst[j] = e;
                        }
                        if (lst.Count == 0) book[bookPrice] = new List<(int count, DateTime timestamp)>();
                    }
                    SimulatedRestingOrders.RemoveAt(i);
                    continue;
                }

                if (deltas == null || !deltas.TryGetValue(bookPrice, out int deltaAtPrice) || deltaAtPrice >= 0)
                    continue; // no reduction at our price -> no taker hit, or volume added

                // Respect FIFO: your fill begins only after everything *ahead* of you has been consumed.
                int totalDepth = 0;
                if (bookPrice >= 1 && bookPrice <= 99 && book[bookPrice] != null)
                    totalDepth = book[bookPrice].Sum(t => t.count);

                int depthAhead = Math.Max(0, totalDepth - o.count);
                int consumed = -deltaAtPrice;

                int fillQuantity = Math.Max(0, Math.Min(o.count, consumed - depthAhead));
                if (fillQuantity <= 0) continue;

                double px = o.price / 100.0;
                if (isBidYes || isBidNo)
                {
                    Cash     -= fillQuantity * px;
                    Position += (o.side == "yes" ? fillQuantity : -fillQuantity);
                }
                else
                {
                    Cash     += fillQuantity * px;
                    Position -= (o.side == "yes" ? fillQuantity : -fillQuantity);
                }

                o.count -= fillQuantity;

                // External taker consumed book from the *front* at this price
                SimulatedBook.ReduceDepth(book, bookPrice, fillQuantity);

                if (o.count <= 0) SimulatedRestingOrders.RemoveAt(i);
                else SimulatedRestingOrders[i] = o;
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
    }
}
