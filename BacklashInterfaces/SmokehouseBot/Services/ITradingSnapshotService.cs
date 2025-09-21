using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>ITradingSnapshotService</summary>
    /// <summary>ITradingSnapshotService</summary>
    public interface ITradingSnapshotService
    /// <summary>LoadManySnapshots</summary>
    /// <summary>SaveSnapshotAsync</summary>
    {
        /// <summary>ValidateSnapshotSchema</summary>
        /// <summary>ValidateMarketSnapshot</summary>
        Task<List<string>> SaveSnapshotAsync(string BrainInstance, CacheSnapshot cacheSnapshot);
        /// <summary>ValidateSnapshotSchema</summary>
        Task<Dictionary<string, List<MarketSnapshot>>> LoadManySnapshots(List<SnapshotDTO> snapshots, bool forceLoad = false);
        /// <summary>Gets or sets the NextExpectedSnapshotTimestamp.</summary>
        bool ValidateMarketSnapshot(MarketSnapshot marketSnapshot);
        void ResetSnapshotTracking();
        Task<bool> ValidateSnapshotSchema();
        string SanitizeSnapshotJson(int currentVersion, string JSON);
        public DateTime? NextExpectedSnapshotTimestamp { get; set; }
    }
}
