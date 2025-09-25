// TradingStrategies.Models/DataCache.cs
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using System.Collections.Concurrent;

namespace BacklashBot.State
{
    /// <summary>
    /// Provides a centralized cache for market data, account information, and trading status.
    /// Implements thread-safe storage for real-time trading data and state management.
    /// </summary>
    public class DataCache : IDataCache
    {
        /// <summary>
        /// Initializes a new instance of the DataCache class.
        /// </summary>
        public DataCache()
        {
        }

        /// <summary>
        /// Gets the thread-safe dictionary containing market data for all tracked markets.
        /// Key is market ticker, value is the market data interface.
        /// </summary>
        public ConcurrentDictionary<string, IMarketData> Markets { get; } = new();
        //public ConcurrentDictionary<string, List<OrderbookData>> OrderBooks { get; set; } = new();
        private double _accountBalance = 0;
        private DateTime _lastWebSocketTimestamp = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the set of market tickers currently being watched by the bot.
        /// </summary>
        public HashSet<string> WatchedMarkets
        {
            get;
            set;
        } = new HashSet<string>();
        private bool _exchangeStatus = false;
        private bool _tradingStatus = false;

        /// <summary>
        /// Gets the software version of the application in Major.Minor.Build format.
        /// </summary>
        public string SoftwareVersion
        {
            get
            {
                var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        /// <summary>
        /// Event raised when the exchange or trading status changes.
        /// </summary>
        public event EventHandler<StatusChangedEventArgs>? ExchangeStatusChanged;

        /// <summary>
        /// Gets or sets the current account balance in dollars.
        /// </summary>
        public double AccountBalance { get { return _accountBalance; } set { _accountBalance = value; } }

        /// <summary>
        /// Gets or sets the set of market tickers that were recently removed from watch list.
        /// Used to prevent immediate re-watching of recently removed markets.
        /// </summary>
        public HashSet<string> RecentlyRemovedMarkets { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets the total portfolio value calculated as the sum of market exposure and position ROI amounts across all markets.
        /// Rounded to 2 decimal places.
        /// </summary>
        public double PortfolioValue
        {
            get
            {
                return Math.Round(Markets.Values.Select(x => x.MarketExposure + x.PositionROIAmt).Sum(), 2);
            }
        }

        /// <summary>
        /// Gets or sets the current exchange operational status.
        /// When changed, raises the ExchangeStatusChanged event.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current trading operational status.
        /// When changed, raises the ExchangeStatusChanged event.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the timestamp of the last received WebSocket message.
        /// Used to track the freshness of real-time data connections.
        /// </summary>
        public DateTime LastWebSocketTimestamp
        {
            get { return _lastWebSocketTimestamp; }
            set { _lastWebSocketTimestamp = value; }
        }

    }
}
