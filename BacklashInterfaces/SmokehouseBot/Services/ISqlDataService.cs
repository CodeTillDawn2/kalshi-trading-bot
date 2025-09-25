using System.Text.Json;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles SQL data operations,
    /// including storing various types of trading data and executing import jobs.
    /// </summary>
    public interface ISqlDataService : IDisposable
    {
        /// <summary>
        /// Stores order book data asynchronously.
        /// </summary>
        /// <param name="data">The JSON data containing order book information.</param>
        /// <param name="offerType">The type of offer (e.g., "yes" or "no").</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreOrderBookAsync(JsonElement data, string offerType);

        /// <summary>
        /// Stores ticker data asynchronously.
        /// </summary>
        /// <param name="data">The JSON data containing ticker information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreTickerAsync(JsonElement data);

        /// <summary>
        /// Stores trade data asynchronously.
        /// </summary>
        /// <param name="data">The JSON data containing trade information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreTradeAsync(JsonElement data);

        /// <summary>
        /// Stores fill data asynchronously.
        /// </summary>
        /// <param name="data">The JSON data containing fill information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreFillAsync(JsonElement data);

        /// <summary>
        /// Stores event lifecycle data asynchronously.
        /// </summary>
        /// <param name="data">The JSON data containing event lifecycle information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreEventLifecycleAsync(JsonElement data);

        /// <summary>
        /// Stores market lifecycle data asynchronously.
        /// </summary>
        /// <param name="data">The JSON data containing market lifecycle information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreMarketLifecycleAsync(JsonElement data);

        /// <summary>
        /// Executes the snapshot import job asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteSnapshotImportJobAsync(CancellationToken cancellationToken = default);
    }
}
