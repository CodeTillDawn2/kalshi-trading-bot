using BacklashDTOs;
using System.Collections.Concurrent;

namespace BacklashBot.State.Interfaces
{
    /// <summary>
    /// Defines the contract for a data cache that stores and manages trading-related data
    /// including market information, account details, and system status.
    /// </summary>
    public interface IDataCache
    {
        /// <summary>
        /// Gets the concurrent dictionary containing market data keyed by market ticker.
        /// </summary>
        ConcurrentDictionary<string, IMarketData> Markets { get; }

        /// <summary>
        /// Gets or sets the hash set of market tickers currently being watched.
        /// </summary>
        HashSet<string> WatchedMarkets { get; set; }

        /// <summary>
        /// Gets or sets the current account balance.
        /// </summary>
        double AccountBalance { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last WebSocket message received.
        /// </summary>
        DateTime LastWebSocketTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the current exchange operational status.
        /// </summary>
        bool ExchangeStatus { get; set; }

        /// <summary>
        /// Gets or sets the current trading status.
        /// </summary>
        bool TradingStatus { get; set; }

        /// <summary>
        /// Gets the current software version.
        /// </summary>
        string SoftwareVersion { get; }

        /// <summary>
        /// Occurs when the exchange status changes.
        /// </summary>
        event EventHandler<StatusChangedEventArgs> ExchangeStatusChanged;

        /// <summary>
        /// Gets the current portfolio value.
        /// </summary>
        double PortfolioValue { get; }

        /// <summary>
        /// Gets or sets the hash set of markets recently removed from watch lists.
        /// </summary>
        HashSet<string> RecentlyRemovedMarkets { get; set; }
    }
}
