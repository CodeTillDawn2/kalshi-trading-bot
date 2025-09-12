using System.Text.Json;

namespace BacklashBot.Services.Interfaces
{
    public interface ISqlDataService : IDisposable
    {
        Task StoreOrderBookAsync(JsonElement data, string offerType);
        Task StoreTickerAsync(JsonElement data);
        Task StoreTradeAsync(JsonElement data);
        Task StoreFillAsync(JsonElement data);
        Task StoreEventLifecycleAsync(JsonElement data);
        Task StoreMarketLifecycleAsync(JsonElement data);
        Task ExecuteSnapshotImportJobAsync(CancellationToken cancellationToken = default);
    }
}
