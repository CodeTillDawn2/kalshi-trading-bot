using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration class for data loader settings.
    /// </summary>
    public class DataLoaderConfig
    {
        /// <summary>
        /// The configuration section name for DataLoaderConfig.
        /// </summary>
        public const string SectionName = "SnapshotHandling:DataLoader";

        /// <summary>
        /// Gets or sets whether to enable validation of market snapshots before processing.
        /// When enabled, snapshots are checked for data integrity and completeness.
        /// Default is true.
        /// </summary>
        [Required(ErrorMessage = "The 'EnableSnapshotValidation' is missing in the configuration.")]
        public bool EnableSnapshotValidation { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of snapshots required for validation to be meaningful.
        /// Markets with fewer snapshots will still be processed but with warnings.
        /// Default is 1.
        /// </summary>
        [Required(ErrorMessage = "The 'MinSnapshotCountForValidation' is missing in the configuration.")]
        public int MinSnapshotCountForValidation { get; set; }
    }
}
