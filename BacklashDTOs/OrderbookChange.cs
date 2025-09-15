namespace BacklashDTOs
{
    /// <summary>
    /// Represents a change in the orderbook.
    /// </summary>
    public class OrderbookChange
    {
        /// <summary>
        /// Gets or sets the unique identifier for the change.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Gets or sets the sequence number.
        /// </summary>
        public long Sequence { get; set; }
        /// <summary>
        /// Gets or sets the side of the orderbook change.
        /// </summary>
        public string? Side { get; set; }
        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        public int Price { get; set; }
        /// <summary>
        /// Gets or sets the delta contracts.
        /// </summary>
        public int DeltaContracts { get; set; }
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the change is trade related.
        /// </summary>
        public bool IsTradeRelated { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the change is canceled.
        /// </summary>
        public bool IsCanceled { get; set; } = false;
        /// <summary>
        /// Gets or sets the matched trade ID.
        /// </summary>
        public string? MatchedTradeId { get; set; } // New property
        /// <summary>
        /// Gets or sets the list of canceled pairs.
        /// </summary>
        public List<(OrderbookChange ExistingChange, OrderbookChange NewChange)> CanceledPairs { get; set; } = new List<(OrderbookChange, OrderbookChange)>();
    }

}

