namespace BacklashDTOs
{
    /// <summary>
    /// Represents a trade event in the market.
    /// </summary>
    public class TradeEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier of the trade event.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Gets or sets the taker side of the trade.
        /// </summary>
        public string? TakerSide { get; set; }
        /// <summary>
        /// Gets or sets the yes price.
        /// </summary>
        public int YesPrice { get; set; }
        /// <summary>
        /// Gets or sets the no price.
        /// </summary>
        public int NoPrice { get; set; }
        /// <summary>
        /// Gets or sets the count of the trade.
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// Gets or sets the timestamp of the trade event.
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether there is a matching orderbook change.
        /// </summary>
        public bool HasMatchingOrderbookChange { get; set; } = false;
    }

}

