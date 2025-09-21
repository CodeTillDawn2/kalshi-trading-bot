using BacklashDTOs;
using System.Collections.Concurrent;

namespace BacklashBot.State.Interfaces
{
    /// <summary>IDataCache</summary>
    /// <summary>IDataCache</summary>
    public interface IDataCache
    /// <summary>Gets or sets the Markets.</summary>
    {
        /// <summary>Gets or sets the AccountBalance.</summary>
        /// <summary>Gets or sets the WatchedMarkets.</summary>
        ConcurrentDictionary<string, IMarketData> Markets { get; }
        /// <summary>Gets or sets the TradingStatus.</summary>
        /// <summary>Gets or sets the LastWebSocketTimestamp.</summary>
        //ConcurrentDictionary<string, List<OrderbookData>> OrderBooks { get; set; }
        /// <summary>Gets or sets the PortfolioValue.</summary>
        /// <summary>Gets or sets the TradingStatus.</summary>
        HashSet<string> WatchedMarkets { get; set; }
        /// <summary>Gets or sets the ExchangeStatusChanged.</summary>
        double AccountBalance { get; set; }
        /// <summary>Gets or sets the RecentlyRemovedMarkets.</summary>
        DateTime LastWebSocketTimestamp { get; set; }
        bool ExchangeStatus { get; set; }
        bool TradingStatus { get; set; }
        string SoftwareVersion { get; }
        event EventHandler<StatusChangedEventArgs> ExchangeStatusChanged;
        double PortfolioValue { get; }
        HashSet<string> RecentlyRemovedMarkets { get; set; }
    }
}
