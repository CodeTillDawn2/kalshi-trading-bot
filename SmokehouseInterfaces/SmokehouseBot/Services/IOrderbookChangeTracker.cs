using SmokehouseBot.State.Interfaces;
using SmokehouseDTOs;

namespace SmokehouseBot.Services.Interfaces
{
    public interface IOrderbookChangeTracker : IDisposable
    {
        IMarketData Market { get; }
        bool IsMature { get; }
        TimeSpan ChangeWindowDuration { get; }
        TimeSpan TradeMatchingWindow { get; }
        TimeSpan OrderbookCancelWindow { get; }
        DateTime LastMarketOpenTime { get; set; }
        void UpdateMarketStatus(bool isExchangeActive, bool isTradingActive);
        void Stop();
        void LogOrderbookSnapshot(List<OrderbookData> originalOrderbook, List<OrderbookData> newOrderbook);
        void LogChange(string side, int price, int deltaContracts);
        void LogTrade(string takerSide, int yesPrice, int noPrice, int count, DateTime timestamp);
        (double Volume, int Levels) GetBottomNoVelocityPerMinute(List<OrderbookData> noBids, List<OrderbookChange> orderbookChanges, int threshold);
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
