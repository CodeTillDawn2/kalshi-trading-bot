using BacklashDTOs;
using TradingStrategies.Extensions;
using TradingStrategies.Strategies;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Simulates trading strategies against historical market snapshots, managing position, cash, and order book state.
    /// This class processes market data sequentially, applies trading decisions from strategies, and tracks performance metrics.
    /// It maintains a simulated order book and resting orders to accurately model market interactions during backtesting.
    /// </summary>
    /// <remarks>
    /// The simulation handles various action types including market orders, limit orders, and position exits.
    /// It applies realistic trading fees and ensures FIFO order matching for accurate simulation results.
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
        /// Gets the simulated order book containing bid/ask levels.
        /// </summary>
        public SimulatedOrderbook SimulatedBook { get; private set; }

        /// <summary>
        /// Gets the list of resting orders placed during simulation.
        /// Each tuple contains (action, side, type, count, price, expiration).
        /// </summary>
        public List<(string action, string side, string type, int count, int price, DateTime? expiration)> SimulatedRestingOrders { get; private set; } = new List<(string, string, string, int, int, DateTime?)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StrategySimulation"/> class.
        /// </summary>
        /// <param name="strategy">The trading strategy to simulate.</param>
        /// <param name="initialCash">The initial cash balance for the simulation (default is 100.0).</param>
        public StrategySimulation(Strategy strategy, double initialCash = 100.0)
        {
            Strategy = strategy;
            Position = 0;
            Cash = initialCash;
            InitialCash = initialCash;
            SimulatedBook = new SimulatedOrderbook();
        }

        /// <summary>
        /// Processes a market snapshot by applying deltas, updating the order book, and executing trading decisions.
        /// </summary>
        /// <param name="snapshot">The current market snapshot to process.</param>
        /// <param name="prevSnapshot">The previous market snapshot for delta calculation (optional).</param>
        /// <remarks>
        /// This method handles the core simulation loop: updating the order book with deltas, getting strategy decisions,
        /// and applying those decisions to the simulated state. It ensures the simulated order book reflects current market conditions.
        /// </remarks>
        public void ProcessSnapshot(MarketSnapshot snapshot, MarketSnapshot? prevSnapshot)
        {
            // Apply deltas if previous snapshot provided
            Dictionary<int, int> yesDeltas = new Dictionary<int, int>();
            Dictionary<int, int> noDeltas = new Dictionary<int, int>();
            if (prevSnapshot != null)
            {
                yesDeltas = ComputeDeltas(prevSnapshot.GetYesBids(), snapshot.GetYesBids());
                noDeltas = ComputeDeltas(prevSnapshot.GetNoBids(), snapshot.GetNoBids());
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
        }


        /// <summary>
        /// Applies a trading action decision to the simulation state.
        /// </summary>
        /// <param name="decision">The action decision to apply.</param>
        /// <param name="effectiveSnapshot">The market snapshot with simulated order book state.</param>
        /// <remarks>
        /// Handles different action types: market orders, limit orders, cancellations, and exits.
        /// Updates position, cash, and order book accordingly. Applies trading fees and ensures proper order matching.
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
        /// Handles limit order actions (posting or canceling orders).
        /// </summary>
        /// <param name="decision">The action decision containing order details.</param>
        /// <param name="effectiveSnapshot">The market snapshot for timestamp reference.</param>
        /// <remarks>
        /// Processes PostYes/PostAsk actions by adding orders to the simulated order book and resting orders list.
        /// Handles Cancel actions by removing orders from both the order book and resting orders list.
        /// Ensures proper price validation and order book updates.
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
        /// Calculates the final equity value at the end of simulation.
        /// </summary>
        /// <param name="lastSnapshot">The final market snapshot for valuation.</param>
        /// <returns>The total equity value including cash and position value.</returns>
        /// <remarks>
        /// Computes the liquidation value of the current position using the best available bid prices
        /// and adds it to the remaining cash balance.
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
        /// Simulates order fills resulting from market order book deltas.
        /// </summary>
        /// <param name="yesDeltas">Price deltas for YES side orders.</param>
        /// <param name="noDeltas">Price deltas for NO side orders.</param>
        /// <param name="currentTime">The current timestamp for processing.</param>
        /// <remarks>
        /// Processes resting orders to determine if they get filled by market movements.
        /// Handles order expiration and ensures FIFO matching. Updates cash and position accordingly.
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
        /// Computes the differences between previous and current order book depths.
        /// </summary>
        /// <param name="prev">The previous order book depths by price.</param>
        /// <param name="curr">The current order book depths by price.</param>
        /// <returns>A dictionary of price deltas (only non-zero changes).</returns>
        /// <remarks>
        /// Calculates the change in order book depth at each price level.
        /// Positive delta indicates volume added, negative indicates volume removed.
        /// </remarks>
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
    }
}
