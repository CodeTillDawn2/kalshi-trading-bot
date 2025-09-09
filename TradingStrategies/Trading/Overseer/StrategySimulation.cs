using BacklashDTOs;
using TradingStrategies.Extensions;
using TradingStrategies.Strategies;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Overseer
{
    public class StrategySimulation
    {
        public Strategy Strategy { get; private set; }
        public int Position { get; private set; }
        public double Cash { get; private set; }
        public double InitialCash { get; private set; }
        public SimulatedOrderbook? SimulatedBook { get; private set; }
        public List<(string action, string side, string type, int count, int price, DateTime? expiration)> SimulatedRestingOrders { get; private set; } = new List<(string, string, string, int, int, DateTime?)>();

        public StrategySimulation(Strategy strategy, double initialCash = 100.0)
        {
            Strategy = strategy;
            Position = 0;
            Cash = initialCash;
            InitialCash = initialCash;
            SimulatedBook = null;
        }

        public void ProcessSnapshot(MarketSnapshot snapshot, MarketSnapshot? prevSnapshot)
        {
            // Apply deltas if previous snapshot provided
            Dictionary<int, int> yesDeltas = new Dictionary<int, int>();
            Dictionary<int, int> noDeltas = new Dictionary<int, int>();
            if (prevSnapshot != null && SimulatedBook != null)
            {
                yesDeltas = ComputeDeltas(prevSnapshot.GetYesBids(), snapshot.GetYesBids());
                noDeltas = ComputeDeltas(prevSnapshot.GetNoBids(), snapshot.GetNoBids());
                // FIFO: append deltas with the *snapshot* time
                SimulatedBook.ApplyDeltas(yesDeltas, noDeltas);
                SimulateFillsFromDeltas(yesDeltas, noDeltas, snapshot.Timestamp);
            }

            // Initialize book if first snapshot
            if (SimulatedBook == null)
            {
                SimulatedBook = new SimulatedOrderbook();
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
            int remainingQty = qty;
            double totalCost = 0;

            var bookToReduce = longSide ? SimulatedBook.NoBids : SimulatedBook.YesBids;

            bool isPaying = longSide || (shortSide && action != ActionType.Exit);
            double LevelPriceFunc(int price) => isPaying ? (100 - price) / 100.0 : price / 100.0;

            for (int p = 99; p >= 1; p--)
            {
                if (remainingQty <= 0) break;
                if (bookToReduce[p] == null || bookToReduce[p].Count == 0) continue;
                int depth = bookToReduce[p].Sum(o => o.count);
                int fill = Math.Min(remainingQty, depth);
                double levelPrice = LevelPriceFunc(p);
                totalCost += fill * levelPrice;
                SimulatedBook.ReduceDepth(bookToReduce, p, fill);
                remainingQty -= fill;
            }

            int filled = qty - remainingQty;
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

            // Combo “take then rest” sized to 100% of current position
            if (isComboLongPostAsk && Position > 0)
            {
                int sellYesPrice = decision.Price;   // 1..99 (YES ask)
                int noBidPrice = 100 - sellYesPrice;
                int postQty = Position;         // 100% of position
                if (sellYesPrice > 0 && sellYesPrice < 100 && noBidPrice >= 1 && noBidPrice <= 99 && postQty > 0)
                {
                    if (SimulatedBook.NoBids[noBidPrice] == null)
                        SimulatedBook.NoBids[noBidPrice] = new List<(int count, DateTime timestamp)>();
                    SimulatedBook.NoBids[noBidPrice].Add((postQty, effectiveSnapshot.Timestamp));
                    SimulatedRestingOrders.Add(("sell", "yes", "limit", postQty, sellYesPrice, decision.Expiration));
                }
            }
            else if (isComboShortPostYes && Position < 0)
            {
                int yesBidPrice = decision.Price;    // 1..99 (YES bid)
                int postQty = -Position;         // 100% of position
                if (yesBidPrice > 0 && yesBidPrice < 100 && postQty > 0)
                {
                    if (SimulatedBook.YesBids[yesBidPrice] == null)
                        SimulatedBook.YesBids[yesBidPrice] = new List<(int count, DateTime timestamp)>();
                    SimulatedBook.YesBids[yesBidPrice].Add((postQty, effectiveSnapshot.Timestamp));
                    SimulatedRestingOrders.Add(("buy", "yes", "limit", postQty, yesBidPrice, decision.Expiration));
                }
            }
        }



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
            int qty = decision.Qty;
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

                int fillQty = Math.Max(0, Math.Min(o.count, consumed - depthAhead));
                if (fillQty <= 0) continue;

                double px = o.price / 100.0;
                if (isBidYes || isBidNo)
                {
                    Cash     -= fillQty * px;
                    Position += (o.side == "yes" ? fillQty : -fillQty);
                }
                else
                {
                    Cash     += fillQty * px;
                    Position -= (o.side == "yes" ? fillQty : -fillQty);
                }

                o.count -= fillQty;

                // External taker consumed book from the *front* at this price
                SimulatedBook.ReduceDepth(book, bookPrice, fillQty);

                if (o.count <= 0) SimulatedRestingOrders.RemoveAt(i);
                else SimulatedRestingOrders[i] = o;
            }
        }


        private void SimulateFillsFromTrade(List<(string action, string side, string type, int count, int price, DateTime? expiration)> resting, string tradeSide, int tradePrice, int tradeQty, ref int position, ref double cash, DateTime currentTime)
        {
            for (int i = resting.Count - 1; i >= 0; i--)
            {
                var order = resting[i];
                if (order.price == tradePrice &&
                    ((tradeSide == "yes" && order.side == "no" && order.action == "buy") ||
                     (tradeSide == "no" && order.side == "yes" && order.action == "buy")))
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
    }
}
