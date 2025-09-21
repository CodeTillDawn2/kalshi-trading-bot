using BacklashBot.State.Interfaces;
using BacklashDTOs;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>IOrderbookChangeTracker</summary>
    /// <summary>IOrderbookChangeTracker</summary>
    public interface IOrderbookChangeTracker : IDisposable
    /// <summary>Gets or sets the IsMature.</summary>
    /// <summary>Gets or sets the Market.</summary>
    {
        /// <summary>Gets or sets the OrderbookCancelWindow.</summary>
        /// <summary>Gets or sets the ChangeWindowDuration.</summary>
        event EventHandler<string> MarketInvalid;
        IMarketData Market { get; }
        /// <summary>Stop</summary>
        /// <summary>Gets or sets the OrderbookCancelWindow.</summary>
        bool IsMature { get; }
        /// <summary>RecordTrade</summary>
        /// <summary>UpdateMarketStatus</summary>
        TimeSpan ChangeWindowDuration { get; }
        /// <summary>GetTopNoVelocityPerMinute</summary>
        /// <summary>ProcessOrderbookSnapshot</summary>
        TimeSpan TradeMatchingWindow { get; }
        /// <summary>GetNoNetOrderVolumePerMinute</summary>
        /// <summary>RecordTrade</summary>
        TimeSpan OrderbookCancelWindow { get; }
        /// <summary>GetAverageTradeSize_MakerYes</summary>
        /// <summary>GetBottomYesVelocityPerMinute</summary>
        DateTime LastMarketOpenTime { get; set; }
        /// <summary>GetTradeCount_MakerNo</summary>
        /// <summary>GetTopYesVelocityPerMinute</summary>
        void UpdateMarketStatus(bool isExchangeActive, bool isTradingActive);
        /// <summary>RecalculateAllMetrics</summary>
        /// <summary>GetNoNetOrderVolumePerMinute</summary>
        void Stop();
        /// <summary>GetTradeRatePerMinute_MakerNo</summary>
        void ProcessOrderbookSnapshot(List<OrderbookData> originalOrderbook, List<OrderbookData> newOrderbook);
        /// <summary>GetAverageTradeSize_MakerNo</summary>
        void RecordOrderbookChange(string side, int price, int deltaContracts);
        /// <summary>GetTradeCount_MakerNo</summary>
        void RecordTrade(string takerSide, int yesPrice, int noPrice, int count, DateTime timestamp);
        /// <summary>GetBottomLevels</summary>
        (double Volume, int Levels) GetBottomNoVelocityPerMinute(List<OrderbookData> noBids, List<OrderbookChange> orderbookChanges, int threshold);
        /// <summary>Shutdown</summary>
        (double Volume, int Levels) GetBottomYesVelocityPerMinute(List<OrderbookData> yesBids, List<OrderbookChange> orderbookChanges, int threshold);
        (double Volume, int Levels) GetTopNoVelocityPerMinute(List<OrderbookData> noBids, List<OrderbookChange> orderbookChanges, int threshold);
        (double Volume, int Levels) GetTopYesVelocityPerMinute(List<OrderbookData> yesBids, List<OrderbookChange> orderbookChanges, int threshold);
        (double Volume, int Count) GetYesNetOrderVolumePerMinute(List<OrderbookChange> yesBidChanges);
        (double Volume, int Count) GetNoNetOrderVolumePerMinute(List<OrderbookChange> noBidChanges);
        (double rate, double volume) GetTradeRatePerMinute_MakerYes(List<OrderbookChange> yesTradeRelatedChanges);
        (double rate, double volume) GetTradeRatePerMinute_MakerNo(List<OrderbookChange> noTradeRelatedChanges);
        double GetAverageTradeSize_MakerYes(List<OrderbookChange> tradeEvents);
        double GetAverageTradeSize_MakerNo(List<OrderbookChange> tradeEvents);
        int GetTradeCount_MakerYes();
        int GetTradeCount_MakerNo();
        int GetTopLevels(List<OrderbookData> Bids, int lowerBound);
        int GetBottomLevels(List<OrderbookData> Bids, int upperBound);
        void RecalculateAllMetrics();
        void Shutdown();
    }
}
