namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for order data.
    /// </summary>
    public class OrderDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the order.
        /// </summary>
        public string? OrderId { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string? Ticker { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the action type.
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the side of the order (buy/sell).
        /// </summary>
        public string? Side { get; set; }

        /// <summary>
        /// Gets or sets the order type.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the order status.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the yes price.
        /// </summary>
        public int YesPrice { get; set; }

        /// <summary>
        /// Gets or sets the no price.
        /// </summary>
        public int NoPrice { get; set; }

        /// <summary>
        /// Gets or sets the creation time in UTC.
        /// </summary>
        public DateTime CreatedTimeUTC { get; set; }

        /// <summary>
        /// Gets or sets the last update time in UTC.
        /// </summary>
        public DateTime? LastUpdateTimeUTC { get; set; }

        /// <summary>
        /// Gets or sets the expiration time in UTC.
        /// </summary>
        public DateTime? ExpirationTimeUTC { get; set; }

        /// <summary>
        /// Gets or sets the client order identifier.
        /// </summary>
        public string? ClientOrderId { get; set; }

        /// <summary>
        /// Gets or sets the place count.
        /// </summary>
        public int PlaceCount { get; set; }

        /// <summary>
        /// Gets or sets the decrease count.
        /// </summary>
        public int DecreaseCount { get; set; }

        /// <summary>
        /// Gets or sets the amend count.
        /// </summary>
        public int AmendCount { get; set; }

        /// <summary>
        /// Gets or sets the amend taker fill count.
        /// </summary>
        public int AmendTakerFillCount { get; set; }

        /// <summary>
        /// Gets or sets the maker fill count.
        /// </summary>
        public int MakerFillCount { get; set; }

        /// <summary>
        /// Gets or sets the taker fill count.
        /// </summary>
        public int TakerFillCount { get; set; }

        /// <summary>
        /// Gets or sets the remaining count.
        /// </summary>
        public int RemainingCount { get; set; }

        /// <summary>
        /// Gets or sets the queue position.
        /// </summary>
        public int QueuePosition { get; set; }

        /// <summary>
        /// Gets or sets the maker fill cost.
        /// </summary>
        public long MakerFillCost { get; set; }

        /// <summary>
        /// Gets or sets the taker fill cost.
        /// </summary>
        public long TakerFillCost { get; set; }

        /// <summary>
        /// Gets or sets the maker fees.
        /// </summary>
        public long MakerFees { get; set; }

        /// <summary>
        /// Gets or sets the taker fees.
        /// </summary>
        public long TakerFees { get; set; }

        /// <summary>
        /// Gets or sets the FCC cancel count.
        /// </summary>
        public int FccCancelCount { get; set; }

        /// <summary>
        /// Gets or sets the close cancel count.
        /// </summary>
        public int CloseCancelCount { get; set; }

        /// <summary>
        /// Gets or sets the taker self-trade cancel count.
        /// </summary>
        public int TakerSelfTradeCancelCount { get; set; }

        /// <summary>
        /// Gets or sets the last modified timestamp.
        /// </summary>
        public DateTime? LastModified { get; set; }
    }
}
