using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies
{
    /// <summary>
    /// Represents the output of a trading strategy's decision-making process in the Kalshi trading bot system.
    /// This class encapsulates the recommended trading action, including the action type, associated parameters
    /// for order execution, and explanatory information. It serves as the communication mechanism between
    /// trading strategies and the simulation or execution engine, providing all necessary details for
    /// processing the recommended trade action.
    /// </summary>
    /// <remarks>
    /// ActionDecision is used throughout the trading simulator to convey strategy recommendations.
    /// The Type property determines the primary action (Buy, Sell, Exit, None, etc.), while Price and
    /// Quantity specify order parameters for limit orders. Expiration allows for time-limited orders,
    /// and Memo provides context for the decision that can be used for logging, analysis, or debugging.
    /// </remarks>
    public class ActionDecision
    {
        /// <summary>
        /// Gets or sets the type of trading action recommended by the strategy.
        /// This determines the primary operation to be performed (e.g., Buy, Sell, Exit, None).
        /// </summary>
        public ActionType Type { get; set; }

        /// <summary>
        /// Gets or sets the price level for limit orders.
        /// This value is used when Type indicates a limit order action, specifying the target price
        /// at which the order should be executed. For market orders, this value is typically ignored.
        /// </summary>
        public int Price { get; set; } = 0; // For limit orders

        /// <summary>
        /// Gets or sets the quantity of contracts to trade.
        /// This specifies the number of units for the recommended action. Defaults to 1 for single contract trades.
        /// </summary>
        public int Quantity { get; set; } = 1; // For limit orders

        /// <summary>
        /// Gets or sets the optional expiration date and time for limit orders.
        /// When set, this indicates that the order should only remain active until the specified time.
        /// If null, the order remains active until filled or cancelled by other means.
        /// </summary>
        public DateTime? Expiration { get; set; } = null; // Optional expiration for limits

        /// <summary>
        /// Gets or sets an optional explanatory note or memo for the decision.
        /// This field provides context about why the strategy made this particular recommendation,
        /// which can be useful for logging, performance analysis, or debugging strategy behavior.
        /// </summary>
        public string? Memo { get; set; }
    }
}
