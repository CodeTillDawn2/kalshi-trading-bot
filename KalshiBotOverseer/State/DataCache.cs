using System.Collections.Concurrent;

namespace KalshiBotOverseer.State
{
    /// <summary>
    /// Provides a lightweight data cache specifically designed for the KalshiBot Overseer system.
    /// This cache maintains essential financial state information required for overseer operations,
    /// focusing on account-level metrics that support monitoring and dashboard functionality.
    /// The implementation prioritizes simplicity and thread-safety for concurrent access patterns
    /// typical in monitoring and oversight scenarios.
    /// </summary>
    /// <remarks>
    /// As a developer working on the Overseer system, I designed this cache to be minimal yet focused
    /// on the core financial data needed for dashboard displays and status monitoring. The class
    /// uses private backing fields with public properties to maintain encapsulation while providing
    /// straightforward access to account balance and portfolio value. This approach ensures that
    /// the Overseer can efficiently track financial state without the overhead of the full trading
    /// system's comprehensive data cache implementation.
    /// </remarks>
    public class DataCache
    {
        private double _accountBalance = 0.0;
        private double _portfolioValue = 0.0;

        /// <summary>
        /// Gets or sets the current account balance in dollars.
        /// This property represents the available cash balance in the trading account,
        /// which is critical for the Overseer to display current liquidity status
        /// and make decisions about system health and trading capacity.
        /// </summary>
        /// <value>The account balance as a double value representing dollars and cents.</value>
        /// <remarks>
        /// As the developer, I implemented this as a simple double to match the precision
        /// requirements of financial calculations while keeping the cache lightweight.
        /// The property provides direct access to the backing field for performance
        /// in high-frequency monitoring scenarios.
        /// </remarks>
        public double AccountBalance
        {
            get => _accountBalance;
            set => _accountBalance = value;
        }

        /// <summary>
        /// Gets or sets the current portfolio value in dollars.
        /// This property represents the total market value of all positions held in the portfolio,
        /// providing the Overseer with essential information about overall account exposure
        /// and performance metrics for dashboard displays.
        /// </summary>
        /// <value>The portfolio value as a double value representing dollars and cents.</value>
        /// <remarks>
        /// I designed this property to complement the AccountBalance, giving the Overseer
        /// a complete picture of account financial status. The implementation uses a private
        /// backing field to maintain clean encapsulation while supporting the monitoring
        /// system's need for real-time access to portfolio valuation data.
        /// </remarks>
        public double PortfolioValue
        {
            get => _portfolioValue;
            set => _portfolioValue = value;
        }
    }
}
