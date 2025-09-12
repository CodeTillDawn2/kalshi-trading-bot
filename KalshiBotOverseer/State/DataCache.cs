using System.Collections.Concurrent;

namespace KalshiBotOverseer.State
{
    /// <summary>
    /// Simple data cache for storing basic financial information in the Overseer system.
    /// This class provides storage for account balance and portfolio value data.
    /// Note: This is a minimal implementation and may not be actively used in the current system.
    /// </summary>
    /// <remarks>
    /// This DataCache implementation is very basic and only stores account balance and portfolio value.
    /// It does not implement the full IDataCache interface like the main BacklashBot.State.DataCache.
    /// Consider using the full DataCache implementation from BacklashBot.State for complete functionality.
    /// </remarks>
    public class DataCache
    {
        private double _accountBalance = 0.0;
        private double _portfolioValue = 0.0;

        /// <summary>
        /// Gets or sets the current account balance in dollars.
        /// This represents the available cash balance in the trading account.
        /// </summary>
        /// <value>The account balance as a double value.</value>
        public double AccountBalance
        {
            get => _accountBalance;
            set => _accountBalance = value;
        }

        /// <summary>
        /// Gets or sets the current portfolio value in dollars.
        /// This represents the total value of all positions in the portfolio.
        /// </summary>
        /// <value>The portfolio value as a double value.</value>
        public double PortfolioValue
        {
            get => _portfolioValue;
            set => _portfolioValue = value;
        }
    }
}
