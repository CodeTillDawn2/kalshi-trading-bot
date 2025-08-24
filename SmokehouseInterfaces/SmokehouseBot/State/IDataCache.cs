using SmokehouseDTOs;
using System.Collections.Concurrent;

namespace SmokehouseBot.State.Interfaces
{
    public interface IDataCache
    {
        ConcurrentDictionary<string, IMarketData> Markets { get; }
        //ConcurrentDictionary<string, List<OrderbookData>> OrderBooks { get; set; }
        HashSet<string> WatchedMarkets { get; set; }
        double AccountBalance { get; set; }
        DateTime LastWebSocketTimestamp { get; set; }
        bool ExchangeStatus { get; set; }
        bool TradingStatus { get; set; }
        string SoftwareVersion { get; }
        event EventHandler<StatusChangedEventArgs> ExchangeStatusChanged;
        double PortfolioValue { get; }
        HashSet<string> RecentlyRemovedMarkets { get; set; }
    }
}