using BacklashDTOs.Data;

namespace TradingStrategies.Classification.Interfaces
{
    public interface ISnapshotPeriodHelper
    {
        Task<List<SnapshotGroupDTO>> SplitIntoValidGroups(List<SnapshotDTO> snapshots, string snapshotDirectory, IProgress<double>? progress = null);
        Task<List<SnapshotDTO>> LoadSnapshotGroup(string filePath);
    }
}
