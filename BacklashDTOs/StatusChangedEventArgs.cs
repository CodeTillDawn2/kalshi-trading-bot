namespace BacklashDTOs
{
    /// <summary>
    /// Provides data for the status changed event.
    /// </summary>
    public class StatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the exchange status.
        /// </summary>
        public bool ExchangeStatus { get; }
        /// <summary>
        /// Gets the trading status.
        /// </summary>
        public bool TradingStatus { get; }

        /// <summary>
        /// Initializes a new instance of the StatusChangedEventArgs class.
        /// </summary>
        /// <param name="exchangeStatus">The exchange status.</param>
        /// <param name="tradingStatus">The trading status.</param>
        public StatusChangedEventArgs(bool exchangeStatus, bool tradingStatus)
        {
            ExchangeStatus = exchangeStatus;
            TradingStatus = tradingStatus;
        }
    }
}
