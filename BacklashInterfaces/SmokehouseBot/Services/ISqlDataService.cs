using System.Text.Json;

namespace BacklashBot.Services.Interfaces
{
    public interface ISqlDataService : IDisposable
    {
        Task StoreOrderBookAsync(JsonElement data, string offerType);
        Task StoreTradeAsync(JsonElement data);
        Task StoreFillAsync(JsonElement data);
        Task StoreEventLifecycleAsync(JsonElement data);
        Task StoreMarketLifecycleAsync(JsonElement data);
        Task ImportSnapshotsFromFilesAsync(CancellationToken cancellationToken = default);
    }
}
