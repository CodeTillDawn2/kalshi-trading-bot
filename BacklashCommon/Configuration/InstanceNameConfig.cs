using System.ComponentModel.DataAnnotations;

namespace BacklashCommon.Configuration
{

    public class InstanceNameConfig
    {
        /// <summary>
        /// The configuration section name for GeneralExecutionConfig.
        /// </summary>
        public const string SectionName = "InstanceName";

        /// <summary>
        /// Gets or sets the brain instance identifier.
        /// </summary>
        [Required(ErrorMessage = "The 'InstanceName' is missing in the configuration.")]
        public string Name { get; set; } = null!;
    }
}
