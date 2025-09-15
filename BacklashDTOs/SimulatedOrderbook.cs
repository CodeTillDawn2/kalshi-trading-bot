namespace BacklashDTOs
{
    /// <summary>
    /// Represents a simulated orderbook for trading operations.
    /// </summary>
    public class SimulatedOrderbook
    {
        // Array of lists for prices 1-99; index 0 unused
        /// <summary>
        /// Gets the array of yes bid orders organized by price level.
        /// </summary>
        public readonly List<(int count, DateTime timestamp)>[] YesBids = new List<(int count, DateTime timestamp)>[100];
        /// <summary>
        /// Gets the array of no bid orders organized by price level.
        /// </summary>
        public readonly List<(int count, DateTime timestamp)>[] NoBids = new List<(int count, DateTime timestamp)>[100];

        /// <summary>
        /// Initializes a new instance of the SimulatedOrderbook class.
        /// </summary>
        public SimulatedOrderbook()
        {
            for (int i = 0; i < 100; i++)
            {
                YesBids[i] = new(); // Lazy init
                NoBids[i] = new();
            }
        }

        /// <summary>
        /// Gets the best (highest) yes bid price.
        /// </summary>
        /// <returns>The best yes bid price, or 0 if no bids exist.</returns>
        public int GetBestYesBid()
        {
            for (int p = 99; p >= 1; p--)
            {
                if (YesBids[p] != null && YesBids[p].Count > 0) return p;
            }
            return 0;
        }

        /// <summary>
        /// Gets the best (highest) no bid price.
        /// </summary>
        /// <returns>The best no bid price, or 0 if no bids exist.</returns>
        public int GetBestNoBid()
        {
            for (int p = 99; p >= 1; p--)
            {
                if (NoBids[p] != null && NoBids[p].Count > 0) return p;
            }
            return 0;
        }

        /// <summary>
        /// Gets the best yes ask price (derived from no bids).
        /// </summary>
        /// <returns>The best yes ask price, or 0 if no bids exist.</returns>
        public int GetBestYesAsk() => GetBestNoBid() > 0 ? 100 - GetBestNoBid() : 0;

        /// <summary>
        /// Gets the best no ask price (derived from yes bids).
        /// </summary>
        /// <returns>The best no ask price, or 0 if no bids exist.</returns>
        public int GetBestNoAsk() => GetBestYesBid() > 0 ? 100 - GetBestYesBid() : 0;

        /// <summary>
        /// Gets the total depth at the best yes bid price.
        /// </summary>
        /// <returns>The total contract count at the best yes bid price.</returns>
        public int GetDepthAtBestYesBid() => YesBids[GetBestYesBid()]?.Sum(o => o.count) ?? 0;

        /// <summary>
        /// Gets the total depth at the best no bid price.
        /// </summary>
        /// <returns>The total contract count at the best no bid price.</returns>
        public int GetDepthAtBestNoBid() => NoBids[GetBestNoBid()]?.Sum(o => o.count) ?? 0;

        /// <summary>
        /// Gets the total depth at the best yes ask price.
        /// </summary>
        /// <returns>The total contract count at the best yes ask price.</returns>
        public int GetDepthAtBestYesAsk() => GetDepthAtBestNoBid();

        /// <summary>
        /// Gets the total depth at the best no ask price.
        /// </summary>
        /// <returns>The total contract count at the best no ask price.</returns>
        public int GetDepthAtBestNoAsk() => GetDepthAtBestYesBid();

        /// <summary>
        /// Creates a deep copy of the simulated orderbook.
        /// </summary>
        /// <returns>A new SimulatedOrderbook instance with copied data.</returns>
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

        /// <summary>
        /// Initializes the orderbook from a market snapshot.
        /// </summary>
        /// <param name="snapshot">The market snapshot to initialize from.</param>
        public void InitializeFromSnapshot(MarketSnapshot snapshot)
        {
            for (int i = 1; i <= 99; i++)
            {
                YesBids[i] = new();
                NoBids[i] = new();
            }

            DateTime initTime = snapshot.Timestamp; // Assume all initial orders at snapshot time
            if (snapshot.OrderbookData is null) return;
            foreach (var entry in snapshot.OrderbookData)
            {
                if (entry["price"]?.ToString() is not string priceStr || !Int32.TryParse(priceStr, out int price)) continue;
                if (entry["resting_contracts"]?.ToString() is not string contractsStr || !Int32.TryParse(contractsStr, out int contracts)) continue;
                string? side = entry["side"]?.ToString();

                if (price < 1 || price > 99 || side is null) continue;

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

        /// <summary>
        /// Reduces the depth at a specific price level by removing orders.
        /// </summary>
        /// <param name="book">The order book array to modify.</param>
        /// <param name="price">The price level to modify.</param>
        /// <param name="qty">The quantity to remove.</param>
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

        /// <summary>
        /// Adds depth at a specific price level by adding new orders.
        /// </summary>
        /// <param name="book">The order book array to modify.</param>
        /// <param name="price">The price level to modify.</param>
        /// <param name="qty">The quantity to add.</param>
        /// <param name="timestamp">The timestamp for the new orders.</param>
        public void AddToDepth(List<(int count, DateTime timestamp)>[] book, int price, int qty, DateTime timestamp)
        {
            if (price < 1 || price > 99) return;
            if (book[price] == null)
            {
                book[price] = new List<(int count, DateTime timestamp)>();
            }
            book[price].Add((qty, timestamp)); // Add to end (newest)
        }

        /// <summary>
        /// Applies deltas to both yes and no order books.
        /// </summary>
        /// <param name="yesDeltas">Dictionary of price deltas for yes orders.</param>
        /// <param name="noDeltas">Dictionary of price deltas for no orders.</param>
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
