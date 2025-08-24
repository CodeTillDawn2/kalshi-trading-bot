using SmokehouseDTOs;

namespace SmokehouseBot.Services.Interfaces
{
    public interface IOrderBookService
    {
        event EventHandler<string> OrderBookUpdated;
        void AssignWebSocketHandlers();
        List<OrderbookData> GetCurrentOrderBook(string marketTicker);
        Task SyncOrderBookAsync(string marketTicker);
        void ClearQueueForMarketAsync(string marketTicker);
        Task StartServicesAsync();

        bool IsEventQueueUnderLimit(int limit);

        Task StopServicesAsync();

        (int EventQueueCount, int TickerQueueCount, int NotificationQueueCount) GetQueueCounts();
    }
}
