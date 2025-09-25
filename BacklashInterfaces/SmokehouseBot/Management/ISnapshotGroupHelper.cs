namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Defines the contract for a helper service that manages snapshot grouping operations
    /// for market analysis and data organization.
    /// </summary>
    public interface ISnapshotGroupHelper
    {
        /// <summary>
        /// Generates snapshot groups by analyzing market data and organizing snapshots
        /// into logical groupings for efficient processing and analysis.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task GenerateSnapshotGroups();
    }
}
