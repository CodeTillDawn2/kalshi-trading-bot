namespace BacklashDTOs
{
    /// <summary>
    /// Represents a simulated orderbook for trading operations.
    /// </summary>
    public class SimulatedOrderbook
    {
        /// <summary>
        /// Represents an order level in the order book.
        /// </summary>
        public class OrderLevel
        {
            /// <summary>
            /// Gets or sets the unique identifier for the order.
            /// </summary>
            public string Id { get; set; } = Guid.NewGuid().ToString();

            /// <summary>
            /// Gets or sets the count of contracts at this level.
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// Gets or sets the timestamp when the order was placed.
            /// </summary>
            public DateTime Timestamp { get; set; }

            /// <summary>
            /// Gets or sets whether this is an order placed by our own strategy.
            /// </summary>
            public bool IsOwnOrder { get; set; }
        }

        /// <summary>
        /// Gets the sorted dictionary of yes bid orders organized by price level.
        /// </summary>
        public readonly SortedDictionary<int, List<OrderLevel>> YesBids = new SortedDictionary<int, List<OrderLevel>>();
        /// <summary>
        /// Gets the sorted dictionary of no bid orders organized by price level.
        /// </summary>
        public readonly SortedDictionary<int, List<OrderLevel>> NoBids = new SortedDictionary<int, List<OrderLevel>>();

        /// <summary>
        /// Initializes a new instance of the SimulatedOrderbook class.
        /// </summary>
        public SimulatedOrderbook()
        {
            // SortedDictionary is initialized automatically
        }

        /// <summary>
        /// Gets the best (highest) yes bid price.
        /// </summary>
        /// <returns>The best yes bid price, or 0 if no bids exist.</returns>
        public int GetBestYesBid()
        {
            return YesBids.Count > 0 ? YesBids.Keys.Last() : 0;
        }

        /// <summary>
        /// Gets the best (highest) no bid price.
        /// </summary>
        /// <returns>The best no bid price, or 0 if no bids exist.</returns>
        public int GetBestNoBid()
        {
            return NoBids.Count > 0 ? NoBids.Keys.Last() : 0;
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
        public int GetDepthAtBestYesBid()
        {
            int best = GetBestYesBid();
            return best > 0 && YesBids.TryGetValue(best, out var list) ? list.Sum(o => o.Count) : 0;
        }

        /// <summary>
        /// Gets the total depth at the best no bid price.
        /// </summary>
        /// <returns>The total contract count at the best no bid price.</returns>
        public int GetDepthAtBestNoBid()
        {
            int best = GetBestNoBid();
            return best > 0 && NoBids.TryGetValue(best, out var list) ? list.Sum(o => o.Count) : 0;
        }

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
            foreach (var kvp in YesBids)
            {
                clone.YesBids[kvp.Key] = kvp.Value.Select(o => new OrderLevel
                {
                    Id = o.Id,
                    Count = o.Count,
                    Timestamp = o.Timestamp,
                    IsOwnOrder = o.IsOwnOrder
                }).ToList();
            }
            foreach (var kvp in NoBids)
            {
                clone.NoBids[kvp.Key] = kvp.Value.Select(o => new OrderLevel
                {
                    Id = o.Id,
                    Count = o.Count,
                    Timestamp = o.Timestamp,
                    IsOwnOrder = o.IsOwnOrder
                }).ToList();
            }
            return clone;
        }

        /// <summary>
        /// Initializes the orderbook from a market snapshot.
        /// </summary>
        /// <param name="snapshot">The market snapshot to initialize from.</param>
        public void InitializeFromSnapshot(MarketSnapshot snapshot)
        {
            YesBids.Clear();
            NoBids.Clear();

            DateTime initTime = snapshot.Timestamp; // Assume all initial orders at snapshot time
            if (snapshot.OrderbookData is null) return;
            foreach (var entry in snapshot.OrderbookData)
            {
                if (entry["price"]?.ToString() is not string priceStr || !Int32.TryParse(priceStr, out int price)) continue;
                if (entry["resting_contracts"]?.ToString() is not string contractsStr || !Int32.TryParse(contractsStr, out int contracts)) continue;
                string? side = entry["side"]?.ToString();

                if (price < 1 || price > 99 || side is null) continue;

                var list = new List<OrderLevel>();
                list.Add(new OrderLevel { Count = contracts, Timestamp = initTime, IsOwnOrder = false });

                if (side == "yes")
                {
                    YesBids[price] = list;
                }
                else if (side == "no")
                {
                    NoBids[price] = list;
                }
            }
        }

        /// <summary>
        /// Reduces the depth at a specific price level by removing orders.
        /// </summary>
        /// <param name="book">The order book dictionary to modify.</param>
        /// <param name="price">The price level to modify.</param>
        /// <param name="qty">The quantity to remove.</param>
        /// <param name="fromFront">Whether to remove from the front (FIFO) or back (LIFO for expiration).</param>
        public void ReduceDepth(SortedDictionary<int, List<OrderLevel>> book, int price, int qty, bool fromFront = true)
        {
            if (price < 1 || price > 99 || !book.TryGetValue(price, out var list)) return;

            int remainingQty = qty;
            while (remainingQty > 0 && list.Count > 0)
            {
                int index = fromFront ? 0 : list.Count - 1;
                var order = list[index];
                int fill = Math.Min(remainingQty, order.Count);
                remainingQty -= fill;
                order.Count -= fill;
                if (order.Count <= 0)
                {
                    list.RemoveAt(index);
                }
            }
            if (list.Count == 0)
            {
                book.Remove(price);
            }
        }

        /// <summary>
        /// Adds depth at a specific price level by adding new orders.
        /// </summary>
        /// <param name="book">The order book dictionary to modify.</param>
        /// <param name="price">The price level to modify.</param>
        /// <param name="qty">The quantity to add.</param>
        /// <param name="timestamp">The timestamp for the new orders.</param>
        /// <param name="isOwn">Whether this is an order placed by our own strategy.</param>
        public void AddToDepth(SortedDictionary<int, List<OrderLevel>> book, int price, int qty, DateTime timestamp, bool isOwn = true)
        {
            if (price < 1 || price > 99) return;
            if (!book.TryGetValue(price, out var list))
            {
                list = new List<OrderLevel>();
                book[price] = list;
            }
            list.Add(new OrderLevel { Count = qty, Timestamp = timestamp, IsOwnOrder = isOwn });
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

        private void ApplyDeltasInternal(SortedDictionary<int, List<OrderLevel>> book, Dictionary<int, int> deltas, DateTime currentTime)
        {
            foreach (var kv in deltas)
            {
                int price = kv.Key;
                if (price < 1 || price > 99) continue;
                int delta = kv.Value;

                if (delta > 0)
                {
                    // Add new external orders (isOwn = false)
                    AddToDepth(book, price, delta, currentTime, isOwn: false);
                }
                else if (delta < 0)
                {
                    // Reduce from front (FIFO)
                    ReduceDepth(book, price, -delta);
                }
            }
        }
    }
}
