namespace BacklashDTOs
{
    public class SimulatedOrderbook
    {
        // Array of lists for prices 1-99; index 0 unused
        public readonly List<(int count, DateTime timestamp)>[] YesBids = new List<(int count, DateTime timestamp)>[100];
        public readonly List<(int count, DateTime timestamp)>[] NoBids = new List<(int count, DateTime timestamp)>[100];

        public SimulatedOrderbook()
        {
            for (int i = 0; i < 100; i++)
            {
                YesBids[i] = new(); // Lazy init
                NoBids[i] = new();
            }
        }

        public int GetBestYesBid()
        {
            for (int p = 99; p >= 1; p--)
            {
                if (YesBids[p] != null && YesBids[p].Count > 0) return p;
            }
            return 0;
        }

        public int GetBestNoBid()
        {
            for (int p = 99; p >= 1; p--)
            {
                if (NoBids[p] != null && NoBids[p].Count > 0) return p;
            }
            return 0;
        }

        public int GetBestYesAsk() => GetBestNoBid() > 0 ? 100 - GetBestNoBid() : 0;

        public int GetBestNoAsk() => GetBestYesBid() > 0 ? 100 - GetBestYesBid() : 0;

        public int GetDepthAtBestYesBid() => YesBids[GetBestYesBid()]?.Sum(o => o.count) ?? 0;

        public int GetDepthAtBestNoBid() => NoBids[GetBestNoBid()]?.Sum(o => o.count) ?? 0;

        public int GetDepthAtBestYesAsk() => GetDepthAtBestNoBid();

        public int GetDepthAtBestNoAsk() => GetDepthAtBestYesBid();

        public SimulatedOrderbook Clone()
        {
            var clone = new SimulatedOrderbook();
            for (int p = 1; p <= 99; p++)
            {
                if (YesBids[p] != null)
                {
                    clone.YesBids[p] = YesBids[p].Select(o => o).ToList(); // Deep copy list
                }
                if (NoBids[p] != null)
                {
                    clone.NoBids[p] = NoBids[p].Select(o => o).ToList();
                }
            }
            return clone;
        }

        public void InitializeFromSnapshot(MarketSnapshot snapshot)
        {
            for (int i = 1; i <= 99; i++)
            {
                YesBids[i] = new();
                NoBids[i] = new();
            }

            DateTime initTime = snapshot.Timestamp; // Assume all initial orders at snapshot time
            foreach (var entry in snapshot.OrderbookData)
            {
                int price = Int32.Parse(entry["price"].ToString());
                int contracts = Int32.Parse(entry["resting_contracts"].ToString());
                string side = entry["side"].ToString();

                if (price < 1 || price > 99) continue;

                var ordersList = new List<(int count, DateTime timestamp)> { (contracts, initTime) }; // Single aggregated order for init

                if (side == "yes")
                {
                    YesBids[price] = ordersList;
                }
                else if (side == "no")
                {
                    NoBids[price] = ordersList;
                }
            }
        }

        public void ReduceDepth(List<(int count, DateTime timestamp)>[] book, int price, int qty)
        {
            if (price < 1 || price > 99) return;
            var orders = book[price];
            if (orders == null) return;

            int remainingQty = qty;
            for (int i = 0; i < orders.Count && remainingQty > 0; i++)
            {
                var order = orders[i];
                int fill = Math.Min(remainingQty, order.count);
                remainingQty -= fill;
                orders[i] = (order.count - fill, order.timestamp);
            }
            // Remove zero-count orders
            orders.RemoveAll(o => o.count <= 0);
            if (orders.Count == 0)
            {
                book[price] = new();
            }
        }

        public void AddToDepth(List<(int count, DateTime timestamp)>[] book, int price, int qty, DateTime timestamp)
        {
            if (price < 1 || price > 99) return;
            if (book[price] == null)
            {
                book[price] = new List<(int count, DateTime timestamp)>();
            }
            book[price].Add((qty, timestamp)); // Add to end (newest)
        }

        public void ApplyDeltas(Dictionary<int, int> yesDeltas, Dictionary<int, int> noDeltas)
        {
            DateTime currentTime = DateTime.UtcNow; // Or pass snapshot.Timestamp for sim consistency
            ApplyDeltasInternal(YesBids, yesDeltas, currentTime);
            ApplyDeltasInternal(NoBids, noDeltas, currentTime);
        }

        private void ApplyDeltasInternal(List<(int count, DateTime timestamp)>[] book, Dictionary<int, int> deltas, DateTime currentTime)
        {
            foreach (var kv in deltas)
            {
                int price = kv.Key;
                if (price < 1 || price > 99) continue;
                int delta = kv.Value;

                if (delta > 0)
                {
                    // Add new orders at end (assume aggregated new order)
                    AddToDepth(book, price, delta, currentTime);
                }
                else if (delta < 0)
                {
                    // Reduce from front (oldest)
                    ReduceDepth(book, price, -delta);
                }
            }
        }
    }
}
