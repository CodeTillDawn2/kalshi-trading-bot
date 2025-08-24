using SmokehouseDTOs.Data;

namespace TradingStrategies.Classification.Interfaces
{
    public interface ISnapshotPeriodHelper
    {
        List<SnapshotGroupDTO> SplitIntoValidGroups(List<SnapshotDTO> snapshots, string snapshotDirectory);
        List<SnapshotDTO> LoadSnapshotGroup(string filePath);
    }
}