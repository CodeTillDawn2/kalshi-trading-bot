using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for data loader settings.
    /// </summary>
    public class DataLoaderConfig
    {
        /// <summary>
        /// Gets or sets whether to enable validation of market snapshots before processing.
        /// When enabled, snapshots are checked for data integrity and completeness.
        /// Default is true.
        /// </summary>
        [JsonRequired]
        public bool EnableSnapshotValidation { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of snapshots required for validation to be meaningful.
        /// Markets with fewer snapshots will still be processed but with warnings.
        /// Default is 1.
        /// </summary>
        [JsonRequired]
        public int MinSnapshotCountForValidation { get; set; }
    }
}