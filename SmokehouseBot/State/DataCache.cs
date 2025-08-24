// TradingStrategies.Models/DataCache.cs
using SmokehouseBot.State.Interfaces;
using SmokehouseDTOs;
using System.Collections.Concurrent;

namespace SmokehouseBot.State
{
    public class DataCache : IDataCache
    {

        public DataCache()
        {
        }

        public ConcurrentDictionary<string, IMarketData> Markets { get; } = new();
        //public ConcurrentDictionary<string, List<OrderbookData>> OrderBooks { get; set; } = new();
        private double _accountBalance = 0;
        private DateTime _lastWebSocketTimestamp = DateTime.MinValue;
        public HashSet<string> WatchedMarkets
        {
            get;
            set;
        } = new HashSet<string>();
        private bool _exchangeStatus = false;
        private bool _tradingStatus = false;

        public string SoftwareVersion
        {
            get
            {
                var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        public event EventHandler<StatusChangedEventArgs>? ExchangeStatusChanged;

        public double AccountBalance { get { return _accountBalance; } set { _accountBalance = value; } }
        public HashSet<string> RecentlyRemovedMarkets { get; set; } = new HashSet<string>();
        public double PortfolioValue
        {
            get
            {
                return Math.Round(Markets.Values.Select(x => x.MarketExposure + x.PositionROIAmt).Sum(), 2);
            }
        }

        public bool ExchangeStatus
        {
            get { return _exchangeStatus; }
            set
            {
                if (value != _exchangeStatus)
                {
                    _exchangeStatus = value;
                    if (ExchangeStatusChanged != null)
                        ExchangeStatusChanged?.Invoke(this, new StatusChangedEventArgs(_exchangeStatus, _tradingStatus));
                }
            }
        }
        public bool TradingStatus
        {
            get { return _tradingStatus; }
            set
            {
                if (value != _tradingStatus)
                {
                    _tradingStatus = value;
                    if (ExchangeStatusChanged != null)
                        ExchangeStatusChanged?.Invoke(this, new StatusChangedEventArgs(_exchangeStatus, _tradingStatus));
                }
            }
        }

        public DateTime LastWebSocketTimestamp
        {
            get { return _lastWebSocketTimestamp; }
            set { _lastWebSocketTimestamp = value; }
        }

    }
}