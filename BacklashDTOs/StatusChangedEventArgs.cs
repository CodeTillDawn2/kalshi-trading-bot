namespace BacklashDTOs
{
    public class StatusChangedEventArgs : EventArgs
    {
        public bool ExchangeStatus { get; }
        public bool TradingStatus { get; }

        public StatusChangedEventArgs(bool exchangeStatus, bool tradingStatus)
        {
            ExchangeStatus = exchangeStatus;
            TradingStatus = tradingStatus;
        }
    }
}
