using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages trading snapshots,
    /// including saving, loading, validation, and schema management.
    /// </summary>
    public interface ITradingSnapshotService
    {
        /// <summary>
        /// Saves a snapshot asynchronously for the specified brain instance.
        /// </summary>
        /// <param name="brainInstance">The name of the brain instance.</param>
        /// <param name="cacheSnapshot">The cache snapshot to save.</param>
        /// <returns>A list of saved snapshot identifiers.</returns>
        Task<List<string>> SaveSnapshotAsync(string brainInstance, CacheSnapshot cacheSnapshot);

        /// <summary>
        /// Loads multiple snapshots based on the provided snapshot DTOs.
        /// </summary>
        /// <param name="snapshots">The list of snapshot DTOs to load.</param>
        /// <param name="forceLoad">Whether to force loading even if cached.</param>
        /// <returns>A dictionary mapping snapshot identifiers to lists of market snapshots.</returns>
        Task<Dictionary<string, List<MarketSnapshot>>> LoadManySnapshots(List<SnapshotDTO> snapshots, bool forceLoad = false);

        /// <summary>
        /// Validates a market snapshot for correctness and completeness.
        /// </summary>
        /// <param name="marketSnapshot">The market snapshot to validate.</param>
        /// <returns><c>true</c> if the snapshot is valid; otherwise, <c>false</c>.</returns>
        bool ValidateMarketSnapshot(MarketSnapshot marketSnapshot);

        /// <summary>
        /// Resets the snapshot tracking state.
        /// </summary>
        void ResetSnapshotTracking();

        /// <summary>
        /// Validates the snapshot schema asynchronously.
        /// </summary>
        /// <returns><c>true</c> if the schema is valid; otherwise, <c>false</c>.</returns>
        Task<bool> ValidateSnapshotSchema();

        /// <summary>
        /// Sanitizes snapshot JSON data based on the current schema version.
        /// </summary>
        /// <param name="currentVersion">The current schema version.</param>
        /// <param name="json">The JSON data to sanitize.</param>
        /// <returns>The sanitized JSON string.</returns>
        string SanitizeSnapshotJson(int currentVersion, string json);

        /// <summary>
        /// Gets or sets the timestamp of the next expected snapshot.
        /// </summary>
        DateTime? NextExpectedSnapshotTimestamp { get; set; }
    }
}
