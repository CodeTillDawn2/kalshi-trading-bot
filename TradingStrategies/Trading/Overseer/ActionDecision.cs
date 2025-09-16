using static BacklashInterfaces.Enums.StrategyEnums;
using System;

namespace TradingStrategies.Trading.Overseer
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
        private ActionType _type;
        private int _price = 0;
        private int _quantity = 1;
        private DateTime? _expiration = null;
        private string _memo = "";

        /// <summary>
        /// Gets or sets the type of trading action recommended by the strategy.
        /// This determines the primary operation to be performed (e.g., Buy, Sell, Exit, None).
        /// </summary>
        public ActionType Type
        {
            get => _type;
            set => _type = value;
        }

        /// <summary>
        /// Gets or sets the price level for limit orders.
        /// This value is used when Type indicates a limit order action, specifying the target price
        /// at which the order should be executed. For market orders, this value is typically ignored.
        /// </summary>
        public int Price
        {
            get => _price;
            set
            {
                if (value < 0) throw new ArgumentException("Price must be non-negative", nameof(Price));
                _price = value;
            }
        }

        /// <summary>
        /// Gets or sets the quantity of contracts to trade.
        /// This specifies the number of units for the recommended action. Defaults to 1 for single contract trades.
        /// </summary>
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value <= 0) throw new ArgumentException("Quantity must be positive", nameof(Quantity));
                _quantity = value;
            }
        }

        /// <summary>
        /// Gets or sets the optional expiration date and time for limit orders.
        /// When set, this indicates that the order should only remain active until the specified time.
        /// If null, the order remains active until filled or cancelled by other means.
        /// </summary>
        public DateTime? Expiration
        {
            get => _expiration;
            set
            {
                if (value.HasValue && value.Value <= DateTime.Now) throw new ArgumentException("Expiration must be in the future", nameof(Expiration));
                _expiration = value;
            }
        }

        /// <summary>
        /// Gets or sets an optional explanatory note or memo for the decision.
        /// This field provides context about why the strategy made this particular recommendation,
        /// which can be useful for logging, performance analysis, or debugging strategy behavior.
        /// </summary>
        public string Memo
        {
            get => _memo;
            set => _memo = value ?? "";
        }

        /// <summary>
        /// Creates a deep clone of this ActionDecision instance.
        /// </summary>
        /// <returns>A new ActionDecision with the same property values.</returns>
        public ActionDecision Clone()
        {
            return new ActionDecision
            {
                Type = _type,
                Price = _price,
                Quantity = _quantity,
                Expiration = _expiration,
                Memo = _memo
            };
        }
    }
}
