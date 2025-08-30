using SmokehouseDTOs;
using TradingStrategies.Extensions;
using TradingStrategies.Strategies;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Overseer
{
    public class StrategySimulation
    {
        public Strategy Strategy { get; private set; }
        public int Position { get; private set; }
        public double Cash { get; private set; }
        public double InitialCash { get; private set; }
        public SimulatedOrderbook SimulatedBook { get; private set; }
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
            Dictionary<int, int> yesDeltas = null;
            Dictionary<int, int> noDeltas = null;
            if (prevSnapshot != null && SimulatedBook != null)
            {
                yesDeltas = ComputeDeltas(prevSnapshot.GetYesBids(), snapshot.GetYesBids());
                noDeltas = ComputeDeltas(prevSnapshot.GetNoBids(), snapshot.GetNoBids());
                SimulatedBook.ApplyDeltas(yesDeltas, noDeltas);
                SimulateFillsFromDeltas(yesDeltas, noDeltas, snapshot.Timestamp);
            }

            // Initialize book if first snapshot
            if (SimulatedBook == null)
            {
                SimulatedBook = new SimulatedOrderbook();
                SimulatedBook.InitializeFromSnapshot(snapshot);
            }

            // Create effective snapshot with simulated state
            var effectiveSnapshot = snapshot.Clone();
            effectiveSnapshot.UpdateOrderbookMetricsFromSimulated(SimulatedBook);
            effectiveSnapshot.PositionSize = Position;
            effectiveSnapshot.RestingOrders = SimulatedRestingOrders;

            // Get decision
            var decision = Strategy.GetAction(effectiveSnapshot, prevSnapshot, Position);
            ApplyAction(decision, effectiveSnapshot);
        }

        private void ApplyAction(ActionDecision decision, MarketSnapshot effectiveSnapshot)
        {
            var action = decision.Type;
            if (action == ActionType.None) return;

            if (action == ActionType.PostYes || action == ActionType.PostAsk || action == ActionType.Cancel)
            {
                HandleLimitAction(decision, effectiveSnapshot);
                return;
            }

            bool longSide = action == ActionType.Long || (action == ActionType.Exit && Position < 0);
            bool shortSide = action == ActionType.Short || (action == ActionType.Exit && Position > 0);
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

            // Apply taker fees
            tempCash -= 0.0007 * totalCost;

            int posDelta = longSide ? filled : -filled;
            tempPosition += posDelta;

            // Simulate self-fill if applicable
            int avgPrice = (int)(totalCost / filled * 100);
            int effectiveAvgPrice = isPaying ? (100 - avgPrice) : avgPrice;
            SimulateFillsFromTrade(SimulatedRestingOrders, tradeSide, effectiveAvgPrice, filled, ref tempPosition, ref tempCash, effectiveSnapshot.Timestamp);

            Cash = tempCash;
            Position = tempPosition;

            if (action == ActionType.Exit)
            {
                Position = 0; // Ensure closed
            }
        }

        private void HandleLimitAction(ActionDecision decision, MarketSnapshot effectiveSnapshot)
        {
            var action = decision.Type;
            if (action == ActionType.Cancel)
            {
                foreach (var order in SimulatedRestingOrders)
                {
                    var cancelTargetBook = (order.action == "buy" && order.side == "yes") || (order.action == "sell" && order.side == "no") ? SimulatedBook.YesBids : SimulatedBook.NoBids;
                    SimulatedBook.ReduceDepth(cancelTargetBook, order.price, order.count);
                }
                SimulatedRestingOrders.Clear();
                return;
            }

            string limitAction = action == ActionType.PostYes ? "buy" : "sell";
            string limitSide = action == ActionType.PostYes ? "yes" : "yes"; // PostBid buy yes, PostAsk sell yes
            int limitPrice = decision.Price;
            int qty = decision.Qty;
            DateTime? exp = decision.Expiration;
            if (limitPrice <= 0 || qty <= 0) return;

            var addTargetBook = action == ActionType.PostYes ? SimulatedBook.YesBids : SimulatedBook.NoBids;
            int bookPrice = action == ActionType.PostAsk ? 100 - limitPrice : limitPrice;

            if (addTargetBook[bookPrice] == null)
            {
                addTargetBook[bookPrice] = new List<(int count, DateTime timestamp)>();
            }
            addTargetBook[bookPrice].Add((qty, effectiveSnapshot.Timestamp));
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
                var order = SimulatedRestingOrders[i];
                if (order.expiration.HasValue && order.expiration < currentTime)
                {
                    var targetBook = (order.action == "buy" && order.side == "yes") || (order.action == "sell" && order.side == "no") ? SimulatedBook.YesBids : SimulatedBook.NoBids;
                    SimulatedBook.ReduceDepth(targetBook, order.price, order.count);
                    SimulatedRestingOrders.RemoveAt(i);
                    continue;
                }

                Dictionary<int, int> relevantDeltas = (order.action == "buy" && order.side == "yes") || (order.action == "sell" && order.side == "no") ? yesDeltas : noDeltas;
                if (relevantDeltas.TryGetValue(order.price, out int delta) && delta < 0)
                {
                    int fillQty = Math.Min(order.count, -delta);
                    double fillPrice = order.price / 100.0;

                    if (order.action == "buy")
                    {
                        Cash -= fillQty * fillPrice;
                        Position += order.side == "yes" ? fillQty : -fillQty;
                    }
                    else // sell
                    {
                        Cash += fillQty * fillPrice;
                        Position -= order.side == "yes" ? fillQty : -fillQty;
                    }

                    order.count -= fillQty;
                    if (order.count <= 0)
                    {
                        SimulatedRestingOrders.RemoveAt(i);
                    }
                    else
                    {
                        SimulatedRestingOrders[i] = order;
                    }
                }
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