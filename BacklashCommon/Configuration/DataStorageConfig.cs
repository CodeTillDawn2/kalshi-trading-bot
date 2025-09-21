using System.ComponentModel.DataAnnotations;

namespace BacklashCommon.Configuration
{

    public class DataStorageConfig
    {
        /// <summary>
        /// The configuration section name for GeneralExecutionConfig.
        /// </summary>
        public const string SectionName = "DataStorage";

        /// <summary>
        /// Gets or sets the hard data storage location.
        /// </summary>
        [Required(ErrorMessage = "The 'HardDataStorageLocation' is missing in the configuration.")]
        public string HardDataStorageLocation { get; set; } = null!;
    }
}
