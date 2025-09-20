using BacklashDTOs.Data;

namespace TradingStrategies.Classification.Interfaces
{
/// <summary>ISnapshotPeriodHelper</summary>
/// <summary>ISnapshotPeriodHelper</summary>
    public interface ISnapshotPeriodHelper
/// <summary>LoadSnapshotGroup</summary>
/// <summary>SplitIntoValidGroups</summary>
    {
        Task<List<SnapshotGroupDTO>> SplitIntoValidGroups(List<SnapshotDTO> snapshots, string snapshotDirectory, IProgress<double>? progress = null);
        Task<List<SnapshotDTO>> LoadSnapshotGroup(string filePath);
    }
}
