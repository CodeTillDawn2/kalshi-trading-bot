using BacklashDTOs.Data;

namespace TradingStrategies.Classification.Interfaces
{
    /// <summary>
    /// Defines the contract for a helper service that manages snapshot period operations,
    /// including splitting snapshots into valid groups and loading snapshot groups.
    /// </summary>
    public interface ISnapshotPeriodHelper
    {
        /// <summary>
        /// Splits a list of snapshots into valid groups based on specified criteria.
        /// </summary>
        /// <param name="snapshots">The list of snapshot DTOs to split.</param>
        /// <param name="snapshotDirectory">The directory containing the snapshots.</param>
        /// <param name="progress">Optional progress reporter for the operation.</param>
        /// <returns>A list of snapshot group DTOs.</returns>
        Task<List<SnapshotGroupDTO>> SplitIntoValidGroups(List<SnapshotDTO> snapshots, string snapshotDirectory, IProgress<double>? progress = null);

        /// <summary>
        /// Loads a snapshot group from the specified file path.
        /// </summary>
        /// <param name="filePath">The file path to load the snapshot group from.</param>
        /// <returns>A list of snapshot DTOs from the loaded group.</returns>
        Task<List<SnapshotDTO>> LoadSnapshotGroup(string filePath);
    }
}
