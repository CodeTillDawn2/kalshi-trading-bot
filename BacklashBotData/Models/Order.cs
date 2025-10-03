namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a trading order in the Kalshi system with comprehensive execution tracking.
    /// This entity captures all aspects of an order's lifecycle including pricing, status,
    /// execution details, and performance metrics. Orders are the fundamental unit of trading
    /// activity and this entity provides complete visibility into order management and execution.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Gets or sets the unique identifier for this order.
        /// This is the primary key used to track the order throughout its lifecycle.
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol for this order.
        /// This identifies the specific market contract being traded.
        /// </summary>
        public string Ticker { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who placed this order.
        /// This links the order to the trading account that initiated it.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the action type for this order (e.g., "buy", "sell").
        /// This indicates the direction of the trade from the user's perspective.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the market side for this order ("yes" or "no").
        /// This specifies which side of the binary contract the order is for.
        /// </summary>
        public string Side { get; set; }

        /// <summary>
        /// Gets or sets the order type (e.g., "limit", "market").
        /// This defines how the order should be executed in the market.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the current status of the order (e.g., "pending", "filled", "cancelled").
        /// This tracks the order's progress through its lifecycle.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the price for the "yes" side of the contract in cents.
        /// This is the limit price at which the user is willing to buy/sell the yes side.
        /// </summary>
        public int YesPrice { get; set; }

        /// <summary>
        /// Gets or sets the price for the "no" side of the contract in cents.
        /// This is the limit price at which the user is willing to buy/sell the no side.
        /// </summary>
        public int NoPrice { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this order was created, in UTC.
        /// This marks the beginning of the order's lifecycle.
        /// </summary>
        public DateTime CreatedTimeUTC { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this order was last updated, in UTC.
        /// This tracks any modifications or status changes to the order.
        /// </summary>
        public DateTime? LastUpdateTimeUTC { get; set; }

        /// <summary>
        /// Gets or sets the expiration timestamp for this order, in UTC.
        /// After this time, the order will be automatically cancelled if not filled.
        /// </summary>
        public DateTime? ExpirationTimeUTC { get; set; }

        /// <summary>
        /// Gets or sets the client-provided order identifier.
        /// This allows clients to track orders using their own reference numbers.
        /// </summary>
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Gets or sets the number of times this order has been placed or re-placed.
        /// This tracks order modification history.
        /// </summary>
        public int PlaceCount { get; set; }

        /// <summary>
        /// Gets or sets the number of times this order's quantity has been decreased.
        /// This tracks partial order modifications.
        /// </summary>
        public int DecreaseCount { get; set; }

        /// <summary>
        /// Gets or sets the number of times this order has been amended.
        /// This includes price changes and other order modifications.
        /// </summary>
        public int AmendCount { get; set; }

        /// <summary>
        /// Gets or sets the number of times this order was amended and then immediately filled as a taker.
        /// This tracks aggressive order modifications that resulted in immediate execution.
        /// </summary>
        public int AmendTakerFillCount { get; set; }

        /// <summary>
        /// Gets or sets the number of contracts filled as a maker (added liquidity).
        /// This tracks the portion of the order that was filled by resting on the book.
        /// </summary>
        public int MakerFillCount { get; set; }

        /// <summary>
        /// Gets or sets the number of contracts filled as a taker (took liquidity).
        /// This tracks the portion of the order that was filled by hitting existing orders.
        /// </summary>
        public int TakerFillCount { get; set; }

        /// <summary>
        /// Gets or sets the remaining quantity of contracts still unfilled.
        /// This shows how much of the original order quantity is still active.
        /// </summary>
        public int RemainingCount { get; set; }

        /// <summary>
        /// Gets or sets the current position of this order in the market queue.
        /// This indicates how many orders are ahead of this one at the same price level.
        /// </summary>
        public int QueuePosition { get; set; }

        /// <summary>
        /// Gets or sets the total cost of maker fills in cents.
        /// This represents the value of contracts filled when this order was resting on the book.
        /// </summary>
        public long MakerFillCost { get; set; }

        /// <summary>
        /// Gets or sets the total cost of taker fills in cents.
        /// This represents the value of contracts filled when this order hit existing resting orders.
        /// </summary>
        public long TakerFillCost { get; set; }

        /// <summary>
        /// Gets or sets the total fees paid for maker fills in cents.
        /// This tracks the trading costs associated with providing liquidity.
        /// </summary>
        public long MakerFees { get; set; }

        /// <summary>
        /// Gets or sets the total fees paid for taker fills in cents.
        /// This tracks the trading costs associated with taking liquidity.
        /// </summary>
        public long TakerFees { get; set; }

        /// <summary>
        /// Gets or sets the number of times this order was cancelled due to FCC regulations.
        /// This tracks regulatory cancellations for pattern day trading rules.
        /// </summary>
        public int FccCancelCount { get; set; }

        /// <summary>
        /// Gets or sets the number of times this order was cancelled due to market closure.
        /// This tracks automatic cancellations when markets close.
        /// </summary>
        public int CloseCancelCount { get; set; }

        /// <summary>
        /// Gets or sets the number of times this order was cancelled due to taker self-trading prevention.
        /// This tracks cancellations that prevent a user from trading against their own orders.
        /// </summary>
        public int TakerSelfTradeCancelCount { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this order record was last modified.
        /// This is used for auditing and tracking order data changes.
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Market entity.
        /// This provides access to the market details and related trading information.
        /// </summary>
        public Market? Market { get; set; }
    }
}
