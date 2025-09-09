using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBot.Services.Interfaces
{
    public interface ITradingSnapshotService
    {
        Task<List<string>> SaveSnapshotAsync(string BrainInstance, CacheSnapshot cacheSnapshot);
        Task<Dictionary<string, List<MarketSnapshot>>> LoadManySnapshots(List<SnapshotDTO> snapshots, bool forceLoad = false);
        bool SnapshotIsValid(MarketSnapshot marketSnapshot);
        void ResetLastSnapshot();
        Task<bool> CheckSchemaMatches();
        string SterilizeJSON(int currentVersion, string JSON);
        public DateTime? NextExpectedSnapshotTimestamp { get; set; }
    }
}
