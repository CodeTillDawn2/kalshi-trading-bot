using System.Text.Json;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>ISqlDataService</summary>
    /// <summary>ISqlDataService</summary>
    public interface ISqlDataService : IDisposable
    /// <summary>StoreTickerAsync</summary>
    /// <summary>StoreOrderBookAsync</summary>
    {
        /// <summary>StoreEventLifecycleAsync</summary>
        /// <summary>StoreTradeAsync</summary>
        Task StoreOrderBookAsync(JsonElement data, string offerType);
        /// <summary>StoreEventLifecycleAsync</summary>
        Task StoreTickerAsync(JsonElement data);
        /// <summary>ExecuteSnapshotImportJobAsync</summary>
        Task StoreTradeAsync(JsonElement data);
        Task StoreFillAsync(JsonElement data);
        Task StoreEventLifecycleAsync(JsonElement data);
        Task StoreMarketLifecycleAsync(JsonElement data);
        Task ExecuteSnapshotImportJobAsync(CancellationToken cancellationToken = default);
    }
}
